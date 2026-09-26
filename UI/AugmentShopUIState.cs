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
			backPanel.SetPadding(0f);
			backPanel.BackgroundColor = new Color(20, 28, 54);
			backPanel.BorderColor = new Color(38, 52, 98);

			// Close Button in top-right corner (ends at 782px, exact 18px buffer from right edge)
			var closeButton = new CloseButton();
			closeButton.Width.Set(24f, 0f);
			closeButton.Height.Set(24f, 0f);
			closeButton.Top.Set(10f, 0f);
			closeButton.Left.Set(758f, 0f);
			closeButton.Clicked += () => ModContent.GetInstance<AugmentUISystem>().HideShop();
			backPanel.Append(closeButton);

			// Currency Badge pill in top right (556f to 746f, 12px gap before close button)
			var essenceBadge = new EssenceBadge();
			essenceBadge.Width.Set(190f, 0f);
			essenceBadge.Height.Set(26f, 0f);
			essenceBadge.Left.Set(556f, 0f);
			essenceBadge.Top.Set(9f, 0f);

			essenceLabel = new ColoredLabel("[c/D4B872:Machine Cores:] [c/68C2D8:0]", 0.82f);
			essenceBadge.Append(essenceLabel);
			backPanel.Append(essenceBadge);

			// Title Header
			UIText title = new UIText("Plugin Storage", 1.15f)
			{
				HAlign = 0.5f,
				TextColor = new Color(255, 235, 175)
			};
			title.Top.Set(42f, 0f);
			backPanel.Append(title);

			// Subtitle
			UIText subtitle = new UIText("Mistress 2B's Archive — Re-acquire archived plugins or dismantle equipped plugins", 0.76f)
			{
				HAlign = 0.5f,
				TextColor = new Color(155, 170, 200)
			};
			subtitle.Top.Set(66f, 0f);
			backPanel.Append(subtitle);

			// Optional Undo Reforge Bar (spans 18f to 782f)
			undoReforgeBar = new UndoReforgeBar(TryUndoReforge);
			undoReforgeBar.Left.Set(18f, 0f);
			undoReforgeBar.Width.Set(764f, 0f);
			undoReforgeBar.Top.Set(88f, 0f);
			undoReforgeBar.Height.Set(26f, 0f);

			// Column Headers (symmetrical at Left = 18f and Left = 410f)
			UIText buyBackHeader = new UIText("Re-acquire (Buy Back)", 0.85f)
			{
				HAlign = 0f,
				TextColor = new Color(150, 225, 255)
			};
			buyBackHeader.Left.Set(18f, 0f);
			buyBackHeader.Top.Set(122f, 0f);
			backPanel.Append(buyBackHeader);

			UIText removeHeader = new UIText("Equipped (Dismantle)", 0.85f)
			{
				HAlign = 0f,
				TextColor = new Color(255, 185, 160)
			};
			removeHeader.Left.Set(410f, 0f);
			removeHeader.Top.Set(122f, 0f);
			backPanel.Append(removeHeader);

			// Left List: Buy Back (Left = 18f, Width = 356f, Scrollbar = 378f)
			buyBackList = new UIList();
			buyBackList.ManualSortMethod = _ => { };
			buyBackList.Top.Set(ListsTop, 0f);
			buyBackList.Left.Set(18f, 0f);
			buyBackList.Width.Set(356f, 0f);
			buyBackList.Height.Set(-(ListsTop + 14f), 1f);
			buyBackList.ListPadding = 6f;
			backPanel.Append(buyBackList);

			ShopScrollbar buyBackScrollbar = new ShopScrollbar();
			buyBackScrollbar.Top.Set(ListsTop, 0f);
			buyBackScrollbar.Height.Set(-(ListsTop + 14f), 1f);
			buyBackScrollbar.Left.Set(378f, 0f);
			buyBackScrollbar.Width.Set(8f, 0f);
			buyBackList.SetScrollbar(buyBackScrollbar);
			backPanel.Append(buyBackScrollbar);

			// Right List: Remove (Left = 410f, Width = 356f, Scrollbar = 770f)
			removeList = new UIList();
			removeList.ManualSortMethod = _ => { };
			removeList.Top.Set(ListsTop, 0f);
			removeList.Left.Set(410f, 0f);
			removeList.Width.Set(356f, 0f);
			removeList.Height.Set(-(ListsTop + 14f), 1f);
			removeList.ListPadding = 6f;
			backPanel.Append(removeList);

			ShopScrollbar removeScrollbar = new ShopScrollbar();
			removeScrollbar.Top.Set(ListsTop, 0f);
			removeScrollbar.Height.Set(-(ListsTop + 14f), 1f);
			removeScrollbar.Left.Set(770f, 0f);
			removeScrollbar.Width.Set(8f, 0f);
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
				var empty = new UIText("No archived plugins.\nPlugins you sell will appear here.", 0.8f)
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
				var empty = new UIText("No plugins currently equipped.", 0.8f)
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
			essenceLabel?.SetText($"[c/D4B872:Machine Cores:] [c/68C2D8:{count:N0}]");
		}

		private void BuyBack(Augment augment)
		{
			var player = Main.LocalPlayer;
			var augmentPlayer = player.GetModPlayer<AugmentPlayer>();

			if (augmentPlayer.Owned.Count >= AugmentPlayer.MaxOwnedAugments)
			{
				Main.NewText("Plugin slots full.", 255, 80, 80);
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

		private class ShopBackPanel : UIPanel
		{
			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				base.DrawSelf(spriteBatch);

				CalculatedStyle dims = GetDimensions();

				// Header horizontal divider (spans X = 18f to X = 782f)
				int divY = (int)dims.Y + 118;
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)dims.X + 18, divY, (int)dims.Width - 36, 1), new Color(45, 62, 105) * 0.7f);

				// Center vertical divider between columns
				int midX = (int)dims.X + (int)(dims.Width * 0.5f);
				int listStartY = divY + 6;
				int listHeight = (int)dims.Height - 138;
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
					? new Color(88, 164, 208)
					: Color.Lerp(new Color(46, 72, 108), new Color(212, 184, 114), pulse * 0.5f);

				Color bgCol = isHovered ? new Color(22, 32, 54) : new Color(14, 18, 34);

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
				Width.Set(8f, 0f);
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				if (!CanScroll)
					return;

				CalculatedStyle dims = GetDimensions();
				Rectangle trackRect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);

				// Dark sleek cybernetic track backing
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, trackRect, new Color(10, 16, 32) * 0.92f);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(trackRect.X, trackRect.Y, 1, trackRect.Height), new Color(34, 48, 86) * 0.6f);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(trackRect.Right - 1, trackRect.Y, 1, trackRect.Height), new Color(34, 48, 86) * 0.6f);

				base.DrawSelf(spriteBatch);
			}
		}

		private class EssenceBadge : UIElement
		{
			public EssenceBadge()
			{
				SetPadding(0f);
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				CalculatedStyle dims = GetDimensions();
				var rect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);
				Texture2D pixel = TextureAssets.MagicPixel.Value;

				// Ambient subtle cyan underglow
				spriteBatch.Draw(pixel, new Rectangle(rect.X - 1, rect.Y - 1, rect.Width + 2, rect.Height + 2), new Color(56, 189, 248) * 0.06f);

				// Chassis Fill (#0A101C)
				spriteBatch.Draw(pixel, rect, new Color(10, 16, 28, 248));

				// 1px Inner Hairline
				Color innerHairline = Color.White * 0.05f;
				spriteBatch.Draw(pixel, new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, 1), innerHairline);
				spriteBatch.Draw(pixel, new Rectangle(rect.X + 1, rect.Bottom - 2, rect.Width - 2, 1), innerHairline);
				spriteBatch.Draw(pixel, new Rectangle(rect.X + 1, rect.Y + 1, 1, rect.Height - 2), innerHairline);
				spriteBatch.Draw(pixel, new Rectangle(rect.Right - 2, rect.Y + 1, 1, rect.Height - 2), innerHairline);

				// 1px Outer Border
				Color border = new Color(56, 189, 248) * 0.65f;
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), border);
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), border);
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), border);
				spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), border);

				// 2px Corner Accent Ticks
				Color cornerTick = new Color(56, 189, 248) * 0.85f;
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 2, 2), cornerTick);
				spriteBatch.Draw(pixel, new Rectangle(rect.Right - 2, rect.Y, 2, 2), cornerTick);
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 2, 2, 2), cornerTick);
				spriteBatch.Draw(pixel, new Rectangle(rect.Right - 2, rect.Bottom - 2, 2, 2), cornerTick);
			}
		}

		private class CloseButton : UIElement
		{
			public event Action Clicked;
			private bool isHovered;

			public override void LeftClick(UIMouseEvent evt)
			{
				base.LeftClick(evt);
				SoundEngine.PlaySound(SoundID.MenuClose);
				Clicked?.Invoke();
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
				CalculatedStyle dims = GetDimensions();
				var rect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);
				Texture2D pixel = TextureAssets.MagicPixel.Value;

				Color bg = isHovered ? new Color(76, 24, 30) : new Color(36, 14, 18);
				Color border = isHovered ? new Color(248, 113, 113) : new Color(140, 45, 55);

				if (isHovered)
				{
					spriteBatch.Draw(pixel, new Rectangle(rect.X - 1, rect.Y - 1, rect.Width + 2, rect.Height + 2), border * 0.15f);
				}

				spriteBatch.Draw(pixel, rect, bg);

				Color innerHairline = Color.White * (isHovered ? 0.10f : 0.04f);
				spriteBatch.Draw(pixel, new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, 1), innerHairline);
				spriteBatch.Draw(pixel, new Rectangle(rect.X + 1, rect.Bottom - 2, rect.Width - 2, 1), innerHairline);
				spriteBatch.Draw(pixel, new Rectangle(rect.X + 1, rect.Y + 1, 1, rect.Height - 2), innerHairline);
				spriteBatch.Draw(pixel, new Rectangle(rect.Right - 2, rect.Y + 1, 1, rect.Height - 2), innerHairline);

				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), border);
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), border);
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), border);
				spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), border);

				Color cornerTick = border * (isHovered ? 1.0f : 0.70f);
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 2, 2), cornerTick);
				spriteBatch.Draw(pixel, new Rectangle(rect.Right - 2, rect.Y, 2, 2), cornerTick);
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 2, 2, 2), cornerTick);
				spriteBatch.Draw(pixel, new Rectangle(rect.Right - 2, rect.Bottom - 2, 2, 2), cornerTick);

				var font = FontAssets.MouseText.Value;
				Vector2 xSize = ChatManager.GetStringSize(font, "✕", new Vector2(0.80f));
				Vector2 xPos = new Vector2(
					rect.X + (rect.Width - xSize.X) * 0.5f,
					rect.Y + (rect.Height - xSize.Y) * 0.5f + 1f
				);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, "✕", xPos, isHovered ? Color.White : new Color(254, 202, 202), 0f, Vector2.Zero, new Vector2(0.80f));
			}
		}

		private class UndoReforgeBar : UIElement
		{
			private readonly Action onUndo;
			private string text = "";
			private bool isHovered;

			private bool owns;
			private bool enabled;

			public UndoReforgeBar(Action onUndo)
			{
				this.onUndo = onUndo;
			}

			public void SetState(bool owns, bool pending, Item item, int cost)
			{
				this.owns = owns;
				Width.Set(-28f, owns ? 1f : 0f);
				Height.Set(owns ? 26f : 0f, 0f);

				if (!owns)
					return;

				enabled = pending;
				text = pending
					? $"Undo Reforge: {item.Name} (+{Main.ValueToCoins(cost)})"
					: "Undo Reforge: nothing to undo";
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
				if (owns && enabled)
				{
					isHovered = true;
					SoundEngine.PlaySound(SoundID.MenuTick);
				}
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				isHovered = false;
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				if (!owns)
					return;

				CalculatedStyle dims = GetDimensions();
				var rect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);
				Texture2D pixel = TextureAssets.MagicPixel.Value;

				Color bg;
				Color border;
				Color textColor;

				if (!enabled)
				{
					bg = new Color(10, 16, 28) * 0.95f;
					border = new Color(30, 42, 60);
					textColor = new Color(120, 130, 150);
				}
				else
				{
					bg = isHovered ? new Color(18, 32, 56) * 0.98f : new Color(12, 22, 40) * 0.94f;
					border = isHovered ? new Color(56, 189, 248) : new Color(30, 58, 92);
					textColor = isHovered ? Color.White : new Color(220, 235, 250);
				}

				if (isHovered && enabled)
				{
					spriteBatch.Draw(pixel, new Rectangle(rect.X - 1, rect.Y - 1, rect.Width + 2, rect.Height + 2), border * 0.12f);
				}

				spriteBatch.Draw(pixel, rect, bg);

				Color innerHairline = Color.White * (isHovered ? 0.08f : 0.04f);
				spriteBatch.Draw(pixel, new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, 1), innerHairline);
				spriteBatch.Draw(pixel, new Rectangle(rect.X + 1, rect.Bottom - 2, rect.Width - 2, 1), innerHairline);
				spriteBatch.Draw(pixel, new Rectangle(rect.X + 1, rect.Y + 1, 1, rect.Height - 2), innerHairline);
				spriteBatch.Draw(pixel, new Rectangle(rect.Right - 2, rect.Y + 1, 1, rect.Height - 2), innerHairline);

				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), border);
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), border);
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), border);
				spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), border);

				Color cornerTick = border * (isHovered ? 1.0f : 0.70f);
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 2, 2), cornerTick);
				spriteBatch.Draw(pixel, new Rectangle(rect.Right - 2, rect.Y, 2, 2), cornerTick);
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 2, 2, 2), cornerTick);
				spriteBatch.Draw(pixel, new Rectangle(rect.Right - 2, rect.Bottom - 2, 2, 2), cornerTick);

				var font = FontAssets.MouseText.Value;
				Vector2 textSize = ChatManager.GetStringSize(font, text, new Vector2(0.75f));
				Vector2 textPos = new Vector2(
					rect.X + (rect.Width - textSize.X) * 0.5f,
					rect.Y + (rect.Height - textSize.Y) * 0.5f + 1f
				);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, textPos, textColor, 0f, Vector2.Zero, new Vector2(0.75f));
			}
		}
	}
}
