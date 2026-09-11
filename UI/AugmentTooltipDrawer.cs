using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace Augments
{
    public static class AugmentTooltipDrawer
    {
        private const float MaxTextWidth = 500f;

        public static void DrawIfHovering(SpriteBatch spriteBatch)
        {
            var augment = AugmentListEntry.HoveredAugment;
            if (augment == null)
                return;

            var rawLines = new List<(string text, Color color)>
            {
                (augment.DisplayName, AugmentListEntry.RarityColor(augment.Rarity)),
                (augment.Description, Color.White)
            };

            DrawTooltipBox(spriteBatch, rawLines);
        }

        private static void DrawTooltipBox(SpriteBatch spriteBatch, List<(string text, Color color)> rawLines)
        {
            var font = FontAssets.MouseText.Value;
            const float padding = 10f;
            const float lineSpacing = -2f;

            var drawLines = new List<(string text, Color color)>();

            foreach (var raw in rawLines)
            {
                foreach (var wrapped in WrapText(font, raw.text, MaxTextWidth))
                    drawLines.Add((wrapped, raw.color));
            }

            float contentWidth = 0f;
            float totalHeight = 0f;

            foreach (var line in drawLines)
            {
                Vector2 size = ChatManager.GetStringSize(font, line.text, Vector2.One);

                if (size.X > contentWidth)
                    contentWidth = size.X;

                totalHeight += size.Y + lineSpacing;
            }

            float boxWidth = contentWidth + padding * 2f;
            float boxHeight = totalHeight + padding * 2f;

            float posX = Main.MouseScreen.X + 18f;
            float posY = Main.MouseScreen.Y + 18f;

            if (posX + boxWidth > Main.screenWidth - 12f)
                posX = Main.MouseScreen.X - 18f - boxWidth;

            if (posY + boxHeight > Main.screenHeight - 12f)
                posY = Main.screenHeight - 12f - boxHeight;

            if (posX < 12f)
                posX = 12f;
            if (posY < 12f)
                posY = 12f;

            Vector2 boxPos = new Vector2(posX, posY);

            var boxRect = new Rectangle(
                (int)boxPos.X,
                (int)boxPos.Y,
                (int)boxWidth,
                (int)boxHeight
            );

            spriteBatch.Draw(TextureAssets.MagicPixel.Value, boxRect, new Color(20, 25, 50) * 0.95f);

            const int border = 2;
            Color borderColor = Color.White * 0.5f;
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(boxRect.X, boxRect.Y, boxRect.Width, border), borderColor);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(boxRect.X, boxRect.Bottom - border, boxRect.Width, border), borderColor);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(boxRect.X, boxRect.Y, border, boxRect.Height), borderColor);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(boxRect.Right - border, boxRect.Y, border, boxRect.Height), borderColor);

            float y = boxRect.Y + padding;

            foreach (var line in drawLines)
            {
                ChatManager.DrawColorCodedStringWithShadow(
                    spriteBatch,
                    font,
                    line.text,
                    new Vector2(boxRect.X + padding, y),
                    line.color,
                    0f,
                    Vector2.Zero,
                    Vector2.One
                );

                y += ChatManager.GetStringSize(font, line.text, Vector2.One).Y + lineSpacing;
            }
        }

        private static List<string> WrapText(DynamicSpriteFont font, string text, float maxWidth)
        {
            return AugmentColorText.Wrap(font, text, maxWidth, Vector2.One);
        }
    }
}
