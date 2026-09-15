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
		private UIText essenceText;
		private UIText protocolText;
		private UIText oddsText;

		// Interactive Content Container
		private UIPanel chamberContainer;

		private ChamberState state = ChamberState.Idle;
		private int animTimer;
		private int lastTickFrame;
		private Augment wonAugment;
		private bool wonAugmentArchived;
		private int wonRefundCores;

		private readonly List<Augment> sampleChips = new List<Augment>();
		private int currentCycleIndex;

		private const float PanelWidth = 640f;
		private const float PanelHeight = 490f;

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (backPanel != null && backPanel.ContainsPoint(Main.MouseScreen))
			{
				Main.LocalPlayer.mouseInterface = true;
			}

			if (state == ChamberState.Decrypting)
			{
				animTimer++;

				// Deceleration curve over 170 frames (~2.8s)
				int delay = 4;
				if (animTimer > 130) delay = 18;
				else if (animTimer > 95) delay = 11;
				else if (animTimer > 60) delay = 6;

				if (animTimer - lastTickFrame >= delay && animTimer < 165)
				{
					lastTickFrame = animTimer;
					if (sampleChips.Count > 0)
						currentCycleIndex = (currentCycleIndex + 1) % sampleChips.Count;

					float pitch = -0.1f + (animTimer / 170f) * 0.35f;
					SoundEngine.PlaySound(SoundID.MenuTick with { Pitch = pitch, Volume = 0.85f });
				}

				if (animTimer >= 170)
				{
					state = ChamberState.Revealed;
					OnDecryptionRevealed();
					RebuildChamberContent();
				}
			}
		}

		public override void OnInitialize()
		{
			sampleChips.Clear();
			sampleChips.AddRange(AugmentDatabase.All);

			backPanel = new UIPanel();
			backPanel.Width.Set(PanelWidth, 0f);
			backPanel.Height.Set(PanelHeight, 0f);
			backPanel.HAlign = 0.5f;
			backPanel.VAlign = 0.5f;
			backPanel.BackgroundColor = new Color(16, 22, 42) * 0.98f;
			backPanel.BorderColor = new Color(0, 190, 255) * 0.7f;

			// Top Navigation Tab: Return to Storage
			var storageTab = new TabButton("← Chip Storage & Dismantle", () => ModContent.GetInstance<AugmentUISystem>().ShowShop());
			storageTab.Left.Set(14f, 0f);
			storageTab.Top.Set(10f, 0f);
			storageTab.Width.Set(210f, 0f);
			storageTab.Height.Set(26f, 0f);
			backPanel.Append(storageTab);

			// Machine Cores Badge
			UIPanel essenceBadge = new UIPanel();
			essenceBadge.Width.Set(190f, 0f);
			essenceBadge.Height.Set(26f, 0f);
			essenceBadge.Left.Set(-234f, 1f);
			essenceBadge.Top.Set(10f, 0f);
			essenceBadge.SetPadding(0f);
			essenceBadge.BackgroundColor = new Color(12, 18, 36) * 0.95f;
			essenceBadge.BorderColor = new Color(80, 180, 255) * 0.7f;

			essenceText = new UIText("Machine Cores: 0", 0.82f)
			{
				HAlign = 0.5f,
				VAlign = 0.5f,
				TextColor = new Color(100, 225, 255)
			};
			essenceBadge.Append(essenceText);
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

			protocolText = new UIText("Active Protocol: Pre-Hardmode Protocol", 0.82f)
			{
				HAlign = 0.5f,
				TextColor = new Color(100, 220, 255)
			};
			protocolText.Top.Set(68f, 0f);
			backPanel.Append(protocolText);

			oddsText = new UIText("Odds: Common 80%  |  Rare 15%  |  Epic 5%  |  Legendary 0%", 0.78f)
			{
				HAlign = 0.5f,
				TextColor = new Color(175, 195, 225)
			};
			oddsText.Top.Set(88f, 0f);
			backPanel.Append(oddsText);

			// Main Containment Chamber Container
			chamberContainer = new UIPanel();
			chamberContainer.Width.Set(-28f, 1f);
			chamberContainer.Height.Set(350f, 0f);
			chamberContainer.Left.Set(14f, 0f);
			chamberContainer.Top.Set(114f, 0f);
			chamberContainer.BackgroundColor = new Color(10, 14, 28) * 0.95f;
			chamberContainer.BorderColor = new Color(40, 60, 100) * 0.8f;
			backPanel.Append(chamberContainer);

			Append(backPanel);

			RebuildChamberContent();
		}

		public void Refresh()
		{
			int count = Main.LocalPlayer.CountItem(ModContent.ItemType<AugmentEssenceItem>());
			essenceText?.SetText($"Machine Cores: {count}");

			RarityBracket bracket = BossTierMap.GetCurrentWorldBracket();
			string bracketName = BossTierMap.GetBracketName(bracket);
			protocolText?.SetText($"Active Protocol: {bracketName} Protocol");

			RarityRollChances chances = BossRarityRoller.GetChancesForBracket(bracket);
			oddsText?.SetText($"Odds: Common {chances.Common}%  |  Rare {chances.Rare}%  |  Epic {chances.Epic}%  |  Legendary {chances.Legendary}%");

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
			podView.Top.Set(24f, 0f);
			chamberContainer.Append(podView);

			var readyText = new UIText("YoRHa Encrypted Salvage Pod Loaded", 0.92f)
			{
				HAlign = 0.5f,
				TextColor = new Color(200, 225, 255)
			};
			readyText.Top.Set(156f, 0f);
			chamberContainer.Append(readyText);

			var subtext = new UIText("Consume 3 Machine Cores to synthesize neural frequency and extract 1 Plug-in Chip", 0.74f)
			{
				HAlign = 0.5f,
				TextColor = new Color(140, 160, 190)
			};
			subtext.Top.Set(182f, 0f);
			chamberContainer.Append(subtext);

			// Decrypt action button
			var decryptBtn = new ChamberActionButton(
				"INITIALIZE DECRYPTION (3 Cores)",
				new Color(22, 65, 100),
				new Color(35, 105, 160),
				new Color(0, 200, 255) * 0.9f,
				StartDecryption
			);
			decryptBtn.Width.Set(340f, 0f);
			decryptBtn.Height.Set(52f, 0f);
			decryptBtn.HAlign = 0.5f;
			decryptBtn.Top.Set(230f, 0f);
			chamberContainer.Append(decryptBtn);

			var footer = new UIText("Rolls are guaranteed to prioritize unowned chips from your active progression bracket.", 0.70f)
			{
				HAlign = 0.5f,
				TextColor = new Color(120, 140, 170)
			};
			footer.Top.Set(298f, 0f);
			chamberContainer.Append(footer);
		}

		private void BuildDecryptingChamber()
		{
			var cyclingView = new CyclingDataCard(() => {
				if (sampleChips.Count == 0) return null;
				return sampleChips[currentCycleIndex % sampleChips.Count];
			});
			cyclingView.Width.Set(360f, 0f);
			cyclingView.Height.Set(200f, 0f);
			cyclingView.HAlign = 0.5f;
			cyclingView.Top.Set(30f, 0f);
			chamberContainer.Append(cyclingView);

			var statusText = new UIText("SYNCHRONIZING NEURAL FREQUENCY...", 0.95f)
			{
				HAlign = 0.5f,
				TextColor = new Color(255, 215, 100)
			};
			statusText.Top.Set(250f, 0f);
			chamberContainer.Append(statusText);

			var sub = new UIText("Deciphering combat memory stream", 0.76f)
			{
				HAlign = 0.5f,
				TextColor = new Color(100, 220, 255)
			};
			sub.Top.Set(280f, 0f);
			chamberContainer.Append(sub);
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

			// Winning Chip Card Container
			var chipCard = new UIPanel();
			chipCard.Width.Set(500f, 0f);
			chipCard.Height.Set(180f, 0f);
			chipCard.HAlign = 0.5f;
			chipCard.Top.Set(20f, 0f);
			chipCard.BackgroundColor = new Color(18, 25, 48);
			chipCard.BorderColor = rarityColor * 0.9f;

			// Icon Drawer
			var iconElement = new RevealedChipIcon(wonAugment);
			iconElement.Width.Set(50f, 0f);
			iconElement.Height.Set(50f, 0f);
			iconElement.Left.Set(20f, 0f);
			iconElement.Top.Set(24f, 0f);
			chipCard.Append(iconElement);

			// Chip Name
			var nameText = new UIText(wonAugment.DisplayName, 1.15f)
			{
				TextColor = rarityColor
			};
			nameText.Left.Set(84f, 0f);
			nameText.Top.Set(18f, 0f);
			chipCard.Append(nameText);

			// Rarity & Class pill
			string rarityLabel = wonAugment.KeystoneFamily != null ? $"[{wonAugment.Rarity.ToString().ToUpper()} KEYSTONE]" : $"[{wonAugment.Rarity.ToString().ToUpper()}]";
			var tierText = new UIText($"{rarityLabel}  •  Class: {wonAugment.Class}", 0.78f)
			{
				TextColor = new Color(200, 215, 240)
			};
			tierText.Left.Set(84f, 0f);
			tierText.Top.Set(46f, 0f);
			chipCard.Append(tierText);

			// Description
			var descText = new UIText(wonAugment.Description, 0.74f)
			{
				TextColor = new Color(165, 185, 215)
			};
			descText.Left.Set(84f, 0f);
			descText.Top.Set(72f, 0f);
			descText.Width.Set(390f, 0f);
			chipCard.Append(descText);

			// Install notice badge
			string statusNotice = wonAugmentArchived
				? $"✦ Neural Frame full (5/5) — Archived at Mistress 2B (+{wonRefundCores} Cores) ✦"
				: "✦ Installed directly into your Neural Frame! ✦";
			var statusText = new UIText(statusNotice, 0.78f)
			{
				HAlign = 0.5f,
				TextColor = wonAugmentArchived ? new Color(255, 160, 100) : new Color(100, 255, 180)
			};
			statusText.Top.Set(146f, 0f);
			chipCard.Append(statusText);

			chamberContainer.Append(chipCard);

			// Action Buttons
			var decryptAgainBtn = new ChamberActionButton(
				"DECRYPT AGAIN (3 Cores)",
				new Color(22, 65, 100),
				new Color(35, 105, 160),
				new Color(0, 200, 255) * 0.9f,
				StartDecryption
			);
			decryptAgainBtn.Width.Set(240f, 0f);
			decryptAgainBtn.Height.Set(46f, 0f);
			decryptAgainBtn.Left.Set(50f, 0f);
			decryptAgainBtn.Top.Set(230f, 0f);
			chamberContainer.Append(decryptAgainBtn);

			var returnBtn = new ChamberActionButton(
				"RETURN TO STORAGE",
				new Color(40, 50, 75),
				new Color(60, 75, 110),
				new Color(120, 140, 180) * 0.8f,
				() => ModContent.GetInstance<AugmentUISystem>().ShowShop()
			);
			returnBtn.Width.Set(240f, 0f);
			returnBtn.Height.Set(46f, 0f);
			returnBtn.Left.Set(-290f, 1f);
			returnBtn.Top.Set(230f, 0f);
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

			// Authoritatively grant or archive the augment
			wonAugmentArchived = ap.Owned.Count >= AugmentPlayer.MaxOwnedAugments;
			wonRefundCores = wonAugmentArchived ? AugmentPlayer.GetRemoveRefund(wonAugment.Rarity) : 0;
			ap.ChooseReward(wonAugment);

			// Start decryption animation
			state = ChamberState.Decrypting;
			animTimer = 0;
			lastTickFrame = 0;
			currentCycleIndex = Main.rand.Next(sampleChips.Count);

			SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.8f, Pitch = -0.2f }, player.Center);
			RebuildChamberContent();
		}

		private void OnDecryptionRevealed()
		{
			var player = Main.LocalPlayer;
			if (wonAugment == null) return;

			// Sound and particle fanfare based on rarity
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

		// Clean 2.5x pixel art pod preview with zero fuzzy glow box
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
						float bob = (float)Math.Sin(Main.timeForVisualEffects * 0.04f) * 3f;
						Vector2 center = new Vector2(d.X + d.Width * 0.5f, d.Y + d.Height * 0.5f + bob);
						Vector2 origin = tex.Size() * 0.5f;
						spriteBatch.Draw(tex, center, null, Color.White, 0f, origin, 2.5f, SpriteEffects.None, 0f);
					}
				}
			}
		}

		// Rapidly cycling holographic chip display during extraction
		private class CyclingDataCard : UIPanel
		{
			private readonly Func<Augment> getChip;

			public CyclingDataCard(Func<Augment> getChip)
			{
				this.getChip = getChip;
				BackgroundColor = new Color(14, 20, 40) * 0.95f;
				BorderColor = new Color(0, 200, 255) * 0.8f;
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				base.DrawSelf(spriteBatch);

				CalculatedStyle d = GetDimensions();
				Augment chip = getChip();
				if (chip == null) return;

				Color c = GetRarityColor(chip.Rarity);

				// Draw chip icon
				Texture2D icon = AugmentSlotElement.GetClassIcon(chip.Class);
				if (icon != null)
				{
					Vector2 iconCenter = new Vector2(d.X + d.Width * 0.5f, d.Y + 70f);
					spriteBatch.Draw(icon, iconCenter, null, c, 0f, icon.Size() * 0.5f, 1.8f, SpriteEffects.None, 0f);
				}

				// Draw cycling chip name
				string text = chip.DisplayName;
				Vector2 textSize = FontAssets.MouseText.Value.MeasureString(text) * 0.95f;
				Vector2 textPos = new Vector2(d.X + (d.Width - textSize.X) * 0.5f, d.Y + 130f);
				Utils.DrawBorderString(spriteBatch, text, textPos, c, 0.95f);

				// Scanning bracket lines
				float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.timeForVisualEffects * 0.15f);
				Color scanColor = Color.Cyan * (0.4f * pulse);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)d.X + 20, (int)d.Y + 165, (int)d.Width - 40, 2), scanColor);
			}
		}

		// Display for revealed chip icon
		private class RevealedChipIcon : UIElement
		{
			private readonly Augment augment;

			public RevealedChipIcon(Augment augment)
			{
				this.augment = augment;
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				CalculatedStyle d = GetDimensions();
				Texture2D icon = AugmentSlotElement.GetClassIcon(augment.Class);
				if (icon != null)
				{
					Vector2 center = new Vector2(d.X + d.Width * 0.5f, d.Y + d.Height * 0.5f);
					Color iconColor = GetRarityColor(augment.Rarity);
					spriteBatch.Draw(icon, center, null, iconColor, 0f, icon.Size() * 0.5f, 1.6f, SpriteEffects.None, 0f);
				}
			}
		}

		private class ChamberActionButton : UIPanel
		{
			private readonly Action onClick;
			private readonly Color idleBg;
			private readonly Color hoverBg;
			private readonly Color border;

			public ChamberActionButton(string text, Color idleBg, Color hoverBg, Color border, Action onClick)
			{
				this.onClick = onClick;
				this.idleBg = idleBg;
				this.hoverBg = hoverBg;
				this.border = border;

				SetPadding(0f);
				BackgroundColor = idleBg;
				BorderColor = border;

				var label = new UIText(text, 0.88f)
				{
					HAlign = 0.5f,
					VAlign = 0.5f,
					TextColor = Color.White
				};
				Append(label);
			}

			public override void LeftClick(UIMouseEvent evt)
			{
				base.LeftClick(evt);
				onClick?.Invoke();
			}

			public override void MouseOver(UIMouseEvent evt)
			{
				base.MouseOver(evt);
				BackgroundColor = hoverBg;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				BackgroundColor = idleBg;
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
