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
using Augments.Core;

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

		private const string KeystoneTagText = "CORE OVERRIDE • UNIQUE";
		private const string SupportTagText = "SUPPORT CLASS";

		private static readonly Color KeystoneTagColor = new Color(248, 113, 113);
		private static readonly Color SupportTagColor = new Color(74, 222, 128);

		// Animation timings & parameters
		private static readonly float[] PulseSpeeds = { 1.0f, 1.8f, 2.6f, 3.4f };

		private readonly int cardIndex;
		private readonly List<string> nameLines;
		private readonly List<string> descLines;
		private readonly bool isKeystone;
		private readonly bool isSupport;
		private readonly Color baseBorderColor;
		private readonly float pulseSpeed;
		private float pulseTimer;
		private bool isHovered;

		// Stored bounds of the bottom tags for mouse hover check
		private Rectangle specialTagRect;
		private Rectangle familyTagRect;

		public AugmentChoiceCard(Augment augment, float width, int cardIndex = 0)
		{
			Augment = augment;
			this.cardIndex = cardIndex;
			isKeystone = augment.KeystoneFamily != null;
			isSupport = augment.Class == AugmentClass.Support;

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
			// 5. CLASS ICON (Larger, no box border, no black background)
			// =================================================================
			int cx = rect.X + rect.Width / 2;
			int cy = rect.Y + 44;

			// Draw Class Icon (Larger: 52x52)
			Texture2D iconTex = AugmentSlotElement.GetClassIcon(Augment.Class);
			if (iconTex != null)
			{
				float iconScale = Math.Min(52f / iconTex.Width, 52f / iconTex.Height);
				Vector2 iconPos = new Vector2(
					cx - iconTex.Width * iconScale * 0.5f,
					cy - iconTex.Height * iconScale * 0.5f
				);
				Color iconColor = isHovered ? Color.White : rarityColor;
				if (Augment.Rarity == AugmentRarity.Common) iconColor = new Color(225, 230, 240);
				spriteBatch.Draw(iconTex, iconPos, null, iconColor, 0f, Vector2.Zero, iconScale, SpriteEffects.None, 0f);
			}

			// =================================================================
			// 6. AUGMENT DISPLAY NAME (Directly under icon, no star/class pill)
			// =================================================================
			float nameY = cy + 34f;
			float centerX = rect.X + rect.Width * 0.5f;
			Color nameColor = isHovered ? Color.Lerp(rarityColor, Color.White, 0.45f) : rarityColor;
			nameY = DrawCenteredLines(spriteBatch, font, nameLines, NameScale, centerX, nameY, nameColor);

			// =================================================================
			// 7. GLOWING HEADER DIVIDER LINE
			// =================================================================
			float divY = Math.Max(nameY + 6f, rect.Y + 118f);
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
			if (familyTagRect.Contains(Main.MouseScreen.ToPoint()))
			{
				var family = AugmentFamilyRegistry.Get(Augment.FamilyId);
				if (family != null)
					DrawFamilyTooltip(spriteBatch, font, family);
			}
			else if (specialTagRect.Contains(Main.MouseScreen.ToPoint()))
			{
				if (isSupport)
					DrawSupportTooltip(spriteBatch, font);
				else if (isKeystone)
					DrawKeystoneTooltip(spriteBatch, font);
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
				// Soft pulsing Cyan Edge Glow
				int auraDist = (int)(2f + pulse * 2.5f) + (isHovered ? 2 : 0);
				Rectangle auraRect = new Rectangle(rect.X - auraDist, rect.Y - auraDist, rect.Width + auraDist * 2, rect.Height + auraDist * 2);
				Color auraCol = new Color(60, 180, 255) * ((0.08f + pulse * 0.10f) * (isHovered ? 1.4f : 1f));
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, auraRect, auraCol);
			}
		}

		private void DrawHoloSweep(SpriteBatch spriteBatch, Rectangle rect, float time)
		{
			if (Augment.Rarity != AugmentRarity.Legendary)
				return;

			float sweepPeriod = 3.5f;
			float sweepTime = time % sweepPeriod;
			float sweepDuration = 1.6f;

			if (sweepTime < sweepDuration)
			{
				float t = sweepTime / sweepDuration;
				float sweepCenter = (rect.Width + rect.Height) * t;
				int beamWidth = 28;
				Color beamColor = new Color(255, 245, 215);

				for (int py = 4; py < rect.Height - 4; py += 3)
				{
					int centerPx = (int)(sweepCenter - py);
					int startPx = Math.Max(3, centerPx - beamWidth / 2);
					int endPx = Math.Min(rect.Width - 3, centerPx + beamWidth / 2);
					if (endPx > startPx)
					{
						float dist = Math.Abs((startPx + endPx) * 0.5f - centerPx);
						float beamA = (1f - dist / (beamWidth * 0.6f)) * 0.24f;
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
			else if (Augment.Rarity == AugmentRarity.Rare)
				color = Color.Lerp(new Color(75, 180, 250), new Color(140, 225, 255), pulse * 0.35f);

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
			if (Augment.Rarity == AugmentRarity.Rare)
			{
				// 2 Delicate Cyan Corner Accents (top-right & bottom-left, 3x3)
				Color cornerCyan = new Color(130, 220, 255) * (0.65f + pulse * 0.35f);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 3, rect.Y, 3, 3), cornerCyan);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - 3, 3, 3), cornerCyan);
			}
			else if (Augment.Rarity == AugmentRarity.Epic)
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
			familyTagRect = Rectangle.Empty;

			bool hasSpecialTag = isKeystone || isSupport;
			bool hasFamily = Augment.FamilyId != null;

			if (hasSpecialTag && hasFamily)
			{
				int yFamily = rect.Bottom - 84;
				int ySpecial = rect.Bottom - 58;

				var fam = AugmentFamilyRegistry.Get(Augment.FamilyId);
				if (fam != null)
				{
					string famText = $"{fam.DisplayName.ToUpper()} PROTOCOL";
					familyTagRect = DrawProtocolTextBadge(spriteBatch, font, rect, yFamily, famText, fam.ThemeColor);
				}

				if (isKeystone)
					specialTagRect = DrawPillBadge(spriteBatch, font, rect, ySpecial, KeystoneTagText, KeystoneTagColor, Color.Transparent);
				else if (isSupport)
					specialTagRect = DrawPillBadge(spriteBatch, font, rect, ySpecial, SupportTagText, SupportTagColor, Color.Transparent);
			}
			else if (hasFamily)
			{
				int yFamily = rect.Bottom - 54;
				var fam = AugmentFamilyRegistry.Get(Augment.FamilyId);
				if (fam != null)
				{
					string famText = $"{fam.DisplayName.ToUpper()} PROTOCOL";
					familyTagRect = DrawProtocolTextBadge(spriteBatch, font, rect, yFamily, famText, fam.ThemeColor);
				}
			}
			else if (hasSpecialTag)
			{
				int ySpecial = rect.Bottom - 60;
				if (isKeystone)
					specialTagRect = DrawPillBadge(spriteBatch, font, rect, ySpecial, KeystoneTagText, KeystoneTagColor, Color.Transparent);
				else if (isSupport)
					specialTagRect = DrawPillBadge(spriteBatch, font, rect, ySpecial, SupportTagText, SupportTagColor, Color.Transparent);
			}
		}

		private Rectangle DrawProtocolTextBadge(SpriteBatch spriteBatch, DynamicSpriteFont font, Rectangle cardRect, int y, string text, Color themeColor)
		{
			Vector2 scale = new Vector2(0.66f);
			Vector2 textSize = ChatManager.GetStringSize(font, text, scale);
			float maxTextWidth = cardRect.Width - 24f;
			if (textSize.X > maxTextWidth)
			{
				float fitFactor = maxTextWidth / textSize.X;
				scale *= fitFactor;
				textSize = ChatManager.GetStringSize(font, text, scale);
			}

			int textX = cardRect.X + (int)((cardRect.Width - textSize.X) * 0.5f);
			int textY = y;

			// Hitbox for mouse hover tooltip (with 8px horizontal, 4px vertical padding for easy hovering)
			Rectangle hitRect = new Rectangle(textX - 8, textY - 2, (int)textSize.X + 16, (int)textSize.Y + 4);
			bool isTagHovered = hitRect.Contains(Main.MouseScreen.ToPoint());
			Color drawColor = isTagHovered ? Color.Lerp(themeColor, Color.White, 0.45f) : themeColor;

			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch,
				font,
				text,
				new Vector2(textX, textY),
				drawColor,
				0f,
				Vector2.Zero,
				scale
			);

			return hitRect;
		}

		private Rectangle DrawPillBadge(SpriteBatch spriteBatch, DynamicSpriteFont font, Rectangle cardRect, int y, string text, Color accentColor, Color _)
		{
			Vector2 scale = new Vector2(0.70f);
			Vector2 textSize = ChatManager.GetStringSize(font, text, scale);
			int pillW = (int)textSize.X + 24;
			int pillH = 24;
			int pillX = cardRect.X + (cardRect.Width - pillW) / 2;
			Rectangle pillRect = new Rectangle(pillX, y, pillW, pillH);

			// Clean dark sci-fi navy background
			Color bgNavy = new Color(12, 18, 34) * 0.95f;
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, pillRect, bgNavy);

			// Subtle colored underglow fill
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, pillRect, accentColor * 0.12f);

			// Top & Bottom subtle rail lines
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(pillRect.X + 4, pillRect.Y, pillRect.Width - 8, 1), accentColor * 0.55f);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(pillRect.X + 4, pillRect.Bottom - 1, pillRect.Width - 8, 1), accentColor * 0.55f);

			// High-tech corner bracket notches (left & right edge caps)
			// Left bracket: full height vertical line + top/bottom corner hooks
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(pillRect.X, pillRect.Y, 2, pillRect.Height), accentColor * 0.85f);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(pillRect.X, pillRect.Y, 5, 2), accentColor * 0.85f);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(pillRect.X, pillRect.Bottom - 2, 5, 2), accentColor * 0.85f);

			// Right bracket: full height vertical line + top/bottom corner hooks
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(pillRect.Right - 2, pillRect.Y, 2, pillRect.Height), accentColor * 0.85f);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(pillRect.Right - 5, pillRect.Y, 5, 2), accentColor * 0.85f);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(pillRect.Right - 5, pillRect.Bottom - 2, 5, 2), accentColor * 0.85f);

			// Text centered with shadow (+4f optical offset for MouseText font line-height/leading)
			float textY = pillRect.Y + (pillRect.Height - textSize.Y) * 0.5f + 4f;
			Vector2 textPos = new Vector2(pillRect.X + (pillRect.Width - textSize.X) * 0.5f, textY);
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, textPos, accentColor, 0f, Vector2.Zero, scale);

			return pillRect;
		}

		private void DrawInstallActionBar(SpriteBatch spriteBatch, DynamicSpriteFont font, Rectangle rect, Color borderColor)
		{
			float textY = rect.Bottom - 26f;
			var ap = Main.LocalPlayer?.GetModPlayer<AugmentPlayer>();
			bool slotsFull = ap != null && ap.Owned.Count >= AugmentPlayer.MaxOwnedAugments;
			int coreRefund = AugmentPlayer.GetRewardRefund(Augment.Rarity);

			string installText;
			Color installColor;

			if (slotsFull)
			{
				installText = isHovered ? $"▶  TRANSFER (+{coreRefund} CORE{(coreRefund > 1 ? "S" : "")})  ◀" : $"Transfer to 2B (+{coreRefund} Core{(coreRefund > 1 ? "s" : "")})";
				installColor = isHovered ? new Color(255, 175, 120) : new Color(225, 120, 110) * 0.9f;
			}
			else
			{
				installText = isHovered ? "▶  CLICK TO INSTALL  ◀" : "Click to select";
				installColor = isHovered ? Color.White : new Color(130, 150, 185) * 0.85f;
			}

			Vector2 installScale = new Vector2(isHovered ? 0.74f : 0.68f);
			Vector2 installSize = ChatManager.GetStringSize(font, installText, installScale);
			Vector2 installPos = new Vector2(rect.X + (rect.Width - installSize.X) * 0.5f, textY);
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

		internal static void DrawKeystoneTooltip(SpriteBatch spriteBatch, DynamicSpriteFont font)
		{
			const float padding = 12f;
			const float lineSpacing = 3f;

			var lines = new (string Text, Color Color)[]
			{
				("CORE OVERRIDE ARCHITECTURE", KeystoneTagColor),
				("Unique protocol that rewires chassis combat specifications.", new Color(220, 230, 245)),
				("• Limit 1 Core Override chip per chassis.", new Color(248, 113, 113)),
				("• Permanent installation — cannot be sold or removed.", new Color(200, 215, 235)),
				("• Grants massive combat power with operational trade-offs.", new Color(175, 190, 215))
			};

			var scale = new Vector2(0.80f);
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
				Math.Clamp(Main.MouseScreen.X - 24f - boxWidth, 10f, Main.screenWidth - boxWidth - 10f),
				Math.Clamp(Main.MouseScreen.Y - boxHeight / 2f, 10f, Main.screenHeight - boxHeight - 10f)
			);

			var boxRect = new Rectangle((int)boxPos.X, (int)boxPos.Y, (int)boxWidth, (int)boxHeight);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, boxRect, new Color(12, 18, 34) * 0.96f);
			DrawRectBorder(spriteBatch, boxRect, KeystoneTagColor * 0.8f, 2);

			float y = boxRect.Y + padding;
			float x = boxRect.X + padding;
			foreach (var (text, color) in lines)
			{
				Vector2 size = ChatManager.GetStringSize(font, text, scale);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, new Vector2(x, y), color, 0f, Vector2.Zero, scale);
				y += size.Y + lineSpacing;
			}
		}

		internal static void DrawSupportTooltip(SpriteBatch spriteBatch, DynamicSpriteFont font)
		{
			const float padding = 12f;
			const float lineSpacing = 3f;

			var lines = new (string Text, Color Color)[]
			{
				("SUPPORT STANCE",                        SupportTagColor),
				("2 chips: -30% damage, +20 defense",     new Color(220, 230, 245)),
				("3 chips: -23% damage, +30 defense",     new Color(220, 230, 245)),
				("4 chips: -16% damage, +40 defense",     new Color(220, 230, 245)),
				("5 chips: -5% damage, +60 defense",      new Color(220, 230, 245)),
			};

			var scale = new Vector2(0.80f);
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
				Math.Clamp(Main.MouseScreen.X - 24f - boxWidth, 10f, Main.screenWidth - boxWidth - 10f),
				Math.Clamp(Main.MouseScreen.Y - boxHeight / 2f, 10f, Main.screenHeight - boxHeight - 10f)
			);

			var boxRect = new Rectangle((int)boxPos.X, (int)boxPos.Y, (int)boxWidth, (int)boxHeight);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, boxRect, new Color(12, 18, 34) * 0.96f);
			DrawRectBorder(spriteBatch, boxRect, SupportTagColor * 0.8f, 2);

			float y = boxRect.Y + padding;
			float x = boxRect.X + padding;
			foreach (var (text, color) in lines)
			{
				Vector2 size = ChatManager.GetStringSize(font, text, scale);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, new Vector2(x, y), color, 0f, Vector2.Zero, scale);
				y += size.Y + lineSpacing;
			}
		}

		internal static void DrawFamilyTooltip(SpriteBatch spriteBatch, DynamicSpriteFont font, AugmentFamily family)
		{
			const float padding = 12f;
			const float lineSpacing = 3f;

			var player = Main.LocalPlayer;
			var ap = player?.GetModPlayer<AugmentPlayer>();
			int ownedCount = ap != null ? AugmentFamilyRegistry.GetOwnedCount(ap, family.Id) : 0;

			var lines = new List<(string Text, Color Color)>
			{
				($"{family.DisplayName.ToUpper()} PROTOCOL", family.ThemeColor),
				(family.Description, new Color(200, 220, 245)),
				($"Progress: {ownedCount}/{family.MaxMembers} Installed", ownedCount >= family.MaxMembers ? AugmentTextColors.Healing : new Color(150, 165, 190)),
			};

			if (family.Id == AugmentFamilyRegistry.FortuneId && ap != null)
			{
				lines.Add(($"Active Fortune: +{(int)System.MathF.Round(ap.TotalFortune * 100f)}%  •  World Luck: +{player.luck:0.00}", new Color(255, 220, 120)));
			}

			lines.Add(("─────────────────────────────", new Color(60, 80, 120) * 0.7f));
			lines.Add(("Protocol Specifications:", Color.White));

			foreach (var kv in family.ThresholdBonuses)
			{
				int threshold = kv.Key;
				var bonus = kv.Value;
				bool unlocked = ownedCount >= threshold;
				string header = unlocked
					? $"  ✓ ({threshold}) {bonus.Title} (Active)"
					: $"  • ({threshold}) {bonus.Title} (Locked)";
				Color headerCol = unlocked ? AugmentTextColors.Healing : new Color(160, 170, 185);
				lines.Add((header, headerCol));

				foreach (var dl in bonus.Descriptions)
				{
					lines.Add(($"      {dl.Trim()}", unlocked ? new Color(220, 245, 230) : new Color(125, 140, 160)));
				}
			}

			lines.Add(("─────────────────────────────", new Color(60, 80, 120) * 0.7f));
			lines.Add(("Assigned Plug-in Chips:", Color.White));

			foreach (var memberId in family.MemberIds)
			{
				Augment m = AugmentDatabase.GetById(memberId);
				string name = m?.DisplayName ?? memberId;
				bool owned = ap != null && ap.HasAugment(memberId);
				if (owned)
					lines.Add(($"  ✓ {name} (Installed)", AugmentTextColors.Healing));
				else
					lines.Add(($"  • {name}", new Color(175, 190, 215)));
			}

			var scale = new Vector2(0.80f);
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
				Math.Clamp(Main.MouseScreen.X - 24f - boxWidth, 10f, Main.screenWidth - boxWidth - 10f),
				Math.Clamp(Main.MouseScreen.Y - boxHeight / 2f, 10f, Main.screenHeight - boxHeight - 10f)
			);

			var boxRect = new Rectangle((int)boxPos.X, (int)boxPos.Y, (int)boxWidth, (int)boxHeight);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, boxRect, new Color(12, 18, 34) * 0.96f);
			DrawRectBorder(spriteBatch, boxRect, family.ThemeColor * 0.8f, 2);

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
