using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;
using Augments.Core;

namespace Augments
{
    public static class AugmentTooltipDrawer
    {
        private const float MaxContentWidth = 350f;
        private const float MinBoxWidth = 280f;
        private const float Padding = 12f;
        private const float LineSpacing = -2f;

        private static readonly Vector2 TitleScale = new Vector2(0.92f);
        private static readonly Vector2 BadgeScale = new Vector2(0.68f);
        private static readonly Vector2 DescScale = new Vector2(0.80f);
        private static readonly Vector2 MetaScale = new Vector2(0.74f);
        private static readonly Vector2 ProtocolScale = new Vector2(0.72f);
        private static readonly Vector2 BonusScale = new Vector2(0.70f);

        public static void DrawIfHovering(SpriteBatch spriteBatch)
        {
            var augment = AugmentListEntry.HoveredAugment;
            if (augment == null)
                return;

            DrawTooltip(spriteBatch, augment);
        }

        public static string GetClassDisplayName(AugmentClass augmentClass)
        {
            return augmentClass switch
            {
                AugmentClass.Melee => "MELEE",
                AugmentClass.Ranged => "RANGED",
                AugmentClass.Magic => "MAGIC",
                AugmentClass.Summon => "SUMMON",
                AugmentClass.Support => "SUPPORT",
                _ => "GENERAL"
            };
        }

        public static Color GetClassColor(AugmentClass augmentClass)
        {
            return augmentClass switch
            {
                AugmentClass.Melee => new Color(255, 130, 110),
                AugmentClass.Ranged => new Color(130, 235, 130),
                AugmentClass.Magic => new Color(110, 215, 255),
                AugmentClass.Summon => new Color(255, 215, 110),
                AugmentClass.Support => new Color(74, 222, 128),
                _ => new Color(190, 205, 225)
            };
        }

        public static string GetRarityDisplayName(AugmentRarity rarity)
        {
            return rarity switch
            {
                AugmentRarity.Common => "COMMON",
                AugmentRarity.Rare => "RARE",
                AugmentRarity.Epic => "EPIC",
                AugmentRarity.Legendary => "LEGENDARY",
                _ => "COMMON"
            };
        }

        public static Color GetRarityColor(AugmentRarity rarity)
        {
            return rarity switch
            {
                AugmentRarity.Common => new Color(185, 200, 225),
                AugmentRarity.Rare => new Color(85, 195, 255),
                AugmentRarity.Epic => new Color(195, 115, 255),
                AugmentRarity.Legendary => new Color(255, 185, 45),
                _ => new Color(185, 200, 225)
            };
        }

