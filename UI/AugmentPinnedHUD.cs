using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI.Chat;
using Augments.Core;

namespace Augments
{
	public enum PinnedStatType
	{
		LiveDPS,
		HitsAndCrits,
		TotalDamage,
		CombatTime,
		DamageBlocked
	}

	public class PinnedWidgetData
	{
		public PinnedStatType Type;
		public bool IsPinned;
		public Vector2 Position;
		public bool IsDragging;
		public Vector2 DragOffset;
		public float Width = 152f;
		public float Height = 36f;
	}

	// In-game HUD overlay for pinned combat analytics telemetry cards.
	// Completely accident-proof: in normal gameplay, widgets are locked and mouse clicks pass through
	// without interfering with combat or weapon swings.
	// Hold Left Alt (or Left Shift) to unlock dragging and reposition widgets anywhere on screen.
	public static class AugmentPinnedHUD
	{
		private static readonly Dictionary<PinnedStatType, PinnedWidgetData> widgets = new()
		{
			[PinnedStatType.LiveDPS] = new PinnedWidgetData
			{
				Type = PinnedStatType.LiveDPS,
				IsPinned = false,
				Position = new Vector2(20f, 110f)
			},
			[PinnedStatType.HitsAndCrits] = new PinnedWidgetData
			{
				Type = PinnedStatType.HitsAndCrits,
				IsPinned = false,
				Position = new Vector2(20f, 154f)
			},
			[PinnedStatType.TotalDamage] = new PinnedWidgetData
			{
				Type = PinnedStatType.TotalDamage,
				IsPinned = false,
				Position = new Vector2(20f, 198f)
			},
			[PinnedStatType.CombatTime] = new PinnedWidgetData
			{
				Type = PinnedStatType.CombatTime,
				IsPinned = false,
				Position = new Vector2(20f, 242f)
			},
			[PinnedStatType.DamageBlocked] = new PinnedWidgetData
			{
				Type = PinnedStatType.DamageBlocked,
				IsPinned = false,
				Position = new Vector2(20f, 286f)
			}
		};

		private static bool wasMouseLeft = false;
		private static bool wasMouseRight = false;
		private static float pulseTimer = 0f;

		public static bool IsPinned(PinnedStatType type) => widgets.TryGetValue(type, out var w) && w.IsPinned;

		public static void SetPinned(PinnedStatType type, bool pinned)
		{
			if (widgets.TryGetValue(type, out var w))
			{
				w.IsPinned = pinned;
			}
		}

		public static void TogglePin(PinnedStatType type)
		{
			if (widgets.TryGetValue(type, out var w))
			{
				w.IsPinned = !w.IsPinned;
			}
		}

		public static bool AnyPinned
		{
			get
			{
				foreach (var w in widgets.Values)
				{
					if (w.IsPinned) return true;
				}
				return false;
			}
		}

		public static bool IsInteractingAny
		{
			get
			{
				if (Main.dedServ || Main.gameMenu)
					return false;

				bool isAltDown = Main.keyState.IsKeyDown(Keys.LeftAlt)
				              || Main.keyState.IsKeyDown(Keys.RightAlt)
				              || Main.keyState.IsKeyDown(Keys.LeftShift);
				if (!isAltDown)
					return false;

				Vector2 mouse = new Vector2(Main.mouseX, Main.mouseY);
				foreach (var w in widgets.Values)
				{
					if (!w.IsPinned)
						continue;
					if (w.IsDragging)
						return true;
					Rectangle rect = new Rectangle((int)w.Position.X, (int)w.Position.Y, (int)w.Width, (int)w.Height);
					if (rect.Contains((int)mouse.X, (int)mouse.Y))
						return true;
				}
				return false;
			}
		}

