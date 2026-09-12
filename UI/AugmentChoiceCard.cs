using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Augments
{
	// One clickable "card" in the choice panel, representing a single augment option.
	// Text is drawn directly via ChatManager (same approach as AugmentTooltipDrawer)
	// instead of UIText, since UIText's built-in word-wrap doesn't treat
	// [c/RRGGBB:...] tags as atomic and can split them mid-tag, leaving raw
	// tag text on screen.
	public class AugmentChoiceCard : UIPanel
	{
		public Augment Augment { get; }
		public event Action<Augment> OnAugmentChosen;

		public const float CardPadding = 11f;
		private const float NameDescSpacing = 8f;
		private const float LineSpacing = -1f;
		public const float MinCardHeight = 370f;

		private static readonly Vector2 NameScale = new Vector2(0.9f);
		private static readonly Vector2 DescScale = new Vector2(0.8f);

		// Small warning tag shown above the name on any Keystone card, so the
		// "this is a permanent, mutually-exclusive choice" implication is
		// visible before the player ever clicks - the actual confirmation
		// gate lives in AugmentChoiceUIState, this is just the heads-up.
		private const string KeystoneTagText = "KEYSTONE - PERMANENT CHOICE";
		private const float KeystoneTagSpacing = 6f;
		private static readonly Vector2 KeystoneTagScale = new Vector2(0.6f);
		private static readonly Color KeystoneTagColor = new Color(220, 60, 60);

		// Tag drawn at the bottom of Support cards to communicate the class
		// identity before the player clicks. Hover reveals the stance table.
		private const string SupportTagText = "SUPPORT CLASS";
		private const float SupportTagSpacing = 8f;
		private static readonly Vector2 SupportTagScale = new Vector2(0.65f);
		private static readonly Color SupportTagColor = new Color(100, 220, 140);

		// Tag drawn at the bottom of Lucky-themed cards, same treatment as the
		// Support tag. Hover reveals the full Fortune family. No current
		// augment is both Support and Lucky-themed (all Lucky-themed ones are
		// AugmentClass.Universal), so the two tags never need to stack.
		private const string FortuneTagText = "FORTUNE";
		private const float FortuneTagSpacing = 8f;
		private static readonly Vector2 FortuneTagScale = new Vector2(0.65f);
		private static readonly Color FortuneTagColor = new Color(230, 190, 60);

		// Higher rarity = faster/brighter border pulse. Index matches AugmentRarity.
		private static readonly float[] PulseSpeed = { 0f, 1.6f, 2.4f, 3.4f };
		private static readonly float[] PulseStrength = { 0f, 0.18f, 0.32f, 0.5f };

		private readonly List<string> nameLines;
		private readonly List<string> descLines;
		private readonly bool isKeystone;
		private readonly bool isSupport;
		private readonly bool isFortuneThemed;
		private readonly Color baseBorderColor;
		private readonly float pulseSpeed;
		private readonly float pulseStrength;
		private float pulseTimer;

		public AugmentChoiceCard(Augment augment, float width)
		{
			Augment = augment;
			isKeystone = augment.KeystoneFamily != null;
			isSupport = augment.Class == AugmentClass.Support;
			isFortuneThemed = augment.IsLuckyThemed;

			baseBorderColor = RarityColor(augment.Rarity);
			BackgroundColor = baseBorderColor * 0.35f;
			BorderColor = baseBorderColor;

			int rarityIndex = (int)augment.Rarity;
			pulseSpeed = PulseSpeed[rarityIndex];
			pulseStrength = PulseStrength[rarityIndex];

			SetPadding(CardPadding);
			Width.Set(width, 0f);
			Height.Set(MinCardHeight, 0f);

			var font = FontAssets.MouseText.Value;
			// Derived from the card's own Width (just set above), not a
			// hardcoded/shared constant - this is what the wrap below and
			// the actual on-screen inner area both have to agree on.
			float contentWidth = Width.Pixels - PaddingLeft - PaddingRight;

			nameLines = AugmentColorText.Wrap(font, augment.DisplayName, contentWidth, NameScale);
			descLines = AugmentColorText.Wrap(font, augment.Description, contentWidth, DescScale);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			pulseTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			// 0..1 sine pulse; rarities with no pulseStrength stay flat at 0.
			float pulse = pulseStrength > 0f ? (float)Math.Sin(pulseTimer * pulseSpeed) * 0.5f + 0.5f : 0f;

			if (pulse > 0f)
			{
				CalculatedStyle dims = GetDimensions();
				float glowSize = 4f + pulse * pulseStrength * 18f;
				Rectangle glowRect = new Rectangle(
					(int)(dims.X - glowSize),
					(int)(dims.Y - glowSize),
					(int)(dims.Width + glowSize * 2f),
					(int)(dims.Height + glowSize * 2f));

				spriteBatch.Draw(TextureAssets.MagicPixel.Value, glowRect, baseBorderColor * (pulse * pulseStrength * 0.5f));
			}

			BorderColor = Color.Lerp(baseBorderColor, Color.White, pulse * pulseStrength);

			base.DrawSelf(spriteBatch);

			var font = FontAssets.MouseText.Value;
			CalculatedStyle inner = GetInnerDimensions();
			float centerX = inner.X + inner.Width / 2f;
			float y = inner.Y;

			if (isKeystone)
			{
				Vector2 tagSize = ChatManager.GetStringSize(font, KeystoneTagText, KeystoneTagScale);

				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch,
					font,
					KeystoneTagText,
					new Vector2(centerX - tagSize.X / 2f, y),
					KeystoneTagColor,
					0f,
					Vector2.Zero,
					KeystoneTagScale
				);

				y += tagSize.Y + KeystoneTagSpacing;
			}

			y = DrawCenteredLines(spriteBatch, font, nameLines, NameScale, centerX, y);
			y += NameDescSpacing;
			DrawLeftAlignedLines(spriteBatch, font, descLines, DescScale, inner.X, y);

			if (isSupport)
			{
				Vector2 tagSize = ChatManager.GetStringSize(font, SupportTagText, SupportTagScale);
				float tagY = inner.Y + inner.Height - (tagSize.Y + SupportTagSpacing);
				Vector2 tagPos = new Vector2(centerX - tagSize.X / 2f, tagY);

				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch,
					font,
					SupportTagText,
					tagPos,
					SupportTagColor,
					0f,
					Vector2.Zero,
					SupportTagScale
				);

				// NOTE: tooltip is drawn in DrawSelf, same depth as the card.
				// If another card renders after this one in the same container it
				// could paint over the tooltip box. Flag for review if observed.
				if (IsMouseHoveringRect(tagPos, tagSize))
					DrawSupportTooltip(spriteBatch, font);
			}

			if (isFortuneThemed)
			{
				Vector2 tagSize = ChatManager.GetStringSize(font, FortuneTagText, FortuneTagScale);
				float tagY = inner.Y + inner.Height - (tagSize.Y + FortuneTagSpacing);
				Vector2 tagPos = new Vector2(centerX - tagSize.X / 2f, tagY);

				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch,
					font,
					FortuneTagText,
					tagPos,
					FortuneTagColor,
					0f,
					Vector2.Zero,
					FortuneTagScale
				);

				// NOTE: tooltip is drawn in DrawSelf, same depth as the card.
				// If another card renders after this one in the same container it
				// could paint over the tooltip box. Flag for review if observed.
				if (IsMouseHoveringRect(tagPos, tagSize))
					DrawFortuneTooltip(spriteBatch, font);
			}
		}

		// Tag text is drawn directly via ChatManager (not a child UIElement),
		// so there's no built-in hover state for just the tag - check the
		// actual mouse screen position against the tag's own drawn bounds
		// instead of falling back to the whole card's IsMouseHovering.
		private static bool IsMouseHoveringRect(Vector2 pos, Vector2 size)
		{
			return Main.MouseScreen.X >= pos.X && Main.MouseScreen.X <= pos.X + size.X
				&& Main.MouseScreen.Y >= pos.Y && Main.MouseScreen.Y <= pos.Y + size.Y;
		}

		private static float DrawCenteredLines(SpriteBatch spriteBatch, DynamicSpriteFont font, List<string> lines, Vector2 scale, float centerX, float y)
		{
			foreach (var line in lines)
			{
				Vector2 size = ChatManager.GetStringSize(font, line, scale);

				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch,
					font,
					line,
					new Vector2(centerX - size.X / 2f, y),
					Color.White,
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

			spriteBatch.Draw(TextureAssets.MagicPixel.Value, boxRect, new Color(20, 25, 50) * 0.95f);

			const int border = 2;
			Color borderColor = Color.White * 0.5f;
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(boxRect.X, boxRect.Y, boxRect.Width, border), borderColor);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(boxRect.X, boxRect.Bottom - border, boxRect.Width, border), borderColor);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(boxRect.X, boxRect.Y, border, boxRect.Height), borderColor);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(boxRect.Right - border, boxRect.Y, border, boxRect.Height), borderColor);

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

			spriteBatch.Draw(TextureAssets.MagicPixel.Value, boxRect, new Color(20, 25, 50) * 0.95f);

			const int border = 2;
			Color borderColor = Color.White * 0.5f;
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(boxRect.X, boxRect.Y, boxRect.Width, border), borderColor);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(boxRect.X, boxRect.Bottom - border, boxRect.Width, border), borderColor);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(boxRect.X, boxRect.Y, border, boxRect.Height), borderColor);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(boxRect.Right - border, boxRect.Y, border, boxRect.Height), borderColor);

			float y = boxRect.Y + padding;
			float x = boxRect.X + padding;
			foreach (var (text, color) in lines)
			{
				Vector2 size = ChatManager.GetStringSize(font, text, scale);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, new Vector2(x, y), color, 0f, Vector2.Zero, scale);
				y += size.Y + lineSpacing;
			}
		}

		public override void LeftClick(UIMouseEvent evt)
		{
			base.LeftClick(evt);
			SoundEngine.PlaySound(SoundID.MenuTick);
			OnAugmentChosen?.Invoke(Augment);
		}

		public override void MouseOver(UIMouseEvent evt)
		{
			base.MouseOver(evt);
			BackgroundColor = RarityColor(Augment.Rarity) * 0.55f;
			SoundEngine.PlaySound(SoundID.MenuTick);
		}

		public override void MouseOut(UIMouseEvent evt)
		{
			base.MouseOut(evt);
			BackgroundColor = RarityColor(Augment.Rarity) * 0.35f;
		}

		private static Color RarityColor(AugmentRarity rarity)
		{
			switch (rarity)
			{
				case AugmentRarity.Rare:
					return Color.SkyBlue;
				case AugmentRarity.Epic:
					return Color.MediumPurple;
				case AugmentRarity.Legendary:
					return Color.Orange;
				default:
					return Color.White;
			}
		}
	}
}