        private static void DrawTooltip(SpriteBatch spriteBatch, Augment augment)
        {
            var font = FontAssets.MouseText.Value;
            var rarityColor = GetRarityColor(augment.Rarity);
            var player = Main.LocalPlayer;
            var ap = player?.GetModPlayer<AugmentPlayer>();

            // 1. Title lines
            var titleLines = AugmentColorText.Wrap(font, augment.DisplayName, MaxContentWidth, TitleScale);

            // 2. Badges row
            var badges = new List<(string text, Color color)>();
            badges.Add((GetRarityDisplayName(augment.Rarity), rarityColor));
            badges.Add((GetClassDisplayName(augment.Class), GetClassColor(augment.Class)));

            if (augment.KeystoneFamily != null)
                badges.Add(("CORE OVERRIDE", new Color(248, 113, 113)));

            if (augment.IsPermanent)
                badges.Add(("PERMANENT", new Color(255, 205, 120)));

            // 3. Description lines
            var descLines = AugmentColorText.Wrap(font, augment.Description, MaxContentWidth, DescScale);

            // 4. Metadata (Fortune & Keybind)
            var metaLines = new List<(string text, Color color)>();
            if (augment.FortuneBonus > 0f)
            {
                metaLines.Add(($"• +{(int)Math.Round(augment.FortuneBonus * 100f)}% Fortune Bonus", new Color(255, 220, 120)));
            }

            if (augment.ActiveModKeybind != null)
            {
                var kb = augment.ActiveModKeybind;
                string kbKey = kb.GetAssignedKeys().Count > 0 ? string.Join(", ", kb.GetAssignedKeys()) : null;
                if (!string.IsNullOrEmpty(kbKey))
                    metaLines.Add(($"• Keybind: [{kbKey}]", new Color(125, 215, 255)));
                else
                    metaLines.Add(("• Keybind: [Unassigned]", new Color(255, 135, 120)));
            }

            // 5. Protocol synergy section
            AugmentFamily family = null;
            int ownedMembers = 0;
            var protocolLines = new List<(string text, Color color, Vector2 scale)>();

            if (augment.FamilyId != null)
            {
                family = AugmentFamilyRegistry.Get(augment.FamilyId);
                if (family != null)
                {
                    ownedMembers = ap != null ? AugmentFamilyRegistry.GetOwnedCount(ap, family.Id) : 0;

                    string protoHeader = $"{family.DisplayName.ToUpper()} PROTOCOL  •  {ownedMembers}/{family.MaxMembers} INSTALLED";
                    protocolLines.Add((protoHeader, family.ThemeColor, ProtocolScale));

                    if (!string.IsNullOrEmpty(family.Description))
                    {
                        var familyDescLines = AugmentColorText.Wrap(font, family.Description, MaxContentWidth, ProtocolScale);
                        foreach (var fdl in familyDescLines)
                            protocolLines.Add((fdl, new Color(185, 205, 230), ProtocolScale));
                    }

                    foreach (var kv in family.ThresholdBonuses)
                    {
                        int threshold = kv.Key;
                        var bonus = kv.Value;
                        bool unlocked = ownedMembers >= threshold;

                        string bonusHeader = (unlocked ? "  ✓ (" : "  • (") + $"{threshold}) {bonus.Title}" + (unlocked ? " (Active)" : " (Locked)");
                        Color headerCol = unlocked ? AugmentTextColors.Healing : new Color(145, 160, 185);
                        protocolLines.Add((bonusHeader, headerCol, BonusScale));

                        foreach (var desc in bonus.Descriptions)
                        {
                            var wrappedPerk = AugmentColorText.Wrap(font, "      " + desc.Trim(), MaxContentWidth, BonusScale);
                            foreach (var line in wrappedPerk)
                            {
                                Color perkCol = unlocked ? new Color(210, 238, 225) : new Color(125, 138, 158);
                                protocolLines.Add((line, perkCol, BonusScale));
                            }
                        }
                    }
                }
            }

            // 6. Calculate total content width and height
            float maxContentWidth = 0f;

            // Title width
            foreach (var line in titleLines)
            {
                float w = ChatManager.GetStringSize(font, line, TitleScale).X;
                if (w > maxContentWidth) maxContentWidth = w;
            }

            // Badges row width
            float badgesRowWidth = 0f;
            foreach (var (bText, _) in badges)
            {
                float bw = ChatManager.GetStringSize(font, bText, BadgeScale).X + 14f;
                badgesRowWidth += bw + 6f;
            }
            if (badgesRowWidth > 0f) badgesRowWidth -= 6f; // remove trailing gap
            if (badgesRowWidth > maxContentWidth) maxContentWidth = badgesRowWidth;

            // Description lines width
            foreach (var line in descLines)
            {
                float w = ChatManager.GetStringSize(font, line, DescScale).X;
                if (w > maxContentWidth) maxContentWidth = w;
            }

            // Meta lines width
            foreach (var (mText, _) in metaLines)
            {
                float w = ChatManager.GetStringSize(font, mText, MetaScale).X;
                if (w > maxContentWidth) maxContentWidth = w;
            }

            // Protocol lines width
            foreach (var (pText, _, pScale) in protocolLines)
            {
                float w = ChatManager.GetStringSize(font, pText, pScale).X;
                if (w > maxContentWidth) maxContentWidth = w;
            }

            float contentWidth = Math.Clamp(maxContentWidth, MinBoxWidth, MaxContentWidth);

            // Compute total height
            float totalHeight = 0f;

            // Title height
            foreach (var line in titleLines)
                totalHeight += ChatManager.GetStringSize(font, line, TitleScale).Y + LineSpacing;

            totalHeight += 4f; // spacing to badges

            // Badges height
            totalHeight += 18f;

            totalHeight += 8f; // spacing to divider

            // Divider 1
            totalHeight += 7f; // divider line + margin

            // Description height
            foreach (var line in descLines)
                totalHeight += ChatManager.GetStringSize(font, line, DescScale).Y + LineSpacing;

            // Meta height
            if (metaLines.Count > 0)
            {
                totalHeight += 4f;
                foreach (var (mText, _) in metaLines)
                    totalHeight += ChatManager.GetStringSize(font, mText, MetaScale).Y + LineSpacing;
            }

            // Protocol section height
            if (protocolLines.Count > 0)
            {
                totalHeight += 8f; // spacing to divider
                totalHeight += 7f; // divider 2 + margin

                foreach (var (pText, _, pScale) in protocolLines)
                    totalHeight += ChatManager.GetStringSize(font, pText, pScale).Y + LineSpacing;
            }

            float boxWidth = contentWidth + Padding * 2f;
            float boxHeight = totalHeight + Padding * 2f;

            // 7. Smart boundary positioning
            Vector2 boxPos = GetSmartTooltipPosition(boxWidth, boxHeight);
            Rectangle boxRect = new Rectangle((int)boxPos.X, (int)boxPos.Y, (int)boxWidth, (int)boxHeight);

            // 8. Draw cybernetic chassis
            DrawChassis(spriteBatch, boxRect, rarityColor);

            // 9. Render content
            float curY = boxRect.Y + Padding;
            float contentX = boxRect.X + Padding;

            // Title
            foreach (var line in titleLines)
            {
                ChatManager.DrawColorCodedStringWithShadow(
                    spriteBatch, font, line, new Vector2(contentX, curY), rarityColor, 0f, Vector2.Zero, TitleScale
                );
                curY += ChatManager.GetStringSize(font, line, TitleScale).Y + LineSpacing;
            }

            curY += 4f;

            // Badges row
            float curBadgeX = contentX;
            foreach (var (bText, bColor) in badges)
            {
                DrawBadge(spriteBatch, font, bText, new Vector2(curBadgeX, curY), bColor, out float badgeW);
                curBadgeX += badgeW + 6f;
            }
            curY += 18f + 6f;

            // Divider 1
            DrawDivider(spriteBatch, (int)contentX, (int)curY, (int)contentWidth, rarityColor);
            curY += 7f;

            // Description
            foreach (var line in descLines)
            {
                ChatManager.DrawColorCodedStringWithShadow(
                    spriteBatch, font, line, new Vector2(contentX, curY), new Color(228, 236, 248), 0f, Vector2.Zero, DescScale
                );
                curY += ChatManager.GetStringSize(font, line, DescScale).Y + LineSpacing;
            }

            // Meta lines
            if (metaLines.Count > 0)
            {
                curY += 4f;
                foreach (var (mText, mColor) in metaLines)
                {
                    ChatManager.DrawColorCodedStringWithShadow(
                        spriteBatch, font, mText, new Vector2(contentX, curY), mColor, 0f, Vector2.Zero, MetaScale
                    );
                    curY += ChatManager.GetStringSize(font, mText, MetaScale).Y + LineSpacing;
                }
            }

            // Protocol section
            if (protocolLines.Count > 0 && family != null)
            {
                curY += 6f;
                DrawDivider(spriteBatch, (int)contentX, (int)curY, (int)contentWidth, family.ThemeColor);
                curY += 7f;

                foreach (var (pText, pColor, pScale) in protocolLines)
                {
                    ChatManager.DrawColorCodedStringWithShadow(
                        spriteBatch, font, pText, new Vector2(contentX, curY), pColor, 0f, Vector2.Zero, pScale
                    );
                    curY += ChatManager.GetStringSize(font, pText, pScale).Y + LineSpacing;
                }
            }
        }

