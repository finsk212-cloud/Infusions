using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
    public class RicochetEngineProjectile : ModProjectile
    {
        private const int MaxHits = 3;
        private const float SearchRadius = 500f;
        private const float MoveSpeed = 14f;
        private const float HomingStrength = 0.18f;

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Bullet;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            AugmentProjectileTag tag = Projectile.GetGlobalProjectile<AugmentProjectileTag>();
            tag.IsAugmentProcDamage = true;
            tag.CanTriggerOnHitAugments = true;
            tag.OnHitEffectiveness = 0.5f;
            tag.PreventRicochetEngineCopy = true;
            tag.SourceAugmentId = "ricochet_engine";

            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = MaxHits;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.alpha = 35;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(190, 95, 255, 220);
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                int originalTarget = (int)Projectile.ai[0];
                if (originalTarget >= 0 && originalTarget < Main.maxNPCs)
                    Projectile.localNPCImmunity[originalTarget] = -1;

                SetTarget(FindNearestTarget());

                // Initial ricochet deflection sound and sparks
                SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.5f, Pitch = 0.2f }, Projectile.Center);
                for (int i = 0; i < 6; i++)
                {
                    Vector2 sparkVel = Main.rand.NextVector2Circular(2.5f, 2.5f);
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center,
                        Main.rand.NextBool() ? DustID.PurpleTorch : DustID.GemAmethyst,
                        sparkVel,
                        100,
                        default,
                        Main.rand.NextFloat(0.8f, 1.2f)
                    );
                    d.noGravity = true;
                }
            }

            NPC target = GetCurrentTarget();
            if (target == null)
            {
                SetTarget(FindNearestTarget());
                target = GetCurrentTarget();
            }

            if (target == null)
            {
                Projectile.Kill();
                return;
            }

            Vector2 desiredVelocity = Projectile.DirectionTo(target.Center) * MoveSpeed;
            if (Projectile.velocity.LengthSquared() < 0.01f)
                Projectile.velocity = desiredVelocity;
            else
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, HomingStrength);

            // Rotate vertical bullet sprite 90 degrees so it faces horizontally along flight velocity
            if (Projectile.velocity.LengthSquared() > 0.01f)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Lighting.AddLight(Projectile.Center, 0.45f, 0.12f, 0.60f);

            // Shimmering neon purple tracer sparks
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity * 0.25f,
                    Main.rand.NextBool() ? DustID.PurpleTorch : DustID.GemAmethyst,
                    -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.3f, 0.3f),
                    80,
                    default,
                    Main.rand.NextFloat(0.75f, 1.1f)
                );
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);

            // Ethereal tracer afterimages
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 drawPos = Projectile.oldPos[i] - Main.screenPosition + origin + new Vector2(0f, Projectile.gfxOffY);
                float progress = 1f - (float)i / Projectile.oldPos.Length;
                Color trailColor = new Color(190, 80, 255, 0) * (progress * 0.45f);
                float trailScale = Projectile.scale * MathHelper.Lerp(0.6f, 1f, progress);
                float trailRot = Projectile.oldRot[i];

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

            Vector2 currentDrawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Color drawColor = new Color(225, 140, 255, 230);
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

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.localNPCImmunity[target.whoAmI] = -1;
            Projectile.ai[1]++;

            // Ricochet impact sound and spark burst
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.6f, Pitch = 0.35f }, Projectile.Center);
            for (int i = 0; i < 8; i++)
            {
                Vector2 sparkVel = Main.rand.NextVector2Circular(3.5f, 3.5f) - Projectile.velocity * 0.15f;
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool() ? DustID.PurpleTorch : DustID.GemAmethyst,
                    sparkVel,
                    80,
                    default,
                    Main.rand.NextFloat(1f, 1.4f)
                );
                d.noGravity = true;
            }

            if (Projectile.ai[1] >= MaxHits)
                return;

            NPC nextTarget = FindNearestTarget();
            if (nextTarget == null)
            {
                Projectile.Kill();
                return;
            }

            SetTarget(nextTarget);
            Projectile.velocity = Projectile.DirectionTo(nextTarget.Center) * MoveSpeed;
            Projectile.netUpdate = true;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.35f, Pitch = 0.5f }, Projectile.Center);
            for (int i = 0; i < 6; i++)
            {
                Vector2 sparkVel = Main.rand.NextVector2Circular(2.5f, 2.5f);
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.GemAmethyst,
                    sparkVel,
                    100,
                    default,
                    1f
                );
                d.noGravity = true;
            }
        }

        public override bool? CanHitNPC(NPC target)
        {
            int originalTarget = (int)Projectile.ai[0];
            if (target.whoAmI == originalTarget || Projectile.localNPCImmunity[target.whoAmI] != 0)
                return false;

            return null;
        }

        private NPC FindNearestTarget()
        {
            NPC nearest = null;
            float nearestDistanceSquared = SearchRadius * SearchRadius;

            foreach (NPC npc in Main.npc)
            {
                if (!npc.CanBeChasedBy(Projectile) || Projectile.localNPCImmunity[npc.whoAmI] != 0)
                    continue;

                if (npc.whoAmI == (int)Projectile.ai[0])
                    continue;

                float distanceSquared = Vector2.DistanceSquared(Projectile.Center, npc.Center);
                if (distanceSquared >= nearestDistanceSquared)
                    continue;

                nearest = npc;
                nearestDistanceSquared = distanceSquared;
            }

            return nearest;
        }

        private NPC GetCurrentTarget()
        {
            int targetIndex = (int)Projectile.ai[2] - 1;
            if (targetIndex < 0 || targetIndex >= Main.maxNPCs)
                return null;

            NPC target = Main.npc[targetIndex];
            return target.CanBeChasedBy(Projectile) && Projectile.localNPCImmunity[targetIndex] == 0 ? target : null;
        }

        private void SetTarget(NPC target)
        {
            Projectile.ai[2] = target == null ? 0f : target.whoAmI + 1f;
        }
    }
}
