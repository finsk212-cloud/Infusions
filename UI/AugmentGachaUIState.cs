using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Augments
{
	public class AugmentGachaUIState : UIState
	{
		private enum ChamberState
		{
			Idle,
			Decrypting,
			Revealed
		}

		private UIPanel backPanel;
		private ColoredLabel essenceLabel;
		private ColoredLabel protocolLabel;
		private ColoredLabel oddsLabel;

		// Interactive Content Container
		private UIPanel chamberContainer;

		private ChamberState state = ChamberState.Idle;
		private int animTimer;
		private const int RollDuration = 180; // ~3.0 seconds

		private Augment wonAugment;
		private bool wonAugmentArchived;
		private int wonRefundCores;

		private readonly UIParticleSystem particles = new UIParticleSystem(160);

		// Spacious window layout so all texts and elements breathe cleanly
		private const float PanelWidth = 840f;
		private const float PanelHeight = 560f;

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (backPanel != null && backPanel.ContainsPoint(Main.MouseScreen))
			{
				Main.LocalPlayer.mouseInterface = true;
			}

			particles.Update();

			CalculatedStyle chamberDims = chamberContainer != null ? chamberContainer.GetDimensions() : default;
			Rectangle chamberRect = new Rectangle((int)chamberDims.X, (int)chamberDims.Y, (int)chamberDims.Width, (int)chamberDims.Height);

			if (state == ChamberState.Idle)
			{
				if (chamberDims.Width > 0 && Main.rand.NextBool(6))
				{
					Color emberColor = Main.rand.NextBool() ? new Color(0, 220, 255) : new Color(255, 215, 80);
					particles.SpawnAmbient(chamberRect, emberColor, scale: 2.2f);
				}
			}
			else if (state == ChamberState.Decrypting)
			{
				animTimer++;

				if (chamberDims.Width > 0)
				{
					Vector2 center = new Vector2(chamberDims.X + chamberDims.Width * 0.5f, chamberDims.Y + 140f);

					// Draw particles inward in a spiraling vortex towards the pod core
					if (animTimer < 150 && Main.rand.NextBool(2))
					{
						Color vortexColor = GetTeaseColor(animTimer);
						particles.SpawnVortex(center, vortexColor, radius: Main.rand.NextFloat(90f, 160f));
					}

					// Laser sparks at core
					if (Main.rand.NextBool(3))
					{
						particles.SpawnLaserSpark(center + new Vector2(Main.rand.NextFloat(-20f, 20f), Main.rand.NextFloat(-20f, 20f)), Color.Cyan);
					}
				}

				if (animTimer >= RollDuration)
				{
					state = ChamberState.Revealed;
					OnDecryptionRevealed();
					RebuildChamberContent();
				}
			}
			else if (state == ChamberState.Revealed && wonAugment != null)
			{
				if (chamberDims.Width > 0 && Main.rand.NextBool(8))
				{
					Color auraColor = GetRarityColor(wonAugment.Rarity);
					particles.SpawnAmbient(chamberRect, auraColor, scale: 2.0f);
				}
			}
		}

		public override void OnInitialize()
		{
			backPanel = new UIPanel();
			backPanel.Width.Set(PanelWidth, 0f);
			backPanel.Height.Set(PanelHeight, 0f);
			backPanel.HAlign = 0.5f;
			backPanel.VAlign = 0.5f;
			backPanel.BackgroundColor = new Color(16, 22, 42) * 0.98f;
			backPanel.BorderColor = new Color(0, 190, 255) * 0.75f;

			// Top Navigation Tab: Return to Storage
			var storageTab = new TabButton("← Chip Storage & Dismantle", () => ModContent.GetInstance<AugmentUISystem>().ShowShop());
			storageTab.Left.Set(14f, 0f);
			storageTab.Top.Set(10f, 0f);
			storageTab.Width.Set(220f, 0f);
			storageTab.Height.Set(26f, 0f);
			backPanel.Append(storageTab);

			// Machine Cores Badge with rich colored text
			UIPanel essenceBadge = new UIPanel();
			essenceBadge.Width.Set(200f, 0f);
			essenceBadge.Height.Set(26f, 0f);
			essenceBadge.Left.Set(-244f, 1f);
			essenceBadge.Top.Set(10f, 0f);
			essenceBadge.SetPadding(0f);
			essenceBadge.BackgroundColor = new Color(12, 18, 36) * 0.95f;
			essenceBadge.BorderColor = new Color(80, 180, 255) * 0.7f;

			essenceLabel = new ColoredLabel("[c/FFE080:Machine Cores:] [c/00FFFF:0]", 0.84f);
			essenceLabel.Width.Set(0f, 1f);
			essenceLabel.Height.Set(0f, 1f);
			essenceBadge.Append(essenceLabel);
			backPanel.Append(essenceBadge);

			// Close Button
			var closeBtn = new CloseButton();
			closeBtn.Width.Set(24f, 0f);
			closeBtn.Height.Set(24f, 0f);
			closeBtn.Left.Set(-34f, 1f);
			closeBtn.Top.Set(10f, 0f);
			closeBtn.Clicked += () => ModContent.GetInstance<AugmentUISystem>().HideGacha();
			backPanel.Append(closeBtn);

			// Title Header
			UIText title = new UIText("YoRHa Neural Decryption Chamber", 1.22f)
			{
				HAlign = 0.5f,
				TextColor = new Color(255, 235, 175)
			};
			title.Top.Set(44f, 0f);
			backPanel.Append(title);

			// Active Protocol with bright cyan accent
			protocolLabel = new ColoredLabel("[c/70A0D0:Active Protocol:] [c/00FFFF:Pre-Hardmode Protocol]", 0.84f)
			{
				HAlign = 0.5f
			};
			protocolLabel.Top.Set(70f, 0f);
			protocolLabel.Width.Set(PanelWidth - 40f, 0f);
			protocolLabel.Height.Set(18f, 0f);
			backPanel.Append(protocolLabel);

			// Live Odds Row with distinct rarity colors
			oddsLabel = new ColoredLabel("[c/80B0E0:Decryption Odds:]  [c/D0D8E8:Common 80%]  •  [c/00FFFF:Rare 15%]  •  [c/D060FF:Epic 5%]  •  [c/FFD700:Legendary 0%]", 0.80f)
			{
				HAlign = 0.5f
			};
			oddsLabel.Top.Set(92f, 0f);
			oddsLabel.Width.Set(PanelWidth - 40f, 0f);
			oddsLabel.Height.Set(18f, 0f);
			backPanel.Append(oddsLabel);

			// Main Containment Chamber Container
			chamberContainer = new UIPanel();
			chamberContainer.Width.Set(-28f, 1f);
			chamberContainer.Height.Set(420f, 0f);
			chamberContainer.Left.Set(14f, 0f);
			chamberContainer.Top.Set(118f, 0f);
			chamberContainer.BackgroundColor = new Color(10, 14, 28) * 0.96f;
			chamberContainer.BorderColor = new Color(36, 58, 105) * 0.85f;
			backPanel.Append(chamberContainer);

			Append(backPanel);

			RebuildChamberContent();
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			base.Draw(spriteBatch);
			particles.Draw(spriteBatch);
		}

		public void Refresh()
		{
			int count = Main.LocalPlayer.CountItem(ModContent.ItemType<AugmentEssenceItem>());
			essenceLabel?.SetText($"[c/FFE080:Machine Cores:] [c/00FFFF:{count}]");

			RarityBracket bracket = BossTierMap.GetCurrentWorldBracket();
			string bracketName = BossTierMap.GetBracketName(bracket);
			protocolLabel?.SetText($"[c/70A0D0:Active Protocol:] [c/00FFFF:{bracketName} Protocol]");

			RarityRollChances chances = BossRarityRoller.GetChancesForBracket(bracket);
			oddsLabel?.SetText($"[c/80B0E0:Decryption Odds:]  [c/D0D8E8:Common {chances.Common}%]  •  [c/00FFFF:Rare {chances.Rare}%]  •  [c/D060FF:Epic {chances.Epic}%]  •  [c/FFD700:Legendary {chances.Legendary}%]");

			if (state != ChamberState.Decrypting)
			{
				state = ChamberState.Idle;
				RebuildChamberContent();
			}
		}

		private void RebuildChamberContent()
		{
			if (chamberContainer == null)
				return;

			chamberContainer.RemoveAllChildren();

			if (state == ChamberState.Idle)
			{
				BuildIdleChamber();
			}
			else if (state == ChamberState.Decrypting)
			{
				BuildDecryptingChamber();
			}
			else if (state == ChamberState.Revealed)
			{
				BuildRevealedChamber();
			}
		}

		private void BuildIdleChamber()
		{
			// Pod graphic preview in the center
			var podView = new PodDisplayCard();
			podView.Width.Set(140f, 0f);
			podView.Height.Set(140f, 0f);
			podView.HAlign = 0.5f;
			podView.Top.Set(30f, 0f);
			chamberContainer.Append(podView);

			var readyLabel = new ColoredLabel("[c/FFE080:YoRHa Encrypted Salvage Pod Loaded]", 1.0f)
			{
				HAlign = 0.5f
			};
			readyLabel.Top.Set(185f, 0f);
			readyLabel.Width.Set(500f, 0f);
			readyLabel.Height.Set(24f, 0f);
			chamberContainer.Append(readyLabel);

			var subtext = new ColoredLabel("[c/90B0D0:Consume 3 Machine Cores to synthesize neural frequency and extract 1 combat Plug-in Chip]", 0.78f)
			{
				HAlign = 0.5f
			};
			subtext.Top.Set(215f, 0f);
			subtext.Width.Set(600f, 0f);
			subtext.Height.Set(22f, 0f);
			chamberContainer.Append(subtext);

			// Decrypt action button with glowing cyan & gold styling - clean single-line label with NO overlap
			var decryptBtn = new ChamberActionButton(
				"[c/00FFFF:INITIALIZE DECRYPTION]   [c/FFE066:•   3 Machine Cores]",
				new Color(18, 48, 80),
				new Color(28, 85, 140),
				new Color(0, 220, 255) * 0.9f,
				new Color(255, 215, 80),
				StartDecryption
			);
			decryptBtn.Width.Set(440f, 0f);
			decryptBtn.Height.Set(50f, 0f);
			decryptBtn.HAlign = 0.5f;
			decryptBtn.Top.Set(265f, 0f);
			chamberContainer.Append(decryptBtn);

			var footer = new ColoredLabel("[c/7890B0:Rolls prioritize unowned chips from your active progression bracket.]", 0.72f)
			{
				HAlign = 0.5f
			};
			footer.Top.Set(345f, 0f);
			footer.Width.Set(600f, 0f);
			footer.Height.Set(18f, 0f);
			chamberContainer.Append(footer);
		}

		private void BuildDecryptingChamber()
		{
			// Unique YoRHa Neural Core Synthesizer (Holographic Laser Rings & Frequency Oscilloscope)
			var coreView = new NeuralDecryptionCoreView(() => animTimer, RollDuration, () => wonAugment);
			coreView.Width.Set(-20f, 1f);
			coreView.Height.Set(260f, 0f);
			coreView.HAlign = 0.5f;
			coreView.Top.Set(15f, 0f);
			chamberContainer.Append(coreView);

			var statusLabel = new ColoredLabel("[c/00FFFF:SYNCHRONIZING NEURAL FREQUENCY...]", 1.0f)
			{
				HAlign = 0.5f
			};
			statusLabel.Top.Set(290f, 0f);
			statusLabel.Width.Set(500f, 0f);
			statusLabel.Height.Set(24f, 0f);
			chamberContainer.Append(statusLabel);

			var subLabel = new ColoredLabel("[c/FFE080:Harmonizing combat memory stream]   [c/70A0D0:•   Deciphering YoRHa Pod telemetry]", 0.78f)
			{
				HAlign = 0.5f
			};
			subLabel.Top.Set(322f, 0f);
			subLabel.Width.Set(600f, 0f);
			subLabel.Height.Set(20f, 0f);
			chamberContainer.Append(subLabel);
		}

		private void BuildRevealedChamber()
		{
			if (wonAugment == null)
			{
				state = ChamberState.Idle;
				BuildIdleChamber();
				return;
			}

			Color rarityColor = GetRarityColor(wonAugment.Rarity);

			// Winning Chip Card Container with generous dimensions so all text fits comfortably
			var chipCard = new RevealedChipCard(wonAugment, rarityColor);
			chipCard.Width.Set(660f, 0f);
			chipCard.Height.Set(220f, 0f);
			chipCard.HAlign = 0.5f;
			chipCard.Top.Set(24f, 0f);

			// Chip Name with bold rarity font
			var nameText = new UIText(wonAugment.DisplayName, 1.25f)
			{
				TextColor = rarityColor
			};
			nameText.Left.Set(110f, 0f);
			nameText.Top.Set(20f, 0f);
			chipCard.Append(nameText);

			// Rarity & Class pill
			string rarityLabel = wonAugment.KeystoneFamily != null ? $"[{wonAugment.Rarity.ToString().ToUpper()} KEYSTONE]" : $"[{wonAugment.Rarity.ToString().ToUpper()}]";
			string rarityHex = wonAugment.Rarity switch
			{
				AugmentRarity.Legendary => "FFD700",
				AugmentRarity.Epic => "D060FF",
				AugmentRarity.Rare => "00FFFF",
				_ => "D0D8E8"
			};

			var tierLabel = new ColoredLabel($"[c/{rarityHex}:{rarityLabel}]   •   [c/D0E0FF:Class: {wonAugment.Class}]", 0.82f, centerH: false, centerV: true);
			tierLabel.Left.Set(110f, 0f);
			tierLabel.Top.Set(52f, 0f);
			tierLabel.Width.Set(450f, 0f);
			tierLabel.Height.Set(20f, 0f);
			chipCard.Append(tierLabel);

			// Description with wide space so multi-line text fits without clipping
			var descText = new UIText(wonAugment.Description, 0.78f)
			{
				TextColor = new Color(185, 205, 235),
				IsWrapped = true
			};
			descText.Left.Set(110f, 0f);
			descText.Top.Set(80f, 0f);
			descText.Width.Set(520f, 0f);
			chipCard.Append(descText);

			// Install notice badge
			string statusNotice = wonAugmentArchived
				? $"✦ Neural Frame full (5/5) — Archived at Mistress 2B (+{wonRefundCores} Cores) ✦"
				: "✦ Installed directly into your Neural Frame! ✦";

			var statusLabel = new ColoredLabel(wonAugmentArchived ? $"[c/FFA064:{statusNotice}]" : $"[c/64FFB4:{statusNotice}]", 0.82f)
			{
				HAlign = 0.5f
			};
			statusLabel.Top.Set(176f, 0f);
			statusLabel.Width.Set(560f, 0f);
			statusLabel.Height.Set(22f, 0f);
			chipCard.Append(statusLabel);

			chamberContainer.Append(chipCard);

			// Action Buttons: Clean single-line labels with NO overlapping text
			var decryptAgainBtn = new ChamberActionButton(
				"[c/00FFFF:DECRYPT AGAIN]   [c/FFE066:•   3 Cores]",
				new Color(18, 48, 80),
				new Color(28, 85, 140),
				new Color(0, 220, 255) * 0.9f,
				new Color(255, 215, 80),
				StartDecryption
			);
			decryptAgainBtn.Width.Set(290f, 0f);
			decryptAgainBtn.Height.Set(48f, 0f);
			decryptAgainBtn.Left.Set(70f, 0f);
			decryptAgainBtn.Top.Set(285f, 0f);
			chamberContainer.Append(decryptAgainBtn);

			var returnBtn = new ChamberActionButton(
				"[c/D0E0FF:← RETURN TO STORAGE]",
				new Color(32, 42, 68),
				new Color(48, 62, 98),
				new Color(90, 120, 170) * 0.85f,
				new Color(140, 180, 240),
				() => ModContent.GetInstance<AugmentUISystem>().ShowShop()
			);
			returnBtn.Width.Set(290f, 0f);
			returnBtn.Height.Set(48f, 0f);
			returnBtn.Left.Set(-360f, 1f);
			returnBtn.Top.Set(285f, 0f);
			chamberContainer.Append(returnBtn);
		}

		private void StartDecryption()
		{
			var player = Main.LocalPlayer;
			int coreType = ModContent.ItemType<AugmentEssenceItem>();
			if (player.CountItem(coreType) < 3)
			{
				Main.NewText("Requires 3 Machine Cores to decrypt.", 255, 90, 90);
				SoundEngine.PlaySound(SoundID.MenuClose);
				return;
			}

			// Consume 3 Machine Cores
			for (int i = 0; i < 3; i++)
				player.ConsumeItem(coreType);

			Refresh();

			// Roll winning augment using current world progression bracket
			RarityBracket currentBracket = BossTierMap.GetCurrentWorldBracket();
			AugmentRarity rolledRarity = BossRarityRoller.Roll(currentBracket);
			var ap = player.GetModPlayer<AugmentPlayer>();

			var choices = ap.RollChoices(1, rolledRarity, null);
			if (choices.Count > 0)
			{
				wonAugment = choices[0];
			}
			else
			{
				foreach (AugmentRarity fallbackRarity in AugmentRewardLogic.GetRarityFallbackOrder(rolledRarity))
				{
					choices = ap.RollChoices(1, fallbackRarity, null);
					if (choices.Count > 0)
					{
						wonAugment = choices[0];
						break;
					}
				}
				if (wonAugment == null)
				{
					var all = AugmentDatabase.All;
					wonAugment = all[Main.rand.Next(all.Count)];
				}
			}

			// Transition to Decrypting state
			state = ChamberState.Decrypting;
			animTimer = 0;

			// Spool-up hum
			SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.75f, Pitch = -0.3f }, player.Center);
			RebuildChamberContent();
		}

		private void OnDecryptionRevealed()
		{
			var player = Main.LocalPlayer;
			if (wonAugment == null)
				return;

			// Sound and particle fanfare based on rarity
			CalculatedStyle chamberDims = chamberContainer != null ? chamberContainer.GetDimensions() : default;
			Vector2 revealCenter = new Vector2(chamberDims.X + chamberDims.Width * 0.5f, chamberDims.Y + 130f);

			Color rarityColor = GetRarityColor(wonAugment.Rarity);
			particles.SpawnBurst(revealCenter, rarityColor, count: 60, maxSpeed: 5.2f);

			switch (wonAugment.Rarity)
			{
				case AugmentRarity.Legendary:
					SoundEngine.PlaySound(SoundID.Item29 with { Volume = 1.0f, Pitch = 0.15f }, player.Center);
					for (int i = 0; i < 40; i++)
					{
						Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.GoldFlame, Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f), 0, default, 1.5f);
						d.noGravity = true;
					}
					break;
				case AugmentRarity.Epic:
					SoundEngine.PlaySound(SoundID.Item100 with { Volume = 0.95f, Pitch = 0.1f }, player.Center);
					for (int i = 0; i < 30; i++)
					{
						Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.PurpleTorch, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 0, default, 1.3f);
						d.noGravity = true;
					}
					break;
				case AugmentRarity.Rare:
					SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.90f, Pitch = 0.1f }, player.Center);
					for (int i = 0; i < 22; i++)
					{
						Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Electric, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 0, default, 1.2f);
						d.noGravity = true;
					}
					break;
				default:
					SoundEngine.PlaySound(SoundID.Research with { Volume = 0.90f, Pitch = 0.1f }, player.Center);
					for (int i = 0; i < 16; i++)
					{
						Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Iron, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f), 0, default, 1.1f);
						d.noGravity = true;
					}
					break;
			}

			// Authoritatively grant or archive the augment so chat announces it at the exact moment of reveal!
			var ap = player.GetModPlayer<AugmentPlayer>();
			wonAugmentArchived = ap.Owned.Count >= AugmentPlayer.MaxOwnedAugments;
			wonRefundCores = wonAugmentArchived ? AugmentPlayer.GetRemoveRefund(wonAugment.Rarity) : 0;
			ap.ChooseReward(wonAugment);
		}

		private static Color GetRarityColor(AugmentRarity rarity)
		{
			return rarity switch
			{
				AugmentRarity.Legendary => new Color(255, 200, 50),
				AugmentRarity.Epic => new Color(200, 100, 255),
				AugmentRarity.Rare => new Color(80, 190, 255),
				_ => new Color(220, 230, 245)
			};
		}

		private static Color GetTeaseColor(int timer)
		{
			if (timer < 40) return new Color(80, 190, 255);
			if (timer < 80) return new Color(200, 100, 255);
			if (timer < 120) return new Color(255, 200, 50);
			int cycle = (timer / 6) % 3;
			return cycle switch
			{
				0 => new Color(80, 190, 255),
				1 => new Color(200, 100, 255),
				_ => new Color(255, 200, 50)
			};
		}

		// Floating 2.8x pixel art pod preview
		private class PodDisplayCard : UIElement
		{
			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				CalculatedStyle d = GetDimensions();

				if (ModContent.RequestIfExists<Texture2D>("Augments/Items/SealedChipCacheItem", out var cacheAsset))
				{
					Texture2D tex = cacheAsset.Value;
					if (tex != null)
					{
						float bob = (float)Math.Sin(Main.timeForVisualEffects * 0.04f) * 4f;
						Vector2 center = new Vector2(d.X + d.Width * 0.5f, d.Y + d.Height * 0.5f + bob);
						Vector2 origin = tex.Size() * 0.5f;

						// Glowing aura behind pod
						float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.timeForVisualEffects * 0.08f);
						Color aura = new Color(0, 200, 255) * (0.28f * pulse);
						spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)center.X - 45, (int)center.Y - 45, 90, 90), aura);

						spriteBatch.Draw(tex, center, null, Color.White, 0f, origin, 2.8f, SpriteEffects.None, 0f);
					}
				}
			}
		}

		// Unique YoRHa Neural Core Synthesizer (Concentric Holographic Scanning Rings & Audio Waveform Surge)
		private class NeuralDecryptionCoreView : UIElement
		{
			private readonly Func<int> getTimer;
			private readonly int maxDuration;
			private readonly Func<Augment> getWonChip;
			private int lastTickedStep = -1;

			public NeuralDecryptionCoreView(Func<int> getTimer, int maxDuration, Func<Augment> getWonChip)
			{
				this.getTimer = getTimer;
				this.maxDuration = maxDuration;
				this.getWonChip = getWonChip;
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				CalculatedStyle d = GetDimensions();
				Texture2D pixel = TextureAssets.MagicPixel.Value;
				int timer = getTimer();

				Vector2 center = new Vector2(d.X + d.Width * 0.5f, d.Y + 130f);

				// Pitch rising audio feedback
				int tickInterval = timer > 120 ? 6 : (timer > 70 ? 9 : 14);
				int currentStep = timer / tickInterval;
				if (currentStep != lastTickedStep && timer < maxDuration - 15)
				{
					lastTickedStep = currentStep;
					float pitch = -0.2f + (timer / (float)maxDuration) * 0.5f;
					SoundEngine.PlaySound(SoundID.MenuTick with { Pitch = pitch, Volume = 0.85f });
				}

				// Rarity tease color based on synthesis phase
				Color coreColor;
				if (timer >= 150)
				{
					Augment won = getWonChip();
					coreColor = won != null ? GetRarityColor(won.Rarity) : Color.Cyan;
				}
				else
				{
					coreColor = GetTeaseColor(timer);
				}

				float progress = MathHelper.Clamp(timer / (float)maxDuration, 0f, 1f);

				// Concentric ring radii with implosion contraction in final phase
				float contraction = timer >= 150 ? (1f - (timer - 150) / 30f) : 1f;
				float r1 = 50f * contraction;
				float r2 = 85f * contraction;
				float r3 = 125f * contraction;

				float speedMult = 1f + progress * 3.5f;
				float angle1 = timer * 0.04f * speedMult;
				float angle2 = -timer * 0.03f * speedMult;
				float angle3 = timer * 0.02f * speedMult;

				// Draw Inner Holographic Ring
				DrawDottedRing(spriteBatch, pixel, center, r1, angle1, 28, coreColor * 0.85f, 2);

				// Draw Middle Reticle Ring with 4 Cardinal Crosshair Ticks
				DrawDottedRing(spriteBatch, pixel, center, r2, angle2, 36, coreColor * 0.75f, 2);
				DrawCardinalTicks(spriteBatch, pixel, center, r2, angle2, coreColor * 0.9f);

				// Draw Outer Segmented Ring
				DrawDottedRing(spriteBatch, pixel, center, r3, angle3, 48, coreColor * 0.55f, 2);

				// Draw Central Pod with vibration tremor
				if (ModContent.RequestIfExists<Texture2D>("Augments/Items/SealedChipCacheItem", out var cacheAsset))
				{
					Texture2D tex = cacheAsset.Value;
					if (tex != null)
					{
						float tremor = (timer / (float)maxDuration) * (float)Math.Sin(timer * 1.6f) * 3.5f;
						Vector2 podCenter = center + new Vector2(tremor, tremor * 0.5f);
						Vector2 origin = tex.Size() * 0.5f;

						// Glowing singularity core
						float corePulse = 0.5f + 0.5f * (float)Math.Sin(timer * 0.25f);
						Color aura = coreColor * (0.35f + 0.35f * corePulse);
						int auraSize = (int)(70f + 25f * corePulse);
						spriteBatch.Draw(pixel, new Rectangle((int)center.X - auraSize / 2, (int)center.Y - auraSize / 2, auraSize, auraSize), aura);

						spriteBatch.Draw(tex, podCenter, null, Color.White, 0f, origin, 2.8f, SpriteEffects.None, 0f);
					}
				}

				// Vertical Holographic Scanning Beam
				float scanPulse = 0.5f + 0.5f * (float)Math.Sin(timer * 0.2f);
				spriteBatch.Draw(pixel, new Rectangle((int)center.X - 1, (int)d.Y + 20, 2, 220), coreColor * (0.4f + 0.3f * scanPulse));
			}

			private static void DrawDottedRing(SpriteBatch sb, Texture2D pixel, Vector2 center, float radius, float startAngle, int count, Color col, int dotSize)
			{
				if (radius <= 4f) return;
				float step = MathHelper.TwoPi / count;
				for (int i = 0; i < count; i++)
				{
					float a = startAngle + i * step;
					Vector2 pos = center + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * radius;
					sb.Draw(pixel, new Rectangle((int)pos.X - dotSize / 2, (int)pos.Y - dotSize / 2, dotSize, dotSize), col);
				}
			}

			private static void DrawCardinalTicks(SpriteBatch sb, Texture2D pixel, Vector2 center, float radius, float rot, Color col)
			{
				if (radius <= 6f) return;
				for (int i = 0; i < 4; i++)
				{
					float a = rot + i * MathHelper.PiOver2;
					Vector2 p1 = center + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * (radius - 6f);
					Vector2 p2 = center + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * (radius + 6f);
					sb.Draw(pixel, new Rectangle((int)p1.X - 1, (int)p1.Y - 1, 3, 3), col);
					sb.Draw(pixel, new Rectangle((int)p2.X - 1, (int)p2.Y - 1, 3, 3), col);
				}
			}
		}

		// Revealed Chip Card with glowing rarity backdrop
		private class RevealedChipCard : UIPanel
		{
			private readonly Augment augment;
			private readonly Color rarityColor;

			public RevealedChipCard(Augment augment, Color rarityColor)
			{
				this.augment = augment;
				this.rarityColor = rarityColor;
				BackgroundColor = new Color(16, 24, 46) * 0.96f;
				BorderColor = rarityColor * 0.9f;
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				base.DrawSelf(spriteBatch);

				CalculatedStyle d = GetDimensions();

				// Soft rarity radial glow behind the card
				float pulse = 0.7f + 0.3f * (float)Math.Sin(Main.timeForVisualEffects * 0.08f);
				Color aura = rarityColor * (0.12f * pulse);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)d.X + 6, (int)d.Y + 6, (int)d.Width - 12, (int)d.Height - 12), aura);

				// Draw class emblem icon
				Texture2D icon = AugmentSlotElement.GetClassIcon(augment.Class);
				if (icon != null)
				{
					Vector2 center = new Vector2(d.X + 55f, d.Y + 58f);
					spriteBatch.Draw(icon, center, null, rarityColor, 0f, icon.Size() * 0.5f, 1.8f, SpriteEffects.None, 0f);
				}
			}
		}

		// Action button with single-line centered label to completely eliminate text overlap
		private class ChamberActionButton : UIElement
		{
			private readonly string labelText;
			private readonly Color idleBg;
			private readonly Color hoverBg;
			private readonly Color idleBorder;
			private readonly Color hoverBorder;
			private readonly Action onClick;
			private bool isHovered;

			public ChamberActionButton(string labelText, Color idleBg, Color hoverBg, Color idleBorder, Color hoverBorder, Action onClick)
			{
				this.labelText = labelText;
				this.idleBg = idleBg;
				this.hoverBg = hoverBg;
				this.idleBorder = idleBorder;
				this.hoverBorder = hoverBorder;
				this.onClick = onClick;
			}

			public override void LeftClick(UIMouseEvent evt)
			{
				base.LeftClick(evt);
				SoundEngine.PlaySound(SoundID.MenuTick);
				onClick?.Invoke();
			}

			public override void MouseOver(UIMouseEvent evt)
			{
				base.MouseOver(evt);
				isHovered = true;
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				isHovered = false;
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				CalculatedStyle d = GetDimensions();
				Texture2D pixel = TextureAssets.MagicPixel.Value;

				float pulse = 0.7f + 0.3f * (float)Math.Sin(Main.timeForVisualEffects * 0.12f);
				Color bg = isHovered ? hoverBg : idleBg;
				Color border = isHovered ? hoverBorder : Color.Lerp(idleBorder, hoverBorder, pulse * 0.5f);

				// Background rect
				Rectangle rect = new Rectangle((int)d.X, (int)d.Y, (int)d.Width, (int)d.Height);
				spriteBatch.Draw(pixel, rect, bg * 0.95f);

				// Outer glowing border
				int thickness = isHovered ? 2 : 1;
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), border);
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), border);
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), border);
				spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), border);

				// Inner hover bloom
				if (isHovered)
				{
					spriteBatch.Draw(pixel, new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4), hoverBorder * 0.14f);
				}

				// Draw single crisp, centered formatted text with ChatManager
				var font = FontAssets.MouseText.Value;
				Vector2 textSize = ChatManager.GetStringSize(font, labelText, new Vector2(0.88f));
				Vector2 textPos = new Vector2(d.X + (d.Width - textSize.X) * 0.5f, d.Y + (d.Height - textSize.Y) * 0.5f);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, labelText, textPos, Color.White, 0f, Vector2.Zero, new Vector2(0.88f));
			}
		}

		private class TabButton : UIPanel
		{
			private readonly Action onClick;

			public TabButton(string text, Action onClick)
			{
				this.onClick = onClick;

				SetPadding(0f);
				BackgroundColor = new Color(20, 28, 54);
				BorderColor = new Color(45, 75, 120);

				var label = new UIText(text, 0.82f)
				{
					HAlign = 0.5f,
					VAlign = 0.5f,
					TextColor = new Color(180, 215, 255)
				};
				Append(label);
			}

			public override void LeftClick(UIMouseEvent evt)
			{
				base.LeftClick(evt);
				SoundEngine.PlaySound(SoundID.MenuTick);
				onClick?.Invoke();
			}

			public override void MouseOver(UIMouseEvent evt)
			{
				base.MouseOver(evt);
				BackgroundColor = new Color(30, 42, 80);
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				BackgroundColor = new Color(20, 28, 54);
			}
		}

		private class CloseButton : UIPanel
		{
			public event Action Clicked;

			private static readonly Color IdleColor = new Color(110, 40, 40);
			private static readonly Color HoverColor = new Color(160, 60, 60);

			public CloseButton()
			{
				SetPadding(0f);
				BackgroundColor = IdleColor;
				BorderColor = Color.White * 0.4f;

				UIText labelText = new UIText("x", 0.85f)
				{
					HAlign = 0.5f,
					VAlign = 0.5f
				};
				Append(labelText);
			}

			public override void LeftClick(UIMouseEvent evt)
			{
				base.LeftClick(evt);
				SoundEngine.PlaySound(SoundID.MenuClose);
				Clicked?.Invoke();
			}

			public override void MouseOver(UIMouseEvent evt)
			{
				base.MouseOver(evt);
				BackgroundColor = HoverColor;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				BackgroundColor = IdleColor;
			}
		}
	}
}
