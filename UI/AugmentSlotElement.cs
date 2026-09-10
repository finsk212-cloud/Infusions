using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Augments
{
	public class AugmentSlotElement : UIElement
	{
		public readonly Augment Augment;
		public bool IsSelected { get; set; }
		public bool IsOwned { get; set; }

		public event Action<Augment> Clicked;

		private bool isHovered;

		public AugmentSlotElement(Augment augment)
		{
			Augment = augment;
			Width.Set(64f, 0f);
			Height.Set(64f, 0f);
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

		public override void LeftClick(UIMouseEvent evt)
		{
			base.LeftClick(evt);
			SoundEngine.PlaySound(SoundID.MenuTick);
			Clicked?.Invoke(Augment);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			CalculatedStyle dims = GetDimensions();
			var rect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);

			// 1. Slot Background (Terraria inventory dark slate-blue)
			Color bgColor = isHovered ? new Color(38, 50, 92) * 0.95f : new Color(24, 32, 60) * 0.92f;
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, bgColor);

			// 2. Inset slot bevel (classic Terraria inventory bevel)
			Color topLight = new Color(55, 72, 128);
			Color botDark = new Color(12, 16, 32);

			// Top & Left inner highlight
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, 2), topLight);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X + 1, rect.Y + 1, 2, rect.Height - 2), topLight);

			// Bottom & Right inner shadow
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X + 1, rect.Bottom - 3, rect.Width - 2, 2), botDark);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 3, rect.Y + 1, 2, rect.Height - 2), botDark);

			// 3. Border (Rarity colored or Gold when selected)
			Color borderColor = AugmentListEntry.RarityColor(Augment.Rarity);
			int borderWidth = 2;

			if (IsSelected)
			{
				borderColor = new Color(255, 220, 80);
				borderWidth = 3;
			}
			else if (isHovered)
			{
				borderColor = Color.Lerp(borderColor, Color.White, 0.4f);
			}

			// Draw 4 border lines
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, rect.Width, borderWidth), borderColor);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - borderWidth, rect.Width, borderWidth), borderColor);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, borderWidth, rect.Height), borderColor);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - borderWidth, rect.Y, borderWidth, rect.Height), borderColor);

			// 4. Augment Name Initials (Centered)
			string initials = GetInitials(Augment.DisplayName);
			var font = FontAssets.MouseText.Value;
			Vector2 textSize = ChatManager.GetStringSize(font, initials, new Vector2(0.85f));
			Vector2 textPos = new Vector2(
				rect.X + (rect.Width - textSize.X) * 0.5f,
				rect.Y + (rect.Height - textSize.Y) * 0.5f
			);

			Color nameColor = AugmentListEntry.RarityColor(Augment.Rarity);
			if (isHovered)
				nameColor = Color.White;

			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, font, initials, textPos, nameColor, 0f, Vector2.Zero, new Vector2(0.85f)
			);

			// 5. Class badge in top-left corner
			string classLetter = GetClassLetter(Augment.Class);
			Color classColor = GetClassColor(Augment.Class);
			Vector2 classPos = new Vector2(rect.X + 4f, rect.Y + 3f);
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, font, classLetter, classPos, classColor, 0f, Vector2.Zero, new Vector2(0.65f)
			);

			// 6. Owned Indicator in bottom-right corner
			if (IsOwned)
			{
				Vector2 ownedPos = new Vector2(rect.Right - 14f, rect.Bottom - 18f);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, font, "+", ownedPos, new Color(100, 255, 120), 0f, Vector2.Zero, new Vector2(0.85f)
				);
			}
		}

		private static string GetInitials(string name)
		{
			if (string.IsNullOrEmpty(name))
				return "??";

			string[] words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (words.Length == 1)
				return words[0].Length <= 3 ? words[0] : words[0].Substring(0, 3).ToUpper();

			string result = "";
			foreach (string word in words)
			{
				if (!string.IsNullOrEmpty(word) && char.IsLetterOrDigit(word[0]))
					result += char.ToUpper(word[0]);
			}
			return result.Length > 4 ? result.Substring(0, 4) : result;
		}

		private static string GetClassLetter(AugmentClass augmentClass)
		{
			return augmentClass switch
			{
				AugmentClass.Melee => "M",
				AugmentClass.Ranged => "R",
				AugmentClass.Magic => "Mg",
				AugmentClass.Summon => "S",
				_ => "U"
			};
		}

		private static Color GetClassColor(AugmentClass augmentClass)
		{
			return augmentClass switch
			{
				AugmentClass.Melee => new Color(255, 130, 110),
				AugmentClass.Ranged => new Color(130, 235, 130),
				AugmentClass.Magic => new Color(110, 215, 255),
				AugmentClass.Summon => new Color(255, 215, 110),
				_ => new Color(190, 195, 210)
			};
		}
	}
}
