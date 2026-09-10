using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
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

		public readonly Augment Augment;
		public bool IsSelected { get; set; }
		public bool IsOwned { get; set; }

		public event Action<Augment> Clicked;

		private bool isHovered;

		public AugmentSlotElement(Augment augment)
		{
			Augment = augment;
			Width.Set(72f, 0f);
			Height.Set(72f, 0f);
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

			var font = FontAssets.MouseText.Value;

			// 4. Center Content: Sword for Melee, Angled Flame for Magic, Initials for others
			if (Augment.Class == AugmentClass.Melee)
			{
				if (MeleeIconAsset == null)
					MeleeIconAsset = ModContent.Request<Texture2D>("Augments/UI/MeleeIcon", AssetRequestMode.ImmediateLoad);

				if (MeleeIconAsset?.IsLoaded == true)
				{
					Texture2D swordTex = MeleeIconAsset.Value;
					Color iconColor = AugmentListEntry.RarityColor(Augment.Rarity);
					if (Augment.Rarity == AugmentRarity.Common)
						iconColor = new Color(225, 230, 240); // Clean silver-white for Common tier

					if (isHovered)
						iconColor = Color.Lerp(iconColor, Color.White, 0.4f);

					Vector2 iconPos = new Vector2(
						rect.X + (rect.Width - swordTex.Width) * 0.5f,
						rect.Y + (rect.Height - swordTex.Height) * 0.5f
					);

					spriteBatch.Draw(swordTex, iconPos, iconColor);
				}
			}
			else if (Augment.Class == AugmentClass.Magic)
			{
				if (MagicIconAsset == null)
					MagicIconAsset = ModContent.Request<Texture2D>("Augments/UI/MagicIcon", AssetRequestMode.ImmediateLoad);

				if (MagicIconAsset?.IsLoaded == true)
				{
					Texture2D flameTex = MagicIconAsset.Value;
					Color iconColor = AugmentListEntry.RarityColor(Augment.Rarity);
					if (Augment.Rarity == AugmentRarity.Common)
						iconColor = new Color(225, 230, 240);

					if (isHovered)
						iconColor = Color.Lerp(iconColor, Color.White, 0.4f);

					Vector2 iconPos = new Vector2(
						rect.X + (rect.Width - flameTex.Width) * 0.5f,
						rect.Y + (rect.Height - flameTex.Height) * 0.5f
					);

					spriteBatch.Draw(flameTex, iconPos, iconColor);
				}
			}
			else if (Augment.Class == AugmentClass.Summon)
			{
				if (SummonIconAsset == null)
					SummonIconAsset = ModContent.Request<Texture2D>("Augments/UI/SummonerIcon", AssetRequestMode.ImmediateLoad);

				if (SummonIconAsset?.IsLoaded == true)
				{
					Texture2D slimeTex = SummonIconAsset.Value;
					Color iconColor = AugmentListEntry.RarityColor(Augment.Rarity);
					if (Augment.Rarity == AugmentRarity.Common)
						iconColor = new Color(225, 230, 240);

					if (isHovered)
						iconColor = Color.Lerp(iconColor, Color.White, 0.4f);

					Vector2 iconPos = new Vector2(
						rect.X + (rect.Width - slimeTex.Width) * 0.5f,
						rect.Y + (rect.Height - slimeTex.Height) * 0.5f
					);

					spriteBatch.Draw(slimeTex, iconPos, iconColor);
				}
			}
			else if (Augment.Class == AugmentClass.Ranged)
			{
				if (RangedIconAsset == null)
					RangedIconAsset = ModContent.Request<Texture2D>("Augments/UI/RangedIcon", AssetRequestMode.ImmediateLoad);

				if (RangedIconAsset?.IsLoaded == true)
				{
					Texture2D crosshairTex = RangedIconAsset.Value;
					Color iconColor = AugmentListEntry.RarityColor(Augment.Rarity);
					if (Augment.Rarity == AugmentRarity.Common)
						iconColor = new Color(225, 230, 240);

					if (isHovered)
						iconColor = Color.Lerp(iconColor, Color.White, 0.4f);

					Vector2 iconPos = new Vector2(
						rect.X + (rect.Width - crosshairTex.Width) * 0.5f,
						rect.Y + (rect.Height - crosshairTex.Height) * 0.5f
					);

					spriteBatch.Draw(crosshairTex, iconPos, iconColor);
				}
			}
			else if (Augment.Class == AugmentClass.Support)
			{
				if (SupportIconAsset == null)
					SupportIconAsset = ModContent.Request<Texture2D>("Augments/UI/SupportIcon", AssetRequestMode.ImmediateLoad);

				if (SupportIconAsset?.IsLoaded == true)
				{
					Texture2D plusTex = SupportIconAsset.Value;
					Color iconColor = AugmentListEntry.RarityColor(Augment.Rarity);
					if (Augment.Rarity == AugmentRarity.Common)
						iconColor = new Color(225, 230, 240);

					if (isHovered)
						iconColor = Color.Lerp(iconColor, Color.White, 0.4f);

					Vector2 iconPos = new Vector2(
						rect.X + (rect.Width - plusTex.Width) * 0.5f,
						rect.Y + (rect.Height - plusTex.Height) * 0.5f
					);

					spriteBatch.Draw(plusTex, iconPos, iconColor);
				}
			}
			else
			{
				string initials = GetInitials(Augment.DisplayName);
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
			}

			// 5. Class badge in top-left corner (omit for Melee, Magic, Summon, Ranged, Support since they have custom icons)
			if (Augment.Class != AugmentClass.Melee && Augment.Class != AugmentClass.Magic && Augment.Class != AugmentClass.Summon && Augment.Class != AugmentClass.Ranged && Augment.Class != AugmentClass.Support)
			{
				string classLetter = GetClassLetter(Augment.Class);
				Color classColor = GetClassColor(Augment.Class);
				Vector2 classPos = new Vector2(rect.X + 4f, rect.Y + 3f);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, font, classLetter, classPos, classColor, 0f, Vector2.Zero, new Vector2(0.65f)
				);
			}

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
