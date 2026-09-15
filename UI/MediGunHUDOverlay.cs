using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Augments
{
	public static class MediGunHUDOverlay
	{
		public static void Draw(SpriteBatch spriteBatch)
		{
			Player player = Main.LocalPlayer;
			if (!player.active || player.dead)
				return;

			var mediPlayer = player.GetModPlayer<MediGunPlayer>();
			if (!mediPlayer.IsHoldingMediGun(out int tier))
				return;

			Texture2D pixel = TextureAssets.MagicPixel.Value;
			var font = FontAssets.MouseText.Value;

			// Positioned directly below the player's feet for immediate combat readability
			Vector2 playerScreenPos = player.MountedCenter - Main.screenPosition;
			float barWidth = 120f;
			float barHeight = 12f;
			float x = playerScreenPos.X - barWidth / 2f;
			float y = playerScreenPos.Y + player.height / 2f + 14f;

			// Ensure it stays on screen
			x = MathHelper.Clamp(x, 10f, Main.screenWidth - barWidth - 10f);
			y = MathHelper.Clamp(y, 10f, Main.screenHeight - barHeight - 20f);

			Rectangle bgRect = new Rectangle((int)x, (int)y, (int)barWidth, (int)barHeight);

			// 1. Outer Dark Slate Background with subtle depth
			spriteBatch.Draw(pixel, bgRect, new Color(12, 16, 24, 210));

			// Border
			Color borderColor = mediPlayer.IsUberActive
				? new Color(80, 240, 255, 230)
				: (mediPlayer.UberCharge >= 100f ? new Color(255, 220, 60, 230) : new Color(35, 55, 80, 200));

			DrawBorder(spriteBatch, pixel, bgRect, borderColor);

			// 2. Progress Fill
			float progress;
			Color fillColor;
			string label;

			float time = (float)Main.GlobalTimeWrappedHourly;

			if (mediPlayer.IsUberActive)
			{
				progress = mediPlayer.UberDurationMax > 0
					? (mediPlayer.UberActiveTimer / (float)mediPlayer.UberDurationMax)
					: 0f;

				float pulse = 0.8f + 0.2f * (float)Math.Sin(time * 15f);
				fillColor = (tier == 4 ? new Color(255, 220, 70) : new Color(60, 230, 255)) * pulse;

				float remainingSec = mediPlayer.UberActiveTimer / 60f;
				label = $"ÜBER: {remainingSec:0.0}s";
			}
			else if (mediPlayer.UberCharge >= 100f)
			{
				progress = 1f;
				float flash = 0.75f + 0.25f * (float)Math.Sin(time * 10f);
				fillColor = new Color(255, 215, 60) * flash;
				label = "READY [R-CLICK]";
			}
			else
			{
				progress = MathHelper.Clamp(mediPlayer.UberCharge / 100f, 0f, 1f);
				fillColor = Color.Lerp(new Color(40, 140, 220), new Color(0, 235, 210), progress);
				label = $"ÜBER: {(int)mediPlayer.UberCharge}%";
			}

			int innerPadding = 2;
			int fillWidth = (int)((barWidth - innerPadding * 2) * progress);
			if (fillWidth > 0)
			{
				Rectangle fillRect = new Rectangle(
					(int)x + innerPadding,
					(int)y + innerPadding,
					fillWidth,
					(int)barHeight - innerPadding * 2
				);
				spriteBatch.Draw(pixel, fillRect, fillColor);
			}

			// 3. Compact Centered Label Text
			float scale = 0.55f;
			Vector2 textSize = font.MeasureString(label) * scale;
			Vector2 textPos = new Vector2(
				x + (barWidth - textSize.X) / 2f,
				y + (barHeight - textSize.Y) / 2f - 1f
			);

			// Text drop shadow
			spriteBatch.DrawString(font, label, textPos + new Vector2(1f, 1f), Color.Black * 0.85f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			Color textCol = mediPlayer.IsUberActive
				? Color.White
				: (mediPlayer.UberCharge >= 100f ? new Color(255, 255, 200) : new Color(210, 235, 255));
			spriteBatch.DrawString(font, label, textPos, textCol, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
		}

		private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color)
		{
			// 1px Border
			spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
			spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
			spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
			spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
		}
	}
}