		public static void Update(GameTime gameTime)
		{
			if (Main.dedServ || Main.gameMenu)
				return;

			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			pulseTimer += dt;

			var player = Main.LocalPlayer;
			if (player == null || !player.active)
				return;

			bool isAltDown = Main.keyState.IsKeyDown(Keys.LeftAlt)
			              || Main.keyState.IsKeyDown(Keys.RightAlt)
			              || Main.keyState.IsKeyDown(Keys.LeftShift);

			Vector2 mouse = new Vector2(Main.mouseX, Main.mouseY);
			bool leftPressed = Main.mouseLeft && !wasMouseLeft;
			bool rightPressed = Main.mouseRight && !wasMouseRight;

			foreach (var w in widgets.Values)
			{
				if (!w.IsPinned)
				{
					w.IsDragging = false;
					continue;
				}

				Rectangle rect = new Rectangle((int)w.Position.X, (int)w.Position.Y, (int)w.Width, (int)w.Height);
				bool hovered = rect.Contains((int)mouse.X, (int)mouse.Y);

				if (isAltDown)
				{
					// Check close button [✕] in top right corner (size 16x16)
					Rectangle closeRect = new Rectangle(rect.Right - 18, rect.Y + 2, 16, 16);
					bool closeHover = closeRect.Contains((int)mouse.X, (int)mouse.Y);

					if (hovered && rightPressed)
					{
						w.IsPinned = false;
						w.IsDragging = false;
						SoundEngine.PlaySound(SoundID.MenuClose);
						Main.blockMouse = true;
						continue;
					}

					if (closeHover && leftPressed)
					{
						w.IsPinned = false;
						w.IsDragging = false;
						SoundEngine.PlaySound(SoundID.MenuClose);
						Main.blockMouse = true;
						continue;
					}

					if (hovered && leftPressed && !closeHover)
					{
						w.IsDragging = true;
						w.DragOffset = mouse - w.Position;
						SoundEngine.PlaySound(SoundID.MenuTick);
						Main.blockMouse = true;
					}

					if (w.IsDragging)
					{
						if (Main.mouseLeft)
						{
							w.Position = mouse - w.DragOffset;
							// Clamp to screen boundaries
							w.Position.X = MathHelper.Clamp(w.Position.X, 4f, Main.screenWidth - w.Width - 4f);
							w.Position.Y = MathHelper.Clamp(w.Position.Y, 4f, Main.screenHeight - w.Height - 4f);
							Main.blockMouse = true;
						}
						else
						{
							w.IsDragging = false;
						}
					}
				}
				else
				{
					// When Alt is NOT pressed, dragging is completely locked to prevent accidental movement in combat
					w.IsDragging = false;
				}
			}

			wasMouseLeft = Main.mouseLeft;
			wasMouseRight = Main.mouseRight;
		}

		public static void Draw(SpriteBatch spriteBatch)
		{
			if (Main.dedServ || Main.gameMenu)
				return;

			var player = Main.LocalPlayer;
			if (player == null || !player.active)
				return;

			var ap = player.GetModPlayer<AugmentPlayer>();
			if (ap == null)
				return;

			bool isAltDown = Main.keyState.IsKeyDown(Keys.LeftAlt)
			              || Main.keyState.IsKeyDown(Keys.RightAlt)
			              || Main.keyState.IsKeyDown(Keys.LeftShift);

			var font = FontAssets.MouseText.Value;
			Vector2 mouse = new Vector2(Main.mouseX, Main.mouseY);

			// Pre-fetch telemetry data
			float currentDps = AugmentDamageTracker.GetCurrentDPS();
			var (viewDamage, viewBlocked, sortedRecords) = AugmentDamageTracker.GetCurrentViewData(ap);
			int totalHits = 0;
			int totalCrits = 0;
			foreach (var r in sortedRecords)
			{
				totalHits += r.HitCount;
				totalCrits += r.CritCount;
			}
			float critRate = totalHits > 0 ? ((float)totalCrits / totalHits) * 100f : 0f;
			int combatSec = (int)AugmentDamageTracker.SessionDuration;

			foreach (var w in widgets.Values)
			{
				if (!w.IsPinned)
					continue;

				Rectangle rect = new Rectangle((int)w.Position.X, (int)w.Position.Y, (int)w.Width, (int)w.Height);
				bool hovered = rect.Contains((int)mouse.X, (int)mouse.Y);

				// Configure stat-specific branding
				string iconGlyph;
				Color accentColor;
				string labelText;
				string valueText;

				switch (w.Type)
				{
					case PinnedStatType.LiveDPS:
						iconGlyph = "⚡";
						accentColor = new Color(74, 222, 128); // Emerald Neon Green
						labelText = "LIVE DPS";
						valueText = $"{Math.Round(currentDps):N0} /s";
						break;

					case PinnedStatType.HitsAndCrits:
						iconGlyph = "★";
						accentColor = new Color(250, 204, 21); // Amber Gold
						labelText = "HITS & CRITS";
						valueText = $"{totalHits:N0} ({critRate:0.0}%)";
						break;

					case PinnedStatType.TotalDamage:
						iconGlyph = "❖";
						accentColor = new Color(56, 189, 248); // Electric Cyan
						labelText = "RECORDED DMG";
						valueText = $"{viewDamage:N0}";
						break;

					case PinnedStatType.DamageBlocked:
						iconGlyph = "◈";
						accentColor = new Color(52, 211, 153); // Shield Emerald
						labelText = "DMG BLOCKED";
						valueText = $"{viewBlocked:N0}";
						break;

					case PinnedStatType.CombatTime:
					default:
						iconGlyph = "⏱";
						accentColor = new Color(192, 132, 252); // Violet
						labelText = "COMBAT TIME";
						valueText = $"{combatSec / 60:D2}:{combatSec % 60:D2}";
						break;
				}

				// 1. Background
				Color bgColor = new Color(8, 14, 28) * 0.90f;
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, bgColor);

				// 2. High-Tech Cyber Border
				float pulse = (float)Math.Sin(pulseTimer * 4f) * 0.5f + 0.5f;
				Color borderColor = isAltDown && hovered
					? Color.Lerp(new Color(255, 215, 75), Color.White, pulse * 0.4f)
					: (hovered ? Color.Lerp(accentColor, Color.White, 0.25f) : accentColor * 0.70f);

				DrawCyberBorder(spriteBatch, rect, borderColor);

				// 3. Left Icon Badge Box
				Rectangle iconBox = new Rectangle(rect.X + 3, rect.Y + 3, 26, rect.Height - 6);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, iconBox, accentColor * 0.15f);
				DrawCyberBorder(spriteBatch, iconBox, accentColor * 0.40f);

