using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI.Chat;
using Augments.Core;

namespace Augments
{
	public static class AugmentAnalyticsHUD
	{
		public static bool Visible = false;
		public static Vector2 PanelPosition = new Vector2(24f, 260f);

		private const float PanelWidth = 340f;
		private const float HeaderHeight = 72f;
		private const float EntryHeight = 36f;
		private const float MaxVisibleEntries = 7f;

		private static bool isDragging = false;
		private static Vector2 dragOffset = Vector2.Zero;
		private static float scrollOffset = 0f;

		// Button rectangles for click detection
		private static Rectangle modeButtonRect;
		private static Rectangle resetButtonRect;
		private static Rectangle pauseButtonRect;
		private static Rectangle closeButtonRect;

		private static uint lastToggleFrame = 0;
		private static bool oldLState = false;

		public static void Toggle()
		{
			if (Main.GameUpdateCount == lastToggleFrame)
				return;
			lastToggleFrame = Main.GameUpdateCount;

			Visible = !Visible;
			SoundEngine.PlaySound(Visible ? SoundID.MenuOpen : SoundID.MenuClose);
		}

		public static void Update(GameTime gameTime)
		{
			if (Main.dedServ || Main.gameMenu)
				return;

			// Global hotkey check (works even when inventory is open or if unbound in custom profile)
			if (!Main.drawingPlayerChat && !Main.editSign && !Main.editChest)
			{
				bool triggered = false;
				if (Augments.ToggleCombatAnalyticsKeybind != null && Augments.ToggleCombatAnalyticsKeybind.JustPressed)
				{
					triggered = true;
				}
				else if (Augments.ToggleCombatAnalyticsKeybind == null || Augments.ToggleCombatAnalyticsKeybind.GetAssignedKeys().Count == 0)
				{
					bool isDown = Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.L);
					if (isDown && !oldLState)
					{
						triggered = true;
					}
					oldLState = isDown;
				}

				if (triggered)
				{
					Toggle();
				}
			}

			if (!Visible)
			{
				isDragging = false;
				return;
			}

			Point mouse = new Point(Main.mouseX, Main.mouseY);

			// Dragging handling
			Rectangle headerRect = new Rectangle((int)PanelPosition.X, (int)PanelPosition.Y, (int)PanelWidth, 32);

			if (Main.mouseLeft && !Main.mouseLeftRelease)
			{
				if (!isDragging && headerRect.Contains(mouse) && !closeButtonRect.Contains(mouse))
				{
					isDragging = true;
					dragOffset = new Vector2(mouse.X - PanelPosition.X, mouse.Y - PanelPosition.Y);
				}
				else if (isDragging)
				{
					PanelPosition = new Vector2(mouse.X - dragOffset.X, mouse.Y - dragOffset.Y);
					PanelPosition.X = MathHelper.Clamp(PanelPosition.X, 4f, Main.screenWidth - PanelWidth - 4f);
					PanelPosition.Y = MathHelper.Clamp(PanelPosition.Y, 4f, Main.screenHeight - 200f);
				}
			}
			else
			{
				isDragging = false;
			}

