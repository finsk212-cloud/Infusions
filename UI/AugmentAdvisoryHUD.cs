using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI.Chat;

namespace Augments
{
	public static class AugmentAdvisoryHUD
	{
		private const float DisplayDuration = 7.0f;
		private const float FadeDuration = 0.4f;
		private const float DefaultPeriodicInterval = 420f; // 7 minutes

		private static string currentPrefix = "";
		private static string currentMessage = "";
		private static float displayTimer = 0f;
		private static float fadeAlpha = 0f;
		private static float periodicTimer = 180f; // Start first periodic tip after 3 mins
		private static int lastTipIndex = -1;
		private static Rectangle bounds = Rectangle.Empty;

		// 12 Rotating natural tips
		private static readonly (string Prefix, string Text)[] RotatingTips = new (string, string)[]
		{
			("[POD 042 // OPERATOR TIP]", "Hold Left Alt to freely drag and move your pinned DPS and damage tracker cards anywhere on your screen."),
			("[POD 042 // TACTICAL ADVICE]", "Support Class defense scales higher as you equip more support plugins, reaching up to +60 bonus defense at five plugins while reducing your damage penalty."),
			("[POD 042 // PROTOCOL DATA]", "Protocols unlock powerful team perks when you equip two or four matching plugins. Combining different protocols can create unique builds."),
			("[POD 042 // SYSTEM NOTE]", "Keystone plugins permanently install game changing powers into your build, but each character can only equip one Keystone."),
			("[POD 042 // FIELD INTEL]", "Fortune plugins do more than just drop extra Machine Cores. They also directly boost your character's world luck stat."),
			("[POD 042 // SURVIVAL TIP]", "Equipping two Field Medic plugins cuts your Potion Sickness cooldown by 20%, allowing you to heal much more often."),
			("[POD 042 // COMBAT ANALYSIS]", "Kinetic Protocol turns your movement speed into bonus Melee attack speed. The faster you run or fly, the faster your weapons swing."),
			("[POD 042 // OPERATOR TIP]", "Press [P] whenever you want to search through your plugins by class, rarity, or keywords."),
			("[POD 042 // TELEMETRY]", "The Combat Analytics menu tracks exact damage blocked, showing how much incoming lethal damage your shields and armor absorbed."),
			("[POD 042 // HUD ADVICE]", "Right click any pinned HUD card to quickly unpin it and keep your screen clean."),
			("[POD 042 // FIELD INTEL]", "Hover your mouse over the docked icons on the right edge of the screen to slide out full bonus specifications."),
			("[POD 042 // CHIP INTEL]", "Universal plugins can be used by any class with zero restrictions, making them versatile choices for any build.")
		};

		public static void ShowAdvisory(string prefix, string message, bool playSound = true)
		{
			currentPrefix = prefix;
			currentMessage = message;
			displayTimer = DisplayDuration;
			if (playSound && !Main.dedServ)
			{
				SoundEngine.PlaySound(SoundID.MenuTick with { Pitch = 0.2f, Volume = 0.6f });
			}
		}

		public static void TriggerSmartAdvisory(AugmentPlayer ap, string triggerId, string prefix, string message)
		{
			if (ap == null || ap.SeenAdvisoryTriggers.Contains(triggerId))
				return;

			ap.SeenAdvisoryTriggers.Add(triggerId);
			ShowAdvisory(prefix, message, true);
		}

		public static void Update(GameTime gameTime)
		{
			if (Main.dedServ || Main.gameMenu)
				return;

			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

			var player = Main.LocalPlayer;
			if (player == null || !player.active)
				return;

			var ap = player.GetModPlayer<AugmentPlayer>();
			if (ap == null)
				return;

			// Suppress tips during active boss encounters
			bool isBossActive = false;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (n.active && (n.boss || n.type == NPCID.EaterofWorldsHead || n.type == NPCID.EaterofWorldsBody || n.type == NPCID.EaterofWorldsTail))
				{
					isBossActive = true;
					break;
				}
			}

			Point mouse = new Point(Main.mouseX, Main.mouseY);
			bool isHovered = bounds != Rectangle.Empty && bounds.Contains(mouse) && fadeAlpha > 0.1f;

			// Click to dismiss immediately
			if (isHovered && (Main.mouseLeftRelease && Main.mouseLeft || Main.mouseRightRelease && Main.mouseRight))
			{
				displayTimer = 0f;
			}

