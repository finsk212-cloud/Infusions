using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Augments
{
	public class ColoredLabel : UIElement
	{
		private string text;
		private readonly float scale;

		public ColoredLabel(string text, float scale = 0.85f)
		{
			this.text = text;
			this.scale = scale;
		}

		public void SetText(string newText)
		{
			text = newText;
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			if (string.IsNullOrEmpty(text))
				return;

			CalculatedStyle d = GetDimensions();
			var font = FontAssets.MouseText.Value;
			Vector2 size = ChatManager.GetStringSize(font, text, new Vector2(scale));
			Vector2 pos = new Vector2(d.X + (d.Width - size.X) * 0.5f, d.Y + (d.Height - size.Y) * 0.5f);
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, pos, Color.White, 0f, Vector2.Zero, new Vector2(scale));
		}
	}
}
