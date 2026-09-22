using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
	public class KineticShrapnelProjectile : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BulletHighVelocity;

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
			ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
		}

		public override void SetDefaults()
		{
			AugmentProjectileTag tag = Projectile.GetGlobalProjectile<AugmentProjectileTag>();
			tag.IsAugmentProcDamage = true;
			tag.CanTriggerOnHitAugments = false;
			tag.PreventEchoChamberCopy = true;
			tag.PreventRicochetEngineCopy = true;

			Projectile.width = 6;
			Projectile.height = 6;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 2;
			Projectile.extraUpdates = 1;
			Projectile.timeLeft = 60;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 15;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

			// Fine ruby spark trail
			if (Main.rand.NextBool(2))
			{
				Dust d = Dust.NewDustPerfect(
					Projectile.Center,
					DustID.GemRuby,
					Projectile.velocity * -0.2f + Main.rand.NextVector2Circular(0.5f, 0.5f),
					120,
					new Color(244, 63, 94),
					Main.rand.NextFloat(0.7f, 1.0f)
				);
				d.noGravity = true;
			}

			Lighting.AddLight(Projectile.Center, 0.35f, 0.05f, 0.10f);
		}

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.4f, Pitch = 0.45f }, Projectile.Center);

			for (int i = 0; i < 8; i++)
			{
				Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GemRuby, vel, 100, new Color(244, 63, 94), 1.1f);
				d.noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
			Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);

			// Render glowing afterimage trail
			for (int i = 0; i < Projectile.oldPos.Length; i++)
			{
				if (Projectile.oldPos[i] == Vector2.Zero)
					continue;

				float factor = 1f - (i / (float)Projectile.oldPos.Length);
				Vector2 drawPos = Projectile.oldPos[i] - Main.screenPosition + origin;
				Color trailColor = new Color(244, 63, 94) * (0.6f * factor);
				float rot = Projectile.oldRot[i];

				Main.EntitySpriteDraw(texture, drawPos, null, trailColor, rot, origin, Projectile.scale * (0.6f + 0.4f * factor), SpriteEffects.None, 0);
			}

			// Main projectile draw with ruby glow
			Vector2 currentDrawPos = Projectile.Center - Main.screenPosition;
			Main.EntitySpriteDraw(texture, currentDrawPos, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
			return false;
		}
	}
}
