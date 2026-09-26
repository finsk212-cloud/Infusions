using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Augments
{
    // Fixed-position tag drawn at the bottom of the augment list panel whenever
    // the local player has 1+ Support augments owned.
    // Hover reveals the Support Stance specification matrix table.
    public class SupportClassTagElement : UIElement
    {
        public bool IsVisible { get; private set; }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var ap = Main.LocalPlayer?.GetModPlayer<AugmentPlayer>();
            if (ap == null || ap.SupportAugmentCount < 1)
            {
                IsVisible = false;
                return;
            }

            IsVisible = true;
            CalculatedStyle dims = GetDimensions();
            Rectangle rect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);
            bool isHovered = IsMouseHovering || rect.Contains(Main.MouseScreen.ToPoint());

            int count = ap.SupportAugmentCount;
            bool active = count >= 2;
            Color accentColor = active ? new Color(74, 222, 128) : new Color(148, 163, 184);

            Texture2D pixel = TextureAssets.MagicPixel.Value;

            // 1. Ambient drop shadow (1px expansion)
            spriteBatch.Draw(pixel, new Rectangle(rect.X - 1, rect.Y - 1, rect.Width + 2, rect.Height + 2), new Color(0, 0, 0, 140));

            // 2. High-tech cyber navy background fill (#0A101C)
            Color bgNavy = isHovered ? new Color(16, 26, 44, 250) : new Color(10, 16, 28, 240);
            spriteBatch.Draw(pixel, rect, bgNavy);

            // 3. Subtle ambient accent underglow
            spriteBatch.Draw(pixel, rect, accentColor * (isHovered ? 0.08f : 0.035f));

            // 4. 1px inner hairline accent
            Color innerHairline = Color.White * (isHovered ? 0.08f : 0.04f);
            spriteBatch.Draw(pixel, new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, 1), innerHairline);
            spriteBatch.Draw(pixel, new Rectangle(rect.X + 1, rect.Bottom - 2, rect.Width - 2, 1), innerHairline);
            spriteBatch.Draw(pixel, new Rectangle(rect.X + 1, rect.Y + 1, 1, rect.Height - 2), innerHairline);
            spriteBatch.Draw(pixel, new Rectangle(rect.Right - 2, rect.Y + 1, 1, rect.Height - 2), innerHairline);

            // 5. 1px outer border
            Color border = accentColor * (isHovered ? 0.85f : 0.45f);
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), border);
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), border);
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), border);
            spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), border);

            // 6. Flush corner micro-notches (3x1 and 1x3)
            Color cornerCol = Color.Lerp(accentColor, Color.White, isHovered ? 0.50f : 0.30f);
            const int cLen = 3;
            // Top-left
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, cLen, 1), cornerCol);
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, cLen), cornerCol);
            // Top-right
            spriteBatch.Draw(pixel, new Rectangle(rect.Right - cLen, rect.Y, cLen, 1), cornerCol);
            spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, cLen), cornerCol);
            // Bottom-left
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, cLen, 1), cornerCol);
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - cLen, 1, cLen), cornerCol);
            // Bottom-right
            spriteBatch.Draw(pixel, new Rectangle(rect.Right - cLen, rect.Bottom - 1, cLen, 1), cornerCol);
            spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Bottom - cLen, 1, cLen), cornerCol);

            // 7. Typography
            string text = active
                ? (count >= 5 ? "SUPPORT STANCE  •  ACTIVE (MAX TIER)" : $"SUPPORT STANCE  •  ACTIVE ({count}/5 PLUGINS)")
                : $"SUPPORT STANCE  •  INACTIVE ({count}/2 REQUIRED)";

            var font = FontAssets.MouseText.Value;
            Vector2 scale = new Vector2(0.68f);
            Vector2 textSize = ChatManager.GetStringSize(font, text, scale);
            float textY = rect.Y + (rect.Height - 12f) * 0.5f - 1.5f;
            Vector2 textPos = new Vector2(rect.X + (rect.Width - textSize.X) * 0.5f, textY);

            Color textColor = isHovered ? Color.Lerp(accentColor, Color.White, 0.35f) : accentColor;
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, textPos, textColor, 0f, Vector2.Zero, scale);
        }
    }
}
