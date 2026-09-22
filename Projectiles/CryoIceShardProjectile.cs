using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
    public class CryoIceShardProjectile : ModProjectile
    {
        private const float SearchRadius = 650f;
        private const float MaxHomingSpeed = 12.5f;

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.IceSpike;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2; // Records both position and rotation for afterimages
        }

        public override void SetDefaults()
        {
            Projectile.GetGlobalProjectile<AugmentProjectileTag>().IsAugmentProcDamage = true;
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.scale = 0.85f;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 220;
        }

        public override void AI()
        {
            int shardIndex = (int)Projectile.ai[0];
            ref float timer = ref Projectile.ai[1];
            timer++;

            int homingDelay = 12 + shardIndex * 4;

            // Phase 1: Outward burst and dispersion arc
            if (timer < homingDelay)
            {
                Projectile.velocity *= 0.95f;
                float curl = (shardIndex == 1 ? 0.035f : -0.035f);
                Projectile.velocity = Projectile.velocity.RotatedBy(curl);
            }
            else
            {
                // Phase 2: Independent target acquisition and fluid homing
                NPC target = FindTarget(shardIndex);
                if (target != null)
                {
                    Vector2 targetPos = target.Center;
                    if (shardIndex == 1)
                        targetPos += new Vector2(-22f, -14f);
                    else if (shardIndex == 2)
                        targetPos += new Vector2(22f, -14f);

                    Vector2 toTarget = targetPos - Projectile.Center;
                    float targetAngle = toTarget.ToRotation();
                    float currentAngle = Projectile.velocity.ToRotation();
                    float angleDiff = MathHelper.WrapAngle(targetAngle - currentAngle);

                    float turnLimit = MathHelper.Lerp(0.12f, 0.18f, Math.Min((timer - homingDelay) / 40f, 1f));
                    float turn = Math.Clamp(angleDiff, -turnLimit, turnLimit);
                    float flutter = MathF.Sin(timer * 0.20f + shardIndex * 2.2f) * 0.035f;

                    float currentSpeed = Projectile.velocity.Length();
                    float nextSpeed = MathHelper.Lerp(currentSpeed, MaxHomingSpeed, 0.08f);

                    Projectile.velocity = (currentAngle + turn + flutter).ToRotationVector2() * nextSpeed;
                }
                else
                {
                    // Phase 3: No target nearby - smooth, independent wandering drift without stiffness
                    float wander = (shardIndex - 1) * 0.022f + MathF.Sin(timer * 0.08f + shardIndex * 1.9f) * 0.035f;
                    Projectile.velocity = Projectile.velocity.RotatedBy(wander);
                    if (Projectile.velocity.Length() > 4.5f)
                        Projectile.velocity *= 0.985f;
                }
            }

            // Align needle tip with velocity vector
            if (Projectile.velocity.LengthSquared() > 0.01f)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Lighting.AddLight(Projectile.Center, 0.12f, 0.38f, 0.70f);

            // Shimmering frost dust motes
            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity * 0.25f,
                    DustID.IceTorch,
                    -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.4f, 0.4f),
                    80,
                    default,
                    Main.rand.NextFloat(0.7f, 0.95f)
                );
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);

            // Draw ethereal ice trail afterimages
            for (int k = Projectile.oldPos.Length - 1; k >= 0; k--)
            {
                Vector2 drawPos = Projectile.oldPos[k] - Main.screenPosition + origin + new Vector2(0f, Projectile.gfxOffY);
                float alphaProgress = 1f - (float)k / Projectile.oldPos.Length;
                Color trailColor = new Color(100, 215, 255, 0) * (alphaProgress * 0.40f);
                float trailScale = Projectile.scale * MathHelper.Lerp(0.55f, 1f, alphaProgress);
                float trailRot = Projectile.oldRot[k];

                Main.EntitySpriteDraw(
                    texture,
                    drawPos,
                    null,
                    trailColor,
                    trailRot,
                    origin,
                    trailScale,
                    SpriteEffects.None,
                    0
                );
            }

            // Draw main shard
            Vector2 currentDrawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Color drawColor = Projectile.GetAlpha(lightColor);
            Main.EntitySpriteDraw(
                texture,
                currentDrawPos,
                null,
                drawColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.40f, Pitch = 0.45f }, Projectile.Center);

            for (int i = 0; i < 6; i++)
            {
                Vector2 dustVel = Main.rand.NextVector2Circular(2f, 2f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch, dustVel, 100, default, 1f);
                d.noGravity = true;
            }
        }

        private NPC FindTarget(int shardIndex)
        {
            List<NPC> validTargets = new List<NPC>();
            float searchDistSq = SearchRadius * SearchRadius;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n.active && !n.friendly && !n.dontTakeDamage && n.CanBeChasedBy(Projectile))
                {
                    float dSq = Vector2.DistanceSquared(Projectile.Center, n.Center);
                    if (dSq <= searchDistSq)
                    {
                        validTargets.Add(n);
                    }
                }
            }

            if (validTargets.Count == 0)
                return null;

            validTargets.Sort((a, b) => Vector2.DistanceSquared(Projectile.Center, a.Center).CompareTo(Vector2.DistanceSquared(Projectile.Center, b.Center)));

            // Spread target assignments across available enemies
            int pickIndex = shardIndex % validTargets.Count;
            return validTargets[pickIndex];
        }
    }
}
