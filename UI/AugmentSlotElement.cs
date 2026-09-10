using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Augments
{
	public class AugmentSlotElement : UIElement
	{
		public static Asset<Texture2D> MeleeIconAsset;
		public static Asset<Texture2D> MagicIconAsset;
		public static Asset<Texture2D> SummonIconAsset;
		public static Asset<Texture2D> RangedIconAsset;
		public static Asset<Texture2D> SupportIconAsset;
		public static Asset<Texture2D> UniversalIconAsset;

		public readonly Augment Augment;
		public bool IsSelected { get; set; }
		public bool IsOwned { get; set; }

		public event Action<Augment> Clicked;

		private bool isHovered;

		public AugmentSlotElement(Augment augment)
		{
			Augment = augment;
			Width.Set(74f, 0f);
			Height.Set(84f, 0f);
		}

		public override void MouseOver(UIMouseEvent evt)
		{
			base.MouseOver(evt);
			isHovered = true;
			AugmentListEntry.HoveredAugment = Augment;
			SoundEngine.PlaySound(SoundID.MenuTick);
		}

		public override void MouseOut(UIMouseEvent evt)
		{
			base.MouseOut(evt);
			isHovered = false;
			if (AugmentListEntry.HoveredAugment == Augment)
				AugmentListEntry.HoveredAugment = null;
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

			var font = FontAssets.MouseText.Value;

			// 4. Center Content: Class icon placed in upper portion (40x40 pixel-perfect 1:1)
			Texture2D iconTex = GetClassIcon(Augment.Class);
			Color nameColor = AugmentListEntry.RarityColor(Augment.Rarity);
			if (Augment.Rarity == AugmentRarity.Common)
				nameColor = new Color(225, 230, 240);

			if (isHovered)
				nameColor = Color.Lerp(nameColor, Color.White, 0.4f);

			if (iconTex != null)
			{
				Vector2 iconPos = new Vector2(
					rect.X + (rect.Width - iconTex.Width) * 0.5f,
					rect.Y + 6f
				);
				spriteBatch.Draw(iconTex, iconPos, nameColor);
			}
			else
			{
				string initials = GetInitials(Augment.DisplayName);
				Vector2 textSize = ChatManager.GetStringSize(font, initials, new Vector2(0.85f));
				Vector2 textPos = new Vector2(
					rect.X + (rect.Width - textSize.X) * 0.5f,
					rect.Y + 8f
				);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, font, initials, textPos, nameColor, 0f, Vector2.Zero, new Vector2(0.85f)
				);
			}

			// 5. Augment Display Name auto-fitted cleanly below the icon
			Rectangle textBounds = new Rectangle(rect.X + 3, rect.Y + 47, rect.Width - 6, rect.Height - 49);
			DrawSlotAugmentName(spriteBatch, font, Augment.DisplayName, textBounds, nameColor);

			// 6. Class badge in top-left corner (omit for classes with dedicated custom icons)
			if (Augment.Class != AugmentClass.Melee && Augment.Class != AugmentClass.Magic && Augment.Class != AugmentClass.Summon && Augment.Class != AugmentClass.Ranged && Augment.Class != AugmentClass.Support && Augment.Class != AugmentClass.Universal)
			{
				string classLetter = GetClassLetter(Augment.Class);
				Color classColor = GetClassColor(Augment.Class);
				Vector2 classPos = new Vector2(rect.X + 4f, rect.Y + 3f);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, font, classLetter, classPos, classColor, 0f, Vector2.Zero, new Vector2(0.65f)
				);
			}

			// 7. Owned Indicator in top-right corner (doesn't collide with bottom text)
			if (IsOwned)
			{
				Vector2 ownedPos = new Vector2(rect.Right - 15f, rect.Y + 2f);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, font, "+", ownedPos, new Color(100, 255, 120), 0f, Vector2.Zero, new Vector2(0.85f)
				);
			}
		}

		public static Texture2D GetClassIcon(AugmentClass augmentClass)
		{
			switch (augmentClass)
			{
				case AugmentClass.Melee:
					if (MeleeIconAsset == null)
						MeleeIconAsset = ModContent.Request<Texture2D>("Augments/UI/MeleeIcon", AssetRequestMode.ImmediateLoad);
					return MeleeIconAsset?.IsLoaded == true ? MeleeIconAsset.Value : null;

				case AugmentClass.Magic:
					if (MagicIconAsset == null)
						MagicIconAsset = ModContent.Request<Texture2D>("Augments/UI/MagicIcon", AssetRequestMode.ImmediateLoad);
					return MagicIconAsset?.IsLoaded == true ? MagicIconAsset.Value : null;

				case AugmentClass.Summon:
					if (SummonIconAsset == null)
						SummonIconAsset = ModContent.Request<Texture2D>("Augments/UI/SummonerIcon", AssetRequestMode.ImmediateLoad);
					return SummonIconAsset?.IsLoaded == true ? SummonIconAsset.Value : null;

				case AugmentClass.Ranged:
					if (RangedIconAsset == null)
						RangedIconAsset = ModContent.Request<Texture2D>("Augments/UI/RangedIcon", AssetRequestMode.ImmediateLoad);
					return RangedIconAsset?.IsLoaded == true ? RangedIconAsset.Value : null;

				case AugmentClass.Support:
					if (SupportIconAsset == null)
						SupportIconAsset = ModContent.Request<Texture2D>("Augments/UI/SupportIcon", AssetRequestMode.ImmediateLoad);
					return SupportIconAsset?.IsLoaded == true ? SupportIconAsset.Value : null;

				case AugmentClass.Universal:
					if (UniversalIconAsset == null)
						UniversalIconAsset = ModContent.Request<Texture2D>("Augments/UI/UniversalIcon", AssetRequestMode.ImmediateLoad);
					return UniversalIconAsset?.IsLoaded == true ? UniversalIconAsset.Value : null;

				default:
					return null;
			}
		}

		private static void DrawSlotAugmentName(SpriteBatch spriteBatch, DynamicSpriteFont font, string name, Rectangle textRect, Color color)
		{
			if (string.IsNullOrEmpty(name))
				return;

			float maxW = textRect.Width;

			// Check single-line fit
			Vector2 singleSize = ChatManager.GetStringSize(font, name, Vector2.One);
			float singleScale = Math.Min(0.52f, maxW / Math.Max(1f, singleSize.X));

			string[] words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (singleScale >= 0.44f || words.Length <= 1)
			{
				Vector2 drawScale = new Vector2(singleScale);
				Vector2 drawnSize = singleSize * singleScale;
				Vector2 pos = new Vector2(
					textRect.X + (textRect.Width - drawnSize.X) * 0.5f,
					textRect.Y + (textRect.Height - drawnSize.Y) * 0.5f
				);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, name, pos, color, 0f, Vector2.Zero, drawScale);
				return;
			}

			// Split into 2 lines at the best word boundary (closest to 50/50 character distribution)
			int bestSplit = 1;
			int minDiff = int.MaxValue;
			for (int i = 1; i < words.Length; i++)
			{
				string lineA = string.Join(" ", words, 0, i);
				string lineB = string.Join(" ", words, i, words.Length - i);
				int diff = Math.Abs(lineA.Length - lineB.Length);
				if (diff < minDiff)
				{
					minDiff = diff;
					bestSplit = i;
				}
			}

			string topStr = string.Join(" ", words, 0, bestSplit);
			string botStr = string.Join(" ", words, bestSplit, words.Length - bestSplit);

			Vector2 topSize = ChatManager.GetStringSize(font, topStr, Vector2.One);
			Vector2 botSize = ChatManager.GetStringSize(font, botStr, Vector2.One);

			float maxLineWidth = Math.Max(topSize.X, botSize.X);
			float lineScaleVal = Math.Min(0.46f, maxW / Math.Max(1f, maxLineWidth));
			Vector2 lineScale = new Vector2(lineScaleVal);

			float lineHeight = topSize.Y * lineScaleVal;
			float lineSpacing = lineHeight * 0.88f;
			float totalTextH = lineSpacing + lineHeight;
			float startY = textRect.Y + (textRect.Height - totalTextH) * 0.5f;

			Vector2 topPos = new Vector2(
				textRect.X + (textRect.Width - topSize.X * lineScaleVal) * 0.5f,
				startY
			);
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, topStr, topPos, color, 0f, Vector2.Zero, lineScale);

			Vector2 botPos = new Vector2(
				textRect.X + (textRect.Width - botSize.X * lineScaleVal) * 0.5f,
				startY + lineSpacing
			);
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, botStr, botPos, color, 0f, Vector2.Zero, lineScale);
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