			// Clicks
			if (Main.mouseLeft && Main.mouseLeftRelease)
			{
				if (closeButtonRect.Contains(mouse))
				{
					Toggle();
					Main.mouseLeftRelease = false;
				}
				else if (modeButtonRect.Contains(mouse))
				{
					AugmentDamageTracker.ViewMode = AugmentDamageTracker.ViewMode == AnalyticsViewMode.Last10Minutes
						? AnalyticsViewMode.TotalSession
						: AnalyticsViewMode.Last10Minutes;
					SoundEngine.PlaySound(SoundID.MenuTick);
					Main.mouseLeftRelease = false;
				}
				else if (resetButtonRect.Contains(mouse))
				{
					AugmentDamageTracker.Reset();
					SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.6f, Pitch = 0.2f });
					Main.mouseLeftRelease = false;
				}
				else if (pauseButtonRect.Contains(mouse))
				{
					AugmentDamageTracker.TogglePause();
					SoundEngine.PlaySound(SoundID.MenuTick);
					Main.mouseLeftRelease = false;
				}
			}

			// Scroll wheel over panel
			Rectangle fullPanelRect = new Rectangle((int)PanelPosition.X, (int)PanelPosition.Y, (int)PanelWidth, 400);
			if (fullPanelRect.Contains(mouse))
			{
				PlayerInput.LockVanillaMouseScroll("Augments:AnalyticsHUD");
				int scrollDelta = PlayerInput.ScrollWheelDeltaForUI;
				if (scrollDelta != 0)
				{
					scrollOffset -= (scrollDelta / 120f) * EntryHeight;
				}
			}
		}

		public static void Draw(SpriteBatch spriteBatch)
		{
			if (Main.dedServ || Main.gameMenu || !Visible)
				return;

			var player = Main.LocalPlayer;
			if (player == null || !player.active)
				return;

			var ap = player.GetModPlayer<AugmentPlayer>();
			var (viewDamage, sortedRecords) = AugmentDamageTracker.GetCurrentViewData(ap);

			DynamicSpriteFont font = FontAssets.MouseText.Value;
			Point mouse = new Point(Main.mouseX, Main.mouseY);

			int visibleCount = Math.Min(sortedRecords.Count, (int)MaxVisibleEntries);
			float listHeight = Math.Max(1, visibleCount) * EntryHeight;
			float totalPanelHeight = HeaderHeight + listHeight + 16f;

			// Clamp scroll
			float maxScroll = Math.Max(0f, (sortedRecords.Count - MaxVisibleEntries) * EntryHeight);
			scrollOffset = MathHelper.Clamp(scrollOffset, 0f, maxScroll);

			Rectangle panelRect = new Rectangle((int)PanelPosition.X, (int)PanelPosition.Y, (int)PanelWidth, (int)totalPanelHeight);

			// 1. Background (High-tech dark obsidian + cyan tint)
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, panelRect, new Color(8, 12, 20, 245));
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, panelRect, new Color(56, 189, 248, 8));

			// High-tech cybernetic border
			DrawHighTechBorder(spriteBatch, panelRect, new Color(56, 189, 248, 180));

			float curY = PanelPosition.Y + 6f;
			float leftX = PanelPosition.X + 10f;
			float rightX = PanelPosition.X + PanelWidth - 10f;

			// 2. Header Bar: Title & Close Button
			string title = "COMBAT ANALYTICS";
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, title, new Vector2(leftX, curY), new Color(56, 189, 248), 0f, Vector2.Zero, new Vector2(0.85f));

			// Close [X] button
			closeButtonRect = new Rectangle((int)rightX - 18, (int)curY, 18, 18);
			bool closeHover = closeButtonRect.Contains(mouse);
			Color closeCol = closeHover ? new Color(255, 80, 80) : new Color(160, 175, 190);
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, "✕", new Vector2(closeButtonRect.X + 2, closeButtonRect.Y - 1), closeCol, 0f, Vector2.Zero, new Vector2(0.8f));

			curY += 24f;

			// Controls Bar: [ViewMode]  [Pause]  [Reset]
			float btnY = curY;
			string modeLabel = AugmentDamageTracker.ViewMode == AnalyticsViewMode.Last10Minutes ? "MODE: LAST 10M" : "MODE: TOTAL";
			Vector2 modeSz = ChatManager.GetStringSize(font, modeLabel, new Vector2(0.65f));
			modeButtonRect = new Rectangle((int)leftX, (int)btnY, (int)modeSz.X + 10, 18);
			bool modeHover = modeButtonRect.Contains(mouse);

			spriteBatch.Draw(TextureAssets.MagicPixel.Value, modeButtonRect, modeHover ? new Color(56, 189, 248, 60) : new Color(20, 30, 45, 200));
			DrawHighTechBorder(spriteBatch, modeButtonRect, modeHover ? new Color(56, 189, 248) : new Color(80, 110, 140));
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, modeLabel, new Vector2(modeButtonRect.X + 5, modeButtonRect.Y + 2), modeHover ? Color.White : new Color(180, 215, 255), 0f, Vector2.Zero, new Vector2(0.65f));

			// Pause Button
			string pauseLabel = AugmentDamageTracker.IsPaused ? "RESUME" : "PAUSE";
			Vector2 pauseSz = ChatManager.GetStringSize(font, pauseLabel, new Vector2(0.65f));
			pauseButtonRect = new Rectangle(modeButtonRect.Right + 8, (int)btnY, (int)pauseSz.X + 10, 18);
			bool pauseHover = pauseButtonRect.Contains(mouse);

			Color pauseTheme = AugmentDamageTracker.IsPaused ? new Color(250, 204, 21) : new Color(56, 189, 248);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, pauseButtonRect, pauseHover ? pauseTheme * 0.35f : new Color(20, 30, 45, 200));
			DrawHighTechBorder(spriteBatch, pauseButtonRect, pauseHover ? pauseTheme : new Color(80, 110, 140));
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, pauseLabel, new Vector2(pauseButtonRect.X + 5, pauseButtonRect.Y + 2), pauseHover ? Color.White : pauseTheme, 0f, Vector2.Zero, new Vector2(0.65f));

			// Reset Button
			string resetLabel = "RESET";
			Vector2 resetSz = ChatManager.GetStringSize(font, resetLabel, new Vector2(0.65f));
			resetButtonRect = new Rectangle((int)rightX - (int)resetSz.X - 10, (int)btnY, (int)resetSz.X + 10, 18);
			bool resetHover = resetButtonRect.Contains(mouse);

			spriteBatch.Draw(TextureAssets.MagicPixel.Value, resetButtonRect, resetHover ? new Color(239, 68, 68, 60) : new Color(20, 30, 45, 200));
			DrawHighTechBorder(spriteBatch, resetButtonRect, resetHover ? new Color(239, 68, 68) : new Color(80, 110, 140));
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, resetLabel, new Vector2(resetButtonRect.X + 5, resetButtonRect.Y + 2), resetHover ? Color.White : new Color(248, 113, 113), 0f, Vector2.Zero, new Vector2(0.65f));

			curY += 24f;

			// Metrics Banner: Live DPS & Total Dmg
			float dps = AugmentDamageTracker.GetCurrentDPS();
			string dpsText = $"DPS: {(int)Math.Round(dps):N0}";
			string totalText = $"DMG: {viewDamage:N0}";
			string timerText = $"TIME: {FormatDuration(AugmentDamageTracker.SessionDuration)}";

			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, dpsText, new Vector2(leftX, curY), new Color(74, 222, 128), 0f, Vector2.Zero, new Vector2(0.75f));

			Vector2 totalSz = ChatManager.GetStringSize(font, totalText, new Vector2(0.70f));
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, totalText, new Vector2(leftX + 120f, curY), new Color(225, 235, 245), 0f, Vector2.Zero, new Vector2(0.70f));

			Vector2 timerSz = ChatManager.GetStringSize(font, timerText, new Vector2(0.65f));
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, timerText, new Vector2(rightX - timerSz.X, curY + 2), new Color(148, 163, 184), 0f, Vector2.Zero, new Vector2(0.65f));

			curY += 20f;

			// Divider Line
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)leftX, (int)curY, (int)(PanelWidth - 20f), 1), new Color(56, 189, 248, 60));
			curY += 6f;

			// 3. List of Records (Clean & crash-proof without scissor state disruptions)
			int maxScrollIdx = Math.Max(0, sortedRecords.Count - (int)MaxVisibleEntries);
			int startIndex = (int)MathHelper.Clamp((float)Math.Floor(scrollOffset / EntryHeight), 0f, (float)maxScrollIdx);
			int countToDraw = Math.Min(sortedRecords.Count - startIndex, (int)MaxVisibleEntries);

			if (sortedRecords.Count == 0)
			{
				string emptyText = "No combat damage recorded yet.";
				Vector2 empSz = ChatManager.GetStringSize(font, emptyText, new Vector2(0.7f));
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, emptyText, new Vector2(leftX + (PanelWidth - 20f - empSz.X) * 0.5f, curY + 16f), new Color(148, 163, 184), 0f, Vector2.Zero, new Vector2(0.7f));
			}
			else
			{
				for (int i = 0; i < countToDraw; i++)
				{
					var rec = sortedRecords[startIndex + i];
					float rowY = curY + i * EntryHeight;
					DrawRecordRow(spriteBatch, font, rec, leftX, rowY, PanelWidth - 20f, viewDamage, mouse);
				}
			}

			// Scrollbar if needed
			if (maxScroll > 0f)
			{
				float barAreaHeight = listHeight;
				float thumbHeight = Math.Max(12f, barAreaHeight * (MaxVisibleEntries / sortedRecords.Count));
				float thumbY = curY + (scrollOffset / maxScroll) * (barAreaHeight - thumbHeight);
				Rectangle thumbRect = new Rectangle((int)rightX - 3, (int)thumbY, 3, (int)thumbHeight);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, thumbRect, new Color(56, 189, 248, 160));
			}
		}

		private static void DrawRecordRow(SpriteBatch spriteBatch, DynamicSpriteFont font, DamageSourceRecord rec, float x, float y, float width, long totalDamage, Point mouse)
		{
			Rectangle rowRect = new Rectangle((int)x, (int)y, (int)width, (int)EntryHeight - 2);
			bool hovered = rowRect.Contains(mouse);

			if (hovered)
			{
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, rowRect, new Color(30, 45, 65, 120));
			}

			float percent = totalDamage > 0 ? (float)rec.TotalDamage / totalDamage : 0f;

			// Progress Bar Background
			Rectangle barBg = new Rectangle((int)x, (int)y + 20, (int)width, 6);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, barBg, new Color(15, 20, 30, 200));

			// Progress Bar Fill
			int fillW = Math.Max(rec.TotalDamage > 0 ? 2 : 0, (int)(width * Math.Min(1f, percent)));
			Rectangle barFill = new Rectangle((int)x, (int)y + 20, fillW, 6);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, barFill, rec.Color);

			// Name on Left
			string nameText = rec.DisplayName;
			if (nameText.Length > 22)
				nameText = nameText.Substring(0, 20) + "...";

			Color nameColor = rec.TotalDamage > 0 ? rec.Color : new Color(140, 150, 165);
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, nameText, new Vector2(x + 2, y + 2), nameColor, 0f, Vector2.Zero, new Vector2(0.68f));

			// Numbers on Right: "2,450 (28.4%)"
			string numText = $"{rec.TotalDamage:N0} ({(percent * 100f):0.0}%)";
			Vector2 numSz = ChatManager.GetStringSize(font, numText, new Vector2(0.65f));
			Color numCol = rec.TotalDamage > 0 ? new Color(230, 235, 245) : new Color(120, 130, 145);
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, numText, new Vector2(x + width - numSz.X - 2, y + 3), numCol, 0f, Vector2.Zero, new Vector2(0.65f));

			// Hover tooltip showing hits, crits, max hit
			if (hovered)
			{
				string tip = $"{rec.DisplayName}\nDamage: {rec.TotalDamage:N0} ({(percent * 100f):0.0}%)\nHits: {rec.HitCount:N0}  •  Crits: {rec.CritCount:N0}  •  Max Hit: {rec.MaxHit:N0}";
				Main.instance.MouseText(tip);
			}
		}

		private static void DrawHighTechBorder(SpriteBatch spriteBatch, Rectangle rect, Color color)
		{
			// Outer subtle line
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, rect.Width, 1), color * 0.5f);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color * 0.5f);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, 1, rect.Height), color * 0.5f);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color * 0.5f);

			// Corner accents (3x3 brackets)
			const int c = 4;
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, c, 2), color);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, 2, c), color);

			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - c, rect.Y, c, 2), color);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 2, rect.Y, 2, c), color);

			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - 2, c, 2), color);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - c, 2, c), color);

			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - c, rect.Bottom - 2, c, 2), color);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 2, rect.Bottom - c, 2, c), color);
		}

		private static string FormatDuration(float seconds)
		{
			int s = (int)seconds;
			int mins = s / 60;
			int remSec = s % 60;
			return $"{mins:D2}:{remSec:D2}";
		}
	}
}
