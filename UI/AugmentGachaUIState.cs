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
		private TacticalOddsBarView oddsBarView;

		// Interactive Content Container
		private ChamberPanel chamberContainer;

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
				if (chamberDims.Width > 0 && Main.rand.NextBool(7))
				{
					Color emberColor = Main.rand.NextBool() ? new Color(76, 168, 216) : new Color(212, 184, 114);
					particles.SpawnAmbient(chamberRect, emberColor, scale: 2.0f);
				}
			}
			else if (state == ChamberState.Decrypting)
			{
				animTimer++;

				if (chamberDims.Width > 0)
				{
					Vector2 center = new Vector2(chamberDims.X + chamberDims.Width * 0.5f, chamberDims.Y + 130f);

					// Draw particles inward in a spiraling vortex towards the pod core
					if (animTimer < 150 && Main.rand.NextBool(2))
					{
						Color vortexColor = GetTeaseColor(animTimer);
						particles.SpawnVortex(center, vortexColor, radius: Main.rand.NextFloat(90f, 160f));
					}

					// Laser sparks at core
					if (Main.rand.NextBool(3))
					{
						particles.SpawnLaserSpark(center + new Vector2(Main.rand.NextFloat(-20f, 20f), Main.rand.NextFloat(-20f, 20f)), new Color(76, 168, 216));
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
					particles.SpawnAmbient(chamberRect, auraColor, scale: 1.8f);
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
			backPanel.BackgroundColor = new Color(14, 18, 32) * 0.98f;
			backPanel.BorderColor = new Color(42, 68, 108) * 0.85f;

			// Top Navigation Tab: Return to Storage
			var storageTab = new TabButton("← Chip Storage & Dismantle", () => ModContent.GetInstance<AugmentUISystem>().ShowShop());
			storageTab.Left.Set(14f, 0f);
			storageTab.Top.Set(10f, 0f);
			storageTab.Width.Set(220f, 0f);
			storageTab.Height.Set(26f, 0f);
			backPanel.Append(storageTab);

			// Machine Cores Badge with refined champagne & cerulean text
			UIPanel essenceBadge = new UIPanel();
			essenceBadge.Width.Set(200f, 0f);
			essenceBadge.Height.Set(26f, 0f);
			essenceBadge.Left.Set(-244f, 1f);
			essenceBadge.Top.Set(10f, 0f);
			essenceBadge.SetPadding(0f);
			essenceBadge.BackgroundColor = new Color(12, 16, 28) * 0.95f;
			essenceBadge.BorderColor = new Color(42, 68, 108) * 0.75f;

			essenceLabel = new ColoredLabel("[c/D4B872:Machine Cores:] [c/68C2D8:0]", 0.84f);
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
				TextColor = new Color(234, 216, 176)
			};
			title.Top.Set(42f, 0f);
			backPanel.Append(title);

			// Active Protocol with muted tactical cyan
			protocolLabel = new ColoredLabel("[c/7A9AB8:Active Protocol:] [c/68C2D8:Pre-Hardmode Protocol]", 0.84f)
			{
				HAlign = 0.5f
			};
			protocolLabel.Top.Set(68f, 0f);
			protocolLabel.Width.Set(PanelWidth - 40f, 0f);
			protocolLabel.Height.Set(18f, 0f);
			backPanel.Append(protocolLabel);

			// Unique Tactical Probability Ratio Bar & Badges
			oddsBarView = new TacticalOddsBarView
			{
				HAlign = 0.5f
			};
			oddsBarView.Top.Set(88f, 0f);
			oddsBarView.Width.Set(PanelWidth - 40f, 0f);
			oddsBarView.Height.Set(26f, 0f);
			backPanel.Append(oddsBarView);

			// Main Containment Chamber Container with tactical visual markings
			chamberContainer = new ChamberPanel();
			chamberContainer.Width.Set(-28f, 1f);
			chamberContainer.Height.Set(410f, 0f);
			chamberContainer.Left.Set(14f, 0f);
			chamberContainer.Top.Set(122f, 0f);
			chamberContainer.BackgroundColor = new Color(10, 14, 26) * 0.96f;
			chamberContainer.BorderColor = new Color(34, 52, 92) * 0.85f;
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
			essenceLabel?.SetText($"[c/D4B872:Machine Cores:] [c/68C2D8:{count:N0}]");

			RarityBracket bracket = BossTierMap.GetCurrentWorldBracket();
			string bracketName = BossTierMap.GetBracketName(bracket);
			protocolLabel?.SetText($"[c/7A9AB8:Active Protocol:] [c/68C2D8:{bracketName} Protocol]");

			RarityRollChances chances = BossRarityRoller.GetChancesForBracket(bracket);
			oddsBarView?.SetChances(chances);

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
			podView.Top.Set(24f, 0f);
			chamberContainer.Append(podView);

			var readyLabel = new ColoredLabel("[c/EAD8B0:YoRHa Encrypted Salvage Pod Loaded]", 1.0f)
			{
				HAlign = 0.5f
			};
			readyLabel.Top.Set(176f, 0f);
			readyLabel.Width.Set(500f, 0f);
			readyLabel.Height.Set(24f, 0f);
			chamberContainer.Append(readyLabel);

			var subtext = new ColoredLabel("[c/889EB8:Consume 3 Machine Cores to synthesize neural frequency and extract 1 combat Plug-in Chip]", 0.78f)
			{
				HAlign = 0.5f
			};
			subtext.Top.Set(206f, 0f);
			subtext.Width.Set(620f, 0f);
			subtext.Height.Set(22f, 0f);
			chamberContainer.Append(subtext);

			// Decrypt action button with refined styling - single line, no overlap
			var decryptBtn = new ChamberActionButton(
				"[c/E2ECF8:INITIALIZE DECRYPTION]   [c/D4B872:•   3 Machine Cores]",
				isPrimary: true,
				onClick: StartDecryption
			);
			decryptBtn.Width.Set(440f, 0f);
			decryptBtn.Height.Set(46f, 0f);
			decryptBtn.HAlign = 0.5f;
			decryptBtn.Top.Set(254f, 0f);
			chamberContainer.Append(decryptBtn);

			var footer = new ColoredLabel("[c/627A98:Rolls prioritize unowned chips from your active progression bracket.]", 0.72f)
			{
				HAlign = 0.5f
			};
			footer.Top.Set(330f, 0f);
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

			var statusLabel = new ColoredLabel("[c/68C2D8:SYNCHRONIZING NEURAL FREQUENCY...]", 1.0f)
			{
				HAlign = 0.5f
			};
			statusLabel.Top.Set(285f, 0f);
			statusLabel.Width.Set(500f, 0f);
			statusLabel.Height.Set(24f, 0f);
			chamberContainer.Append(statusLabel);

			var subLabel = new ColoredLabel("[c/D4B872:Harmonizing combat memory stream]   [c/7A9AB8:•   Deciphering YoRHa Pod telemetry]", 0.78f)
			{
				HAlign = 0.5f
			};
			subLabel.Top.Set(316f, 0f);
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

			// Winning Chip Card Container with generous dimensions and strict left-alignment
			var chipCard = new RevealedChipCard(wonAugment, rarityColor);
			chipCard.Width.Set(660f, 0f);
			chipCard.Height.Set(224f, 0f);
			chipCard.HAlign = 0.5f;
			chipCard.Top.Set(20f, 0f);
			chamberContainer.Append(chipCard);

			// Action Buttons: Gapped properly from the bottom of the container
			var decryptAgainBtn = new ChamberActionButton(
				"[c/E2ECF8:DECRYPT AGAIN]   [c/D4B872:•   3 Cores]",
				isPrimary: true,
				onClick: StartDecryption
			);
			decryptAgainBtn.Width.Set(310f, 0f);
			decryptAgainBtn.Height.Set(46f, 0f);
			decryptAgainBtn.Left.Set(70f, 0f);
			decryptAgainBtn.Top.Set(320f, 0f);
			chamberContainer.Append(decryptAgainBtn);

			var returnBtn = new ChamberActionButton(
				"[c/B0C4D8:← RETURN TO STORAGE]",
				isPrimary: false,
				onClick: () => ModContent.GetInstance<AugmentUISystem>().ShowShop()
			);
			returnBtn.Width.Set(310f, 0f);
			returnBtn.Height.Set(46f, 0f);
			returnBtn.Left.Set(-380f, 1f);
			returnBtn.Top.Set(320f, 0f);
			chamberContainer.Append(returnBtn);
		}

		private void StartDecryption()
		{
			var player = Main.LocalPlayer;
			int coreType = ModContent.ItemType<AugmentEssenceItem>();
			if (player.CountItem(coreType) < 3)
			{
				Main.NewText("Requires 3 Machine Cores to decrypt.", 220, 90, 90);
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
			Vector2 revealCenter = new Vector2(chamberDims.X + chamberDims.Width * 0.5f, chamberDims.Y + 120f);

			Color rarityColor = GetRarityColor(wonAugment.Rarity);
			particles.SpawnBurst(revealCenter, rarityColor, count: 60, maxSpeed: 5.0f);

			switch (wonAugment.Rarity)
			{
				case AugmentRarity.Legendary:
					SoundEngine.PlaySound(SoundID.Item29 with { Volume = 1.0f, Pitch = 0.15f }, player.Center);
					for (int i = 0; i < 38; i++)
					{
						Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.GoldFlame, Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f), 0, default, 1.4f);
						d.noGravity = true;
					}
					break;
				case AugmentRarity.Epic:
					SoundEngine.PlaySound(SoundID.Item100 with { Volume = 0.95f, Pitch = 0.1f }, player.Center);
					for (int i = 0; i < 28; i++)
					{
						Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.PurpleTorch, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 0, default, 1.25f);
						d.noGravity = true;
					}
					break;
				case AugmentRarity.Rare:
					SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.90f, Pitch = 0.1f }, player.Center);
					for (int i = 0; i < 20; i++)
					{
						Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Electric, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 0, default, 1.15f);
						d.noGravity = true;
					}
					break;
				default:
					SoundEngine.PlaySound(SoundID.Research with { Volume = 0.90f, Pitch = 0.1f }, player.Center);
					for (int i = 0; i < 15; i++)
					{
						Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Iron, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f), 0, default, 1.05f);
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
				AugmentRarity.Legendary => new Color(230, 190, 68),  // #E6BE44 Warm Imperial Gold
				AugmentRarity.Epic => new Color(180, 112, 224),       // #B470E0 Royal Orchid
				AugmentRarity.Rare => new Color(76, 168, 216),        // #4CA8D8 Cyber Cerulean
				_ => new Color(184, 196, 212)                         // #B8C4D4 Titanium Silver
			};
		}

		private static Color GetTeaseColor(int timer)
		{
			if (timer < 40) return new Color(76, 168, 216);
			if (timer < 80) return new Color(180, 112, 224);
			if (timer < 120) return new Color(230, 190, 68);
			int cycle = (timer / 6) % 3;
			return cycle switch
			{
				0 => new Color(76, 168, 216),
				1 => new Color(180, 112, 224),
				_ => new Color(230, 190, 68)
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
						Color aura = new Color(76, 168, 216) * (0.22f * pulse);
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

				Vector2 center = new Vector2(d.X + d.Width * 0.5f, d.Y + 125f);

				// Pitch rising audio feedback
				int tickInterval = timer > 120 ? 6 : (timer > 70 ? 9 : 14);
				int currentStep = timer / tickInterval;
				if (currentStep != lastTickedStep && timer < maxDuration - 15)
				{
					lastTickedStep = currentStep;
					float pitch = -0.2f + (timer / (float)maxDuration) * 0.45f;
					SoundEngine.PlaySound(SoundID.MenuTick with { Pitch = pitch, Volume = 0.82f });
				}

				// Rarity tease color based on synthesis phase
				Color coreColor;
				if (timer >= 150)
				{
					Augment won = getWonChip();
					coreColor = won != null ? GetRarityColor(won.Rarity) : new Color(76, 168, 216);
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
				spriteBatch.Draw(pixel, new Rectangle((int)center.X - 1, (int)d.Y + 20, 2, 215), coreColor * (0.35f + 0.25f * scanPulse));
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

		// Tactical Chamber Panel with subtle sci-fi crosshair and telemetry grid markings
		private class ChamberPanel : UIPanel
		{
			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				base.DrawSelf(spriteBatch);

				CalculatedStyle d = GetDimensions();
				Rectangle rect = d.ToRectangle();
				Texture2D pixel = TextureAssets.MagicPixel.Value;

				// Subtle corner crosshair reticle markers [+]
				Color gridCol = new Color(42, 62, 98) * 0.42f;
				DrawCrosshair(spriteBatch, pixel, new Vector2(rect.X + 22f, rect.Y + 22f), gridCol);
				DrawCrosshair(spriteBatch, pixel, new Vector2(rect.Right - 22f, rect.Y + 22f), gridCol);
				DrawCrosshair(spriteBatch, pixel, new Vector2(rect.X + 22f, rect.Bottom - 22f), gridCol);
				DrawCrosshair(spriteBatch, pixel, new Vector2(rect.Right - 22f, rect.Bottom - 22f), gridCol);

				// Subtle horizontal telemetry divider lines
				spriteBatch.Draw(pixel, new Rectangle(rect.X + 24, rect.Y + 24, rect.Width - 48, 1), gridCol * 0.45f);
				spriteBatch.Draw(pixel, new Rectangle(rect.X + 24, rect.Bottom - 25, rect.Width - 48, 1), gridCol * 0.45f);

				// Telemetry stamp in bottom-right corner
				var font = FontAssets.MouseText.Value;
				string stamp = "YORHA POD // NEURAL SYNTHESIS MATRIX REV-4.2";
				Vector2 stampSize = ChatManager.GetStringSize(font, stamp, new Vector2(0.64f));
				Vector2 stampPos = new Vector2(rect.Right - stampSize.X - 28f, rect.Bottom - stampSize.Y - 8f);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, stamp, stampPos, new Color(75, 95, 125) * 0.42f, 0f, Vector2.Zero, new Vector2(0.64f));
			}

			private static void DrawCrosshair(SpriteBatch sb, Texture2D pixel, Vector2 pos, Color col)
			{
				sb.Draw(pixel, new Rectangle((int)pos.X - 5, (int)pos.Y, 11, 1), col);
				sb.Draw(pixel, new Rectangle((int)pos.X, (int)pos.Y - 5, 1, 11), col);
			}
		}

		// Unique Tactical Probability Ratio Bar & Badges
		private class TacticalOddsBarView : UIElement
		{
			private RarityRollChances chances;

			public void SetChances(RarityRollChances chances)
			{
				this.chances = chances;
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				CalculatedStyle d = GetDimensions();
				Texture2D pixel = TextureAssets.MagicPixel.Value;
				var font = FontAssets.MouseText.Value;

				// Refined, desaturated color hexes
				const string cHex = "B8C4D4"; // Common Silver
				const string rHex = "4CA8D8"; // Rare Cerulean
				const string eHex = "B470E0"; // Epic Royal Orchid
				const string lHex = "E6BE44"; // Legendary Imperial Gold

				string text = $"[c/7A9AB8:CHANCES //]   [c/{cHex}:• COMMON {chances.Common}%]   [c/{rHex}:• RARE {chances.Rare}%]   [c/{eHex}:• EPIC {chances.Epic}%]   [c/{lHex}:• LEGENDARY {chances.Legendary}%]";
				Vector2 textSize = ChatManager.GetStringSize(font, text, new Vector2(0.78f));
				Vector2 textPos = new Vector2(d.X + (d.Width - textSize.X) * 0.5f, d.Y);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, textPos, Color.White, 0f, Vector2.Zero, new Vector2(0.78f));

				// Sleek segmented probability spectrum bar below the text
				float barWidth = 560f;
				float barHeight = 4f;
				float barX = d.X + (d.Width - barWidth) * 0.5f;
				float barY = d.Y + textSize.Y + 4f;

				// Background trench
				spriteBatch.Draw(pixel, new Rectangle((int)barX - 1, (int)barY - 1, (int)barWidth + 2, (int)barHeight + 2), new Color(10, 16, 28));

				float total = chances.Common + chances.Rare + chances.Epic + chances.Legendary;
				if (total <= 0f) total = 100f;

				float cW = barWidth * (chances.Common / total);
				float rW = barWidth * (chances.Rare / total);
				float eW = barWidth * (chances.Epic / total);
				float lW = barWidth * (chances.Legendary / total);

				float currentX = barX;
				if (cW > 0)
				{
					spriteBatch.Draw(pixel, new Rectangle((int)currentX, (int)barY, (int)Math.Max(1f, cW), (int)barHeight), new Color(110, 125, 145));
					currentX += cW;
				}
				if (rW > 0)
				{
					spriteBatch.Draw(pixel, new Rectangle((int)currentX, (int)barY, (int)Math.Max(1f, rW), (int)barHeight), new Color(55, 135, 185));
					currentX += rW;
				}
				if (eW > 0)
				{
					spriteBatch.Draw(pixel, new Rectangle((int)currentX, (int)barY, (int)Math.Max(1f, eW), (int)barHeight), new Color(145, 80, 195));
					currentX += eW;
				}
				if (lW > 0)
				{
					spriteBatch.Draw(pixel, new Rectangle((int)currentX, (int)barY, (int)Math.Max(1f, lW), (int)barHeight), new Color(215, 175, 60));
				}

				// Thin scanline pulse across the bar
				float pulse = (float)Math.Sin(Main.timeForVisualEffects * 0.08f) * 0.5f + 0.5f;
				int pingX = (int)(barX + barWidth * pulse);
				spriteBatch.Draw(pixel, new Rectangle(pingX - 6, (int)barY - 1, 12, (int)barHeight + 2), Color.White * 0.30f);
			}
		}

		// Revealed Chip Card with exact pixel-matched horizontal alignment
		private class RevealedChipCard : UIPanel
		{
			private readonly Augment augment;
			private readonly Color rarityColor;
			private const float TextLeft = 105f;

			public RevealedChipCard(Augment augment, Color rarityColor)
			{
				this.augment = augment;
				this.rarityColor = rarityColor;
				SetPadding(0f);
				BackgroundColor = new Color(14, 20, 36) * 0.96f;
				BorderColor = rarityColor * 0.85f;

				// Chip Name
				var nameText = new UIText(augment.DisplayName, 1.25f)
				{
					TextColor = rarityColor
				};
				nameText.Left.Set(TextLeft, 0f);
				nameText.Top.Set(18f, 0f);
				Append(nameText);

				// Rarity & Class pill
				string rarityLabel = augment.KeystoneFamily != null ? $"[{augment.Rarity.ToString().ToUpper()} KEYSTONE]" : $"[{augment.Rarity.ToString().ToUpper()}]";
				string rarityHex = augment.Rarity switch
				{
					AugmentRarity.Legendary => "E6BE44",
					AugmentRarity.Epic => "B470E0",
					AugmentRarity.Rare => "4CA8D8",
					_ => "B8C4D4"
				};

				var tierLabel = new ColoredLabel($"[c/{rarityHex}:{rarityLabel}]   •   [c/A2B8D0:Class: {augment.Class}]", 0.82f, centerH: false, centerV: true);
				tierLabel.Left.Set(TextLeft, 0f);
				tierLabel.Top.Set(48f, 0f);
				tierLabel.Width.Set(450f, 0f);
				tierLabel.Height.Set(20f, 0f);
				Append(tierLabel);

				// Status install notice
				var ap = Main.LocalPlayer.GetModPlayer<AugmentPlayer>();
				bool archived = ap.Owned.Count >= AugmentPlayer.MaxOwnedAugments;
				int refund = archived ? AugmentPlayer.GetRemoveRefund(augment.Rarity) : 0;
				string statusNotice = archived
					? $"[c/E8A068:✦ Neural Frame full (5/5) — Archived at Mistress 2B (+{refund} Cores) ✦]"
					: "[c/7AE8A8:✦ Installed directly into your Neural Frame! ✦]";

				var statusLabel = new ColoredLabel(statusNotice, 0.82f)
				{
					HAlign = 0.5f
				};
				statusLabel.Top.Set(185f, 0f);
				statusLabel.Width.Set(560f, 0f);
				statusLabel.Height.Set(22f, 0f);
				Append(statusLabel);
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				base.DrawSelf(spriteBatch);

				CalculatedStyle d = GetDimensions();
				var font = FontAssets.MouseText.Value;

				// Soft rarity radial glow behind the card
				float pulse = 0.7f + 0.3f * (float)Math.Sin(Main.timeForVisualEffects * 0.08f);
				Color aura = rarityColor * (0.10f * pulse);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)d.X + 6, (int)d.Y + 6, (int)d.Width - 12, (int)d.Height - 12), aura);

				// Draw class emblem icon
				Texture2D icon = AugmentSlotElement.GetClassIcon(augment.Class);
				if (icon != null)
				{
					Vector2 center = new Vector2(d.X + 52f, d.Y + 54f);
					spriteBatch.Draw(icon, center, null, rarityColor, 0f, icon.Size() * 0.5f, 1.8f, SpriteEffects.None, 0f);
				}

				// Draw description text strictly starting at X = d.X + TextLeft (matching the name and [EPIC] tag)
				if (!string.IsNullOrEmpty(augment.Description))
				{
					var lines = AugmentColorText.Wrap(font, augment.Description, 530f, new Vector2(0.80f));
					float textX = d.X + TextLeft;
					float textY = d.Y + 76f;
					foreach (var line in lines)
					{
						ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, line, new Vector2(textX, textY), Color.White, 0f, Vector2.Zero, new Vector2(0.80f));
						textY += ChatManager.GetStringSize(font, line, new Vector2(0.80f)).Y + 3f;
					}
				}
			}
		}

		// Tactile sci-fi action button with left status accent, corner cuts, and clean centered label
		private class ChamberActionButton : UIElement
		{
			private readonly string labelText;
			private readonly bool isPrimary;
			private readonly Action onClick;
			private bool isHovered;

			public ChamberActionButton(string labelText, bool isPrimary, Action onClick)
			{
				this.labelText = labelText;
				this.isPrimary = isPrimary;
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
				Color bg = isHovered ? new Color(24, 36, 58) : new Color(14, 20, 34);
				Color border = isHovered ? new Color(88, 164, 208) : (isPrimary ? Color.Lerp(new Color(46, 72, 108), new Color(88, 164, 208), pulse * 0.6f) : new Color(46, 72, 108));
				Color accentBar = isPrimary ? (isHovered ? new Color(230, 190, 68) : new Color(212, 184, 114)) : (isHovered ? new Color(130, 165, 200) : new Color(90, 115, 145));

				// Main background rect
				Rectangle rect = new Rectangle((int)d.X, (int)d.Y, (int)d.Width, (int)d.Height);
				spriteBatch.Draw(pixel, rect, bg * 0.96f);

				// Top bevel highlight strip
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), new Color(74, 108, 152) * 0.4f);

				// Left status accent bar
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 4, rect.Height), accentBar * 0.9f);

				// Outer frame
				int thickness = isHovered ? 2 : 1;
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), border);
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), border);
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), border);
				spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), border);

				// Tech corner notch cuts
				DrawCornerNotch(spriteBatch, pixel, rect.X, rect.Y, border);
				DrawCornerNotch(spriteBatch, pixel, rect.Right - 3, rect.Y, border);
				DrawCornerNotch(spriteBatch, pixel, rect.X, rect.Bottom - 3, border);
				DrawCornerNotch(spriteBatch, pixel, rect.Right - 3, rect.Bottom - 3, border);

				// Inner hover radiance
				if (isHovered)
				{
					spriteBatch.Draw(pixel, new Rectangle(rect.X + 4, rect.Y + 2, rect.Width - 6, rect.Height - 4), border * 0.10f);
				}

				// Draw single crisp, centered formatted text with ChatManager
				var font = FontAssets.MouseText.Value;
				Vector2 textSize = ChatManager.GetStringSize(font, labelText, new Vector2(0.88f));
				Vector2 textPos = new Vector2(d.X + (d.Width - textSize.X) * 0.5f + 2f, d.Y + (d.Height - textSize.Y) * 0.5f);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, labelText, textPos, Color.White, 0f, Vector2.Zero, new Vector2(0.88f));
			}

			private static void DrawCornerNotch(SpriteBatch sb, Texture2D pixel, int x, int y, Color col)
			{
				sb.Draw(pixel, new Rectangle(x, y, 3, 3), col * 0.85f);
			}
		}

		private class TabButton : UIPanel
		{
			private readonly Action onClick;

			public TabButton(string text, Action onClick)
			{
				this.onClick = onClick;

				SetPadding(0f);
				BackgroundColor = new Color(18, 24, 44);
				BorderColor = new Color(42, 64, 102);

				var label = new UIText(text, 0.82f)
				{
					HAlign = 0.5f,
					VAlign = 0.5f,
					TextColor = new Color(165, 195, 230)
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
				BackgroundColor = new Color(28, 38, 70);
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				BackgroundColor = new Color(18, 24, 44);
			}
		}

		private class CloseButton : UIPanel
		{
			public event Action Clicked;

			private static readonly Color IdleColor = new Color(95, 35, 35);
			private static readonly Color HoverColor = new Color(145, 50, 50);

			public CloseButton()
			{
				SetPadding(0f);
				BackgroundColor = IdleColor;
				BorderColor = Color.White * 0.35f;

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
