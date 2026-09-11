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
        private const int HealPulseInterval = 30; // 0.5s per pulse (10 HP/sec)
        private const int HealAmount = 5;

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
                            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.15f, Pitch = 0.5f }, target.Center);
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
                            }
                            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.15f, Pitch = 0.5f }, npc.Center);
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
            if (targetPlayerWhoAmI >= 0 && targetPlayerWhoAmI < Main.maxPlayers && Main.player[targetPlayerWhoAmI].active)
                aimTarget = Main.player[targetPlayerWhoAmI].Center;
            else if (targetNPCWhoAmI >= 0 && targetNPCWhoAmI < Main.maxNPCs && Main.npc[targetNPCWhoAmI].active)
                aimTarget = Main.npc[targetNPCWhoAmI].Center;
            else
                aimTarget = Projectile.owner == Main.myPlayer ? Main.MouseWorld : muzzlePos + player.direction * Vector2.UnitX * 100f;

            Vector2 aimDir = (aimTarget - muzzlePos).SafeNormalize(Vector2.UnitX * player.direction);
            player.ChangeDir(aimDir.X >= 0 ? 1 : -1);
            player.itemRotation = (float)Math.Atan2(aimDir.Y * player.direction, aimDir.X * player.direction);
            player.itemTime = 2;
            player.itemAnimation = 2;
            player.heldProj = Projectile.whoAmI;

            Projectile.Center = muzzlePos + aimDir * 32f;

            // 5. Dust particles along beam
            if (Main.rand.NextBool(2))
            {
                float t = Main.rand.NextFloat();
                Vector2 beamPt = GetBezierPoint(Projectile.Center, aimTarget, t);
                Dust d = Dust.NewDustDirect(beamPt - new Vector2(4, 4), 8, 8, DustID.GemEmerald);
                d.noGravity = true;
                d.velocity = (aimTarget - beamPt).SafeNormalize(Vector2.Zero) * 2f;
                d.scale = 0.85f;
            }
        }

        private Vector2 GetBezierPoint(Vector2 start, Vector2 end, float t)
        {
            Vector2 mid = (start + end) * 0.5f;
            Vector2 diff = end - start;
            Vector2 normal = diff.SafeNormalize(Vector2.UnitY);
            Vector2 perp = new Vector2(-normal.Y, normal.X);

            float time = (float)Main.GlobalTimeWrappedHourly * 16f;
            float wave = (float)Math.Sin(time + Projectile.whoAmI * 2.5f) * 18f;
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
                targetPos = muzzlePos + fallbackDir * 200f;
            }

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int segments = 22;

            Vector2 prevPoint = muzzlePos;
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector2 currentPoint = GetBezierPoint(muzzlePos, targetPos, t);

                Vector2 segDiff = currentPoint - prevPoint;
                float segLen = segDiff.Length();
                float segRot = (float)Math.Atan2(segDiff.Y, segDiff.X);
                Vector2 origin = new Vector2(0f, 0.5f);

                // Outer aura glow (green/cyan)
                Color outerColor = isLocked
                    ? new Color(50, 240, 150, 80) * 0.7f
                    : new Color(80, 180, 255, 60) * 0.5f;
                Main.EntitySpriteDraw(pixel, prevPoint - Main.screenPosition, new Rectangle(0, 0, (int)segLen, 7), outerColor, segRot, origin, 1f, SpriteEffects.None, 0);

                // Inner bright core
                Color innerColor = isLocked
                    ? new Color(220, 255, 240, 220)
                    : new Color(200, 235, 255, 180);
                Main.EntitySpriteDraw(pixel, prevPoint - Main.screenPosition, new Rectangle(0, 0, (int)segLen, 3), innerColor, segRot, origin, 1f, SpriteEffects.None, 0);

                prevPoint = currentPoint;
            }

            // Draw target reticle/cross if locked
            if (isLocked)
            {
                float pulse = 1f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 12f);
                Color crossColor = new Color(80, 255, 170, 220) * pulse;

                // Horizontal bar of cross
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition - new Vector2(10f, 2f), new Rectangle(0, 0, 20, 4), crossColor, 0f, Vector2.Zero, pulse, SpriteEffects.None, 0);
                // Vertical bar of cross
                Main.EntitySpriteDraw(pixel, targetPos - Main.screenPosition - new Vector2(2f, 10f), new Rectangle(0, 0, 4, 20), crossColor, 0f, Vector2.Zero, pulse, SpriteEffects.None, 0);
            }

            return false;
        }
    }
}
