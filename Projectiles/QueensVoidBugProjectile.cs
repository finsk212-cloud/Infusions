using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
    public class QueensVoidBugProjectile : ModProjectile
    {
        private const float SearchRadius = 500f;
        private const float HomingSpeed = 9f;
        private const float HomingStrength = 0.09f;

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.TinyEater;

        public override void SetDefaults()
        {
            var tag = Projectile.GetGlobalProjectile<AugmentProjectileTag>();
            tag.IsAugmentProcDamage = true;
            tag.SourceAugmentId = "queens_swarm";
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 180;
        }

        public override void AI()
        {
            NPC target = FindNearestTarget();
            if (target != null)
            {
                Vector2 desiredVelocity = Projectile.DirectionTo(target.Center) * HomingSpeed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, HomingStrength);
            }

            if (Projectile.velocity.LengthSquared() > 0.01f)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Lighting.AddLight(Projectile.Center, 0.18f, 0.03f, 0.28f);

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center - Projectile.velocity * 0.5f,
                Main.rand.NextBool(3) ? DustID.GemAmethyst : DustID.Shadowflame,
                -Projectile.velocity * 0.08f,
                90,
                default,
                Main.rand.NextFloat(0.65f, 0.95f)
            );
            dust.noGravity = true;
        }

        private NPC FindNearestTarget()
        {
            NPC nearest = null;
            float nearestDistanceSquared = SearchRadius * SearchRadius;

            foreach (NPC npc in Main.npc)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distanceSquared = Vector2.DistanceSquared(Projectile.Center, npc.Center);
                if (distanceSquared >= nearestDistanceSquared)
                    continue;

                nearest = npc;
                nearestDistanceSquared = distanceSquared;
            }

            return nearest;
        }
    }
}
