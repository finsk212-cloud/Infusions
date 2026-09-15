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
using Terraria.UI.Chat;

namespace Augments
{
	// The vendor's dedicated chip storage panel: "Buy Back" and "Dismantle".
	public class AugmentShopUIState : UIState
	{
		private ShopBackPanel backPanel;
		private ColoredLabel essenceLabel;
		private UndoReforgeBar undoReforgeBar;
		private UIList buyBackList;
		private UIList removeList;

		private readonly UIParticleSystem shopParticles = new UIParticleSystem(70);

		private const float PanelWidth = 800f;
		private const float PanelHeight = 520f;
		private const float ListsTop = 142f;

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (backPanel != null && backPanel.ContainsPoint(Main.MouseScreen))
			{
				Main.LocalPlayer.mouseInterface = true;
			}

			shopParticles.Update();

			if (backPanel != null && Main.rand.NextBool(8))
			{
				CalculatedStyle dims = backPanel.GetDimensions();
				if (dims.Width > 0)
				{
					Rectangle rect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);
					Color emberCol = Main.rand.NextBool(3) ? new Color(0, 220, 255) : new Color(60, 140, 240);
					shopParticles.SpawnAmbient(rect, emberCol, 1.8f);
				}
			}
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			base.Draw(spriteBatch);
			shopParticles.Draw(spriteBatch);
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

			// Navigation Tabs
			var decryptTab = new GlowTabButton("[c/00FFFF:★] [c/FFE066:Decrypt Chips] [c/00FFFF:★]", () => ModContent.GetInstance<AugmentUISystem>().ShowGacha());
			decryptTab.Left.Set(14f, 0f);
			decryptTab.Top.Set(10f, 0f);
			decryptTab.Width.Set(156f, 0f);
			decryptTab.Height.Set(26f, 0f);
			backPanel.Append(decryptTab);

			var storageTab = new TabButton("Chip Storage", true, null);
			storageTab.Left.Set(176f, 0f);
			storageTab.Top.Set(10f, 0f);
			storageTab.Width.Set(120f, 0f);
			storageTab.Height.Set(26f, 0f);
			backPanel.Append(storageTab);

			// Close Button in top-right corner
			var closeButton = new CloseButton();
			closeButton.Width.Set(24f, 0f);
			closeButton.Height.Set(24f, 0f);
			closeButton.HAlign = 1f;
			closeButton.Top.Set(10f, 0f);
			closeButton.Left.Set(-10f, 0f);
			closeButton.Clicked += () => ModContent.GetInstance<AugmentUISystem>().HideShop();
			backPanel.Append(closeButton);

			// Currency Badge pill in top right
			UIPanel essenceBadge = new UIPanel();
			essenceBadge.Width.Set(190f, 0f);
			essenceBadge.Height.Set(26f, 0f);
			essenceBadge.Left.Set(-240f, 1f);
			essenceBadge.Top.Set(10f, 0f);
			essenceBadge.SetPadding(0f);
			essenceBadge.BackgroundColor = new Color(15, 22, 42) * 0.95f;
			essenceBadge.BorderColor = new Color(80, 180, 255) * 0.7f;

			essenceLabel = new ColoredLabel("[c/FFE080:Machine Cores:] [c/00FFFF:0]", 0.82f);
			essenceBadge.Append(essenceLabel);
			backPanel.Append(essenceBadge);

			// Title Header
			UIText title = new UIText("Plug-in Chips Storage", 1.15f)
			{
				HAlign = 0.5f,
				TextColor = new Color(255, 235, 175)
			};
			title.Top.Set(44f, 0f);
			backPanel.Append(title);

			// Subtitle
			UIText subtitle = new UIText("Mistress 2B's Archive — Re-acquire archived chips or dismantle equipped chips", 0.76f)
			{
				HAlign = 0.5f,
				TextColor = new Color(155, 170, 200)
			};
			subtitle.Top.Set(68f, 0f);
			backPanel.Append(subtitle);

			// Optional Undo Reforge Bar
			undoReforgeBar = new UndoReforgeBar(TryUndoReforge);
			undoReforgeBar.Width.Set(-28f, 1f);
			undoReforgeBar.HAlign = 0.5f;
			undoReforgeBar.Top.Set(90f, 0f);
			undoReforgeBar.Height.Set(26f, 0f);

			// Column Headers
			UIText buyBackHeader = new UIText("Re-acquire (Buy Back)", 0.85f)
			{
				HAlign = 0f,
				TextColor = new Color(150, 225, 255)
			};
			buyBackHeader.Left.Set(14f, 0f);
			buyBackHeader.Top.Set(118f, 0f);
			backPanel.Append(buyBackHeader);

			UIText removeHeader = new UIText("Equipped (Dismantle)", 0.85f)
			{
				HAlign = 0f,
				TextColor = new Color(255, 185, 160)
			};
			removeHeader.Left.Set(18f, 0.5f);
			removeHeader.Top.Set(118f, 0f);
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
			RefreshUndoReforgeBar();
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
			essenceLabel?.SetText($"[c/FFE080:Machine Cores:] [c/00FFFF:{count}]");
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

		// Custom background panel that draws a subtle dividing line between columns
		private class ShopBackPanel : UIPanel
		{
			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				base.DrawSelf(spriteBatch);

				CalculatedStyle dims = GetDimensions();

				// Header horizontal divider
				int divY = (int)dims.Y + 104;
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)dims.X + 14, divY, (int)dims.Width - 28, 1), new Color(45, 62, 105) * 0.7f);

				// Center vertical divider between columns
				int midX = (int)dims.X + (int)(dims.Width * 0.5f);
				int listStartY = divY + 8;
				int listHeight = (int)dims.Height - 128;
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(midX, listStartY, 1, listHeight), new Color(45, 62, 105) * 0.7f);
			}
		}

		private class GlowTabButton : UIElement
		{
			private readonly string text;
			private readonly Action onClick;
			private bool isHovered;

			public GlowTabButton(string text, Action onClick)
			{
				this.text = text;
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

				float pulse = 0.6f + 0.4f * (float)Math.Sin(Main.timeForVisualEffects * 0.12f);
				Color borderCol = isHovered
					? new Color(0, 255, 255)
					: Color.Lerp(new Color(0, 200, 255), new Color(255, 215, 80), pulse);

				Color bgCol = isHovered ? new Color(26, 44, 82) : new Color(16, 24, 48);

				Rectangle rect = new Rectangle((int)d.X, (int)d.Y, (int)d.Width, (int)d.Height);
				spriteBatch.Draw(pixel, rect, bgCol * 0.95f);

				// Glowing border
				int th = isHovered ? 2 : 1;
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, th), borderCol);
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - th, rect.Width, th), borderCol);
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, th, rect.Height), borderCol);
				spriteBatch.Draw(pixel, new Rectangle(rect.Right - th, rect.Y, th, rect.Height), borderCol);

				if (isHovered)
				{
					spriteBatch.Draw(pixel, new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4), borderCol * 0.12f);
				}

				var font = FontAssets.MouseText.Value;
				Vector2 textSize = ChatManager.GetStringSize(font, text, new Vector2(0.80f));
				Vector2 textPos = new Vector2(d.X + (d.Width - textSize.X) * 0.5f, d.Y + (d.Height - textSize.Y) * 0.5f);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, textPos, Color.White, 0f, Vector2.Zero, new Vector2(0.80f));
			}
		}

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
