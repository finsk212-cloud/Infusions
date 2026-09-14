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
    public class MediGunMK4BeamProjectile : ModProjectile
    {
        private const float MaxAcquireRange = 1000f;
        private const float MaxBreakRange = 1200f;
        private const int HealPulseInterval = 15; // 4 pulses per second (16 HP/sec total)
        private const int HealAmount = 4;

        private int targetPlayerWhoAmI = -1;
        private int targetNPCWhoAmI = -1;
        private int healPulseTimer;
        private Vector2 laggedMidPoint = Vector2.Zero;

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

                int encoded = targetPlayerWhoAmI >= 0 ? targetPlayerWhoAmI : (targetNPCWhoAmI >= 0 ? 1000 + targetNPCWhoAmI : -1);
                if ((int)Projectile.ai[0] != encoded)
                {
                    Projectile.ai[0] = encoded;
                    Projectile.netUpdate = true;
                }

                // 3. Endgame Sustained Healing Pulses
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
                                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.08f, Pitch = 1.35f }, target.Center);
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
                                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.08f, Pitch = 1.35f }, npc.Center);
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
                aimTarget = Projectile.owner == Main.myPlayer ? Main.MouseWorld : muzzlePos + player.direction * Vector2.UnitX * 180f;
            }

            Vector2 aimDir = (aimTarget - muzzlePos).SafeNormalize(Vector2.UnitX * player.direction);
            player.ChangeDir(aimDir.X >= 0 ? 1 : -1);
            player.itemRotation = (float)Math.Atan2(aimDir.Y * player.direction, aimDir.X * player.direction);
            player.itemTime = 2;
            player.itemAnimation = 2;
            player.heldProj = Projectile.whoAmI;

            Projectile.Center = muzzlePos + aimDir * 40f;

            // 5. Dynamic Cosmic Lighting & Dust
            float beamLightIntensity = targetActive ? 0.75f : 0.35f;
            Lighting.AddLight(Projectile.Center, 0.9f * beamLightIntensity, 0.7f * beamLightIntensity, 1.0f * beamLightIntensity);

            if (targetActive)
            {
                Lighting.AddLight(aimTarget, 0.95f, 0.75f, 1.0f);

                // Cosmic Solar Flare & Vortex particles streaming towards ally
                if (Main.rand.NextBool(2))
                {
                    float t = Main.rand.NextFloat();
                    Vector2 beamPt = Vector2.Lerp(Projectile.Center, aimTarget, t);
                    int dustType = Main.rand.NextBool() ? DustID.SolarFlare : DustID.Vortex;
                    Dust d = Dust.NewDustDirect(beamPt - new Vector2(2, 2), 4, 4, dustType);
                    d.noGravity = true;
                    d.velocity = (aimTarget - beamPt).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(5f, 9f);
                    d.scale = Main.rand.NextFloat(0.85f, 1.35f);
                }

                // Orbiting celestial starlight motes rising around target
                if (Main.rand.NextBool(2))
                {
                    Vector2 auraOffset = new Vector2(Main.rand.NextFloat(-24f, 24f), Main.rand.NextFloat(-16f, 26f));
                    int dustType = Main.rand.NextBool() ? DustID.Enchanted_Pink : DustID.GemDiamond;
                    Dust d = Dust.NewDustDirect(aimTarget + auraOffset, 4, 4, dustType);
                    d.noGravity = true;
                    d.velocity = new Vector2(Main.rand.NextFloat(-0.8f, 0.8f), -Main.rand.NextFloat(2f, 3.8f));
                    d.scale = Main.rand.NextFloat(0.9f, 1.4f);
                }
            }
            else
            {
                if (Main.rand.NextBool(2))
                {
                    int dustType = Main.rand.NextBool() ? DustID.SolarFlare : DustID.Vortex;
                    Dust d = Dust.NewDustDirect(Projectile.Center - new Vector2(2, 2), 4, 4, dustType);
                    d.noGravity = true;
                    d.velocity = aimDir * Main.rand.NextFloat(5f, 10f) + Main.rand.NextVector2Circular(1.8f, 1.8f);
                    d.scale = 0.8f;
                }
            }
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
                targetPos = muzzlePos + fallbackDir * 320f;
            }

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int segments = 56;

            Vector2 beamDiff = targetPos - muzzlePos;
            float totalLen = Math.Max(1f, beamDiff.Length());
            Vector2 beamDir = beamDiff / totalLen;
            Vector2 normal = new Vector2(-beamDir.Y, beamDir.X);

            // Dynamic Bézier curvature
            Vector2 idealMid = (muzzlePos + targetPos) * 0.5f;
            if (laggedMidPoint == Vector2.Zero || Vector2.DistanceSquared(laggedMidPoint, idealMid) > 1200f * 1200f)
            {
                laggedMidPoint = idealMid;
            }
            else
            {
                laggedMidPoint = Vector2.Lerp(laggedMidPoint, idealMid, 0.14f);
                Vector2 offset = laggedMidPoint - idealMid;
                float maxBow = Math.Min(65f, totalLen * 0.22f);
                if (offset.Length() > maxBow)
                {
                    laggedMidPoint = idealMid + Vector2.Normalize(offset) * maxBow;
                }
            }

            Vector2 controlPoint = 2f * laggedMidPoint - idealMid;
            float time = (float)Main.GlobalTimeWrappedHourly;

            // 4-Strand Quad-Helix Arrays: 0 = Solar, 1 = Vortex, 2 = Nebula, 3 = Stardust
            Vector2[] centerPoints = new Vector2[segments + 1];
            Vector2[][] strandPoints = new Vector2[4][];
            float[][] strandDepth = new float[4][];
            for (int s = 0; s < 4; s++)
            {
                strandPoints[s] = new Vector2[segments + 1];
                strandDepth[s] = new float[segments + 1];
            }

            Color[] strandColorsLocked = new Color[] {
                new Color(255, 175, 45, 230),  // Solar Gold
                new Color(0, 255, 190, 230),   // Vortex Neon Teal
                new Color(220, 65, 255, 230),  // Nebula Violet
                new Color(45, 180, 255, 230)   // Stardust Azure
            };

            Color[] strandColorsIdle = new Color[] {
                new Color(230, 140, 40, 120),
                new Color(0, 210, 160, 120),
                new Color(180, 50, 220, 120),
                new Color(40, 150, 220, 120)
            };

            float waveSpeed = 14f;
            float waveFreq = 30f;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float invT = 1f - t;

                Vector2 cPt = invT * invT * muzzlePos + 2f * invT * t * controlPoint + t * t * targetPos;
                if (!isLocked)
                {
                    cPt += normal * ((float)Math.Sin(time * 7f + t * 20f) * 5f);
                }
                centerPoints[i] = cPt;

                Vector2 tangent = 2f * invT * (controlPoint - muzzlePos) + 2f * t * (targetPos - controlPoint);
                Vector2 segNormal = tangent.LengthSquared() > 0.001f
                    ? new Vector2(-tangent.Y, tangent.X).SafeNormalize(normal)
                    : normal;

                float envelope = MathHelper.Clamp((float)Math.Sin(t * MathHelper.Pi) * 1.5f, 0.2f, 1f);
                float radius = (isLocked ? 14f : 7f) * envelope;

                // 4 Strands spaced evenly at 90 degree offsets (0, Pi/2, Pi, 3Pi/2)
                for (int s = 0; s < 4; s++)
                {
                    float angle = time * waveSpeed - t * waveFreq + (s * MathHelper.PiOver2);
                    strandPoints[s][i] = cPt + segNormal * ((float)Math.Sin(angle) * radius);
                    strandDepth[s][i] = (float)Math.Cos(angle);
                }
            }

            Vector2 origin = new Vector2(0f, 0.5f);

            // ==========================================
            // PASS 1: Draw All 4 Strands Behind Center Beam (depth < 0)
            // ==========================================
            for (int s = 0; s < 4; s++)
            {
                Color strandCol = isLocked ? strandColorsLocked[s] * 0.5f : strandColorsIdle[s] * 0.4f;
                for (int i = 1; i <= segments; i++)
                {
                    if (strandDepth[s][i] < 0 || strandDepth[s][i - 1] < 0)
                    {
                        Vector2 diff = strandPoints[s][i] - strandPoints[s][i - 1];
                        float len = Math.Max(1f, diff.Length());
                        float rot = (float)Math.Atan2(diff.Y, diff.X);
                        Main.EntitySpriteDraw(pixel, strandPoints[s][i - 1] - Main.screenPosition, new Rectangle(0, 0, (int)len + 1, 2), strandCol, rot, origin, 1f, SpriteEffects.None, 0);
                    }
                }
            }

            // ==========================================
            // PASS 2: Heavy Celestial Super-Conduit Core Beam (Thick, Radiant & Pulsing)
            // ==========================================
            for (int i = 1; i <= segments; i++)
            {
                Vector2 segDiff = centerPoints[i] - centerPoints[i - 1];
                float segLen = Math.Max(1f, segDiff.Length());
                float segRot = (float)Math.Atan2(segDiff.Y, segDiff.X);

                // 1. Broad Solar-Stardust Celestial Corona (14px)
                Color coronaColor = isLocked
                    ? new Color(255, 140, 50, 60) * 0.9f
                    : new Color(120, 80, 255, 30) * 0.6f;
                Main.EntitySpriteDraw(pixel, centerPoints[i - 1] - Main.screenPosition, new Rectangle(0, 0, (int)segLen + 1, 14), coronaColor, segRot, origin, 1f, SpriteEffects.None, 0);

                // 2. Intense Luminite-Solar Plasma Core (7px)
                Color plasmaColor = isLocked
                    ? new Color(255, 235, 130, 200)
                    : new Color(130, 220, 255, 140);
                Main.EntitySpriteDraw(pixel, centerPoints[i - 1] - Main.screenPosition, new Rectangle(0, 0, (int)segLen + 1, 7), plasmaColor, segRot, origin, 1f, SpriteEffects.None, 0);

                // 3. Central Searing White-Hot Singularity (3px)
                Color whiteHot = isLocked ? Color.White : new Color(240, 255, 255, 220);
                Main.EntitySpriteDraw(pixel, centerPoints[i - 1] - Main.screenPosition, new Rectangle(0, 0, (int)segLen + 1, 3), whiteHot, segRot, origin, 1f, SpriteEffects.None, 0);
            }

            // ==========================================
            // PASS 3: Draw All 4 Strands In Front of Center Beam (depth >= 0)
            // ==========================================
            for (int s = 0; s < 4; s++)
            {
                Color strandCol = isLocked ? strandColorsLocked[s] : strandColorsIdle[s];
                for (int i = 1; i <= segments; i++)
                {
                    if (strandDepth[s][i] >= 0 || strandDepth[s][i - 1] >= 0)
                    {
                        Vector2 diff = strandPoints[s][i] - strandPoints[s][i - 1];
                        float len = Math.Max(1f, diff.Length());
                        float rot = (float)Math.Atan2(diff.Y, diff.X);
                        Main.EntitySpriteDraw(pixel, strandPoints[s][i - 1] - Main.screenPosition, new Rectangle(0, 0, (int)len + 1, 3), strandCol, rot, origin, 1f, SpriteEffects.None, 0);
                    }
                }
            }

            // ==========================================
            // PASS 4: Orbiting Celestial Hadron Ring Nodes along the Beam
            // ==========================================
            if (isLocked)
            {
                // 5 superconducting orbital nodes travelling along the beam
                for (int n = 0; n < 5; n++)
                {
                    float nodeT = ((float)Main.GlobalTimeWrappedHourly * 1.6f + n * 0.2f) % 1f;
                    float invN = 1f - nodeT;
                    Vector2 nodePos = invN * invN * muzzlePos + 2f * invN * nodeT * controlPoint + nodeT * nodeT * targetPos;

                    Vector2 tangent = 2f * invN * (controlPoint - muzzlePos) + 2f * nodeT * (targetPos - controlPoint);
                    Vector2 segNorm = tangent.LengthSquared() > 0.001f ? new Vector2(-tangent.Y, tangent.X).SafeNormalize(normal) : normal;
                    float rot = (float)Math.Atan2(segNorm.Y, segNorm.X);

                    float ringScale = 1.1f + 0.3f * (float)Math.Sin(nodeT * MathHelper.Pi);
                    Color nodeCol = strandColorsLocked[n % 4];

                    // Draw perpendicular diamond accelerator ring
                    Main.EntitySpriteDraw(pixel, nodePos - Main.screenPosition, new Rectangle(0, 0, 18, 3), nodeCol * 0.9f, rot, new Vector2(9f, 1.5f), ringScale, SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(pixel, nodePos - Main.screenPosition, new Rectangle(0, 0, 8, 8), Color.White, rot + MathHelper.PiOver4, new Vector2(4f, 4f), ringScale * 0.7f, SpriteEffects.None, 0);
                }
            }

            // ==========================================
            // PASS 5: Muzzle Celestial Supernova Flare
            // ==========================================
            float muzzlePulse = 1f + 0.25f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 24f);
            float muzzleRot = (float)Main.GlobalTimeWrappedHourly * 4f;

            // Solar fire core flare
            Main.EntitySpriteDraw(pixel, muzzlePos - Main.screenPosition, new Rectangle(0, 0, 20, 20), new Color(255, 160, 40, 220) * 0.8f, muzzleRot, new Vector2(10, 10), muzzlePulse, SpriteEffects.None, 0);
            // Vortex star rays
            Main.EntitySpriteDraw(pixel, muzzlePos - Main.screenPosition, new Rectangle(0, 0, 12, 12), new Color(0, 255, 200, 230), muzzleRot + MathHelper.PiOver4, new Vector2(6, 6), muzzlePulse * 1.1f, SpriteEffects.None, 0);
            // Brilliant white core
            Main.EntitySpriteDraw(pixel, muzzlePos - Main.screenPosition, new Rectangle(0, 0, 6, 6), Color.White, muzzleRot * 2f, new Vector2(3, 3), muzzlePulse, SpriteEffects.None, 0);

            // ==========================================
            // PASS 6: Grand Celestial Mandala & Zodiac Aegis at Target Ally
            // ==========================================
            if (isLocked)
            {
                float mandalaTime = (float)Main.GlobalTimeWrappedHourly;
                float pulse = 1f + 0.12f * (float)Math.Sin(mandalaTime * 14f);

                // 1. Massive 8-Spoke Solar Mandala Ring (Radius 36px)
                float rot1 = mandalaTime * 1.5f;
                Color solarColor = new Color(255, 175, 40, 220) * pulse;
                for (int sp = 0; sp < 8; sp++)
                {
                    float angle = rot1 + sp * MathHelper.PiOver4;
                    Vector2 spokeOffset = angle.ToRotationVector2() * 30f;
                    Main.EntitySpriteDraw(pixel, targetPos + spokeOffset - Main.screenPosition, new Rectangle(0, 0, 14, 3), solarColor, angle, new Vector2(7f, 1.5f), 1f, SpriteEffects.None, 0);
                }

                // 2. Counter-Rotating Diamond Ring in Vortex Teal (Radius 24px)
                float rot2 = -mandalaTime * 2.2f;
                Color vortexColor = new Color(0, 255, 200, 220) * pulse;
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition, new Rectangle(0, 0, 36, 2), vortexColor, rot2, new Vector2(18, 1), 1f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition, new Rectangle(0, 0, 2, 36), vortexColor, rot2, new Vector2(1, 18), 1f, SpriteEffects.None, 0);

                // 3. Four Orbiting Lunar Spheres circling the ally (90 deg intervals)
                for (int m = 0; m < 4; m++)
                {
                    float orbAngle = mandalaTime * 3f + (m * MathHelper.PiOver2);
                    Vector2 orbPos = targetPos + orbAngle.ToRotationVector2() * 26f;
                    Color orbCol = strandColorsLocked[m];
                    Main.EntitySpriteDraw(pixel, orbPos - Main.screenPosition, new Rectangle(0, 0, 8, 8), orbCol, orbAngle, new Vector2(4, 4), 1f, SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(pixel, orbPos - Main.screenPosition, new Rectangle(0, 0, 4, 4), Color.White, orbAngle + MathHelper.PiOver4, new Vector2(2, 2), 1f, SpriteEffects.None, 0);
                }

                // 4. Central 8-Pointed Star of Life (Radiant White & Nebula Violet)
                Color starAura = new Color(230, 70, 255, 230) * pulse;
                Color starCore = Color.White * pulse;

                // Cardinal Cross
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition, new Rectangle(0, 0, 20, 5), starAura, 0f, new Vector2(10, 2.5f), 1f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition, new Rectangle(0, 0, 5, 20), starAura, 0f, new Vector2(2.5f, 10), 1f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition, new Rectangle(0, 0, 16, 3), starCore, 0f, new Vector2(8, 1.5f), 1f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition, new Rectangle(0, 0, 3, 16), starCore, 0f, new Vector2(1.5f, 8), 1f, SpriteEffects.None, 0);

                // Diagonal Star Rays
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition, new Rectangle(0, 0, 14, 3), starAura, MathHelper.PiOver4, new Vector2(7, 1.5f), 1f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition, new Rectangle(0, 0, 3, 14), starAura, MathHelper.PiOver4, new Vector2(1.5f, 7), 1f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition, new Rectangle(0, 0, 10, 2), starCore, MathHelper.PiOver4, new Vector2(5, 1), 1f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition, new Rectangle(0, 0, 2, 10), starCore, MathHelper.PiOver4, new Vector2(1, 5), 1f, SpriteEffects.None, 0);

                // 5. Pulsing Celestial Resonant Wave Ring
                float ringProgress = (mandalaTime * 2.5f) % 1f;
                float ringScale = 0.5f + ringProgress * 1.5f;
                Color ringColor = new Color(255, 200, 50, 200) * (1f - ringProgress) * 0.7f;
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition, new Rectangle(0, 0, 34, 2), ringColor, rot1 + MathHelper.PiOver4, new Vector2(17, 1), ringScale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition, new Rectangle(0, 0, 2, 34), ringColor, rot1 + MathHelper.PiOver4, new Vector2(1, 17), ringScale, SpriteEffects.None, 0);
            }

            return false;
        }
    }
}
