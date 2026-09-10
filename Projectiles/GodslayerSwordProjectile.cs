using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
    public class GodslayerSwordProjectile : ModProjectile
    {
        private const float SplashRadius = 160f;
        private const int IchorDurationTicks = 120;
        private const float HomingSpeed = 6f;
        private const float HomingStrength = 0.08f;

        public override string Texture => "Terraria/Images/Item_" + ItemID.StarWrath;

        public override void SetDefaults()
        {
            Projectile.GetGlobalProjectile<AugmentProjectileTag>().IsAugmentProcDamage = true;
            Projectile.width = 36;
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 90;
            Projectile.ignoreWater = true;
            // Star Wrath's item sprite is baked at a 45-degree angle. Rotating
            // it another 135 degrees makes its blade point straight down.
            Projectile.rotation = MathHelper.PiOver2 + MathHelper.PiOver4;
        }

        public override void AI()
        {
            Projectile.rotation = MathHelper.PiOver2 + MathHelper.PiOver4;
            Projectile.localAI[0]++;

            int targetIndex = (int)Projectile.ai[0];
            if (targetIndex >= 0 && targetIndex < Main.maxNPCs)
            {
                NPC target = Main.npc[targetIndex];
                if (target.active && !target.friendly && !target.dontTakeDamage)
                {
                    float desiredXSpeed = MathHelper.Clamp(
                        (target.Center.X - Projectile.Center.X) * 0.04f,
                        -HomingSpeed,
                        HomingSpeed
                    );
                    Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, desiredXSpeed, HomingStrength);
                }
            }

            Lighting.AddLight(Projectile.Center, 0.85f, 0.65f, 0.3f);

            // Two motes spiral around the blade, producing a tight celestial
            // helix instead of an ordinary random dust trail.
            float phase = Projectile.localAI[0] * 0.42f;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 orbitOffset = new Vector2(
                    MathF.Sin(phase) * 22f * side,
                    MathF.Cos(phase) * 5f
                );

                Dust mote = Dust.NewDustPerfect(
                    Projectile.Center + orbitOffset,
                    side == 1 ? DustID.GoldFlame : DustID.GemDiamond,
                    -Projectile.velocity * 0.12f,
                    80,
                    side == 1 ? new Color(255, 205, 80) : new Color(110, 225, 255),
                    1.05f
                );

                mote.noGravity = true;
            }

            // A small six-point seal flashes behind the sword at intervals.
            if ((int)Projectile.localAI[0] % 6 == 0)
                SpawnCelestialSeal();
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 12; i++)
            {
                float angle = MathHelper.TwoPi * i / 12f;
                Vector2 velocity = angle.ToRotationVector2() * 3.5f;
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    i % 2 == 0 ? DustID.GoldFlame : DustID.GemDiamond,
                    velocity,
                    70,
                    default,
                    1.25f
                );
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // A sharp magical crack layered with a lower metallic impact.
            SoundEngine.PlaySound(SoundID.Item71 with { Pitch = 0.25f, Volume = 0.9f }, target.Center);
            SoundEngine.PlaySound(SoundID.Item4 with { Pitch = -0.35f, Volume = 0.65f }, target.Center);

            target.AddBuff(BuffID.Ichor, IchorDurationTicks);
            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendData(MessageID.NPCBuffs, number: target.whoAmI);
            SpawnImpactParticles(target.Center);
            DamageNearbyEnemies(target);
        }

        private void DamageNearbyEnemies(NPC directTarget)
        {
            foreach (NPC npc in Main.npc)
            {
                if (npc == directTarget || !npc.active || npc.friendly || npc.townNPC || npc.dontTakeDamage)
                    continue;

                if (npc.Distance(directTarget.Center) > SplashRadius)
                    continue;

                int hitDirection = npc.Center.X >= directTarget.Center.X ? 1 : -1;
                npc.SimpleStrikeNPC(Projectile.damage, hitDirection);
                npc.AddBuff(BuffID.Ichor, IchorDurationTicks);
                if (Main.netMode != NetmodeID.SinglePlayer)
                    NetMessage.SendData(MessageID.NPCBuffs, number: npc.whoAmI);
                SpawnSplashHitParticles(npc.Center);
            }
        }

        private static void SpawnSplashHitParticles(Vector2 center)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = MathHelper.TwoPi * i / 8f;
                Dust dust = Dust.NewDustPerfect(
                    center,
                    i % 2 == 0 ? DustID.GoldFlame : DustID.GemDiamond,
                    angle.ToRotationVector2() * 3f,
                    60,
                    default,
                    0.9f
                );
                dust.noGravity = true;
            }
        }

        private static void SpawnImpactParticles(Vector2 center)
        {
            // Two expanding, counter-rotated rings make the impact resemble a
            // broken celestial seal rather than a generic circular explosion.
            for (int ring = 0; ring < 2; ring++)
            {
                int count = ring == 0 ? 12 : 18;
                float speed = ring == 0 ? 3.5f : 6.5f;
                float rotation = ring * MathHelper.Pi / count;

                for (int i = 0; i < count; i++)
                {
                    float angle = MathHelper.TwoPi * i / count + rotation;
                    Vector2 velocity = angle.ToRotationVector2() * speed;
                    Dust dust = Dust.NewDustPerfect(
                        center,
                        i % 3 == 0 ? DustID.GemDiamond : DustID.GoldFlame,
                        velocity,
                        40,
                        i % 3 == 0 ? new Color(120, 235, 255) : new Color(255, 210, 85),
                        ring == 0 ? 1.35f : 1.05f
                    );
                    dust.noGravity = true;
                }
            }

            // Energy punches through the enemy along the sword's vertical
            // path, emphasizing that the strike came from the sky.
            for (int i = 0; i < 20; i++)
            {
                float horizontalSpread = Main.rand.NextFloat(-2.2f, 2.2f);
                float verticalSpeed = Main.rand.NextFloat(2.5f, 8f) * (i % 2 == 0 ? -1f : 1f);
                Dust dust = Dust.NewDustPerfect(
                    center + new Vector2(Main.rand.NextFloat(-12f, 12f), Main.rand.NextFloat(-18f, 18f)),
                    i % 4 == 0 ? DustID.GemDiamond : DustID.GoldFlame,
                    new Vector2(horizontalSpread, verticalSpeed),
                    30,
                    default,
                    Main.rand.NextFloat(1.1f, 1.65f)
                );
                dust.noGravity = true;
            }

            // Bright stationary core flash.
            for (int i = 0; i < 8; i++)
            {
                Dust flash = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(16f, 16f),
                    DustID.GemDiamond,
                    Vector2.Zero,
                    0,
                    Color.White,
                    1.6f
                );
                flash.noGravity = true;
            }
        }

        private void SpawnCelestialSeal()
        {
            Vector2 sealCenter = Projectile.Center - Projectile.velocity * 1.5f;

            for (int i = 0; i < 6; i++)
            {
                float angle = MathHelper.TwoPi * i / 6f + Projectile.localAI[0] * 0.08f;
                Vector2 offset = angle.ToRotationVector2() * 18f;
                Vector2 tangent = (angle + MathHelper.PiOver2).ToRotationVector2() * 0.8f;
                Dust dust = Dust.NewDustPerfect(
                    sealCenter + offset,
                    i % 2 == 0 ? DustID.GoldFlame : DustID.GemDiamond,
                    tangent,
                    90,
                    default,
                    0.9f
                );
                dust.noGravity = true;
            }
        }
    }
}
