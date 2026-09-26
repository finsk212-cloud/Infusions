using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Augments
{
	// A beautifully styled row in the shop's "Buy Back" or "Remove" list.
	// Features class icons, rarity badges, smooth hover lighting, and themed action buttons.
	public class AugmentShopEntry : UIPanel
	{
		private readonly Augment augment;
		private readonly string actionLabel;
		private readonly Action<Augment> onAction;
		private readonly bool isBuyAction;
		private readonly bool isPermanent;
		private bool isHovered;

		private readonly ActionButton actionButton;

		public AugmentShopEntry(Augment augment, string actionLabel, Action<Augment> onAction)
		{
			this.augment = augment;
			this.actionLabel = actionLabel;
			this.onAction = onAction;
			this.isBuyAction = actionLabel.StartsWith("Buy", StringComparison.OrdinalIgnoreCase);
			this.isPermanent = onAction == null;

			SetPadding(0f);
			Left.Set(2f, 0f);
			Width.Set(-4f, 1f);
			Height.Set(54f, 0f);

			actionButton = new ActionButton(actionLabel, isBuyAction, isPermanent);
			actionButton.Width.Set(116f, 0f);
			actionButton.Height.Set(26f, 0f);
			actionButton.HAlign = 1f;
			actionButton.VAlign = 0.5f;
			actionButton.Left.Set(-8f, 0f);
			if (!isPermanent && onAction != null)
			{
				actionButton.Clicked += () => onAction(augment);
			}
			Append(actionButton);
		}

		public override void MouseOver(UIMouseEvent evt)
		{
			base.MouseOver(evt);
			isHovered = true;
			AugmentListEntry.HoveredAugment = augment;
			SoundEngine.PlaySound(SoundID.MenuTick);
		}

		public override void MouseOut(UIMouseEvent evt)
		{
			base.MouseOut(evt);
			isHovered = false;
			if (AugmentListEntry.HoveredAugment == augment)
				AugmentListEntry.HoveredAugment = null;
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			CalculatedStyle dims = GetDimensions();
			int rx = (int)dims.X;
			int ry = (int)dims.Y;
			int rw = (int)dims.Width;
			int rh = (int)dims.Height;

			Rectangle rect = new Rectangle(rx, ry, rw, rh);
			Texture2D pixel = TextureAssets.MagicPixel.Value;

			Color rarityColor = AugmentListEntry.RarityColor(augment.Rarity);
			if (augment.Rarity == AugmentRarity.Common)
				rarityColor = new Color(225, 230, 240);

			// 1. Ambient underglow on hover
			if (isHovered)
			{
				spriteBatch.Draw(pixel, new Rectangle(rx - 1, ry - 1, rw + 2, rh + 2), rarityColor * 0.05f);
			}

			// 2. Cybernetic Chassis Fill (#0A101C)
			Color bgColor = isHovered ? new Color(16, 24, 42) * 0.96f : new Color(10, 16, 28) * 0.94f;
			spriteBatch.Draw(pixel, rect, bgColor);

			// 3. 1px Inner Hairline Highlight Accent
			Color innerHairline = Color.White * (isHovered ? 0.08f : 0.04f);
			spriteBatch.Draw(pixel, new Rectangle(rx + 1, ry + 1, rw - 2, 1), innerHairline);
			spriteBatch.Draw(pixel, new Rectangle(rx + 1, ry + rh - 2, rw - 2, 1), innerHairline);
			spriteBatch.Draw(pixel, new Rectangle(rx + 1, ry + 1, 1, rh - 2), innerHairline);
			spriteBatch.Draw(pixel, new Rectangle(rx + rw - 2, ry + 1, 1, rh - 2), innerHairline);

			// 4. 1px Outer Border (clean, no corner ticks)
			Color borderColor = isHovered ? rarityColor * 0.75f : Color.Lerp(new Color(30, 41, 59), rarityColor, 0.30f) * 0.70f;
			spriteBatch.Draw(pixel, new Rectangle(rx, ry, rw, 1), borderColor);
			spriteBatch.Draw(pixel, new Rectangle(rx, ry + rh - 1, rw, 1), borderColor);
			spriteBatch.Draw(pixel, new Rectangle(rx, ry, 1, rh), borderColor);
			spriteBatch.Draw(pixel, new Rectangle(rx + rw - 1, ry, 1, rh), borderColor);

			// 5. Unboxed Class Icon (floated directly, perfectly centered vertically)
			Texture2D iconTex = AugmentSlotElement.GetClassIcon(augment.Class);
			if (iconTex != null)
			{
				float iconScale = Math.Min(24f / iconTex.Width, 24f / iconTex.Height);
				Vector2 iconPos = new Vector2(
					rect.X + 14f,
					rect.Y + (rect.Height - iconTex.Height * iconScale) * 0.5f
				);
				spriteBatch.Draw(iconTex, iconPos, null, rarityColor, 0f, Vector2.Zero, iconScale, SpriteEffects.None, 0f);
			}

			// 6. Name & Subtitle
			var font = FontAssets.MouseText.Value;
			float textLeft = rect.X + 46f;

			// Display Name
			Color nameColor = isHovered ? Color.Lerp(rarityColor, Color.White, 0.35f) : rarityColor;
			Vector2 namePos = new Vector2(textLeft, rect.Y + 9f);
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, font, augment.DisplayName, namePos, nameColor, 0f, Vector2.Zero, new Vector2(0.85f)
			);

			// Subtitle: Unboxed RARITY • CLASS
			string rarName = AugmentTooltipDrawer.GetRarityDisplayName(augment.Rarity);
			string clsName = AugmentTooltipDrawer.GetClassDisplayName(augment.Class);
			Vector2 subPos = new Vector2(textLeft, rect.Y + 28f);
			Vector2 subScale = new Vector2(0.72f);

			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, rarName, subPos, rarityColor * 0.85f, 0f, Vector2.Zero, subScale);
			float curSubX = subPos.X + ChatManager.GetStringSize(font, rarName, subScale).X;

			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, "  •  ", new Vector2(curSubX, subPos.Y), new Color(100, 116, 139), 0f, Vector2.Zero, subScale);
			curSubX += ChatManager.GetStringSize(font, "  •  ", subScale).X;

			Color clsCol = AugmentTooltipDrawer.GetClassColor(augment.Class);
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, clsName, new Vector2(curSubX, subPos.Y), clsCol * 0.85f, 0f, Vector2.Zero, subScale);
		}

		private class ActionButton : UIElement
		{
			public event Action Clicked;

			private readonly string label;
			private readonly bool isBuy;
			private readonly bool disabled;
			private bool isHovered;

			public ActionButton(string label, bool isBuy, bool disabled)
			{
				this.label = label;
				this.isBuy = isBuy;
				this.disabled = disabled;
			}

			public override void LeftClick(UIMouseEvent evt)
			{
				base.LeftClick(evt);
				if (!disabled)
				{
					SoundEngine.PlaySound(isBuy ? SoundID.Item4 : SoundID.MenuClose);
					Clicked?.Invoke();
				}
			}

			public override void MouseOver(UIMouseEvent evt)
			{
				base.MouseOver(evt);
				if (!disabled)
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
				CalculatedStyle dims = GetDimensions();
				var rect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);
				Texture2D pixel = TextureAssets.MagicPixel.Value;

				Color bg;
				Color border;
				Color textColor;

				if (disabled)
				{
					bg = new Color(10, 16, 28) * 0.95f;
					border = new Color(30, 42, 60);
					textColor = new Color(120, 130, 150);
				}
				else if (isBuy)
				{
					bg = isHovered ? new Color(18, 48, 32) * 0.98f : new Color(12, 32, 22) * 0.94f;
					border = isHovered ? new Color(74, 222, 128) : new Color(34, 197, 94) * 0.80f;
					textColor = isHovered ? Color.White : new Color(220, 252, 231);
				}
				else
				{
					// Dismantle / Refund
					bg = isHovered ? new Color(48, 20, 24) * 0.98f : new Color(32, 14, 18) * 0.94f;
					border = isHovered ? new Color(248, 113, 113) : new Color(220, 70, 70) * 0.80f;
					textColor = isHovered ? Color.White : new Color(254, 226, 226);
				}

				// Ambient hover underglow
				if (isHovered && !disabled)
				{
					spriteBatch.Draw(pixel, new Rectangle(rect.X - 1, rect.Y - 1, rect.Width + 2, rect.Height + 2), border * 0.12f);
				}

				// Chassis Fill
				spriteBatch.Draw(pixel, rect, bg);

				// 1px Inner Hairline Highlight Accent
				Color innerHairline = Color.White * (isHovered ? 0.08f : 0.04f);
				spriteBatch.Draw(pixel, new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, 1), innerHairline);
				spriteBatch.Draw(pixel, new Rectangle(rect.X + 1, rect.Bottom - 2, rect.Width - 2, 1), innerHairline);
				spriteBatch.Draw(pixel, new Rectangle(rect.X + 1, rect.Y + 1, 1, rect.Height - 2), innerHairline);
				spriteBatch.Draw(pixel, new Rectangle(rect.Right - 2, rect.Y + 1, 1, rect.Height - 2), innerHairline);

				// 1px Outer Border (clean, no corner ticks)
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), border);
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), border);
				spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), border);
				spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), border);

				// Text label
				var font = FontAssets.MouseText.Value;
				Vector2 textSize = ChatManager.GetStringSize(font, label, new Vector2(0.70f));
				Vector2 textPos = new Vector2(
					rect.X + (rect.Width - textSize.X) * 0.5f,
					rect.Y + (rect.Height - textSize.Y) * 0.5f + 1f
				);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, label, textPos, textColor, 0f, Vector2.Zero, new Vector2(0.70f));
			}
		}
	}
}
