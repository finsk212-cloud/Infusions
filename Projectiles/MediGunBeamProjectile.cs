using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments.Projectiles
{
    public class MediGunBeamProjectile : ModProjectile
    {
        private const float MaxAcquireRange = 650f;
        private const float MaxBreakRange = 750f;
        private const int HealPulseInterval = 20; // 3 pulses per second (3 HP/sec)
        private const int HealAmount = 1;

        private int targetPlayerWhoAmI = -1;
        private int targetNPCWhoAmI = -1;
        private int healPulseTimer;

        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead || !player.channel || player.noItems || player.CCed)
            {
                Projectile.Kill();
                return;
            }

            // Eliminate duplicate projectiles: if another beam projectile exists for this owner, kill self
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.active && other.owner == Projectile.owner && other.type == Projectile.type && other.whoAmI > Projectile.whoAmI)
                {
                    Projectile.Kill();
                    return;
                }
            }

            Projectile.timeLeft = 2;

            Vector2 muzzlePos = player.MountedCenter;

            // ONLY the local owner determines targeting and sends healing requests
            if (Projectile.owner == Main.myPlayer)
            {
                // 1. Maintain or drop current target
                if (targetPlayerWhoAmI >= 0)
                {
                    Player target = Main.player[targetPlayerWhoAmI];
                    if (!target.active || target.dead || Vector2.Distance(muzzlePos, target.Center) > MaxBreakRange ||
                        !Collision.CanHitLine(muzzlePos, 1, 1, target.Center, 1, 1) || !SupportEffects.AreAllies(player, target))
                    {
                        targetPlayerWhoAmI = -1;
                    }
                }

                if (targetNPCWhoAmI >= 0)
                {
                    NPC npc = Main.npc[targetNPCWhoAmI];
                    if (!npc.active || (!npc.townNPC && npc.type != NPCID.TargetDummy && !npc.friendly) ||
                        Vector2.Distance(muzzlePos, npc.Center) > MaxBreakRange ||
                        !Collision.CanHitLine(muzzlePos, 1, 1, npc.Center, 1, 1))
                    {
                        targetNPCWhoAmI = -1;
                    }
                }

                // 2. If no target locked, acquire closest eligible target near cursor
                if (targetPlayerWhoAmI < 0 && targetNPCWhoAmI < 0)
                {
                    float bestScore = float.MaxValue;

                    // Check players first
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player candidate = Main.player[i];
                        if (!candidate.active || candidate.dead || candidate.whoAmI == player.whoAmI)
                            continue;
                        if (!SupportEffects.AreAllies(player, candidate))
                            continue;
                        if (Vector2.Distance(muzzlePos, candidate.Center) > MaxAcquireRange)
                            continue;
                        if (!Collision.CanHitLine(muzzlePos, 1, 1, candidate.Center, 1, 1))
                            continue;

                        float cursorDist = Vector2.Distance(Main.MouseWorld, candidate.Center);
                        if (cursorDist < bestScore)
                        {
                            bestScore = cursorDist;
                            targetPlayerWhoAmI = candidate.whoAmI;
                        }
                    }

                    // If no player in range, check town NPCs or target dummies
                    if (targetPlayerWhoAmI < 0)
                    {
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            NPC candidate = Main.npc[i];
                            if (!candidate.active || (!candidate.townNPC && candidate.type != NPCID.TargetDummy && !candidate.friendly))
                                continue;
                            if (Vector2.Distance(muzzlePos, candidate.Center) > MaxAcquireRange)
                                continue;
                            if (!Collision.CanHitLine(muzzlePos, 1, 1, candidate.Center, 1, 1))
                                continue;

                            float cursorDist = Vector2.Distance(Main.MouseWorld, candidate.Center);
                            if (cursorDist < bestScore)
                            {
                                bestScore = cursorDist;
                                targetNPCWhoAmI = candidate.whoAmI;
                            }
                        }
                    }
                }

                // Sync encoded target across network (players: 0..255, NPCs: 1000+, none: -1)
                int encoded = targetPlayerWhoAmI >= 0 ? targetPlayerWhoAmI : (targetNPCWhoAmI >= 0 ? 1000 + targetNPCWhoAmI : -1);
                if ((int)Projectile.ai[0] != encoded)
                {
                    Projectile.ai[0] = encoded;
                    Projectile.netUpdate = true;
                }

                // 3. Healing pulses (owner only)
                bool isLocked = targetPlayerWhoAmI >= 0 || targetNPCWhoAmI >= 0;
                if (isLocked)
                {
                    healPulseTimer++;
                    if (healPulseTimer >= HealPulseInterval)
                    {
                        healPulseTimer = 0;

                        if (targetPlayerWhoAmI >= 0)
                        {
                            Player target = Main.player[targetPlayerWhoAmI];
                            // Only heal if not already at maximum health
                            if (target.statLife < target.statLifeMax2)
                            {
                                if (Main.netMode == NetmodeID.SinglePlayer)
                                {
                                    SupportEffects.ServerHealPlayer(target, HealAmount);
                                }
                                else if (Main.netMode == NetmodeID.MultiplayerClient)
                                {
                                    ModPacket packet = ModContent.GetInstance<Augments>().GetPacket();
                                    packet.Write((byte)AugmentPacketType.MediGunHealRequest);
                                    packet.Write((byte)target.whoAmI);
                                    packet.Write(HealAmount);
                                    packet.Send();
                                }
                                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.10f, Pitch = 0.6f }, target.Center);
                            }
                        }
                        else if (targetNPCWhoAmI >= 0)
                        {
                            NPC npc = Main.npc[targetNPCWhoAmI];
                            if (npc.life < npc.lifeMax)
                            {
                                npc.life = Math.Min(npc.lifeMax, npc.life + HealAmount);
                                npc.HealEffect(HealAmount);
                                if (Main.netMode == NetmodeID.Server)
                                {
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                                }
                                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.10f, Pitch = 0.6f }, npc.Center);
                            }
                        }
                    }
                }
                else
                {
                    healPulseTimer = 0;
                }
            }
            else
            {
                // Remote clients read target from synced ai[0]
                int encoded = (int)Projectile.ai[0];
                if (encoded >= 1000 && encoded - 1000 < Main.maxNPCs)
                {
                    targetPlayerWhoAmI = -1;
                    targetNPCWhoAmI = encoded - 1000;
                }
                else if (encoded >= 0 && encoded < Main.maxPlayers)
                {
                    targetPlayerWhoAmI = encoded;
                    targetNPCWhoAmI = -1;
                }
                else
                {
                    targetPlayerWhoAmI = -1;
                    targetNPCWhoAmI = -1;
                }
            }

            // 4. Aim player and projectile position
            Vector2 aimTarget;
            bool targetActive = false;
            if (targetPlayerWhoAmI >= 0 && targetPlayerWhoAmI < Main.maxPlayers && Main.player[targetPlayerWhoAmI].active)
            {
                aimTarget = Main.player[targetPlayerWhoAmI].Center;
                targetActive = true;
            }
            else if (targetNPCWhoAmI >= 0 && targetNPCWhoAmI < Main.maxNPCs && Main.npc[targetNPCWhoAmI].active)
            {
                aimTarget = Main.npc[targetNPCWhoAmI].Center;
                targetActive = true;
            }
            else
            {
                aimTarget = Projectile.owner == Main.myPlayer ? Main.MouseWorld : muzzlePos + player.direction * Vector2.UnitX * 120f;
            }

            Vector2 aimDir = (aimTarget - muzzlePos).SafeNormalize(Vector2.UnitX * player.direction);
            player.ChangeDir(aimDir.X >= 0 ? 1 : -1);
            player.itemRotation = (float)Math.Atan2(aimDir.Y * player.direction, aimDir.X * player.direction);
            player.itemTime = 2;
            player.itemAnimation = 2;
            player.heldProj = Projectile.whoAmI;

            Projectile.Center = muzzlePos + aimDir * 32f;

            // 5. Dynamic Lighting & Dust along the beam
            float beamLightIntensity = targetActive ? 0.35f : 0.18f;
            Lighting.AddLight(Projectile.Center, 0.1f * beamLightIntensity, 0.9f * beamLightIntensity, 0.6f * beamLightIntensity);

            if (targetActive)
            {
                Lighting.AddLight(aimTarget, 0.15f, 0.8f, 0.5f);

                // Stream nano-particles along the beam
                if (Main.rand.NextBool(2))
                {
                    float t = Main.rand.NextFloat();
                    Vector2 beamPt = GetBezierPoint(Projectile.Center, aimTarget, t, true);
                    Dust d = Dust.NewDustDirect(beamPt - new Vector2(3, 3), 6, 6, DustID.GemEmerald);
                    d.noGravity = true;
                    d.velocity = (aimTarget - beamPt).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2f, 4f);
                    d.scale = Main.rand.NextFloat(0.7f, 1.1f);
                }

                // Healing aura sparkles drifting upward from target
                if (Main.rand.NextBool(3))
                {
                    Vector2 auraOffset = new Vector2(Main.rand.NextFloat(-16f, 16f), Main.rand.NextFloat(-8f, 20f));
                    Dust d = Dust.NewDustDirect(aimTarget + auraOffset, 4, 4, DustID.GreenFairy);
                    d.noGravity = true;
                    d.velocity = new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -Main.rand.NextFloat(1.2f, 2.5f));
                    d.scale = Main.rand.NextFloat(0.6f, 1.0f);
                }
            }
            else
            {
                // Searching sparks from gun muzzle
                if (Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustDirect(Projectile.Center - new Vector2(2, 2), 4, 4, DustID.Electric);
                    d.noGravity = true;
                    d.velocity = aimDir * Main.rand.NextFloat(3f, 6f) + Main.rand.NextVector2Circular(1f, 1f);
                    d.scale = 0.65f;
                }
            }
        }

        private Vector2 GetBezierPoint(Vector2 start, Vector2 end, float t, bool isLocked)
        {
            Vector2 mid = (start + end) * 0.5f;
            Vector2 diff = end - start;
            Vector2 normal = diff.SafeNormalize(Vector2.UnitY);
            Vector2 perp = new Vector2(-normal.Y, normal.X);

            float time = (float)Main.GlobalTimeWrappedHourly * 14f;
            
            float wave;
            if (isLocked)
            {
                // Smooth organic oscillation
                wave = (float)Math.Sin(time + Projectile.whoAmI * 2f) * 22f;
            }
            else
            {
                // Crackling jitter arc searching for allies
                float jitter = (float)Math.Sin(time * 2.5f + t * 12f) * 14f;
                wave = jitter;
            }

            Vector2 ctrl = mid + perp * wave;
            float u = 1f - t;
            return u * u * start + 2f * u * t * ctrl + t * t * end;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player player = Main.player[Projectile.owner];
            Vector2 muzzlePos = Projectile.Center;

            Vector2 targetPos;
            bool isLocked = false;
            if (targetPlayerWhoAmI >= 0 && targetPlayerWhoAmI < Main.maxPlayers && Main.player[targetPlayerWhoAmI].active)
            {
                targetPos = Main.player[targetPlayerWhoAmI].Center;
                isLocked = true;
            }
            else if (targetNPCWhoAmI >= 0 && targetNPCWhoAmI < Main.maxNPCs && Main.npc[targetNPCWhoAmI].active)
            {
                targetPos = Main.npc[targetNPCWhoAmI].Center;
                isLocked = true;
            }
            else
            {
                Vector2 fallbackDir = Projectile.owner == Main.myPlayer
                    ? (Main.MouseWorld - muzzlePos).SafeNormalize(Vector2.UnitX * player.direction)
                    : Vector2.UnitX * player.direction;
                targetPos = muzzlePos + fallbackDir * 220f;
            }

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int segments = 32;

            Vector2 prevPoint = muzzlePos;
            Vector2 prevRibbon1 = muzzlePos;
            Vector2 prevRibbon2 = muzzlePos;

            float time = (float)Main.GlobalTimeWrappedHourly * 14f;

            // Draw Beam Segments
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector2 currentPoint = GetBezierPoint(muzzlePos, targetPos, t, isLocked);

                Vector2 segDiff = currentPoint - prevPoint;
                float segLen = Math.Max(1f, segDiff.Length());
                float segRot = (float)Math.Atan2(segDiff.Y, segDiff.X);
                Vector2 origin = new Vector2(0f, 0.5f);

                // Perpendicular vector for helix ribbons
                Vector2 tangent = segDiff.SafeNormalize(Vector2.UnitX);
                Vector2 normal = new Vector2(-tangent.Y, tangent.X);

                // Helix Ribbon 1 (Neon Cyan)
                float ribbonWave1 = (float)Math.Sin(time * 1.5f + t * 16f) * (isLocked ? 12f : 6f);
                Vector2 ribbon1Pt = currentPoint + normal * ribbonWave1;

                // Helix Ribbon 2 (Bright Emerald - opposite phase)
                float ribbonWave2 = (float)Math.Sin(time * 1.5f + t * 16f + MathHelper.Pi) * (isLocked ? 12f : 6f);
                Vector2 ribbon2Pt = currentPoint + normal * ribbonWave2;

                // 1. Outer Wide Aura
                Color outerColor = isLocked
                    ? new Color(35, 230, 140, 60) * 0.75f
                    : new Color(50, 160, 255, 45) * 0.55f;
                Main.EntitySpriteDraw(pixel, prevPoint - Main.screenPosition, new Rectangle(0, 0, (int)segLen + 1, 16), outerColor, segRot, origin, 1f, SpriteEffects.None, 0);

                // 2. Mid Energy Sheath
                Color midColor = isLocked
                    ? new Color(75, 255, 180, 150)
                    : new Color(80, 200, 255, 110);
                Main.EntitySpriteDraw(pixel, prevPoint - Main.screenPosition, new Rectangle(0, 0, (int)segLen + 1, 7), midColor, segRot, origin, 1f, SpriteEffects.None, 0);

                // 3. Central Luminous Filament
                Color coreColor = isLocked
                    ? new Color(240, 255, 250, 240)
                    : new Color(220, 245, 255, 200);
                Main.EntitySpriteDraw(pixel, prevPoint - Main.screenPosition, new Rectangle(0, 0, (int)segLen + 1, 3), coreColor, segRot, origin, 1f, SpriteEffects.None, 0);

                // 4. Draw Helix Ribbons
                if (i > 1)
                {
                    // Ribbon 1 Segment
                    Vector2 rDiff1 = ribbon1Pt - prevRibbon1;
                    float rLen1 = Math.Max(1f, rDiff1.Length());
                    float rRot1 = (float)Math.Atan2(rDiff1.Y, rDiff1.X);
                    Color rColor1 = isLocked ? new Color(0, 255, 220, 140) : new Color(60, 180, 255, 80);
                    Main.EntitySpriteDraw(pixel, prevRibbon1 - Main.screenPosition, new Rectangle(0, 0, (int)rLen1 + 1, 3), rColor1, rRot1, origin, 1f, SpriteEffects.None, 0);

                    // Ribbon 2 Segment
                    Vector2 rDiff2 = ribbon2Pt - prevRibbon2;
                    float rLen2 = Math.Max(1f, rDiff2.Length());
                    float rRot2 = (float)Math.Atan2(rDiff2.Y, rDiff2.X);
                    Color rColor2 = isLocked ? new Color(110, 255, 130, 140) : new Color(120, 220, 255, 80);
                    Main.EntitySpriteDraw(pixel, prevRibbon2 - Main.screenPosition, new Rectangle(0, 0, (int)rLen2 + 1, 3), rColor2, rRot2, origin, 1f, SpriteEffects.None, 0);
                }

                prevPoint = currentPoint;
                prevRibbon1 = ribbon1Pt;
                prevRibbon2 = ribbon2Pt;
            }

            // Draw Surging Energy Packets (Travelling from Muzzle to Ally)
            if (isLocked)
            {
                for (int p = 0; p < 3; p++)
                {
                    float pulseT = ((float)Main.GlobalTimeWrappedHourly * 1.5f + p * 0.333f) % 1f;
                    Vector2 pulsePos = GetBezierPoint(muzzlePos, targetPos, pulseT, true);
                    float pulseScale = 1f + 0.3f * (float)Math.Sin(pulseT * MathHelper.Pi);

                    Color packetAura = new Color(0, 255, 200, 120);
                    Color packetCore = new Color(250, 255, 255, 230);

                    // Rotating diamond packet
                    float packetRot = (float)Main.GlobalTimeWrappedHourly * 8f;
                    Main.EntitySpriteDraw(pixel, pulsePos - Main.screenPosition, new Rectangle(0, 0, 10, 10), packetAura, packetRot, new Vector2(5, 5), pulseScale, SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(pixel, pulsePos - Main.screenPosition, new Rectangle(0, 0, 5, 5), packetCore, packetRot, new Vector2(2.5f, 2.5f), pulseScale, SpriteEffects.None, 0);
                }
            }

            // Draw Muzzle Energy Flare
            float muzzlePulse = 1f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 16f);
            float muzzleRot = (float)Main.GlobalTimeWrappedHourly * 3f;
            Color muzzleColor = isLocked ? new Color(60, 255, 170, 200) : new Color(80, 200, 255, 160);
            Main.EntitySpriteDraw(pixel, muzzlePos - Main.screenPosition, new Rectangle(0, 0, 14, 14), muzzleColor * 0.7f, muzzleRot, new Vector2(7, 7), muzzlePulse, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(pixel, muzzlePos - Main.screenPosition, new Rectangle(0, 0, 7, 7), Color.White, muzzleRot + MathHelper.PiOver4, new Vector2(3.5f, 3.5f), muzzlePulse, SpriteEffects.None, 0);

            // Draw Holographic Medical Target Reticle
            if (isLocked)
            {
                float reticlePulse = 1f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 10f);
                float reticleRot = (float)Main.GlobalTimeWrappedHourly * 1.5f;
                Color techGreen = new Color(70, 255, 160, 220) * reticlePulse;
                Color coreWhite = Color.White * reticlePulse;

                // 1. Rotating diamond frame
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition, new Rectangle(0, 0, 24, 2), techGreen * 0.8f, reticleRot, new Vector2(12, 1), 1f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition, new Rectangle(0, 0, 2, 24), techGreen * 0.8f, reticleRot, new Vector2(1, 12), 1f, SpriteEffects.None, 0);

                // 2. Solid Central Medical Cross
                // Horizontal bar
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition, new Rectangle(0, 0, 14, 4), techGreen, 0f, new Vector2(7, 2), 1f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition, new Rectangle(0, 0, 10, 2), coreWhite, 0f, new Vector2(5, 1), 1f, SpriteEffects.None, 0);
                // Vertical bar
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition, new Rectangle(0, 0, 4, 14), techGreen, 0f, new Vector2(2, 7), 1f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition, new Rectangle(0, 0, 2, 10), coreWhite, 0f, new Vector2(1, 5), 1f, SpriteEffects.None, 0);

                // 3. Expanding Radar Ping Ring
                float pingProgress = ((float)Main.GlobalTimeWrappedHourly * 2f) % 1f;
                float pingScale = 0.6f + pingProgress * 1.2f;
                Color pingColor = techGreen * (1f - pingProgress) * 0.6f;
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition, new Rectangle(0, 0, 20, 2), pingColor, reticleRot + MathHelper.PiOver4, new Vector2(10, 1), pingScale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition, new Rectangle(0, 0, 2, 20), pingColor, reticleRot + MathHelper.PiOver4, new Vector2(1, 10), pingScale, SpriteEffects.None, 0);
            }

            return false;
        }
    }
}
