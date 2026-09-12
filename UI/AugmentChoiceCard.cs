using System;
using System.Collections.Generic;
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
	// High-fidelity sci-fi plug-in chip card for the 3-choice roll UI.
	// Features dedicated class icon box, rarity stars rating, dynamic holo-sheen
	// sweep, floating procedural sparkles for Epic/Legendary, glowing multi-layer auras,
	// and interactive install prompts.
	public class AugmentChoiceCard : UIElement
	{
		public Augment Augment { get; }
		public event Action<Augment> OnAugmentChosen;

		public const float MinCardHeight = 410f;
		private const float NameDescSpacing = 6f;
		private const float LineSpacing = 1f;

		private static readonly Vector2 NameScale = new Vector2(0.92f);
		private static readonly Vector2 DescScale = new Vector2(0.78f);

		private const string KeystoneTagText = "KEYSTONE • PERMANENT CHOICE";
		private const string SupportTagText = "SUPPORT CLASS";
		private const string FortuneTagText = "FORTUNE FAMILY";

		private static readonly Color KeystoneTagColor = new Color(240, 80, 80);
		private static readonly Color SupportTagColor = new Color(90, 230, 140);
		private static readonly Color FortuneTagColor = new Color(255, 205, 65);

		private static Asset<Texture2D> RarityStarAsset;

		// Animation timings & parameters
		private static readonly float[] PulseSpeeds = { 1.0f, 1.8f, 2.6f, 3.4f };

		private readonly int cardIndex;
		private readonly List<string> nameLines;
		private readonly List<string> descLines;
		private readonly bool isKeystone;
		private readonly bool isSupport;
		private readonly bool isFortuneThemed;
		private readonly Color baseBorderColor;
		private readonly float pulseSpeed;
		private float pulseTimer;
		private bool isHovered;

		// Stored bounds of the bottom tags for mouse hover check
		private Rectangle specialTagRect;

		public AugmentChoiceCard(Augment augment, float width, int cardIndex = 0)
		{
			Augment = augment;
			this.cardIndex = cardIndex;
			isKeystone = augment.KeystoneFamily != null;
			isSupport = augment.Class == AugmentClass.Support;
			isFortuneThemed = augment.IsLuckyThemed;

			baseBorderColor = RarityColor(augment.Rarity);
			pulseSpeed = PulseSpeeds[(int)augment.Rarity];

			Width.Set(width, 0f);
			Height.Set(MinCardHeight, 0f);

			var font = FontAssets.MouseText.Value;
			float contentWidth = width - 32f; // 16px left/right padding

			nameLines = AugmentColorText.Wrap(font, augment.DisplayName, contentWidth, NameScale);
			descLines = AugmentColorText.Wrap(font, augment.Description, contentWidth, DescScale);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			pulseTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
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
			OnAugmentChosen?.Invoke(Augment);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			CalculatedStyle dims = GetDimensions();
			Rectangle rect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);

			float time = pulseTimer + cardIndex * 0.75f;
			float pulse = (float)Math.Sin(time * pulseSpeed) * 0.5f + 0.5f;

			var font = FontAssets.MouseText.Value;
			Color rarityColor = baseBorderColor;

			// =================================================================
			// 1. OUTER AURAS & CORONAS (Animated for Epic, Legendary, and Rare)
			// =================================================================
			DrawCardAuras(spriteBatch, rect, pulse, time);

			// =================================================================
			// 2. SOLID SCI-FI CARD CHASSIS & HEADER BANNER
			// =================================================================
			Color bgColor = isHovered ? new Color(22, 30, 60) : new Color(15, 20, 42);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, bgColor);

			// Top Header Banner
			Rectangle headerRect = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, 126);
			Color headerBg = isHovered ? new Color(28, 38, 76) * 0.95f : new Color(20, 28, 56) * 0.95f;
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, headerRect, headerBg);

			// =================================================================
			// 3. HOLOGRAPHIC LIGHT SHEEN SWEEP (Legendary & Epic)
			// =================================================================
			DrawHoloSweep(spriteBatch, rect, time);

			// =================================================================
			// 4. CARD FRAME & BORDERS (Pulsing colors, Corner ornaments)
			// =================================================================
			Color borderColor = GetBorderColor(pulse);
			DrawCardBorders(spriteBatch, rect, borderColor, pulse);

			// =================================================================
			// 5. CLASS ICON BOX & ROTATING/PULSING HALO
			// =================================================================
			int iconBoxSize = 44;
			int iconBoxX = rect.X + (rect.Width - iconBoxSize) / 2;
			int iconBoxY = rect.Y + 12;
			Rectangle iconBox = new Rectangle(iconBoxX, iconBoxY, iconBoxSize, iconBoxSize);

			// Icon box inner chassis
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, iconBox, new Color(10, 14, 28) * 0.95f);
			DrawRectBorder(spriteBatch, iconBox, borderColor * 0.7f, 1);

			// Halo animation behind icon
			DrawIconHalo(spriteBatch, iconBoxX, iconBoxY, iconBoxSize, pulse);

			// Draw Class Icon
			Texture2D iconTex = AugmentSlotElement.GetClassIcon(Augment.Class);
			if (iconTex != null)
			{
				float iconScale = Math.Min(30f / iconTex.Width, 30f / iconTex.Height);
				Vector2 iconPos = new Vector2(
					iconBoxX + (iconBoxSize - iconTex.Width * iconScale) * 0.5f,
					iconBoxY + (iconBoxSize - iconTex.Height * iconScale) * 0.5f
				);
				Color iconColor = isHovered ? Color.White : rarityColor;
				if (Augment.Rarity == AugmentRarity.Common) iconColor = new Color(225, 230, 240);
				spriteBatch.Draw(iconTex, iconPos, null, iconColor, 0f, Vector2.Zero, iconScale, SpriteEffects.None, 0f);
			}

			// =================================================================
			// 6. RARITY STARS & CLASS PILL BADGE
			// =================================================================
			int pillY = iconBoxY + iconBoxSize + 6; // Y = 62
			DrawRarityBadge(spriteBatch, font, rect, pillY, borderColor, pulse);

			// =================================================================
			// 7. AUGMENT DISPLAY NAME
			// =================================================================
			float nameY = pillY + 22f; // Y = 84
			float centerX = rect.X + rect.Width * 0.5f;
			Color nameColor = isHovered ? Color.Lerp(rarityColor, Color.White, 0.45f) : rarityColor;
			nameY = DrawCenteredLines(spriteBatch, font, nameLines, NameScale, centerX, nameY, nameColor);

			// =================================================================
			// 8. GLOWING HEADER DIVIDER LINE
			// =================================================================
			float divY = Math.Max(nameY + 4f, rect.Y + 128f);
			int divLeft = rect.X + 16;
			int divW = rect.Width - 32;
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(divLeft, (int)divY, divW, 1), borderColor * 0.45f);

			// Center diamond node
			int midNodeX = rect.X + rect.Width / 2;
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(midNodeX - 1, (int)divY - 1, 3, 3), borderColor * 0.85f);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(midNodeX, (int)divY, 1, 1), Color.White * 0.9f);

			// =================================================================
			// 9. ANIMATED PROCEDURAL STAR SPARKLES (Epic & Legendary)
			// =================================================================
			DrawProceduralSparkles(spriteBatch, rect, time);

			// =================================================================
			// 10. DESCRIPTION BODY
			// =================================================================
			float descY = divY + 8f;
			float descLeft = rect.X + 16;
			DrawLeftAlignedLines(spriteBatch, font, descLines, DescScale, descLeft, descY);

			// =================================================================
			// 11. SPECIAL TAG BADGES (Keystone / Support / Fortune)
			// =================================================================
			DrawSpecialBadges(spriteBatch, font, rect);

			// =================================================================
			// 12. BOTTOM "CLICK TO INSTALL" ACTION BAR
			// =================================================================
			DrawInstallActionBar(spriteBatch, font, rect, borderColor);

			// Tooltips for tags if mouse is hovering over them
			if (specialTagRect.Contains(Main.MouseScreen.ToPoint()))
			{
				if (isSupport)
					DrawSupportTooltip(spriteBatch, font);
				else if (isFortuneThemed)
					DrawFortuneTooltip(spriteBatch, font);
			}
		}

		private void DrawCardAuras(SpriteBatch spriteBatch, Rectangle rect, float pulse, float time)
		{
			if (Augment.Rarity == AugmentRarity.Legendary)
			{
				// Radiant Golden Divine Aura
				int auraDist = (int)(6f + pulse * 6f) + (isHovered ? 3 : 0);
				Rectangle auraRect = new Rectangle(rect.X - auraDist, rect.Y - auraDist, rect.Width + auraDist * 2, rect.Height + auraDist * 2);
				Color auraCol = new Color(255, 160, 25) * ((0.12f + pulse * 0.16f) * (isHovered ? 1.4f : 1f));
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, auraRect, auraCol);

				int midDist = (int)(2f + pulse * 3f);
				Rectangle midRect = new Rectangle(rect.X - midDist, rect.Y - midDist, rect.Width + midDist * 2, rect.Height + midDist * 2);
				Color midCol = new Color(255, 210, 60) * ((0.16f + pulse * 0.20f) * (isHovered ? 1.3f : 1f));
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, midRect, midCol);
			}
			else if (Augment.Rarity == AugmentRarity.Epic)
			{
				// Mystical Amethyst Aura
				int auraDist = (int)(5f + pulse * 5f) + (isHovered ? 3 : 0);
				Rectangle auraRect = new Rectangle(rect.X - auraDist, rect.Y - auraDist, rect.Width + auraDist * 2, rect.Height + auraDist * 2);
				Color auraCol = new Color(175, 95, 245) * ((0.10f + pulse * 0.15f) * (isHovered ? 1.4f : 1f));
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, auraRect, auraCol);

				int midDist = (int)(2f + pulse * 2f);
				Rectangle midRect = new Rectangle(rect.X - midDist, rect.Y - midDist, rect.Width + midDist * 2, rect.Height + midDist * 2);
				Color midCol = new Color(220, 150, 255) * ((0.14f + pulse * 0.18f) * (isHovered ? 1.3f : 1f));
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, midRect, midCol);
			}
			else if (Augment.Rarity == AugmentRarity.Rare)
			{
				// Soft Cyan Edge Glow
				int auraDist = isHovered ? 4 : 2;
				Rectangle auraRect = new Rectangle(rect.X - auraDist, rect.Y - auraDist, rect.Width + auraDist * 2, rect.Height + auraDist * 2);
				Color auraCol = new Color(60, 170, 255) * (isHovered ? 0.22f : 0.10f);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, auraRect, auraCol);
			}
		}

		private void DrawHoloSweep(SpriteBatch spriteBatch, Rectangle rect, float time)
		{
			if (Augment.Rarity != AugmentRarity.Legendary && Augment.Rarity != AugmentRarity.Epic)
				return;

			float sweepPeriod = Augment.Rarity == AugmentRarity.Legendary ? 3.5f : 4.2f;
			float sweepTime = time % sweepPeriod;
			float sweepDuration = 1.6f;

			if (sweepTime < sweepDuration)
			{
				float t = sweepTime / sweepDuration;
				float sweepCenter = (rect.Width + rect.Height) * t;
				int beamWidth = Augment.Rarity == AugmentRarity.Legendary ? 28 : 22;
				Color beamColor = Augment.Rarity == AugmentRarity.Legendary ? new Color(255, 245, 215) : new Color(225, 185, 255);

				for (int py = 4; py < rect.Height - 4; py += 3)
				{
					int centerPx = (int)(sweepCenter - py);
					int startPx = Math.Max(3, centerPx - beamWidth / 2);
					int endPx = Math.Min(rect.Width - 3, centerPx + beamWidth / 2);
					if (endPx > startPx)
					{
						float dist = Math.Abs((startPx + endPx) * 0.5f - centerPx);
						float beamA = (1f - dist / (beamWidth * 0.6f)) * (Augment.Rarity == AugmentRarity.Legendary ? 0.24f : 0.18f);
						if (isHovered) beamA *= 1.35f;
						if (beamA > 0.02f)
						{
							spriteBatch.Draw(TextureAssets.MagicPixel.Value,
								new Rectangle(rect.X + startPx, rect.Y + py, endPx - startPx, 3),
								beamColor * beamA);
						}
					}
				}
			}
		}

		private Color GetBorderColor(float pulse)
		{
			Color color = baseBorderColor;
			if (Augment.Rarity == AugmentRarity.Epic)
				color = Color.Lerp(new Color(175, 95, 245), new Color(230, 185, 255), pulse * 0.45f);
			else if (Augment.Rarity == AugmentRarity.Legendary)
				color = Color.Lerp(new Color(255, 170, 35), new Color(255, 230, 100), pulse * 0.45f);

			if (isHovered)
				color = Color.Lerp(color, Color.White, 0.35f);

			return color;
		}

		private void DrawCardBorders(SpriteBatch spriteBatch, Rectangle rect, Color borderColor, float pulse)
		{
			// 2px outer border
			DrawRectBorder(spriteBatch, rect, borderColor, 2);

			// 1px inner hairline
			Color hairlineColor = borderColor * 0.30f;
			Rectangle innerHairline = new Rectangle(rect.X + 3, rect.Y + 3, rect.Width - 6, rect.Height - 6);
			DrawRectBorder(spriteBatch, innerHairline, hairlineColor, 1);

			// Ornaments
			if (Augment.Rarity == AugmentRarity.Epic)
			{
				// 4 Glowing Amethyst Corner Studs (3x3 pixels)
				Color gemColor = new Color(230, 190, 255) * (0.8f + pulse * 0.2f);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, 3, 3), gemColor);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 3, rect.Y, 3, 3), gemColor);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - 3, 3, 3), gemColor);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 3, rect.Bottom - 3, 3, 3), gemColor);
			}
			else if (Augment.Rarity == AugmentRarity.Legendary)
			{
				// 4 Royal Gold Corner Brackets (8x2 and 2x8 L-shapes)
				Color cornerGold = new Color(255, 225, 85);
				// Top-Left
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, 8, 2), cornerGold);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, 2, 8), cornerGold);
				// Top-Right
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 8, rect.Y, 8, 2), cornerGold);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 2, rect.Y, 2, 8), cornerGold);
				// Bottom-Left
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - 2, 8, 2), cornerGold);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - 8, 2, 8), cornerGold);
				// Bottom-Right
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 8, rect.Bottom - 2, 8, 2), cornerGold);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 2, rect.Bottom - 8, 2, 8), cornerGold);
			}
		}

		private void DrawIconHalo(SpriteBatch spriteBatch, int iconBoxX, int iconBoxY, int iconBoxSize, float pulse)
		{
			int cx = iconBoxX + iconBoxSize / 2;
			int cy = iconBoxY + iconBoxSize / 2;

			if (Augment.Rarity == AugmentRarity.Legendary)
			{
				AugmentSlotElement.DrawStarSparkle(spriteBatch, cx, cy, 0.45f + pulse * 0.35f);
			}
			else if (Augment.Rarity == AugmentRarity.Epic)
			{
				int haloSize = (int)(22 + pulse * 8);
				Rectangle haloRect = new Rectangle(cx - haloSize / 2, cy - haloSize / 2, haloSize, haloSize);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, haloRect, new Color(195, 115, 255) * (0.20f + pulse * 0.20f));
			}
		}

		private void DrawRarityBadge(SpriteBatch spriteBatch, DynamicSpriteFont font, Rectangle rect, int pillY, Color borderColor, float pulse)
		{
			string classLabel = Augment.Class.ToString().ToUpper();
			string rarityLabel = Augment.Rarity.ToString().ToUpper();
			string badgeText = $"{rarityLabel}  •  {classLabel}";
			Vector2 badgeTextSize = ChatManager.GetStringSize(font, badgeText, new Vector2(0.68f));

			if (RarityStarAsset == null)
				RarityStarAsset = ModContent.Request<Texture2D>("Augments/UI/RarityStar", AssetRequestMode.ImmediateLoad);

			int starCount = Augment.Rarity switch
			{
				AugmentRarity.Common => 1,
				AugmentRarity.Rare => 2,
				AugmentRarity.Epic => 3,
				AugmentRarity.Legendary => 4,
				_ => 1
			};

			float starW = 12f;
			float starSpacing = 2f;
			float totalStarsW = (starCount * starW) + ((starCount - 1) * starSpacing);
			float totalBadgeW = badgeTextSize.X + totalStarsW + 18f;

			int pillW = (int)totalBadgeW;
			int pillH = 18;
			int pillX = rect.X + (rect.Width - pillW) / 2;
			Rectangle pillRect = new Rectangle(pillX, pillY, pillW, pillH);

			// Pill background & border
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, pillRect, new Color(10, 14, 28) * 0.9f);
			DrawRectBorder(spriteBatch, pillRect, borderColor * 0.55f, 1);

			// Draw Stars
			float currentX = pillRect.X + 8f;
			float starY = pillRect.Y + (pillH - starW) * 0.5f;

			if (RarityStarAsset?.IsLoaded == true)
			{
				Texture2D starTex = RarityStarAsset.Value;
				float scale = starW / starTex.Width;
				for (int s = 0; s < starCount; s++)
				{
					Color starCol = borderColor;
					if (Augment.Rarity == AugmentRarity.Legendary)
						starCol = Color.Lerp(new Color(255, 215, 80), Color.White, pulse * 0.4f);

					spriteBatch.Draw(starTex, new Vector2(currentX, starY), null, starCol, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
					currentX += starW + starSpacing;
				}
			}

			// Draw Badge Text
			currentX += 4f;
			Vector2 textPos = new Vector2(currentX, pillRect.Y + (pillH - badgeTextSize.Y) * 0.5f);
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, badgeText, textPos, borderColor, 0f, Vector2.Zero, new Vector2(0.68f));
		}

		private void DrawProceduralSparkles(SpriteBatch spriteBatch, Rectangle rect, float time)
		{
			if (Augment.Rarity == AugmentRarity.Legendary)
			{
				(float relX, float relY, float speed, float phase)[] legSparks =
				{
					(0.20f, 0.08f, 2.2f, 0.0f),
					(0.80f, 0.09f, 2.5f, 1.4f),
					(0.12f, 0.45f, 1.9f, 2.8f),
					(0.88f, 0.52f, 2.3f, 0.9f),
					(0.24f, 0.88f, 2.7f, 3.7f),
					(0.76f, 0.85f, 2.1f, 2.1f),
				};

				foreach (var sp in legSparks)
				{
					float intensity = (float)Math.Sin(time * sp.speed + sp.phase);
					if (intensity > 0.15f)
					{
						int sx = rect.X + (int)(rect.Width * sp.relX);
						int sy = rect.Y + (int)(rect.Height * sp.relY);
						AugmentSlotElement.DrawStarSparkle(spriteBatch, sx, sy, intensity);
					}
				}
			}
			else if (Augment.Rarity == AugmentRarity.Epic)
			{
				(float relX, float relY, float speed, float phase)[] epicSparks =
				{
					(0.22f, 0.10f, 2.0f, 0.5f),
					(0.78f, 0.11f, 2.4f, 1.8f),
					(0.15f, 0.48f, 2.2f, 3.1f),
					(0.85f, 0.60f, 1.8f, 0.2f),
					(0.30f, 0.86f, 2.6f, 2.5f),
				};

				foreach (var sp in epicSparks)
				{
					float intensity = (float)Math.Sin(time * sp.speed + sp.phase);
					if (intensity > 0.2f)
					{
						int sx = rect.X + (int)(rect.Width * sp.relX);
						int sy = rect.Y + (int)(rect.Height * sp.relY);
						DrawAmethystSparkle(spriteBatch, sx, sy, intensity);
					}
				}
			}
		}

		private static void DrawAmethystSparkle(SpriteBatch spriteBatch, int cx, int cy, float intensity)
		{
			if (intensity <= 0.05f) return;

			int rayLen = 2 + (int)(intensity * 5f);
			Color violetRay = new Color(215, 140, 255) * (intensity * 0.9f);
			Color whiteRay = Color.White * intensity;

			// Cross rays
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx, cy - rayLen, 1, rayLen * 2 + 1), violetRay);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - rayLen, cy, rayLen * 2 + 1, 1), violetRay);

			// 8-point spikes when intensity > 0.6f
			if (intensity > 0.6f)
			{
				int d = (int)(rayLen * 0.6f);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - d, cy - d, 1, 1), violetRay);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx + d, cy - d, 1, 1), violetRay);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - d, cy + d, 1, 1), violetRay);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx + d, cy + d, 1, 1), violetRay);
			}

			// Inner white core
			int innerRay = (int)(rayLen * 0.4f);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx, cy - innerRay, 1, innerRay * 2 + 1), whiteRay);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - innerRay, cy, innerRay * 2 + 1, 1), whiteRay);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - 1, cy - 1, 3, 3), Color.White * (intensity * 0.95f));
		}

		private void DrawSpecialBadges(SpriteBatch spriteBatch, DynamicSpriteFont font, Rectangle rect)
		{
			specialTagRect = Rectangle.Empty;

			if (isKeystone)
			{
				DrawPillBadge(spriteBatch, font, rect, rect.Bottom - 58, KeystoneTagText, KeystoneTagColor, new Color(50, 18, 18) * 0.9f);
			}
			else if (isSupport)
			{
				specialTagRect = DrawPillBadge(spriteBatch, font, rect, rect.Bottom - 58, SupportTagText, SupportTagColor, new Color(16, 46, 26) * 0.9f);
			}
			else if (isFortuneThemed)
			{
				specialTagRect = DrawPillBadge(spriteBatch, font, rect, rect.Bottom - 58, FortuneTagText, FortuneTagColor, new Color(50, 40, 15) * 0.9f);
			}
		}

		private Rectangle DrawPillBadge(SpriteBatch spriteBatch, DynamicSpriteFont font, Rectangle cardRect, int y, string text, Color textColor, Color bgColor)
		{
			Vector2 textSize = ChatManager.GetStringSize(font, text, new Vector2(0.66f));
			int pillW = (int)textSize.X + 16;
			int pillH = 20;
			int pillX = cardRect.X + (cardRect.Width - pillW) / 2;
			Rectangle pillRect = new Rectangle(pillX, y, pillW, pillH);

			spriteBatch.Draw(TextureAssets.MagicPixel.Value, pillRect, bgColor);
			DrawRectBorder(spriteBatch, pillRect, textColor * 0.7f, 1);

			Vector2 textPos = new Vector2(pillRect.X + (pillRect.Width - textSize.X) * 0.5f, pillRect.Y + (pillRect.Height - textSize.Y) * 0.5f);
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, textPos, textColor, 0f, Vector2.Zero, new Vector2(0.66f));

			return pillRect;
		}

		private void DrawInstallActionBar(SpriteBatch spriteBatch, DynamicSpriteFont font, Rectangle rect, Color borderColor)
		{
			int barH = 24;
			int barW = rect.Width - 30;
			int barX = rect.X + 15;
			int barY = rect.Bottom - 32;
			Rectangle barRect = new Rectangle(barX, barY, barW, barH);

			Color barBg = isHovered ? (Augment.Rarity switch
			{
				AugmentRarity.Legendary => new Color(75, 52, 18) * 0.95f,
				AugmentRarity.Epic => new Color(55, 28, 80) * 0.95f,
				AugmentRarity.Rare => new Color(24, 52, 85) * 0.95f,
				_ => new Color(35, 45, 70) * 0.95f
			}) : new Color(14, 18, 36) * 0.75f;

			Color barBorder = isHovered ? borderColor : borderColor * 0.4f;

			spriteBatch.Draw(TextureAssets.MagicPixel.Value, barRect, barBg);
			DrawRectBorder(spriteBatch, barRect, barBorder, 1);

			string installText = isHovered ? "▶  CLICK TO INSTALL  ◀" : "Click to select";
			Vector2 installScale = new Vector2(isHovered ? 0.72f : 0.65f);
			Vector2 installSize = ChatManager.GetStringSize(font, installText, installScale);
			Vector2 installPos = new Vector2(barRect.X + (barRect.Width - installSize.X) * 0.5f, barRect.Y + (barRect.Height - installSize.Y) * 0.5f);
			Color installColor = isHovered ? Color.White : new Color(130, 148, 180);
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, installText, installPos, installColor, 0f, Vector2.Zero, installScale);
		}

		private static void DrawRectBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int width)
		{
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, rect.Width, width), color);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - width, rect.Width, width), color);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, width, rect.Height), color);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - width, rect.Y, width, rect.Height), color);
		}

		private static float DrawCenteredLines(SpriteBatch spriteBatch, DynamicSpriteFont font, List<string> lines, Vector2 scale, float centerX, float y, Color color)
		{
			foreach (var line in lines)
			{
				Vector2 size = ChatManager.GetStringSize(font, line, scale);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch,
					font,
					line,
					new Vector2(centerX - size.X / 2f, y),
					color,
					0f,
					Vector2.Zero,
					scale
				);
				y += size.Y + LineSpacing;
			}
			return y;
		}

		private static float DrawLeftAlignedLines(SpriteBatch spriteBatch, DynamicSpriteFont font, List<string> lines, Vector2 scale, float x, float y)
		{
			foreach (var line in lines)
			{
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch,
					font,
					line,
					new Vector2(x, y),
					Color.White,
					0f,
					Vector2.Zero,
					scale
				);
				y += ChatManager.GetStringSize(font, line, scale).Y + LineSpacing;
			}
			return y;
		}

		internal static void DrawSupportTooltip(SpriteBatch spriteBatch, DynamicSpriteFont font)
		{
			const float padding = 10f;
			const float lineSpacing = 2f;

			var lines = new (string Text, Color Color)[]
			{
				("Support Stance",                        SupportTagColor),
				("2 chips: -30% damage, +20 defense",     Color.White),
				("3 chips: -23% damage, +30 defense",     Color.White),
				("4 chips: -16% damage, +40 defense",     Color.White),
				("5 chips: -5% damage, +60 defense",      Color.White),
			};

			var scale = Vector2.One;
			float maxWidth = 0f;
			float totalHeight = 0f;
			foreach (var (text, _) in lines)
			{
				Vector2 size = ChatManager.GetStringSize(font, text, scale);
				if (size.X > maxWidth) maxWidth = size.X;
				totalHeight += size.Y + lineSpacing;
			}
			totalHeight -= lineSpacing;

			float boxWidth = maxWidth + padding * 2f;
			float boxHeight = totalHeight + padding * 2f;

			Vector2 boxPos = new Vector2(
				Main.MouseScreen.X - 24f - boxWidth,
				Main.MouseScreen.Y + 24f
			);

			var boxRect = new Rectangle((int)boxPos.X, (int)boxPos.Y, (int)boxWidth, (int)boxHeight);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, boxRect, new Color(16, 22, 44) * 0.96f);
			DrawRectBorder(spriteBatch, boxRect, SupportTagColor * 0.7f, 2);

			float y = boxRect.Y + padding;
			float x = boxRect.X + padding;
			foreach (var (text, color) in lines)
			{
				Vector2 size = ChatManager.GetStringSize(font, text, scale);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, new Vector2(x, y), color, 0f, Vector2.Zero, scale);
				y += size.Y + lineSpacing;
			}
		}

		internal static void DrawFortuneTooltip(SpriteBatch spriteBatch, DynamicSpriteFont font)
		{
			const float padding = 10f;
			const float lineSpacing = 2f;

			var lines = new List<(string Text, Color Color)> { ("Fortune Family", FortuneTagColor) };
			foreach (var other in AugmentDatabase.All)
			{
				if (other.IsLuckyThemed)
					lines.Add((other.DisplayName, Color.White));
			}

			var scale = Vector2.One;
			float maxWidth = 0f;
			float totalHeight = 0f;
			foreach (var (text, _) in lines)
			{
				Vector2 size = ChatManager.GetStringSize(font, text, scale);
				if (size.X > maxWidth) maxWidth = size.X;
				totalHeight += size.Y + lineSpacing;
			}
			totalHeight -= lineSpacing;

			float boxWidth = maxWidth + padding * 2f;
			float boxHeight = totalHeight + padding * 2f;

			Vector2 boxPos = new Vector2(
				Main.MouseScreen.X - 24f - boxWidth,
				Main.MouseScreen.Y + 24f
			);

			var boxRect = new Rectangle((int)boxPos.X, (int)boxPos.Y, (int)boxWidth, (int)boxHeight);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, boxRect, new Color(16, 22, 44) * 0.96f);
			DrawRectBorder(spriteBatch, boxRect, FortuneTagColor * 0.7f, 2);

			float y = boxRect.Y + padding;
			float x = boxRect.X + padding;
			foreach (var (text, color) in lines)
			{
				Vector2 size = ChatManager.GetStringSize(font, text, scale);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, new Vector2(x, y), color, 0f, Vector2.Zero, scale);
				y += size.Y + lineSpacing;
			}
		}

		public static Color RarityColor(AugmentRarity rarity)
		{
			switch (rarity)
			{
				case AugmentRarity.Rare:
					return new Color(85, 195, 255);
				case AugmentRarity.Epic:
					return new Color(195, 115, 255);
				case AugmentRarity.Legendary:
					return new Color(255, 185, 45);
				default:
					return new Color(185, 200, 225);
			}
		}
	}
}
