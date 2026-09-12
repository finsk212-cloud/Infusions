using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
    // First pass: just the item shell needed to host the recolored icon -
    // no economy/shop wiring yet, that's a future pass.
    public class AugmentEssenceItem : ModItem
    {
        // TEMPORARY PLACEHOLDER ART: no mod-owned sprite exists yet, so this
        // points at the Soul of Light's own vanilla texture for anything the
        // draw hooks below don't cover. Replace with a real
        // "Items/AugmentEssenceItem" png (and drop this override and the
        // recolor logic below) once actual art exists.
        public override string Texture => "Terraria/Images/Item_" + ItemID.SoulofLight;

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.maxStack = 9999;
            Item.rare = ItemRarityID.Blue;
            Item.value = 0;
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            // The frame/origin/scale passed in here are all computed against
            // the placeholder Texture (the full multi-frame Soul of Light
            // sheet), since this item has no animation registered - vanilla
            // thinks the "single frame" is 4x taller than it really is, and
            // shrinks scale accordingly. Recompute everything from the real
            // cropped texture instead of trusting the stale values.
            Texture2D texture = AugmentEssenceRecolor.GetTexture();
            var sourceRect = texture.Bounds;
            var drawOrigin = new Vector2(sourceRect.Width * 0.5f, sourceRect.Height * 0.5f);
            float autoScale = AugmentEssenceRecolor.GetAutoFitScale(sourceRect.Width, sourceRect.Height);

            spriteBatch.Draw(texture, position, sourceRect, drawColor, 0f, drawOrigin, autoScale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = AugmentEssenceRecolor.GetTexture();
            var frame = texture.Bounds;
            var origin = new Vector2(frame.Width * 0.5f, frame.Height * 0.5f);
            float autoScale = AugmentEssenceRecolor.GetAutoFitScale(frame.Width, frame.Height);

            spriteBatch.Draw(texture, Item.position - Main.screenPosition + origin, frame, lightColor, rotation, origin, autoScale, SpriteEffects.None, 0f);
            return false;
        }

        public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
        {
            for (int i = tooltips.Count - 1; i >= 0; i--)
            {
                TooltipLine line = tooltips[i];
                if (line.Name == "ModName" && (line.Text.Contains("Augments") || line.Text.Contains("[Augments]")))
                {
                    tooltips.RemoveAt(i);
                    continue;
                }

                if (line.Text.Contains("[Augments]"))
                {
                    line.Text = line.Text.Replace("[Augments]", "").TrimEnd();
                }
            }
        }
    }

    // Recolors the Soul of Light's sprite to a saturated gold by shifting
    // every non-transparent pixel's hue/saturation to gold's while keeping
    // that pixel's own brightness (HSV "value") - this preserves the
    // sprite's existing highlight/shadow shape instead of flattening it to
    // one flat color. Built once on first use and cached for the session.
    //
    // HONEST CAVEAT: this technique doesn't need to guess the source
    // sprite's colors (unlike the Stylist recolor, which had to guess
    // anchor colors to match against) - it reads real brightness values at
    // runtime regardless of what they are. But it does throw away the
    // source's ORIGINAL saturation entirely, forcing full saturation
    // everywhere. Soul of Light is described as a pale, near-white-yellow
    // sprite, which likely means its brightest highlight pixels have low
    // original saturation (closer to white) specifically to read as a
    // "shine/glint" - this technique turns those into fully saturated solid
    // gold instead, which can flatten or lose that glint look rather than
    // just retinting it. Needs an actual in-game look to confirm whether
    // that's acceptable or whether saturation should be blended down
    // instead of forced to 100%.
    public static class AugmentEssenceRecolor
    {
        private const float MaxIconDimension = 32f;

        private static Texture2D recoloredTexture;

        // Mirrors vanilla's own inventory-icon auto-fit rule: only shrink
        // sprites bigger than a standard 32px slot icon, never scale small
        // ones up or down further than that.
        public static float GetAutoFitScale(int width, int height)
        {
            int largestDimension = System.Math.Max(width, height);
            return largestDimension > MaxIconDimension ? MaxIconDimension / largestDimension : 1f;
        }

        // Hue/saturation of target gold RGB (255, 215, 0).
        private const float GoldHue = 50.6f;
        private const float GoldSaturation = 1f;

        public static Texture2D GetTexture()
        {
            if (recoloredTexture != null)
                return recoloredTexture;

            Texture2D source = TextureAssets.Item[ItemID.SoulofLight].Value;

            // Soul of Light's texture is a vertical spritesheet (it has a
            // pulse/glow animation in vanilla) - only the first frame is the
            // actual single-item icon, the rest are the rest of that
            // animation stacked underneath it.
            int frameCount = Main.itemAnimations[ItemID.SoulofLight]?.FrameCount ?? 1;
            int frameHeight = source.Height / frameCount;

            var fullSheet = new Color[source.Width * source.Height];
            source.GetData(fullSheet);

            var pixels = new Color[source.Width * frameHeight];
            for (int y = 0; y < frameHeight; y++)
            {
                for (int x = 0; x < source.Width; x++)
                    pixels[y * source.Width + x] = fullSheet[y * source.Width + x];
            }

            for (int i = 0; i < pixels.Length; i++)
            {
                Color pixel = pixels[i];
                if (pixel.A == 0)
                    continue;

                float value = System.Math.Max(pixel.R, System.Math.Max(pixel.G, pixel.B)) / 255f;
                Color recolored = HsvToRgb(GoldHue, GoldSaturation, value);
                pixels[i] = new Color(recolored.R, recolored.G, recolored.B, pixel.A);
            }

            recoloredTexture = new Texture2D(Main.graphics.GraphicsDevice, source.Width, frameHeight);
            recoloredTexture.SetData(pixels);
            return recoloredTexture;
        }

        private static Color HsvToRgb(float h, float s, float v)
        {
            float c = v * s;
            float x = c * (1f - System.Math.Abs(h / 60f % 2f - 1f));
            float m = v - c;

            float r, g, b;
            if (h < 60f) { r = c; g = x; b = 0f; }
            else if (h < 120f) { r = x; g = c; b = 0f; }
            else if (h < 180f) { r = 0f; g = c; b = x; }
            else if (h < 240f) { r = 0f; g = x; b = c; }
            else if (h < 300f) { r = x; g = 0f; b = c; }
            else { r = c; g = 0f; b = x; }

            return new Color(
                (byte)Microsoft.Xna.Framework.MathHelper.Clamp((r + m) * 255f, 0f, 255f),
                (byte)Microsoft.Xna.Framework.MathHelper.Clamp((g + m) * 255f, 0f, 255f),
                (byte)Microsoft.Xna.Framework.MathHelper.Clamp((b + m) * 255f, 0f, 255f));
        }
    }
}
