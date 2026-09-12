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
	// The vendor's shop panel: two side-by-side lists, "Buy Back" (ever-owned
	// chips not currently held, re-acquirable for Essence) and "Remove / Sell"
	// (currently-equipped chips, free or Essence-refunding depending on rarity).
	public class AugmentShopUIState : UIState
	{
		private ShopBackPanel backPanel;
		private UIText essenceText;
		private UndoReforgeBar undoReforgeBar;
		private UIList buyBackList;
		private UIList removeList;

		private const float PanelWidth = 800f;
		private const float PanelHeight = 520f;
		private const float ListsTop = 136f;

		public override void OnInitialize()
		{
			backPanel = new ShopBackPanel();
			backPanel.Width.Set(PanelWidth, 0f);
			backPanel.Height.Set(PanelHeight, 0f);
			backPanel.HAlign = 0.5f;
			backPanel.VAlign = 0.5f;
			backPanel.BackgroundColor = new Color(22, 30, 58) * 0.98f;
			backPanel.BorderColor = new Color(38, 52, 98);

			// Title Header (no unicode stars)
			UIText title = new UIText("Plug-in Chips Storage", 1.15f)
			{
				HAlign = 0.5f,
				TextColor = new Color(255, 235, 175)
			};
			title.Top.Set(12f, 0f);
			backPanel.Append(title);

			// Subtitle
			UIText subtitle = new UIText("Mistress 2B's Archive — Re-acquire archived chips or dismantle equipped chips", 0.76f)
			{
				HAlign = 0.5f,
				TextColor = new Color(155, 170, 200)
			};
			subtitle.Top.Set(36f, 0f);
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

			// Currency Badge pill in the center
			UIPanel essenceBadge = new UIPanel();
			essenceBadge.Width.Set(260f, 0f);
			essenceBadge.Height.Set(26f, 0f);
			essenceBadge.HAlign = 0.5f;
			essenceBadge.Top.Set(58f, 0f);
			essenceBadge.SetPadding(0f);
			essenceBadge.BackgroundColor = new Color(15, 22, 42) * 0.95f;
			essenceBadge.BorderColor = new Color(80, 180, 255) * 0.7f;

			essenceText = new UIText("Plug-in Essence: 0", 0.82f)
			{
				HAlign = 0.5f,
				VAlign = 0.5f,
				TextColor = new Color(100, 225, 255)
			};
			essenceBadge.Append(essenceText);
			backPanel.Append(essenceBadge);

			// Optional Undo Reforge Bar
			undoReforgeBar = new UndoReforgeBar(TryUndoReforge);
			undoReforgeBar.Width.Set(-24f, 1f);
			undoReforgeBar.HAlign = 0.5f;
			undoReforgeBar.Top.Set(88f, 0f);
			undoReforgeBar.Height.Set(26f, 0f);
			backPanel.Append(undoReforgeBar);

			// Column Headers
			UIText buyBackHeader = new UIText("📥  Re-acquire (Buy Back)", 0.85f)
			{
				HAlign = 0f,
				TextColor = new Color(150, 225, 255)
			};
			buyBackHeader.Left.Set(14f, 0f);
			buyBackHeader.Top.Set(108f, 0f);
			backPanel.Append(buyBackHeader);

			UIText removeHeader = new UIText("📤  Equipped (Dismantle)", 0.85f)
			{
				HAlign = 0f,
				TextColor = new Color(255, 185, 160)
			};
			removeHeader.Left.Set(18f, 0.5f);
			removeHeader.Top.Set(108f, 0f);
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
				var entry = new AugmentShopEntry(augment, $"Buy ({buyBackCost} Essence)", BuyBack);
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
					string label = removeRefund > 0 ? $"Remove (+{removeRefund} Essence)" : "Remove (Free)";
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
			RefreshUndoReforgeBar();
		}

		private void RefreshUndoReforgeBar()
		{
			var augmentPlayer = Main.LocalPlayer.GetModPlayer<AugmentPlayer>();
			bool owns = augmentPlayer.HasAugment("reforgers_patience");
			bool pending = owns && augmentPlayer.HasPendingReforgeUndo;

			undoReforgeBar.SetState(owns, pending, pending ? augmentPlayer.LastReforgedItem : null, pending ? augmentPlayer.LastReforgeCost : 0);
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
			essenceText.SetText($"Plug-in Essence: {count}");
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
				Main.NewText("Not enough Plug-in Essence.", 255, 80, 80);
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

				// Header horizontal divider
				int divY = (int)dims.Y + 98;
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)dims.X + 14, divY, (int)dims.Width - 28, 1), new Color(45, 62, 105) * 0.7f);

				// Center vertical divider between columns
				int midX = (int)dims.X + (int)(dims.Width * 0.5f);
				int listStartY = divY + 8;
				int listHeight = (int)dims.Height - 120;
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(midX, listStartY, 1, listHeight), new Color(45, 62, 105) * 0.7f);
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
				Width.Set(0f, owns ? 1f : 0f);
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

			public override void LeftClick(UIMouseEvent evt)
			{
				base.LeftClick(evt);
				if (enabled)
				{
					SoundEngine.PlaySound(SoundID.Item4);
					onUndo();
				}
			}

			public override void MouseOver(UIMouseEvent evt)
			{
				base.MouseOver(evt);
				if (enabled)
				{
					BackgroundColor = HoverColor;
					SoundEngine.PlaySound(SoundID.MenuTick);
				}
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				BackgroundColor = enabled ? IdleColor : DisabledColor;
			}
		}
	}
}
