using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace Augments
{
    // Small semi-transparent squares modeled after vanilla's buff icon row,
    // showing either a cooldown countdown or a live status value (e.g.
    // Adaptive Armor's growing defense bonus). Hovering reveals the augment
    // name via a tooltip box drawn the same way AugmentTooltipDrawer draws its.
    public static class AugmentCooldownDrawer
    {
        private const float IconSize = 36f;
        private const float Gap = 4f;
        private const float Scale = 0.7f;
        private const float TopOffset = 40f;

        // Digits have no descenders, so the font's measured line height leaves
        // empty space below the glyphs - nudge down to compensate so the text
        // looks centered instead of sitting in the upper half of the box.
        private const float VerticalCenterNudge = 0.15f;

        // Largest dimension an icon graphic is scaled down to, leaving a small
        // margin inside the box so the border stays visible around it.
        private const float IconGraphicMaxSize = 28f;

        private readonly struct StatusIcon
        {
            public readonly string Text;
            public readonly Color Color;
            public readonly string HoverText;
            public readonly Texture2D Icon;

            public StatusIcon(string text, Color color, string hoverText, Texture2D icon)
            {
                Text = text;
                Color = color;
                HoverText = hoverText;
                Icon = icon;
            }
        }

        public static void DrawCooldowns(SpriteBatch spriteBatch)
        {
            var augmentPlayer = Main.LocalPlayer.GetModPlayer<AugmentPlayer>();

            var icons = new List<StatusIcon>();
            foreach (var augment in augmentPlayer.Owned)
            {
                if (augment.CooldownRemaining > 0)
                {
                    string text = augment.CooldownDisplayInHours
                        ? HoursText(augment.CooldownRemaining)
                        : SecondsText(augment.CooldownRemaining);

                    icons.Add(new StatusIcon(
                        text,
                        AugmentTextColors.Cooldown,
                        $"{augment.DisplayName} is on cooldown",
                        augment.Icon));
                }

                if (augment.StatusValue is int value)
                {
                    icons.Add(new StatusIcon(
                        $"+{value}{augment.StatusValueSuffix}",
                        augment.StatusValueColor,
                        augment.DisplayName,
                        augment.Icon));
                }
            }

            // Cooldown icons for triggered Support protections received from an ally
            // when the local player does not directly own the augment itself.
            if (augmentPlayer.LifelineCooldown > 0 && !augmentPlayer.HasAugment("lifeline"))
            {
                var lifelineAugment = AugmentDatabase.GetById("lifeline");
                icons.Add(new StatusIcon(
                    SecondsText(augmentPlayer.LifelineCooldown),
                    AugmentTextColors.Cooldown,
                    "Lifeline is on cooldown",
                    lifelineAugment?.Icon));
            }

            if (augmentPlayer.LastRitesCooldown > 0 && !augmentPlayer.HasAugment("last_rites"))
            {
                var lastRitesAugment = AugmentDatabase.GetById("last_rites");
                icons.Add(new StatusIcon(
                    SecondsText(augmentPlayer.LastRitesCooldown),
                    AugmentTextColors.Cooldown,
                    "Last Rites is on cooldown",
                    lastRitesAugment?.Icon));
            }

            // Icons for aura effects received from a nearby Support player.
            // Warcry: the ModBuff being active is the source of truth.
            // Ironclad Aura: the flag is set each UpdateEquips tick by the pull loop.
            if (Main.LocalPlayer.FindBuffIndex(ModContent.BuffType<WarCryBuff>()) >= 0)
            {
                icons.Add(new StatusIcon(
                    "+10%",
                    AugmentTextColors.BonusDamage,
                    "Warcry: +10% damage from nearby ally",
                    null));
            }

            if (augmentPlayer.ReceivedIroncladAura)
            {
                icons.Add(new StatusIcon(
                    "+8",
                    AugmentTextColors.SpecialDamage,
                    "Ironclad Aura: +8 defense from nearby ally",
                    null));
            }

            if (augmentPlayer.ReceivedSwiftnessAura)
            {
                icons.Add(new StatusIcon(
                    "+15%",
                    AugmentTextColors.MovementSpeed,
                    "Swiftness Aura: +15% movement speed from nearby ally",
                    null));
            }

            if (augmentPlayer.ReceivedManaWell)
            {
                icons.Add(new StatusIcon(
                    "+30%",
                    AugmentTextColors.Mana,
                    "Mana Well: 30% faster mana regen from nearby ally",
                    null));
            }

            if (icons.Count == 0)
                return;

            var font = FontAssets.MouseText.Value;
            var scale = new Vector2(Scale);

            float totalWidth = icons.Count * IconSize + Gap * (icons.Count - 1);
            float x = Main.screenWidth / 2f - totalWidth / 2f;

            string hoveredText = null;
            Point mouse = new Point(Main.mouseX, Main.mouseY);

            for (int i = 0; i < icons.Count; i++)
            {
                var boxRect = new Rectangle((int)x, (int)TopOffset, (int)IconSize, (int)IconSize);

                DrawIcon(spriteBatch, font, scale, icons[i].Text, icons[i].Color, icons[i].Icon, boxRect);

                if (boxRect.Contains(mouse))
                    hoveredText = icons[i].HoverText;

                x += IconSize + Gap;
            }

            if (hoveredText != null)
                DrawHoverTooltip(spriteBatch, font, hoveredText);
        }

        private static void DrawIcon(SpriteBatch spriteBatch, DynamicSpriteFont font, Vector2 scale, string text, Color textColor, Texture2D icon, Rectangle boxRect)
        {
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, boxRect, new Color(20, 25, 50) * 0.4f);

            const int border = 1;
            Color borderColor = Color.White * 0.3f;
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(boxRect.X, boxRect.Y, boxRect.Width, border), borderColor);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(boxRect.X, boxRect.Bottom - border, boxRect.Width, border), borderColor);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(boxRect.X, boxRect.Y, border, boxRect.Height), borderColor);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(boxRect.Right - border, boxRect.Y, border, boxRect.Height), borderColor);

            if (icon != null)
            {
                float iconScale = IconGraphicMaxSize / Math.Max(icon.Width, icon.Height);
                Vector2 iconSize = new Vector2(icon.Width, icon.Height) * iconScale;
                Vector2 iconPos = new Vector2(
                    boxRect.X + (boxRect.Width - iconSize.X) / 2f,
                    boxRect.Y + (boxRect.Height - iconSize.Y) / 2f
                );

                spriteBatch.Draw(icon, iconPos, null, Color.White, 0f, Vector2.Zero, iconScale, SpriteEffects.None, 0f);
            }

            Vector2 textSize = ChatManager.GetStringSize(font, text, scale);
            Vector2 textPos = new Vector2(
                boxRect.X + (boxRect.Width - textSize.X) / 2f,
                boxRect.Y + (boxRect.Height - textSize.Y) / 2f + textSize.Y * VerticalCenterNudge
            );

            ChatManager.DrawColorCodedStringWithShadow(
                spriteBatch, font, text, textPos,
                textColor, 0f, Vector2.Zero, scale
            );
        }

        // Same background box + border + ChatManager text draw approach as
        // AugmentTooltipDrawer, just for a single line near the cursor.
        private static void DrawHoverTooltip(SpriteBatch spriteBatch, DynamicSpriteFont font, string text)
        {
            const float padding = 10f;

            Vector2 textSize = ChatManager.GetStringSize(font, text, Vector2.One);
            float boxWidth = textSize.X + padding * 2f;
            float boxHeight = textSize.Y + padding * 2f;

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

            ChatManager.DrawColorCodedStringWithShadow(
                spriteBatch, font, text,
                new Vector2(boxRect.X + padding, boxRect.Y + padding),
                Color.White, 0f, Vector2.Zero, Vector2.One
            );
        }

        private static string SecondsText(int cooldownRemainingTicks)
        {
            int seconds = (int)Math.Ceiling(cooldownRemainingTicks / 60f);
            return $"{seconds}s";
        }

        private static string HoursText(int cooldownRemainingTicks)
        {
            int hours = (int)Math.Ceiling(cooldownRemainingTicks / 3600f);
            return $"{hours}";
        }
    }
}