        private static Vector2 GetSmartTooltipPosition(float boxWidth, float boxHeight)
        {
            const float margin = 12f;
            const float cursorGap = 20f;

            float posX;
            // Prefer right of cursor if space allows
            if (Main.MouseScreen.X + cursorGap + boxWidth <= Main.screenWidth - margin)
            {
                posX = Main.MouseScreen.X + cursorGap;
            }
            // Flip to left of cursor if overflowing right
            else if (Main.MouseScreen.X - cursorGap - boxWidth >= margin)
            {
                posX = Main.MouseScreen.X - cursorGap - boxWidth;
            }
            else
            {
                // Tight screen: select side with more available clearance
                float spaceRight = Main.screenWidth - Main.MouseScreen.X;
                float spaceLeft = Main.MouseScreen.X;
                posX = spaceRight >= spaceLeft
                    ? Main.MouseScreen.X + cursorGap
                    : Main.MouseScreen.X - cursorGap - boxWidth;
            }

            // Vertically align near cursor tip
            float posY = Main.MouseScreen.Y - 14f;
            if (posY + boxHeight > Main.screenHeight - margin)
                posY = Main.screenHeight - margin - boxHeight;
            if (posY < margin)
                posY = margin;

            // Boundary clamping safety
            posX = Math.Clamp(posX, margin, Math.Max(margin, Main.screenWidth - boxWidth - margin));
            posY = Math.Clamp(posY, margin, Math.Max(margin, Main.screenHeight - boxHeight - margin));

            return new Vector2(posX, posY);
        }

