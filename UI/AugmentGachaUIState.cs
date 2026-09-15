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
		private const int RollDuration = 195; // ~3.25 seconds
		private const int WinnerReelIndex = 22;

		private Augment wonAugment;
		private bool wonAugmentArchived;
		private int wonRefundCores;

		private readonly List<Augment> reelCards = new List<Augment>();
		private readonly UIParticleSystem particles = new UIParticleSystem(160);

		private const float PanelWidth = 640f;
		private const float PanelHeight = 490f;

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
					Vector2 reticlePos = new Vector2(chamberDims.X + chamberDims.Width * 0.5f, chamberDims.Y + 25f + Main.rand.NextFloat(chamberDims.Height - 50f));
					Color sparkColor = Main.rand.NextBool(3) ? new Color(255, 225, 100) : new Color(0, 230, 255);
					particles.SpawnLaserSpark(reticlePos, sparkColor);
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
			storageTab.Width.Set(210f, 0f);
			storageTab.Height.Set(26f, 0f);
			backPanel.Append(storageTab);

			// Machine Cores Badge with rich colored text
			UIPanel essenceBadge = new UIPanel();
			essenceBadge.Width.Set(190f, 0f);
			essenceBadge.Height.Set(26f, 0f);
			essenceBadge.Left.Set(-234f, 1f);
			essenceBadge.Top.Set(10f, 0f);
			essenceBadge.SetPadding(0f);
			essenceBadge.BackgroundColor = new Color(12, 18, 36) * 0.95f;
			essenceBadge.BorderColor = new Color(80, 180, 255) * 0.7f;

			essenceLabel = new ColoredLabel("[c/FFE080:Machine Cores:] [c/00FFFF:0]", 0.82f);
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
			UIText title = new UIText("YoRHa Neural Decryption Chamber", 1.15f)
			{
				HAlign = 0.5f,
				TextColor = new Color(255, 235, 175)
			};
			title.Top.Set(44f, 0f);
			backPanel.Append(title);

			// Active Protocol with bright cyan accent
			protocolLabel = new ColoredLabel("[c/70A0D0:Active Protocol:] [c/00FFFF:Pre-Hardmode Protocol]", 0.82f)
			{
				HAlign = 0.5f
			};
			protocolLabel.Top.Set(68f, 0f);
			protocolLabel.Width.Set(PanelWidth - 40f, 0f);
			protocolLabel.Height.Set(18f, 0f);
			backPanel.Append(protocolLabel);

			// Live Odds Row with distinct rarity colors
			oddsLabel = new ColoredLabel("[c/80B0E0:Odds:] [c/D0D8E8:Common 80%]  |  [c/00FFFF:Rare 15%]  |  [c/D060FF:Epic 5%]  |  [c/FFD700:Legendary 0%]", 0.78f)
			{
				HAlign = 0.5f
			};
			oddsLabel.Top.Set(88f, 0f);
			oddsLabel.Width.Set(PanelWidth - 40f, 0f);
			oddsLabel.Height.Set(18f, 0f);
			backPanel.Append(oddsLabel);

			// Main Containment Chamber Container
			chamberContainer = new UIPanel();
			chamberContainer.Width.Set(-28f, 1f);
			chamberContainer.Height.Set(350f, 0f);
			chamberContainer.Left.Set(14f, 0f);
			chamberContainer.Top.Set(114f, 0f);
			chamberContainer.BackgroundColor = new Color(10, 14, 28) * 0.95f;
			chamberContainer.BorderColor = new Color(40, 65, 115) * 0.85f;
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
			oddsLabel?.SetText($"[c/80B0E0:Odds:]  [c/D0D8E8:Common {chances.Common}%]  |  [c/00FFFF:Rare {chances.Rare}%]  |  [c/D060FF:Epic {chances.Epic}%]  |  [c/FFD700:Legendary {chances.Legendary}%]");

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
			podView.Width.Set(120f, 0f);
			podView.Height.Set(120f, 0f);
			podView.HAlign = 0.5f;
			podView.Top.Set(20f, 0f);
			chamberContainer.Append(podView);

			var readyLabel = new ColoredLabel("[c/FFE080:YoRHa Encrypted Salvage Pod Loaded]", 0.94f)
			{
				HAlign = 0.5f
			};
			readyLabel.Top.Set(150f, 0f);
			readyLabel.Width.Set(450f, 0f);
			readyLabel.Height.Set(22f, 0f);
			chamberContainer.Append(readyLabel);

			var subtext = new ColoredLabel("[c/90B0D0:Synthesize neural frequency to decrypt and extract 1 combat Plug-in Chip]", 0.76f)
			{
				HAlign = 0.5f
			};
			subtext.Top.Set(176f, 0f);
			subtext.Width.Set(520f, 0f);
			subtext.Height.Set(20f, 0f);
			chamberContainer.Append(subtext);

			// Decrypt action button with glowing cyan & gold styling
			var decryptBtn = new ChamberActionButton(
				"[c/00FFFF:INITIALIZE DECRYPTION]",
				"[c/FFD700:Cost: 3 Machine Cores]",
				new Color(18, 48, 80),
				new Color(28, 85, 140),
				new Color(0, 220, 255) * 0.9f,
				new Color(255, 215, 80),
				StartDecryption
			);
			decryptBtn.Width.Set(360f, 0f);
			decryptBtn.Height.Set(56f, 0f);
			decryptBtn.HAlign = 0.5f;
			decryptBtn.Top.Set(222f, 0f);
			chamberContainer.Append(decryptBtn);

			var footer = new ColoredLabel("[c/7890B0:Rolls prioritize unowned chips from your active progression bracket.]", 0.70f)
			{
				HAlign = 0.5f
			};
			footer.Top.Set(294f, 0f);
			footer.Width.Set(500f, 0f);
			footer.Height.Set(18f, 0f);
			chamberContainer.Append(footer);
		}

		private void BuildDecryptingChamber()
		{
			// High-speed Roulette Reel rolling horizontal tape
			var reelView = new RouletteReelView(reelCards, WinnerReelIndex, RollDuration, () => animTimer);
			reelView.Width.Set(-20f, 1f);
			reelView.Height.Set(190f, 0f);
			reelView.HAlign = 0.5f;
			reelView.Top.Set(18f, 0f);
			chamberContainer.Append(reelView);

			var statusLabel = new ColoredLabel("[c/00FFFF:SYNCHRONIZING NEURAL FREQUENCY...]", 0.95f)
			{
				HAlign = 0.5f
			};
			statusLabel.Top.Set(232f, 0f);
			statusLabel.Width.Set(450f, 0f);
			statusLabel.Height.Set(24f, 0f);
			chamberContainer.Append(statusLabel);

			var subLabel = new ColoredLabel("[c/FFE080:Decelerating target reticle across active combat memory streams]", 0.76f)
			{
				HAlign = 0.5f
			};
			subLabel.Top.Set(262f, 0f);
			subLabel.Width.Set(520f, 0f);
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

			// Winning Chip Card Container with rarity glowing frame
			var chipCard = new RevealedChipCard(wonAugment, rarityColor);
			chipCard.Width.Set(520f, 0f);
			chipCard.Height.Set(185f, 0f);
			chipCard.HAlign = 0.5f;
			chipCard.Top.Set(18f, 0f);

			// Chip Name with bold rarity font
			var nameText = new UIText(wonAugment.DisplayName, 1.18f)
			{
				TextColor = rarityColor
			};
			nameText.Left.Set(88f, 0f);
			nameText.Top.Set(18f, 0f);
			chipCard.Append(nameText);

			// Rarity & Class pill
			string rarityLabel = wonAugment.KeystoneFamily != null ? $"[{wonAugment.Rarity.ToString().ToUpper()} KEYSTONE]" : $"[{wonAugment.Rarity.ToString().ToUpper()}]";
			var tierText = new UIText($"{rarityLabel}  •  Class: {wonAugment.Class}", 0.80f)
			{
				TextColor = new Color(205, 220, 245)
			};
			tierText.Left.Set(88f, 0f);
			tierText.Top.Set(48f, 0f);
			chipCard.Append(tierText);

			// Description
			var descText = new UIText(wonAugment.Description, 0.76f)
			{
				TextColor = new Color(175, 195, 225)
			};
			descText.Left.Set(88f, 0f);
			descText.Top.Set(74f, 0f);
			descText.Width.Set(400f, 0f);
			chipCard.Append(descText);

			// Install notice badge
			string statusNotice = wonAugmentArchived
				? $"✦ Neural Frame full (5/5) — Archived at Mistress 2B (+{wonRefundCores} Cores) ✦"
				: "✦ Installed directly into your Neural Frame! ✦";

			var statusLabel = new ColoredLabel(wonAugmentArchived ? $"[c/FFA064:{statusNotice}]" : $"[c/64FFB4:{statusNotice}]", 0.78f)
			{
				HAlign = 0.5f
			};
			statusLabel.Top.Set(150f, 0f);
			statusLabel.Width.Set(480f, 0f);
			statusLabel.Height.Set(20f, 0f);
			chipCard.Append(statusLabel);

			chamberContainer.Append(chipCard);

			// Action Buttons
			var decryptAgainBtn = new ChamberActionButton(
				"[c/00FFFF:DECRYPT AGAIN]",
				"[c/FFD700:(3 Cores)]",
				new Color(18, 48, 80),
				new Color(28, 85, 140),
				new Color(0, 220, 255) * 0.9f,
				new Color(255, 215, 80),
				StartDecryption
			);
			decryptAgainBtn.Width.Set(240f, 0f);
			decryptAgainBtn.Height.Set(50f, 0f);
			decryptAgainBtn.Left.Set(45f, 0f);
			decryptAgainBtn.Top.Set(232f, 0f);
			chamberContainer.Append(decryptAgainBtn);

			var returnBtn = new ChamberActionButton(
				"[c/D0E0FF:RETURN TO STORAGE]",
				"[c/80A0C0:View Archive & Frame]",
				new Color(32, 42, 68),
				new Color(48, 62, 98),
				new Color(90, 120, 170) * 0.85f,
				new Color(140, 180, 240),
				() => ModContent.GetInstance<AugmentUISystem>().ShowShop()
			);
			returnBtn.Width.Set(240f, 0f);
			returnBtn.Height.Set(50f, 0f);
			returnBtn.Left.Set(-285f, 1f);
			returnBtn.Top.Set(232f, 0f);
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

			// Pre-generate the 28-card tape strip with wonAugment cleanly placed at WinnerReelIndex (22)
			reelCards.Clear();
			var allChips = AugmentDatabase.All;
			for (int i = 0; i < 28; i++)
			{
				if (i == WinnerReelIndex)
				{
					reelCards.Add(wonAugment);
				}
				else
				{
					// Random chip for rolling suspense
					Augment randomChip = allChips[Main.rand.Next(allChips.Count)];
					reelCards.Add(randomChip);
				}
			}

			// Transition to Decrypting state
			state = ChamberState.Decrypting;
			animTimer = 0;

			// Sound effect for starting decryption cycle
			SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.75f, Pitch = -0.2f }, player.Center);
			RebuildChamberContent();
		}

		private void OnDecryptionRevealed()
		{
			var player = Main.LocalPlayer;
			if (wonAugment == null)
				return;

			// Sound and particle fanfare based on rarity
			CalculatedStyle chamberDims = chamberContainer != null ? chamberContainer.GetDimensions() : default;
			Vector2 revealCenter = new Vector2(chamberDims.X + chamberDims.Width * 0.5f, chamberDims.Y + 110f);

			Color rarityColor = GetRarityColor(wonAugment.Rarity);
			particles.SpawnBurst(revealCenter, rarityColor, count: 55, maxSpeed: 5f);

			switch (wonAugment.Rarity)
			{
				case AugmentRarity.Legendary:
					SoundEngine.PlaySound(SoundID.Item29 with { Volume = 1.0f, Pitch = 0.1f }, player.Center);
					for (int i = 0; i < 35; i++)
					{
						Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.GoldFlame, Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f), 0, default, 1.5f);
						d.noGravity = true;
					}
					break;
				case AugmentRarity.Epic:
					SoundEngine.PlaySound(SoundID.Item100 with { Volume = 0.95f, Pitch = 0.1f }, player.Center);
					for (int i = 0; i < 25; i++)
					{
						Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.PurpleTorch, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 0, default, 1.3f);
						d.noGravity = true;
					}
					break;
				case AugmentRarity.Rare:
					SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.90f, Pitch = 0.1f }, player.Center);
					for (int i = 0; i < 20; i++)
					{
						Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Electric, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 0, default, 1.2f);
						d.noGravity = true;
					}
					break;
				default:
					SoundEngine.PlaySound(SoundID.Research with { Volume = 0.90f, Pitch = 0.1f }, player.Center);
					for (int i = 0; i < 15; i++)
					{
						Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Iron, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f), 0, default, 1.1f);
						d.noGravity = true;
					}
					break;
			}

			// NOW authoritatively grant or archive the augment so chat announces it at the exact moment of reveal!
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

		// Floating 2.5x pixel art pod preview
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
						float bob = (float)Math.Sin(Main.timeForVisualEffects * 0.04f) * 3.5f;
						Vector2 center = new Vector2(d.X + d.Width * 0.5f, d.Y + d.Height * 0.5f + bob);
						Vector2 origin = tex.Size() * 0.5f;

						// Subtle glowing aura behind pod
						float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.timeForVisualEffects * 0.08f);
						Color aura = new Color(0, 200, 255) * (0.25f * pulse);
						spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)center.X - 35, (int)center.Y - 35, 70, 70), aura);

						spriteBatch.Draw(tex, center, null, Color.White, 0f, origin, 2.5f, SpriteEffects.None, 0f);
					}
				}
			}
		}

		// Horizontal Roulette Reel Tape with Deceleration & Sound Ticks
		private class RouletteReelView : UIElement
		{
			private readonly List<Augment> cards;
			private readonly int targetWinnerIndex;
			private readonly int duration;
			private readonly Func<int> getTimer;
			private int lastTickedIndex = -1;

			private const float CardWidth = 112f;
			private const float CardHeight = 150f;
			private const float CardGap = 12f;
			private const float CardPitch = CardWidth + CardGap; // 124px

			public RouletteReelView(List<Augment> cards, int targetWinnerIndex, int duration, Func<int> getTimer)
			{
				this.cards = cards;
				this.targetWinnerIndex = targetWinnerIndex;
				this.duration = duration;
				this.getTimer = getTimer;
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				CalculatedStyle d = GetDimensions();
				Texture2D pixel = TextureAssets.MagicPixel.Value;

				// Background dark viewport panel
				spriteBatch.Draw(pixel, new Rectangle((int)d.X, (int)d.Y, (int)d.Width, (int)d.Height), new Color(8, 12, 24) * 0.96f);
				DrawBorder(spriteBatch, new Rectangle((int)d.X, (int)d.Y, (int)d.Width, (int)d.Height), new Color(40, 65, 115) * 0.8f, 1);

				float viewportCenter = d.Width * 0.5f;
				float targetScroll = (targetWinnerIndex * CardPitch + CardWidth * 0.5f) - viewportCenter;

				int timer = getTimer();
				float progress = MathHelper.Clamp(timer / (float)duration, 0f, 1f);

				// Quintic ease-out curve for fast spin transitioning into suspenseful slow ticks
				float ease = 1f - (float)Math.Pow(1f - progress, 5.0);
				float currentScroll = targetScroll * ease;

				// Sound tick detection when a card boundary crosses the center line
				int currentCenterCardIndex = (int)Math.Floor((currentScroll + viewportCenter) / CardPitch);
				if (currentCenterCardIndex != lastTickedIndex && timer < duration)
				{
					lastTickedIndex = currentCenterCardIndex;
					float pitch = -0.12f + (progress * 0.35f);
					SoundEngine.PlaySound(SoundID.MenuTick with { Pitch = pitch, Volume = 0.82f });
				}

				// Draw all visible cards
				float cardY = d.Y + (d.Height - CardHeight) * 0.5f;
				for (int i = 0; i < cards.Count; i++)
				{
					float cardX = d.X + (i * CardPitch) - currentScroll;
					if (cardX + CardWidth < d.X || cardX > d.X + d.Width)
						continue; // Culled outside viewport

					Augment card = cards[i];
					Color rColor = GetRarityColor(card.Rarity);

					// Card box
					Rectangle cardRect = new Rectangle((int)cardX, (int)cardY, (int)CardWidth, (int)CardHeight);
					spriteBatch.Draw(pixel, cardRect, new Color(14, 20, 38) * 0.95f);
					DrawBorder(spriteBatch, cardRect, rColor * 0.75f, 1);

					// Rarity label at top
					string rarityTag = $"[{card.Rarity.ToString().ToUpper()}]";
					var font = FontAssets.MouseText.Value;
					Vector2 tagSize = font.MeasureString(rarityTag) * 0.65f;
					Vector2 tagPos = new Vector2(cardX + (CardWidth - tagSize.X) * 0.5f, cardY + 8f);
					Utils.DrawBorderString(spriteBatch, rarityTag, tagPos, rColor * 0.9f, 0.65f);

					// Class icon centered
					Texture2D classIcon = AugmentSlotElement.GetClassIcon(card.Class);
					if (classIcon != null)
					{
						Vector2 iconCenter = new Vector2(cardX + CardWidth * 0.5f, cardY + 65f);
						spriteBatch.Draw(classIcon, iconCenter, null, rColor, 0f, classIcon.Size() * 0.5f, 1.45f, SpriteEffects.None, 0f);
					}

					// Chip display name centered below icon
					string displayName = card.DisplayName;
					if (displayName.Length > 12)
						displayName = displayName.Substring(0, 10) + "..";
					Vector2 nameSize = font.MeasureString(displayName) * 0.70f;
					Vector2 namePos = new Vector2(cardX + (CardWidth - nameSize.X) * 0.5f, cardY + 115f);
					Utils.DrawBorderString(spriteBatch, displayName, namePos, Color.White * 0.95f, 0.70f);
				}

				// Soft side vignette fade gradient masks on left and right edges for a seamless cinematic look
				const int fadeWidth = 65;
				const int steps = 13;
				float stepWidth = fadeWidth / (float)steps;
				Color edgeBg = new Color(8, 12, 24);

				for (int s = 0; s < steps; s++)
				{
					float alpha = 1f - (s / (float)steps);
					Color fadeCol = edgeBg * alpha;

					// Left edge fade
					spriteBatch.Draw(pixel, new Rectangle((int)(d.X + s * stepWidth), (int)d.Y, (int)Math.Ceiling(stepWidth) + 1, (int)d.Height), fadeCol);
					// Right edge fade
					spriteBatch.Draw(pixel, new Rectangle((int)(d.X + d.Width - (s + 1) * stepWidth), (int)d.Y, (int)Math.Ceiling(stepWidth) + 1, (int)d.Height), fadeCol);
				}

				// Central Reticle Laser Needle & Neon Indicator Arrows
				float reticleX = d.X + viewportCenter;
				float pulse = 0.6f + 0.4f * (float)Math.Sin(Main.timeForVisualEffects * 0.15f);
				Color laserColor = Color.Lerp(new Color(0, 225, 255), new Color(255, 215, 80), pulse);

				// Vertical neon laser beam
				spriteBatch.Draw(pixel, new Rectangle((int)reticleX - 1, (int)d.Y, 2, (int)d.Height), laserColor * 0.85f);

				// Top neon pointer arrow [▼]
				Vector2 topArrowSize = FontAssets.MouseText.Value.MeasureString("▼") * 0.85f;
				Vector2 topArrowPos = new Vector2(reticleX - topArrowSize.X * 0.5f, d.Y + 2f);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, "▼", topArrowPos, laserColor, 0f, Vector2.Zero, new Vector2(0.85f));

				// Bottom neon pointer arrow [▲]
				Vector2 botArrowSize = FontAssets.MouseText.Value.MeasureString("▲") * 0.85f;
				Vector2 botArrowPos = new Vector2(reticleX - botArrowSize.X * 0.5f, d.Y + d.Height - botArrowSize.Y - 2f);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, "▲", botArrowPos, laserColor, 0f, Vector2.Zero, new Vector2(0.85f));
			}

			private static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color, int thickness)
			{
				Texture2D pixel = TextureAssets.MagicPixel.Value;
				sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
				sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
				sb.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
				sb.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
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

				// Draw class icon
				Texture2D icon = AugmentSlotElement.GetClassIcon(augment.Class);
				if (icon != null)
				{
					Vector2 center = new Vector2(d.X + 46f, d.Y + 52f);
					spriteBatch.Draw(icon, center, null, rarityColor, 0f, icon.Size() * 0.5f, 1.6f, SpriteEffects.None, 0f);
				}
			}
		}

		// Action button with animated glowing borders and colored text
		private class ChamberActionButton : UIElement
		{
			private readonly string primaryText;
			private readonly string subText;
			private readonly Color idleBg;
			private readonly Color hoverBg;
			private readonly Color idleBorder;
			private readonly Color hoverBorder;
			private readonly Action onClick;
			private bool isHovered;

			public ChamberActionButton(string primaryText, string subText, Color idleBg, Color hoverBg, Color idleBorder, Color hoverBorder, Action onClick)
			{
				this.primaryText = primaryText;
				this.subText = subText;
				this.idleBg = idleBg;
				this.hoverBg = hoverBg;
				this.idleBorder = idleBorder;
				this.hoverBorder = hoverBorder;
				this.onClick = onClick;
			}

			public override void LeftClick(UIMouseEvent evt)
			{
				base.LeftClick(evt);
				onClick?.Invoke();
			}

			public override void MouseOver(UIMouseEvent evt)
			{
				base.MouseOver(evt);
				isHovered = true;
				SoundEngine.PlaySound(SoundID.MenuTick);
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
					spriteBatch.Draw(pixel, new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4), hoverBorder * 0.12f);
				}

				// Draw Primary and Subtext with ChatManager
				var font = FontAssets.MouseText.Value;
				if (!string.IsNullOrEmpty(primaryText))
				{
					Vector2 primSize = ChatManager.GetStringSize(font, primaryText, new Vector2(0.92f));
					float primY = string.IsNullOrEmpty(subText) ? d.Y + (d.Height - primSize.Y) * 0.5f : d.Y + 8f;
					Vector2 primPos = new Vector2(d.X + (d.Width - primSize.X) * 0.5f, primY);
					ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, primaryText, primPos, Color.White, 0f, Vector2.Zero, new Vector2(0.92f));
				}

				if (!string.IsNullOrEmpty(subText))
				{
					Vector2 subSize = ChatManager.GetStringSize(font, subText, new Vector2(0.74f));
					Vector2 subPos = new Vector2(d.X + (d.Width - subSize.X) * 0.5f, d.Y + d.Height - subSize.Y - 8f);
					ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, subText, subPos, Color.White, 0f, Vector2.Zero, new Vector2(0.74f));
				}
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

				var label = new UIText(text, 0.80f)
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
