using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Augments.Core
{
	public class GreenHeartDust : ModDust
	{
		public override string Texture => "Terraria/Images/MagicPixel";

		public override void OnSpawn(Dust dust)
		{
			dust.noGravity = true;
			dust.frame = new Rectangle(0, 0, 1, 1);
			dust.rotation = Main.rand.NextFloat(-0.25f, 0.25f);
		}

		public override bool Update(Dust dust)
		{
			dust.position += dust.velocity;
			dust.velocity *= 0.94f;
			dust.rotation += dust.velocity.X * 0.04f;
			dust.scale *= 0.965f;

			Lighting.AddLight(dust.position, 0.06f * dust.scale, 0.35f * dust.scale, 0.12f * dust.scale);

			if (dust.scale < 0.18f)
			{
				dust.active = false;
			}

			return false;
		}

		public override bool PreDraw(Dust dust)
		{
			Texture2D heartTex = TextureAssets.Heart.Value;
			if (heartTex == null)
				return false;

			Vector2 origin = heartTex.Size() * 0.5f;
			Color drawColor = new Color(70, 255, 130, 210) * (dust.color.A > 0 ? dust.color.A / 255f : 1f);
			float drawScale = dust.scale * 0.42f;

			Main.spriteBatch.Draw(
				heartTex,
				dust.position - Main.screenPosition,
				null,
				drawColor,
				dust.rotation,
				origin,
				drawScale,
				SpriteEffects.None,
				0f
			);

			return false;
		}
	}
}