				Vector2 glyphSz = ChatManager.GetStringSize(font, iconGlyph, new Vector2(0.80f));
				Vector2 glyphPos = new Vector2(iconBox.X + (iconBox.Width - glyphSz.X) * 0.5f, iconBox.Y + (iconBox.Height - glyphSz.Y) * 0.5f);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, iconGlyph, glyphPos, accentColor, 0f, Vector2.Zero, new Vector2(0.80f));

				// 4. Content Typography
				float textX = rect.X + 34f;

				// Label
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, labelText, new Vector2(textX, rect.Y + 4f), new Color(148, 163, 184), 0f, Vector2.Zero, new Vector2(0.56f));

				// Value
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, valueText, new Vector2(textX, rect.Y + 16f), Color.White, 0f, Vector2.Zero, new Vector2(0.76f));

				// 5. Alt Drag / Unpin Controls Overlay
				if (isAltDown)
				{
					// Top-right close button [✕]
					Rectangle closeRect = new Rectangle(rect.Right - 18, rect.Y + 2, 16, 16);
					bool closeHover = closeRect.Contains((int)mouse.X, (int)mouse.Y);
					Color closeColor = closeHover ? new Color(248, 113, 113) : new Color(148, 163, 184) * 0.8f;

					Vector2 xSz = ChatManager.GetStringSize(font, "✕", new Vector2(0.65f));
					Vector2 xPos = new Vector2(closeRect.X + (closeRect.Width - xSz.X) * 0.5f, closeRect.Y + (closeRect.Height - xSz.Y) * 0.5f);
					ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, "✕", xPos, closeColor, 0f, Vector2.Zero, new Vector2(0.65f));

					// Drag grip indicator on bottom right
					ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, "⠿", new Vector2(rect.Right - 14, rect.Bottom - 16), new Color(255, 215, 75) * 0.75f, 0f, Vector2.Zero, new Vector2(0.60f));

					if (hovered)
					{
						Main.instance.MouseText($"[Pinned {labelText}]\n• Left Alt + Drag: Reposition anywhere on screen\n• Click ✕ or Right-click: Unpin from HUD");
					}
				}
				else if (hovered)
				{
					Main.instance.MouseText($"[Pinned {labelText}]\n(Hold Left Alt or Shift to drag & move)");
				}
			}
		}

		private static void DrawCyberBorder(SpriteBatch spriteBatch, Rectangle rect, Color color)
		{
			// Thin line frame
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, rect.Width, 1), color * 0.55f);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color * 0.55f);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, 1, rect.Height), color * 0.55f);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color * 0.55f);

			// Corner brackets
			const int cLen = 4;
			const int cThick = 2;

			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, cLen, cThick), color);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, cThick, cLen), color);

			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - cLen, rect.Y, cLen, cThick), color);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - cThick, rect.Y, cThick, cLen), color);

			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - cThick, cLen, cThick), color);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - cLen, cThick, cLen), color);

			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - cLen, rect.Bottom - cThick, cLen, cThick), color);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - cThick, rect.Bottom - cLen, cThick, cLen), color);
		}
	}
}