			// Active toast countdown (pause countdown if player is actively hovering to read)
			if (displayTimer > 0f)
			{
				if (isBossActive)
				{
					displayTimer = 0f; // Instantly dismiss on boss summon
				}
				else if (!isHovered)
				{
					displayTimer -= dt;
				}

				if (fadeAlpha < 1f)
					fadeAlpha = Math.Min(1f, fadeAlpha + dt / FadeDuration);
			}
			else
			{
				if (fadeAlpha > 0f)
					fadeAlpha = Math.Max(0f, fadeAlpha - dt / FadeDuration);
				else
					bounds = Rectangle.Empty;
			}

			// Periodic random tip timer (paused during boss fights)
			if (!isBossActive && displayTimer <= 0f && fadeAlpha <= 0f)
			{
				periodicTimer -= dt;
				if (periodicTimer <= 0f)
				{
					periodicTimer = DefaultPeriodicInterval;
					TriggerNextRandomTip();
				}
			}
		}

		private static void TriggerNextRandomTip()
		{
			if (RotatingTips.Length == 0)
				return;

			int index;
			do
			{
				index = Main.rand.Next(RotatingTips.Length);
			} while (index == lastTipIndex && RotatingTips.Length > 1);

			lastTipIndex = index;
			var (prefix, text) = RotatingTips[index];
			ShowAdvisory(prefix, text, true);
		}

		public static void Draw(SpriteBatch spriteBatch)
		{
			if (Main.dedServ || Main.gameMenu || fadeAlpha <= 0.001f || string.IsNullOrEmpty(currentMessage))
				return;

			var font = FontAssets.MouseText.Value;
			Vector2 scale = new Vector2(0.72f);

			Vector2 prefixSize = ChatManager.GetStringSize(font, currentPrefix, scale);
			Vector2 msgSize = ChatManager.GetStringSize(font, currentMessage, scale);

			const float padX = 16f;
			const float padY = 7f;
			const float gap = 8f;

			float totalTextWidth = prefixSize.X + gap + msgSize.X;
			float boxWidth = totalTextWidth + padX * 2f;
			float boxHeight = Math.Max(prefixSize.Y, msgSize.Y) + padY * 2f;

			float posX = (Main.screenWidth - boxWidth) * 0.5f;
			float posY = 75f;

			Rectangle rect = new Rectangle((int)posX, (int)posY, (int)boxWidth, (int)boxHeight);
			bounds = rect;

			Texture2D pixel = TextureAssets.MagicPixel.Value;

			// 1. Ambient drop shadow (2px expansion)
			spriteBatch.Draw(pixel, new Rectangle(rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4), new Color(0, 0, 0, (int)(160 * fadeAlpha)));

			// 2. Chassis background fill (#0A101C)
			spriteBatch.Draw(pixel, rect, new Color(10, 16, 28) * (0.94f * fadeAlpha));

			// 3. Subtle ambient cyan glow
			Color glowCol = new Color(56, 189, 248);
			spriteBatch.Draw(pixel, rect, glowCol * (0.045f * fadeAlpha));

			// 4. 1px inner hairline accent
			Color innerHairline = Color.White * (0.05f * fadeAlpha);
			spriteBatch.Draw(pixel, new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, 1), innerHairline);
			spriteBatch.Draw(pixel, new Rectangle(rect.X + 1, rect.Bottom - 2, rect.Width - 2, 1), innerHairline);
			spriteBatch.Draw(pixel, new Rectangle(rect.X + 1, rect.Y + 1, 1, rect.Height - 2), innerHairline);
			spriteBatch.Draw(pixel, new Rectangle(rect.Right - 2, rect.Y + 1, 1, rect.Height - 2), innerHairline);

			// 5. 1px outer frame
			Color frameCol = Color.Lerp(new Color(30, 41, 59), glowCol, 0.40f) * fadeAlpha;
			spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), frameCol);
			spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), frameCol);
			spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), frameCol);
			spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), frameCol);

			// 6. Flush corner micro-notches (3x1 and 1x3)
			Color cornerCol = Color.Lerp(glowCol, Color.White, 0.40f) * fadeAlpha;
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

			// 7. Typography (Prefix + Message)
			float textY = rect.Y + (rect.Height - 12f) * 0.5f - 1.5f;
			Vector2 prefixPos = new Vector2(rect.X + padX, textY);
			Vector2 msgPos = new Vector2(prefixPos.X + prefixSize.X + gap, textY);

			Color pColor = new Color(56, 189, 248) * fadeAlpha; // Cyan
			Color mColor = new Color(226, 232, 240) * fadeAlpha; // Crisp slate-white

			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, currentPrefix, prefixPos, pColor, 0f, Vector2.Zero, scale);
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, currentMessage, msgPos, mColor, 0f, Vector2.Zero, scale);
		}
	}
}
