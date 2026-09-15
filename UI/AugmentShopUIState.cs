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
	public class AugmentShopUIState : UIState
	{
		private ShopBackPanel backPanel;
		private UIText essenceText;
		private UndoReforgeBar undoReforgeBar;
		private UIList buyBackList;
		private UIList removeList;

		// YoRHa Gacha Decryption Terminal elements
		private DecryptionTerminalPanel decryptionTerminal;
		private UIText protocolText;
		private UIText oddsText;
		private GachaButton decryptNowButton;
		private GachaButton buyCacheButton;

		private const float PanelWidth = 840f;
		private const float PanelHeight = 630f;
		private const float TerminalTop = 52f;
		private const float TerminalHeight = 104f;
		private const float ListsTop = 208f;

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
			backPanel = new ShopBackPanel();
			backPanel.Width.Set(PanelWidth, 0f);
			backPanel.Height.Set(PanelHeight, 0f);
			backPanel.HAlign = 0.5f;
			backPanel.VAlign = 0.5f;
			backPanel.BackgroundColor = new Color(20, 28, 54);
			backPanel.BorderColor = new Color(38, 52, 98);

			// Title Header
			UIText title = new UIText("YoRHa Tactical Terminal", 1.15f)
			{
				HAlign = 0.5f,
				TextColor = new Color(255, 235, 175)
			};
			title.Top.Set(10f, 0f);
			backPanel.Append(title);

			// Subtitle
			UIText subtitle = new UIText("Mistress 2B's Console — Data Decryption, Chip Archive & Neural Frame Maintenance", 0.74f)
			{
				HAlign = 0.5f,
				TextColor = new Color(155, 170, 200)
			};
			subtitle.Top.Set(32f, 0f);
			backPanel.Append(subtitle);

			// Close Button in top-right corner
			var closeButton = new CloseButton();
			closeButton.Width.Set(24f, 0f);
			closeButton.Height.Set(24f, 0f);
			closeButton.HAlign = 1f;
			closeButton.Top.Set(10f, 0f);
			closeButton.Left.Set(-10f, 0f);
			closeButton.Clicked += () => ModContent.GetInstance<AugmentUISystem>().HideShop();
			backPanel.Append(closeButton);

			// Currency Badge pill in top-left
			UIPanel essenceBadge = new UIPanel();
			essenceBadge.Width.Set(210f, 0f);
			essenceBadge.Height.Set(26f, 0f);
			essenceBadge.Left.Set(14f, 0f);
			essenceBadge.Top.Set(10f, 0f);
			essenceBadge.SetPadding(0f);
			essenceBadge.BackgroundColor = new Color(15, 22, 42) * 0.95f;
			essenceBadge.BorderColor = new Color(80, 180, 255) * 0.7f;

			essenceText = new UIText("Machine Cores: 0", 0.82f)
			{
				HAlign = 0.5f,
				VAlign = 0.5f,
				TextColor = new Color(100, 225, 255)
			};
			essenceBadge.Append(essenceText);
			backPanel.Append(essenceBadge);

			// --- YoRHa Gacha Decryption Terminal ---
			decryptionTerminal = new DecryptionTerminalPanel();
			decryptionTerminal.Width.Set(-28f, 1f);
			decryptionTerminal.Height.Set(TerminalHeight, 0f);
			decryptionTerminal.Left.Set(14f, 0f);
			decryptionTerminal.Top.Set(TerminalTop, 0f);
			decryptionTerminal.BackgroundColor = new Color(10, 16, 32) * 0.95f;
			decryptionTerminal.BorderColor = new Color(0, 190, 255) * 0.8f;

			var termTitle = new UIText("YORHA DATA DECRYPTION PROTOCOL", 0.90f)
			{
				TextColor = new Color(100, 225, 255)
			};
			termTitle.Left.Set(74f, 0f);
			termTitle.Top.Set(8f, 0f);
			decryptionTerminal.Append(termTitle);

			protocolText = new UIText("Active Protocol: Pre-Hardmode Protocol", 0.80f)
			{
				TextColor = new Color(255, 225, 140)
			};
			protocolText.Left.Set(74f, 0f);
			protocolText.Top.Set(30f, 0f);
			decryptionTerminal.Append(protocolText);

			oddsText = new UIText("Odds: Common: 80% | Rare: 15% | Epic: 5% | Leg: 0%", 0.75f)
			{
				TextColor = new Color(175, 195, 225)
			};
			oddsText.Left.Set(74f, 0f);
			oddsText.Top.Set(52f, 0f);
			decryptionTerminal.Append(oddsText);

			var costNotice = new UIText("Cost: 3 Machine Cores  --  Decryption offers 3 unowned chips", 0.72f)
			{
				TextColor = new Color(130, 215, 180)
			};
			costNotice.Left.Set(74f, 0f);
			costNotice.Top.Set(74f, 0f);
			decryptionTerminal.Append(costNotice);

			// Decrypt Now Button
			decryptNowButton = new GachaButton("DECRYPT NOW (3 Cores)", new Color(18, 55, 85), new Color(28, 90, 140), new Color(40, 210, 255) * 0.8f, new Color(100, 240, 255));
			decryptNowButton.Width.Set(210f, 0f);
			decryptNowButton.Height.Set(38f, 0f);
			decryptNowButton.Left.Set(-224f, 1f);
			decryptNowButton.Top.Set(10f, 0f);
			decryptNowButton.Clicked += PerformDecryptNow;
			decryptionTerminal.Append(decryptNowButton);

			// Buy Cache Item Button
			buyCacheButton = new GachaButton("BUY CACHE ITEM (3 Cores)", new Color(55, 42, 18), new Color(90, 70, 25), new Color(240, 180, 50) * 0.8f, new Color(255, 220, 90), 0.76f);
			buyCacheButton.Width.Set(210f, 0f);
			buyCacheButton.Height.Set(34f, 0f);
			buyCacheButton.Left.Set(-224f, 1f);
			buyCacheButton.Top.Set(56f, 0f);
			buyCacheButton.Clicked += PerformBuyCacheItem;
			decryptionTerminal.Append(buyCacheButton);

			backPanel.Append(decryptionTerminal);

			// Optional Undo Reforge Bar
			undoReforgeBar = new UndoReforgeBar(TryUndoReforge);
			undoReforgeBar.Width.Set(-28f, 1f);
			undoReforgeBar.HAlign = 0.5f;
			undoReforgeBar.Top.Set(166f, 0f);
			undoReforgeBar.Height.Set(26f, 0f);

			// Column Headers
			UIText buyBackHeader = new UIText("Re-acquire (Buy Back)", 0.85f)
			{
				HAlign = 0f,
				TextColor = new Color(150, 225, 255)
			};
			buyBackHeader.Left.Set(14f, 0f);
			buyBackHeader.Top.Set(184f, 0f);
			backPanel.Append(buyBackHeader);

			UIText removeHeader = new UIText("Equipped (Dismantle)", 0.85f)
			{
				HAlign = 0f,
				TextColor = new Color(255, 185, 160)
			};
			removeHeader.Left.Set(18f, 0.5f);
			removeHeader.Top.Set(184f, 0f);
			backPanel.Append(removeHeader);

			// Left List: Buy Back
			buyBackList = new UIList();
			buyBackList.ManualSortMethod = _ => { };
			buyBackList.Top.Set(ListsTop, 0f);
			buyBackList.Left.Set(14f, 0f);
			buyBackList.Width.Set(-46f, 0.5f);
			buyBackList.Height.Set(-(ListsTop + 14f), 1f);
			buyBackList.ListPadding = 6f;
			backPanel.Append(buyBackList);

			ShopScrollbar buyBackScrollbar = new ShopScrollbar();
			buyBackScrollbar.Top.Set(ListsTop, 0f);
			buyBackScrollbar.Height.Set(-(ListsTop + 14f), 1f);
			buyBackScrollbar.Left.Set(-22f, 0.5f);
			buyBackList.SetScrollbar(buyBackScrollbar);
			backPanel.Append(buyBackScrollbar);

			// Right List: Remove
			removeList = new UIList();
			removeList.ManualSortMethod = _ => { };
			removeList.Top.Set(ListsTop, 0f);
			removeList.Left.Set(18f, 0.5f);
			removeList.Width.Set(-46f, 0.5f);
			removeList.Height.Set(-(ListsTop + 14f), 1f);
			removeList.ListPadding = 6f;
			backPanel.Append(removeList);

			ShopScrollbar removeScrollbar = new ShopScrollbar();
			removeScrollbar.Top.Set(ListsTop, 0f);
			removeScrollbar.Height.Set(-(ListsTop + 14f), 1f);
			removeScrollbar.Left.Set(-14f, 1f);
			removeList.SetScrollbar(removeScrollbar);
			backPanel.Append(removeScrollbar);

			Append(backPanel);
		}

		public void Refresh()
		{
			buyBackList.Clear();
			removeList.Clear();

			var player = Main.LocalPlayer;
			var augmentPlayer = player.GetModPlayer<AugmentPlayer>();

			int buyBackCount = 0;
			foreach (var id in augmentPlayer.SoldAugmentIds)
			{
				var augment = AugmentDatabase.GetById(id);
				if (augment == null || augment.Class == AugmentClass.Support)
					continue;

				int buyBackCost = AugmentPlayer.GetBuyBackCost(augment.Rarity);
				var entry = new AugmentShopEntry(augment, $"Buy ({buyBackCost} Core{(buyBackCost > 1 ? "s" : "")})", BuyBack);
				entry.Width.Set(0f, 1f);
				entry.Height.Set(54f, 0f);
				buyBackList.Add(entry);
				buyBackCount++;
			}

			if (buyBackCount == 0)
			{
				var empty = new UIText("No archived chips.\nChips you sell will appear here.", 0.8f)
				{
					HAlign = 0.5f,
					TextColor = new Color(130, 145, 175)
				};
				empty.Top.Set(40f, 0f);
				buyBackList.Add(empty);
			}

			int removeCount = 0;
			foreach (var augment in augmentPlayer.Owned)
			{
				AugmentShopEntry entry;
				if (augment.IsPermanent)
				{
					entry = new AugmentShopEntry(augment, "Permanent", null);
				}
				else
				{
					int removeRefund = AugmentPlayer.GetRemoveRefund(augment.Rarity);
					string label = removeRefund > 0 ? $"Remove (+{removeRefund} Core{(removeRefund > 1 ? "s" : "")})" : "Remove (Free)";
					entry = new AugmentShopEntry(augment, label, SellOwned);
				}
				entry.Width.Set(0f, 1f);
				entry.Height.Set(54f, 0f);
				removeList.Add(entry);
				removeCount++;
			}

			if (removeCount == 0)
			{
				var empty = new UIText("No chips currently equipped.", 0.8f)
				{
					HAlign = 0.5f,
					TextColor = new Color(130, 145, 175)
				};
				empty.Top.Set(40f, 0f);
				removeList.Add(empty);
			}

			RefreshEssenceText();
			RefreshGachaTerminal();
			RefreshUndoReforgeBar();
		}

		private void RefreshGachaTerminal()
		{
			RarityBracket bracket = BossTierMap.GetCurrentWorldBracket();
			string bracketName = BossTierMap.GetBracketName(bracket);
			protocolText.SetText($"Active Protocol: {bracketName} Protocol");

			RarityRollChances chances = BossRarityRoller.GetChancesForBracket(bracket);
			oddsText.SetText($"Odds: [Common: {chances.Common}%]  [Rare: {chances.Rare}%]  [Epic: {chances.Epic}%]  [Legendary: {chances.Legendary}%]");
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

			// Close shop and launch 3-Card Decryption modal
			ModContent.GetInstance<AugmentUISystem>().HideShop();
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

		private void RefreshUndoReforgeBar()
		{
			var augmentPlayer = Main.LocalPlayer.GetModPlayer<AugmentPlayer>();
			bool owns = augmentPlayer.HasAugment("reforgers_patience");
			bool pending = owns && augmentPlayer.HasPendingReforgeUndo;

			if (!owns)
			{
				if (backPanel.HasChild(undoReforgeBar))
					backPanel.RemoveChild(undoReforgeBar);
				undoReforgeBar.SetState(false, false, null, 0);
				return;
			}

			if (!backPanel.HasChild(undoReforgeBar))
				backPanel.Append(undoReforgeBar);

			undoReforgeBar.SetState(true, pending, pending ? augmentPlayer.LastReforgedItem : null, pending ? augmentPlayer.LastReforgeCost : 0);
		}

		private void TryUndoReforge()
		{
			var augmentPlayer = Main.LocalPlayer.GetModPlayer<AugmentPlayer>();
			if (augmentPlayer.TryUndoLastReforge())
				RefreshUndoReforgeBar();
		}

		private void RefreshEssenceText()
		{
			int count = Main.LocalPlayer.CountItem(ModContent.ItemType<AugmentEssenceItem>());
			essenceText.SetText($"Machine Cores: {count}");
		}

		private void BuyBack(Augment augment)
		{
			var player = Main.LocalPlayer;
			var augmentPlayer = player.GetModPlayer<AugmentPlayer>();

			if (augmentPlayer.Owned.Count >= AugmentPlayer.MaxOwnedAugments)
			{
				Main.NewText("Plug-in Chip slots full.", 255, 80, 80);
				return;
			}

			int cost = AugmentPlayer.GetBuyBackCost(augment.Rarity);
			if (player.CountItem(ModContent.ItemType<AugmentEssenceItem>(), cost) < cost)
			{
				Main.NewText("Not enough Machine Cores.", 255, 80, 80);
				return;
			}

			if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
				AugmentNet.SendVendorBuyBackRequest(augment.Id);
			else if (augmentPlayer.BuyBackSoldAugmentByIdServerAuthoritative(augment.Id))
				Refresh();
		}

		private void SellOwned(Augment augment)
		{
			var player = Main.LocalPlayer;
			var augmentPlayer = player.GetModPlayer<AugmentPlayer>();
			if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
				AugmentNet.SendVendorSellRequest(augment.Id);
			else if (augmentPlayer.SellAugmentByIdServerAuthoritative(augment.Id))
				Refresh();
		}

		// Custom panel drawing header & column divider lines
		private class ShopBackPanel : UIPanel
		{
			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				base.DrawSelf(spriteBatch);

				CalculatedStyle dims = GetDimensions();

				// Header horizontal divider below gacha terminal
				int divY = (int)dims.Y + 164;
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)dims.X + 14, divY, (int)dims.Width - 28, 1), new Color(45, 62, 105) * 0.7f);

				// Center vertical divider between columns
				int midX = (int)dims.X + (int)(dims.Width * 0.5f);
				int listStartY = divY + 16;
				int listHeight = (int)dims.Height - 195;
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(midX, listStartY, 1, listHeight), new Color(45, 62, 105) * 0.7f);
			}
		}

		// Animated YoRHa Decryption Terminal display panel
		private class DecryptionTerminalPanel : UIPanel
		{
			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				base.DrawSelf(spriteBatch);

				CalculatedStyle d = GetDimensions();

				// Draw animated floating Sealed Chip Cache pod sprite
				if (ModContent.RequestIfExists<Texture2D>("Augments/Items/SealedChipCacheItem", out var cacheAsset))
				{
					Texture2D cacheTex = cacheAsset.Value;
					if (cacheTex != null)
					{
						float time = (float)Main.timeForVisualEffects * 0.05f;
						float bob = (float)Math.Sin(time) * 3f;
						float pulse = 0.6f + 0.4f * (float)Math.Sin(time * 1.5f);
						Vector2 podCenter = new Vector2(d.X + 38f, d.Y + d.Height * 0.5f + bob);

						// Cyan ambient glow
						Texture2D pixel = TextureAssets.MagicPixel.Value;
						int glowRadius = (int)(22f * pulse);
						Rectangle glowRect = new Rectangle((int)podCenter.X - glowRadius, (int)podCenter.Y - glowRadius, glowRadius * 2, glowRadius * 2);
						spriteBatch.Draw(pixel, glowRect, new Color(0, 180, 255) * 0.22f);

						// Draw pod at 1.75x scale
						Vector2 origin = cacheTex.Size() * 0.5f;
						spriteBatch.Draw(cacheTex, podCenter, null, Color.White, 0f, origin, 1.75f, SpriteEffects.None, 0f);
					}
				}
			}
		}

		// Styled Gacha Button
		private class GachaButton : UIPanel
		{
			public event Action Clicked;
			private readonly UIText label;
			private readonly Color idleBg;
			private readonly Color hoverBg;
			private readonly Color idleBorder;
			private readonly Color hoverBorder;

			public GachaButton(string text, Color idleBg, Color hoverBg, Color idleBorder, Color hoverBorder, float textScale = 0.82f)
			{
				this.idleBg = idleBg;
				this.hoverBg = hoverBg;
				this.idleBorder = idleBorder;
				this.hoverBorder = hoverBorder;

				SetPadding(0f);
				BackgroundColor = idleBg;
				BorderColor = idleBorder;

				label = new UIText(text, textScale)
				{
					HAlign = 0.5f,
					VAlign = 0.5f,
					TextColor = Color.White
				};
				Append(label);
			}

			public void SetText(string text) => label.SetText(text);

			public override void LeftClick(UIMouseEvent evt)
			{
				base.LeftClick(evt);
				Clicked?.Invoke();
			}

			public override void MouseOver(UIMouseEvent evt)
			{
				base.MouseOver(evt);
				BackgroundColor = hoverBg;
				BorderColor = hoverBorder;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				BackgroundColor = idleBg;
				BorderColor = idleBorder;
			}
		}

		// Modern scrollbar that only renders when the list has enough items to scroll
		private class ShopScrollbar : UIScrollbar
		{
			public ShopScrollbar()
			{
				Width.Set(10f, 0f);
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				if (!CanScroll)
					return;

				base.DrawSelf(spriteBatch);
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

		private class UndoReforgeBar : UIPanel
		{
			private readonly Action onUndo;
			private readonly UIText labelText;

			private static readonly Color IdleColor = new Color(60, 70, 110);
			private static readonly Color HoverColor = new Color(90, 105, 160);
			private static readonly Color DisabledColor = new Color(50, 50, 55);

			private bool owns;
			private bool enabled;

			public UndoReforgeBar(Action onUndo)
			{
				this.onUndo = onUndo;

				SetPadding(0f);
				BorderColor = Color.White * 0.4f;

				labelText = new UIText("", 0.75f)
				{
					HAlign = 0.5f,
					VAlign = 0.5f
				};
				Append(labelText);
			}

			public void SetState(bool owns, bool pending, Item item, int cost)
			{
				this.owns = owns;
				Width.Set(-28f, owns ? 1f : 0f);
				Height.Set(owns ? 26f : 0f, 0f);

				if (!owns)
					return;

				enabled = pending;
				BackgroundColor = pending ? IdleColor : DisabledColor;
				labelText.TextColor = pending ? Color.White : Color.White * 0.6f;
				labelText.SetText(pending
					? $"Undo Reforge: {item.Name} (+{Main.ValueToCoins(cost)})"
					: "Undo Reforge: nothing to undo");
			}

			public override void Draw(SpriteBatch spriteBatch)
			{
				if (!owns)
					return;

				base.Draw(spriteBatch);
			}

			public override void LeftClick(UIMouseEvent evt)
			{
				if (!owns)
					return;

				base.LeftClick(evt);
				if (enabled)
				{
					SoundEngine.PlaySound(SoundID.Item4);
					onUndo();
				}
			}

			public override void MouseOver(UIMouseEvent evt)
			{
				if (!owns)
					return;

				base.MouseOver(evt);
				if (enabled)
				{
					BackgroundColor = HoverColor;
					SoundEngine.PlaySound(SoundID.MenuTick);
				}
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				if (!owns)
					return;

				base.MouseOut(evt);
				BackgroundColor = enabled ? IdleColor : DisabledColor;
			}
		}
	}
}
