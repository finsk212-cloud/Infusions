using System;
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
		private UIPanel backPanel;
		private UIText essenceText;
		private UIText protocolText;
		private UIText oddsText;

		private const float PanelWidth = 580f;
		private const float PanelHeight = 410f;

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (backPanel != null && backPanel.ContainsPoint(Main.MouseScreen))
			{
				Main.LocalPlayer.mouseInterface = true;
			}
		}

		public override void OnInitialize()
		{
			backPanel = new UIPanel();
			backPanel.Width.Set(PanelWidth, 0f);
			backPanel.Height.Set(PanelHeight, 0f);
			backPanel.HAlign = 0.5f;
			backPanel.VAlign = 0.5f;
			backPanel.BackgroundColor = new Color(18, 24, 46);
			backPanel.BorderColor = new Color(0, 180, 240) * 0.7f;

			// Top Navigation Tabs
			var decryptTab = new TabButton("Decrypt Chips", true, null);
			decryptTab.Left.Set(14f, 0f);
			decryptTab.Top.Set(10f, 0f);
			decryptTab.Width.Set(120f, 0f);
			decryptTab.Height.Set(26f, 0f);
			backPanel.Append(decryptTab);

			var storageTab = new TabButton("Chip Storage", false, () => ModContent.GetInstance<AugmentUISystem>().ShowShop());
			storageTab.Left.Set(140f, 0f);
			storageTab.Top.Set(10f, 0f);
			storageTab.Width.Set(120f, 0f);
			storageTab.Height.Set(26f, 0f);
			backPanel.Append(storageTab);

			// Machine Cores Badge
			UIPanel essenceBadge = new UIPanel();
			essenceBadge.Width.Set(180f, 0f);
			essenceBadge.Height.Set(26f, 0f);
			essenceBadge.Left.Set(-224f, 1f);
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

			// Main Center Pod Card
			var podCard = new PodDisplayCard();
			podCard.Width.Set(-28f, 1f);
			podCard.Height.Set(190f, 0f);
			podCard.Left.Set(14f, 0f);
			podCard.Top.Set(46f, 0f);
			podCard.BackgroundColor = new Color(12, 16, 32) * 0.95f;
			podCard.BorderColor = new Color(45, 65, 110) * 0.8f;

			UIText title = new UIText("YoRHa Data Decryption", 1.15f)
			{
				HAlign = 0.5f,
				TextColor = new Color(255, 235, 175)
			};
			title.Top.Set(12f, 0f);
			podCard.Append(title);

			protocolText = new UIText("Active Protocol: Pre-Hardmode Protocol", 0.82f)
			{
				HAlign = 0.5f,
				TextColor = new Color(100, 220, 255)
			};
			protocolText.Top.Set(36f, 0f);
			podCard.Append(protocolText);

			oddsText = new UIText("Odds: Common 80%  |  Rare 15%  |  Epic 5%  |  Legendary 0%", 0.80f)
			{
				HAlign = 0.5f,
				TextColor = new Color(185, 205, 235)
			};
			oddsText.Top.Set(156f, 0f);
			podCard.Append(oddsText);

			backPanel.Append(podCard);

			// Action Buttons Row
			// Button 1: Decrypt Now
			var decryptAction = new ActionCardButton(
				"DECRYPT (3 Cores)",
				"Picks 1 of 3 chips immediately",
				new Color(20, 60, 95),
				new Color(30, 100, 155),
				new Color(0, 200, 255) * 0.8f,
				PerformDecryptNow
			);
			decryptAction.Width.Set(260f, 0f);
			decryptAction.Height.Set(68f, 0f);
			decryptAction.Left.Set(14f, 0f);
			decryptAction.Top.Set(248f, 0f);
			backPanel.Append(decryptAction);

			// Button 2: Buy Item
			var buyItemAction = new ActionCardButton(
				"BUY ITEM (3 Cores)",
				"Adds Sealed Cache to inventory",
				new Color(65, 50, 20),
				new Color(105, 80, 30),
				new Color(250, 195, 60) * 0.8f,
				PerformBuyCacheItem
			);
			buyItemAction.Width.Set(260f, 0f);
			buyItemAction.Height.Set(68f, 0f);
			buyItemAction.Left.Set(-274f, 1f);
			buyItemAction.Top.Set(248f, 0f);
			backPanel.Append(buyItemAction);

			// Bottom explanatory note
			var note = new UIText("Decrypted chips roll from your current progression tier and never duplicate already owned chips.", 0.73f)
			{
				HAlign = 0.5f,
				TextColor = new Color(140, 160, 190)
			};
			note.Top.Set(330f, 0f);
			backPanel.Append(note);

			Append(backPanel);
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
		}

		private void PerformDecryptNow()
		{
			var player = Main.LocalPlayer;
			int coreType = ModContent.ItemType<AugmentEssenceItem>();
			int coreCount = player.CountItem(coreType);
			if (coreCount < 3)
			{
				Main.NewText("Requires 3 Machine Cores to decrypt.", 255, 90, 90);
				SoundEngine.PlaySound(SoundID.MenuClose);
				return;
			}

			// Consume 3 Machine Cores
			for (int i = 0; i < 3; i++)
				player.ConsumeItem(coreType);

			// Decryption Audio and FX
			SoundEngine.PlaySound(SoundID.Research with { Volume = 0.95f, Pitch = 0.1f }, player.Center);
			for (int i = 0; i < 25; i++)
			{
				Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Electric, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 0, default, 1.3f);
				d.noGravity = true;
			}
			for (int i = 0; i < 15; i++)
			{
				Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.GoldFlame, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f), 0, default, 1.1f);
				d.noGravity = true;
			}

			// Close gacha UI and open 3-Card Decryption modal
			ModContent.GetInstance<AugmentUISystem>().HideGacha();
			RarityBracket currentBracket = BossTierMap.GetCurrentWorldBracket();
			AugmentRewardLogic.GrantReward(player, currentBracket);
		}

		private void PerformBuyCacheItem()
		{
			var player = Main.LocalPlayer;
			int coreType = ModContent.ItemType<AugmentEssenceItem>();
			int coreCount = player.CountItem(coreType);
			if (coreCount < 3)
			{
				Main.NewText("Requires 3 Machine Cores to purchase.", 255, 90, 90);
				SoundEngine.PlaySound(SoundID.MenuClose);
				return;
			}

			// Consume 3 Machine Cores
			for (int i = 0; i < 3; i++)
				player.ConsumeItem(coreType);

			// Award 1 SealedChipCacheItem
			player.QuickSpawnItem(player.GetSource_FromThis(), ModContent.ItemType<SealedChipCacheItem>());
			SoundEngine.PlaySound(SoundID.Grab);
			Main.NewText("Acquired Sealed Chip Cache!", 100, 225, 255);
			Refresh();
		}

		// Draws clean pixel art pod sprite with NO fuzzy glow box
		private class PodDisplayCard : UIPanel
		{
			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				base.DrawSelf(spriteBatch);

				CalculatedStyle d = GetDimensions();

				if (ModContent.RequestIfExists<Texture2D>("Augments/Items/SealedChipCacheItem", out var cacheAsset))
				{
					Texture2D tex = cacheAsset.Value;
					if (tex != null)
					{
						Vector2 center = new Vector2(d.X + d.Width * 0.5f, d.Y + 98f);
						Vector2 origin = tex.Size() * 0.5f;
						// Clean 2.5x pixel art rendering
						spriteBatch.Draw(tex, center, null, Color.White, 0f, origin, 2.5f, SpriteEffects.None, 0f);
					}
				}
			}
		}

		// Action card button with title and explanation subtitle
		private class ActionCardButton : UIPanel
		{
			private readonly Action onClick;
			private readonly Color idleBg;
			private readonly Color hoverBg;
			private readonly Color idleBorder;

			public ActionCardButton(string mainText, string subText, Color idleBg, Color hoverBg, Color border, Action onClick)
			{
				this.onClick = onClick;
				this.idleBg = idleBg;
				this.hoverBg = hoverBg;
				this.idleBorder = border;

				SetPadding(0f);
				BackgroundColor = idleBg;
				BorderColor = border;

				var main = new UIText(mainText, 0.90f)
				{
					HAlign = 0.5f,
					TextColor = Color.White
				};
				main.Top.Set(12f, 0f);
				Append(main);

				var sub = new UIText(subText, 0.72f)
				{
					HAlign = 0.5f,
					TextColor = new Color(200, 220, 240)
				};
				sub.Top.Set(36f, 0f);
				Append(sub);
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

		// Clean tab switcher button
		private class TabButton : UIPanel
		{
			private readonly Action onClick;
			private readonly bool isActive;

			public TabButton(string text, bool isActive, Action onClick)
			{
				this.isActive = isActive;
				this.onClick = onClick;

				SetPadding(0f);
				BackgroundColor = isActive ? new Color(25, 38, 72) : new Color(14, 18, 34);
				BorderColor = isActive ? new Color(0, 200, 255) : new Color(45, 60, 95);

				var label = new UIText(text, 0.80f)
				{
					HAlign = 0.5f,
					VAlign = 0.5f,
					TextColor = isActive ? Color.White : new Color(140, 160, 190)
				};
				Append(label);
			}

			public override void LeftClick(UIMouseEvent evt)
			{
				base.LeftClick(evt);
				if (!isActive)
				{
					SoundEngine.PlaySound(SoundID.MenuTick);
					onClick?.Invoke();
				}
			}

			public override void MouseOver(UIMouseEvent evt)
			{
				base.MouseOver(evt);
				if (!isActive)
				{
					BackgroundColor = new Color(22, 30, 56);
				}
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				if (!isActive)
				{
					BackgroundColor = new Color(14, 18, 34);
				}
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
