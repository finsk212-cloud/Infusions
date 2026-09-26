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
		public event Action<Augment> RightClicked;

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

		public override void RightClick(UIMouseEvent evt)
		{
			base.RightClick(evt);
			RightClicked?.Invoke(Augment);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			CalculatedStyle dims = GetDimensions();
			var rect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);

			float time = (float)Main.GlobalTimeWrappedHourly;
			float rarePulse = (float)Math.Sin(time * 2.5f + (rect.X + rect.Y) * 0.02f) * 0.5f + 0.5f;
			float epicPulse = (float)Math.Sin(time * 3f + (rect.X + rect.Y) * 0.02f) * 0.5f + 0.5f;
			float legPulse = (float)Math.Sin(time * 4f + (rect.X + rect.Y) * 0.02f) * 0.5f + 0.5f;

			// 0. Outer Aura Glow for Rare, Epic & Legendary (drawn behind the slot box)
			if (Augment.Rarity == AugmentRarity.Rare)
			{
				int glowDist = 1 + (int)(rarePulse * 1.5f);
				Color rareGlow = new Color(60, 175, 255) * (0.06f + rarePulse * 0.10f);
				Rectangle auraRect = new Rectangle(rect.X - glowDist, rect.Y - glowDist, rect.Width + glowDist * 2, rect.Height + glowDist * 2);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, auraRect, rareGlow);
			}
			else if (Augment.Rarity == AugmentRarity.Epic)
			{
				int glowDist = 1 + (int)(epicPulse * 3f);
				Color epicGlow = new Color(170, 90, 255) * (0.10f + epicPulse * 0.20f);
				Rectangle auraRect = new Rectangle(rect.X - glowDist, rect.Y - glowDist, rect.Width + glowDist * 2, rect.Height + glowDist * 2);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, auraRect, epicGlow);
			}
			else if (Augment.Rarity == AugmentRarity.Legendary)
			{
				int glowDist = 2 + (int)(legPulse * 4f);
				Color legGlow = new Color(255, 170, 30) * (0.16f + legPulse * 0.28f);
				Rectangle auraRect = new Rectangle(rect.X - glowDist, rect.Y - glowDist, rect.Width + glowDist * 2, rect.Height + glowDist * 2);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, auraRect, legGlow);
			}

			Color rarityColor = AugmentListEntry.RarityColor(Augment.Rarity);

			// 1. Cybernetic Chassis Background (#0A101C at 94% opacity with subtle ambient underglow)
			float underglowAlpha = isHovered ? 0.08f : 0.035f;
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, rarityColor * underglowAlpha);

			Color bgColor = isHovered ? new Color(14, 22, 38) * 0.96f : new Color(10, 16, 28) * 0.94f;
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, bgColor);

			// 2. 1px Inner Hairline Highlight Accent
			Color innerHairline = Color.White * (isHovered ? 0.09f : 0.06f);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, 1), innerHairline);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X + 1, rect.Bottom - 2, rect.Width - 2, 1), innerHairline);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X + 1, rect.Y + 1, 1, rect.Height - 2), innerHairline);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 2, rect.Y + 1, 1, rect.Height - 2), innerHairline);

			// 2.5. Diagonal Light Gleam (Shine Sweep) across slot for Legendary tier
			if (Augment.Rarity == AugmentRarity.Legendary)
			{
				float sweepPeriod = 4.0f;
				float sweepProgress = (time * 0.6f + (rect.X + rect.Y) * 0.003f) % sweepPeriod;
				if (sweepProgress < 1.8f)
				{
					float t = sweepProgress / 1.8f;
					float sweepCenter = (rect.Width + rect.Height) * t;
					int beamWidth = 18;
					Color beamColor = new Color(255, 245, 215);

					for (int py = 2; py < rect.Height - 2; py += 2)
					{
						int centerPx = (int)(sweepCenter - py);
						int startPx = Math.Max(2, centerPx - beamWidth / 2);
						int endPx = Math.Min(rect.Width - 2, centerPx + beamWidth / 2);
						if (endPx > startPx)
						{
							float dist = Math.Abs((startPx + endPx) * 0.5f - centerPx);
							float beamA = (1f - dist / (beamWidth * 0.6f)) * 0.32f;
							if (beamA > 0.04f)
							{
								spriteBatch.Draw(TextureAssets.MagicPixel.Value,
									new Rectangle(rect.X + startPx, rect.Y + py, endPx - startPx, 2),
									beamColor * beamA);
							}
						}
					}
				}
			}

			// 3. Border (Rarity colored, with Epic/Legendary custom effects)
			Color borderColor = rarityColor;
			int borderWidth = 1;

			if (IsSelected)
			{
				borderColor = new Color(255, 220, 80);
				borderWidth = 2;
			}
			else if (Augment.Rarity == AugmentRarity.Rare)
			{
				borderColor = Color.Lerp(new Color(75, 175, 250), new Color(145, 225, 255), rarePulse * 0.35f);
				if (isHovered)
					borderColor = Color.Lerp(borderColor, Color.White, 0.4f);
			}
			else if (Augment.Rarity == AugmentRarity.Epic)
			{
				borderColor = Color.Lerp(new Color(155, 115, 225), new Color(215, 180, 255), epicPulse * 0.45f);
				if (isHovered)
					borderColor = Color.Lerp(borderColor, Color.White, 0.4f);
			}
			else if (Augment.Rarity == AugmentRarity.Legendary)
			{
				borderColor = Color.Lerp(new Color(255, 160, 20), new Color(255, 210, 60), legPulse * 0.45f);
				if (isHovered)
					borderColor = Color.Lerp(borderColor, Color.White, 0.4f);
			}
			else if (isHovered)
			{
				borderColor = Color.Lerp(borderColor, Color.White, 0.4f);
			}

			// Draw 4 border lines
			Color finalBorderCol = borderColor * (isHovered ? 0.90f : 0.65f);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, rect.Width, borderWidth), finalBorderCol);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - borderWidth, rect.Width, borderWidth), finalBorderCol);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, borderWidth, rect.Height), finalBorderCol);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - borderWidth, rect.Y, borderWidth, rect.Height), finalBorderCol);

			// Corner accent notches for high-tech aesthetic
			const int cLen = 5;
			const int cThick = 2;
			Color cornerCol = IsSelected ? new Color(255, 220, 80) : (isHovered ? Color.Lerp(borderColor, Color.White, 0.35f) : borderColor * 0.90f);

			if (Augment.Rarity != AugmentRarity.Legendary)
			{
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, cLen, cThick), cornerCol);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, cThick, cLen), cornerCol);

				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - cLen, rect.Y, cLen, cThick), cornerCol);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - cThick, rect.Y, cThick, cLen), cornerCol);

				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - cThick, cLen, cThick), cornerCol);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - cLen, cThick, cLen), cornerCol);

				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - cLen, rect.Bottom - cThick, cLen, cThick), cornerCol);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - cThick, rect.Bottom - cLen, cThick, cLen), cornerCol);
			}
			else if (Augment.Rarity == AugmentRarity.Epic)
			{
				// 4 Luminous Amethyst Corner Studs (3x3 pixels)
				Color gemColor = new Color(225, 185, 255) * (0.8f + epicPulse * 0.2f);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X - 1, rect.Y - 1, 3, 3), gemColor);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 2, rect.Y - 1, 3, 3), gemColor);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X - 1, rect.Bottom - 2, 3, 3), gemColor);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 2, rect.Bottom - 2, 3, 3), gemColor);

				// Twinkling Amethyst Star Sparkles along the borders for Epic
				(int x, int y, float offset)[] spPoints = new (int, int, float)[]
				{
					(rect.Right - 1, rect.Y, 0.0f),
					(rect.X + 1, rect.Bottom - 1, 1.2f),
					(rect.X, rect.Y + (int)(rect.Height * 0.45f), 2.4f),
					(rect.Right - 1, rect.Y + (int)(rect.Height * 0.65f), 0.6f)
				};

				foreach (var sp in spPoints)
				{
					float spPhase = (time * 1.1f + sp.offset + (rect.X * 0.02f)) % 3.6f;
					if (spPhase < 1.3f)
					{
						float prog = spPhase / 1.3f;
						float intensity = (float)Math.Sin(prog * MathHelper.Pi);
						DrawAmethystSparkle(spriteBatch, sp.x, sp.y, intensity);
					}
				}
			}
			else if (Augment.Rarity == AugmentRarity.Legendary)
			{
				// Inner Gold Hairline
				Color innerGold = new Color(255, 225, 90) * (0.75f + legPulse * 0.25f);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, 1), innerGold);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X + 2, rect.Y + 2, 1, rect.Height - 4), innerGold);

				// 4 Ornate Royal Gold Corner Brackets (6x2 and 2x6 L-shapes)
				Color cornerGold = new Color(255, 220, 80);
				// Top-Left
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X - 2, rect.Y - 2, 6, 2), cornerGold);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X - 2, rect.Y - 2, 2, 6), cornerGold);
				// Top-Right
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 4, rect.Y - 2, 6, 2), cornerGold);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right, rect.Y - 2, 2, 6), cornerGold);
				// Bottom-Left
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X - 2, rect.Bottom, 6, 2), cornerGold);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X - 2, rect.Bottom - 4, 2, 6), cornerGold);
				// Bottom-Right
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 4, rect.Bottom, 6, 2), cornerGold);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right, rect.Bottom - 4, 2, 6), cornerGold);

				// Multi-Point Twinkling Star Sparkles along the borders (graceful, relaxed pace)
				(int x, int y, float offset)[] spPoints = new (int, int, float)[]
				{
					(rect.Right - 1, rect.Y, 0.0f),                     // Top-right corner
					(rect.X + 1, rect.Bottom - 1, 1.0f),               // Bottom-left corner
					(rect.X, rect.Y + (int)(rect.Height * 0.42f), 2.0f), // Left edge
					(rect.Right - 1, rect.Y + (int)(rect.Height * 0.68f), 3.0f), // Right edge
					(rect.X + (int)(rect.Width * 0.55f), rect.Y + 1, 1.5f) // Top edge
				};

				foreach (var sp in spPoints)
				{
					float spPhase = (time * 1.0f + sp.offset + (rect.X * 0.02f)) % 4.0f;
					if (spPhase < 1.4f)
					{
						float prog = spPhase / 1.4f;
						float intensity = (float)Math.Sin(prog * MathHelper.Pi);
						DrawStarSparkle(spriteBatch, sp.x, sp.y, intensity);
					}
				}
			}

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

			// 7. Installed Indicator Badge in top-right corner
			if (IsOwned)
			{
				Rectangle badgeRect = new Rectangle(rect.Right - 15, rect.Y + 3, 12, 12);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, badgeRect, new Color(8, 14, 24) * 0.90f);
				DrawMiniBorder(spriteBatch, badgeRect, AugmentTextColors.Healing * 0.70f);

				Vector2 checkSz = ChatManager.GetStringSize(font, "✓", new Vector2(0.50f));
				Vector2 checkPos = new Vector2(
					badgeRect.X + (badgeRect.Width - checkSz.X) * 0.5f,
					badgeRect.Y + (badgeRect.Height - checkSz.Y) * 0.5f - 1f
				);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, font, "✓", checkPos, AugmentTextColors.Healing, 0f, Vector2.Zero, new Vector2(0.50f)
				);
			}
		}

		private static void DrawMiniBorder(SpriteBatch spriteBatch, Rectangle rect, Color color)
		{
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
			spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
			spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
			spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
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

		public static void DrawStarSparkle(SpriteBatch spriteBatch, int cx, int cy, float intensity)
		{
			if (intensity <= 0.05f) return;

			int rayLen = 3 + (int)(intensity * 6f); // 3..9px
			Color goldRay = new Color(255, 225, 100) * (intensity * 0.85f);
			Color whiteRay = Color.White * intensity;

			// Outer golden cross rays
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx, cy - rayLen, 1, rayLen * 2 + 1), goldRay);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - rayLen, cy, rayLen * 2 + 1, 1), goldRay);

			// 8-point diagonal glint spikes when intensity > 0.55f
			if (intensity > 0.55f)
			{
				int d = (int)(rayLen * 0.6f);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - d, cy - d, 1, 1), goldRay);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx + d, cy - d, 1, 1), goldRay);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - d, cy + d, 1, 1), goldRay);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx + d, cy + d, 1, 1), goldRay);
			}

			// Inner brilliant white core
			int innerRay = (int)(rayLen * 0.45f);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx, cy - innerRay, 1, innerRay * 2 + 1), whiteRay);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - innerRay, cy, innerRay * 2 + 1, 1), whiteRay);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - 1, cy - 1, 3, 3), Color.White * (intensity * 0.95f));
		}

		public static void DrawAmethystSparkle(SpriteBatch spriteBatch, int cx, int cy, float intensity)
		{
			if (intensity <= 0.05f) return;

			int rayLen = 2 + (int)(intensity * 5f);
			Color violetRay = new Color(215, 140, 255) * (intensity * 0.9f);
			Color whiteRay = Color.White * intensity;

			// Cross rays
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx, cy - rayLen, 1, rayLen * 2 + 1), violetRay);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - rayLen, cy, rayLen * 2 + 1, 1), violetRay);

			// 8-point spikes when intensity > 0.55f
			if (intensity > 0.55f)
			{
				int d = (int)(rayLen * 0.6f);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - d, cy - d, 1, 1), violetRay);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx + d, cy - d, 1, 1), violetRay);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - d, cy + d, 1, 1), violetRay);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx + d, cy + d, 1, 1), violetRay);
			}

			// Inner brilliant white core
			int innerRay = (int)(rayLen * 0.45f);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx, cy - innerRay, 1, innerRay * 2 + 1), whiteRay);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - innerRay, cy, innerRay * 2 + 1, 1), whiteRay);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - 1, cy - 1, 3, 3), Color.White * (intensity * 0.95f));
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
