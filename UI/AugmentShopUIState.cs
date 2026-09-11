using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace Augments
{
	// The vendor's shop panel: two side-by-side lists, "Buy" (ever-owned
	// augments not currently held, re-acquirable for Essence) and "Remove"
	// (currently-held augments, free or Essence-refunding depending on
	// rarity). See GetPricing for the actual rarity-based price table.
	public class AugmentShopUIState : UIState
	{
		private UIPanel backPanel;
		private UIText essenceText;
		private UndoReforgeBar undoReforgeBar;
		private UIList buyBackList;
		private UIList removeList;

		private const float PanelWidth = 760f;
		private const float PanelHeight = 470f;
		private const float ListsTop = 120f;

		public override void OnInitialize()
		{
			backPanel = new UIPanel();
			backPanel.Width.Set(PanelWidth, 0f);
			backPanel.Height.Set(PanelHeight, 0f);
			backPanel.HAlign = 0.5f;
			backPanel.VAlign = 0.5f;
			backPanel.BackgroundColor = new Color(33, 43, 79) * 0.9f;

			UIText title = new UIText("Vendor Wares")
			{
				HAlign = 0.5f
			};
			title.Top.Set(10f, 0f);
			backPanel.Append(title);

			var closeButton = new CloseButton();
			closeButton.Width.Set(24f, 0f);
			closeButton.Height.Set(24f, 0f);
			closeButton.HAlign = 1f;
			closeButton.Top.Set(8f, 0f);
			closeButton.Left.Set(-8f, 0f);
			closeButton.Clicked += () => ModContent.GetInstance<AugmentUISystem>().HideShop();
			backPanel.Append(closeButton);

			essenceText = new UIText("");
			essenceText.HAlign = 0.5f;
			essenceText.Top.Set(34f, 0f);
			backPanel.Append(essenceText);

			// Only shown/enabled while Reforger's Patience is owned AND a
			// valid pending undo exists - hidden entirely otherwise so the
			// panel looks unchanged for players without the augment.
			undoReforgeBar = new UndoReforgeBar(TryUndoReforge);
			undoReforgeBar.Width.Set(-20f, 1f);
			undoReforgeBar.HAlign = 0.5f;
			undoReforgeBar.Top.Set(58f, 0f);
			undoReforgeBar.Height.Set(30f, 0f);
			backPanel.Append(undoReforgeBar);

			UIText buyBackHeader = new UIText("Buy")
			{
				HAlign = 0f
			};
			buyBackHeader.Left.Set(10f, 0f);
			buyBackHeader.Top.Set(92f, 0f);
			backPanel.Append(buyBackHeader);

			UIText removeHeader = new UIText("Remove")
			{
				HAlign = 0f
			};
			removeHeader.Left.Set(15f, 0.5f);
			removeHeader.Top.Set(92f, 0f);
			backPanel.Append(removeHeader);

			buyBackList = new UIList();
			buyBackList.ManualSortMethod = _ => { };
			buyBackList.Top.Set(ListsTop, 0f);
			buyBackList.Left.Set(10f, 0f);
			buyBackList.Width.Set(-45f, 0.5f);
			buyBackList.Height.Set(-(ListsTop + 10f), 1f);
			buyBackList.ListPadding = 6f;
			backPanel.Append(buyBackList);

			UIScrollbar buyBackScrollbar = new UIScrollbar();
			buyBackScrollbar.Top.Set(ListsTop, 0f);
			buyBackScrollbar.Height.Set(-(ListsTop + 10f), 1f);
			buyBackScrollbar.Left.Set(-25f, 0.5f);
			buyBackList.SetScrollbar(buyBackScrollbar);
			backPanel.Append(buyBackScrollbar);

			removeList = new UIList();
			removeList.ManualSortMethod = _ => { };
			removeList.Top.Set(ListsTop, 0f);
			removeList.Left.Set(15f, 0.5f);
			removeList.Width.Set(-45f, 0.5f);
			removeList.Height.Set(-(ListsTop + 10f), 1f);
			removeList.ListPadding = 6f;
			backPanel.Append(removeList);

			UIScrollbar removeScrollbar = new UIScrollbar();
			removeScrollbar.Top.Set(ListsTop, 0f);
			removeScrollbar.Height.Set(-(ListsTop + 10f), 1f);
			removeScrollbar.Left.Set(-10f, 1f);
			removeList.SetScrollbar(removeScrollbar);
			backPanel.Append(removeScrollbar);

			Append(backPanel);
		}

		// Rebuilds both lists and the Essence balance readout from the local
		// player's current state. Call explicitly right before showing the
		// panel, same as AugmentListUIState.Refresh.
		public void Refresh()
		{
			buyBackList.Clear();
			removeList.Clear();

			var player = Main.LocalPlayer;
			var augmentPlayer = player.GetModPlayer<AugmentPlayer>();

			foreach (var id in augmentPlayer.SoldAugmentIds)
			{
				var augment = AugmentDatabase.GetById(id);
				if (augment == null)
					continue;

				// Support class temporarily unobtainable (see RollChoices) - a
				// save from before this change could have one sitting in
				// SoldAugmentIds; don't let buy-back reintroduce it.
				if (augment.Class == AugmentClass.Support)
					continue;

				int buyBackCost = AugmentPlayer.GetBuyBackCost(augment.Rarity);
				var entry = new AugmentShopEntry(augment, $"Buy ({buyBackCost} Essence)", BuyBack);
				entry.Width.Set(0f, 1f);
				entry.Height.Set(50f, 0f);
				buyBackList.Add(entry);
			}

			foreach (var augment in augmentPlayer.Owned)
			{
				AugmentShopEntry entry;
				if (augment.IsPermanent)
				{
					entry = new AugmentShopEntry(augment, "(Permanent)", null);
				}
				else
				{
					int removeRefund = AugmentPlayer.GetRemoveRefund(augment.Rarity);
					string label = removeRefund > 0 ? $"Remove (+{removeRefund} Essence)" : "Remove (Free)";
					entry = new AugmentShopEntry(augment, label, SellOwned);
				}
				entry.Width.Set(0f, 1f);
				entry.Height.Set(50f, 0f);
				removeList.Add(entry);
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
			essenceText.SetText($"Augment Essence: {count}");
		}

		private void BuyBack(Augment augment)
		{
			var player = Main.LocalPlayer;
			var augmentPlayer = player.GetModPlayer<AugmentPlayer>();

			if (augmentPlayer.Owned.Count >= AugmentPlayer.MaxOwnedAugments)
			{
				Main.NewText("Augment cap reached.", 255, 80, 80);
				return;
			}

			int cost = AugmentPlayer.GetBuyBackCost(augment.Rarity);
			if (player.CountItem(ModContent.ItemType<AugmentEssenceItem>(), cost) < cost)
			{
				Main.NewText("Not enough Augment Essence.", 255, 80, 80);
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

		// Small "X" button in the panel's top-right corner. Same manual
		// hover/click approach as AugmentShopEntry's ActionButton, to match
		// this mod's existing UI button style.
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
				Clicked?.Invoke();
			}

			public override void MouseOver(UIMouseEvent evt)
			{
				base.MouseOver(evt);
				BackgroundColor = HoverColor;
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				BackgroundColor = IdleColor;
			}
		}

		// "Undo Reforge" row for Reforger's Patience. Entirely hidden for
		// players who don't own the augment (matches how permanent-augment
		// rows only appear in the Remove list for owners), and shown but
		// disabled with an explanatory label when the augment is owned but
		// there's nothing valid to undo (no reforge yet this session, or the
		// item was sold/dropped/reforged again since).
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

			// owns = player owns Reforger's Patience at all (false hides the
			// row entirely). pending/item/cost describe an actual undoable
			// reforge - when pending is false the row shows but is disabled.
			public void SetState(bool owns, bool pending, Item item, int cost)
			{
				Width.Set(0f, owns ? 1f : 0f);
				Height.Set(owns ? 30f : 0f, 0f);

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
					onUndo();
			}

			public override void MouseOver(UIMouseEvent evt)
			{
				base.MouseOver(evt);
				if (enabled)
					BackgroundColor = HoverColor;
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				BackgroundColor = enabled ? IdleColor : DisabledColor;
			}
		}
	}
}