        private static void DrawChassis(SpriteBatch spriteBatch, Rectangle boxRect, Color rarityColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            // 1. High-tech cyber navy background fill
            Color bgNavy = new Color(10, 16, 30) * 0.96f;
            spriteBatch.Draw(pixel, boxRect, bgNavy);

            // 2. Subtle ambient rarity underglow
            spriteBatch.Draw(pixel, boxRect, rarityColor * 0.04f);

            // 3. 2px outer border tinted with rarity
            const int border = 2;
            Color outerBorder = rarityColor * 0.65f;
            spriteBatch.Draw(pixel, new Rectangle(boxRect.X, boxRect.Y, boxRect.Width, border), outerBorder);
            spriteBatch.Draw(pixel, new Rectangle(boxRect.X, boxRect.Bottom - border, boxRect.Width, border), outerBorder);
            spriteBatch.Draw(pixel, new Rectangle(boxRect.X, boxRect.Y, border, boxRect.Height), outerBorder);
            spriteBatch.Draw(pixel, new Rectangle(boxRect.Right - border, boxRect.Y, border, boxRect.Height), outerBorder);

            // 4. 1px inner hairline accent
            Color innerHairline = Color.White * 0.08f;
            spriteBatch.Draw(pixel, new Rectangle(boxRect.X + 2, boxRect.Y + 2, boxRect.Width - 4, 1), innerHairline);
            spriteBatch.Draw(pixel, new Rectangle(boxRect.X + 2, boxRect.Bottom - 3, boxRect.Width - 4, 1), innerHairline);
            spriteBatch.Draw(pixel, new Rectangle(boxRect.X + 2, boxRect.Y + 2, 1, boxRect.Height - 4), innerHairline);
            spriteBatch.Draw(pixel, new Rectangle(boxRect.Right - 3, boxRect.Y + 2, 1, boxRect.Height - 4), innerHairline);

            // 5. Corner accent notches
            Color cornerColor = rarityColor * 0.95f;
            // Top-Left
            spriteBatch.Draw(pixel, new Rectangle(boxRect.X, boxRect.Y, 5, 2), cornerColor);
            spriteBatch.Draw(pixel, new Rectangle(boxRect.X, boxRect.Y, 2, 5), cornerColor);
            // Top-Right
            spriteBatch.Draw(pixel, new Rectangle(boxRect.Right - 5, boxRect.Y, 5, 2), cornerColor);
            spriteBatch.Draw(pixel, new Rectangle(boxRect.Right - 2, boxRect.Y, 2, 5), cornerColor);
            // Bottom-Left
            spriteBatch.Draw(pixel, new Rectangle(boxRect.X, boxRect.Bottom - 2, 5, 2), cornerColor);
            spriteBatch.Draw(pixel, new Rectangle(boxRect.X, boxRect.Bottom - 5, 2, 5), cornerColor);
            // Bottom-Right
            spriteBatch.Draw(pixel, new Rectangle(boxRect.Right - 5, boxRect.Bottom - 2, 5, 2), cornerColor);
            spriteBatch.Draw(pixel, new Rectangle(boxRect.Right - 2, boxRect.Bottom - 5, 2, 5), cornerColor);
        }

        private static void DrawBadge(SpriteBatch spriteBatch, DynamicSpriteFont font, string text, Vector2 pos, Color accentColor, out float badgeW)
        {
            Vector2 textSize = ChatManager.GetStringSize(font, text, BadgeScale);
            badgeW = textSize.X + 14f;
            float badgeH = 18f;
            Rectangle badgeRect = new Rectangle((int)pos.X, (int)pos.Y, (int)badgeW, (int)badgeH);
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            // Pill dark background
            spriteBatch.Draw(pixel, badgeRect, new Color(14, 22, 40) * 0.92f);
            // Pill accent underglow
            spriteBatch.Draw(pixel, badgeRect, accentColor * 0.12f);

            // 1px border
            spriteBatch.Draw(pixel, new Rectangle(badgeRect.X, badgeRect.Y, badgeRect.Width, 1), accentColor * 0.70f);
            spriteBatch.Draw(pixel, new Rectangle(badgeRect.X, badgeRect.Bottom - 1, badgeRect.Width, 1), accentColor * 0.70f);
            spriteBatch.Draw(pixel, new Rectangle(badgeRect.X, badgeRect.Y, 1, badgeRect.Height), accentColor * 0.70f);
            spriteBatch.Draw(pixel, new Rectangle(badgeRect.Right - 1, badgeRect.Y, 1, badgeRect.Height), accentColor * 0.70f);

            // Text centered with shadow
            Vector2 textPos = new Vector2(
                badgeRect.X + (badgeRect.Width - textSize.X) * 0.5f,
                badgeRect.Y + (badgeRect.Height - textSize.Y) * 0.5f + 2f
            );
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, textPos, accentColor, 0f, Vector2.Zero, BadgeScale);
        }

        private static void DrawDivider(SpriteBatch spriteBatch, int x, int y, int width, Color tint)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            spriteBatch.Draw(pixel, new Rectangle(x, y, width, 1), tint * 0.40f);
        }
    }
}

