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
			Rectangle rect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);

			Color rarityColor = AugmentListEntry.RarityColor(augment.Rarity);
			if (augment.Rarity == AugmentRarity.Common)
				rarityColor = new Color(225, 230, 240);

			// 1. Entry Row Background & Border
			Color bgColor = isHovered ? new Color(30, 42, 76) * 0.96f : new Color(20, 28, 52) * 0.94f;
			Color borderColor = isHovered ? rarityColor * 0.9f : rarityColor * 0.45f;
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, bgColor);

			// 1px Border
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, rect.Width, 1), borderColor);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), borderColor);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, 1, rect.Height), borderColor);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), borderColor);

			// 2. Class Icon Box on the Left
			Rectangle iconBox = new Rectangle(rect.X + 8, rect.Y + 9, 36, 36);
			Color boxBg = new Color(14, 20, 38) * 0.9f;
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, iconBox, boxBg);

			// Icon box border
			Color boxBorder = isHovered ? rarityColor * 0.8f : new Color(40, 55, 95);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(iconBox.X, iconBox.Y, iconBox.Width, 1), boxBorder);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(iconBox.X, iconBox.Bottom - 1, iconBox.Width, 1), boxBorder);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(iconBox.X, iconBox.Y, 1, iconBox.Height), boxBorder);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(iconBox.Right - 1, iconBox.Y, 1, iconBox.Height), boxBorder);

			// Draw Class Icon inside the box
			Texture2D iconTex = AugmentSlotElement.GetClassIcon(augment.Class);
			if (iconTex != null)
			{
				float iconScale = Math.Min(28f / iconTex.Width, 28f / iconTex.Height);
				Vector2 iconPos = new Vector2(
					iconBox.X + (iconBox.Width - iconTex.Width * iconScale) * 0.5f,
					iconBox.Y + (iconBox.Height - iconTex.Height * iconScale) * 0.5f
				);
				spriteBatch.Draw(iconTex, iconPos, null, rarityColor, 0f, Vector2.Zero, iconScale, SpriteEffects.None, 0f);
			}

			// 3. Name & Subtitle
			var font = FontAssets.MouseText.Value;
			float textLeft = iconBox.Right + 10f;

			// Display Name
			Color nameColor = isHovered ? Color.Lerp(rarityColor, Color.White, 0.35f) : rarityColor;
			Vector2 namePos = new Vector2(textLeft, rect.Y + 9f);
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, font, augment.DisplayName, namePos, nameColor, 0f, Vector2.Zero, new Vector2(0.85f)
			);

			// Subtitle: [Tier] • Class
			string subText = $"[{augment.Rarity}] • {augment.Class}";
			Vector2 subPos = new Vector2(textLeft, rect.Y + 28f);
			Color subColor = new Color(165, 180, 205);
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, font, subText, subPos, subColor, 0f, Vector2.Zero, new Vector2(0.72f)
			);
		}

		private class ActionButton : UIPanel
		{
			public event Action Clicked;

			private readonly bool isBuy;
			private readonly bool disabled;

			private readonly Color idleBg;
			private readonly Color hoverBg;
			private readonly Color borderCol;
			private readonly Color textCol;

			public ActionButton(string label, bool isBuy, bool disabled)
			{
				this.isBuy = isBuy;
				this.disabled = disabled;

				SetPadding(0f);

				if (disabled)
				{
					idleBg = new Color(34, 38, 48);
					hoverBg = new Color(34, 38, 48);
					borderCol = new Color(70, 76, 92);
					textCol = new Color(140, 150, 170);
				}
				else if (isBuy)
				{
					idleBg = new Color(24, 52, 38);
					hoverBg = new Color(36, 78, 56);
					borderCol = new Color(60, 200, 120);
					textCol = new Color(180, 255, 205);
				}
				else
				{
					idleBg = new Color(54, 26, 26);
					hoverBg = new Color(82, 38, 38);
					borderCol = new Color(220, 90, 90);
					textCol = new Color(255, 195, 195);
				}

				BackgroundColor = idleBg;
				BorderColor = borderCol;

				UIText labelText = new UIText(label, 0.70f)
				{
					HAlign = 0.5f,
					VAlign = 0.5f,
					TextColor = textCol
				};
				Append(labelText);
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
					BackgroundColor = hoverBg;
					BorderColor = Color.Lerp(borderCol, Color.White, 0.35f);
					SoundEngine.PlaySound(SoundID.MenuTick);
				}
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				if (!disabled)
				{
					BackgroundColor = idleBg;
					BorderColor = borderCol;
				}
			}
		}
	}
}
