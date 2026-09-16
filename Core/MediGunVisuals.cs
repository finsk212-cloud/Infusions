using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments.Core
{
	public static class MediGunVisuals
	{
		public static void SpawnSocketBeamDust(Player player, Vector2 muzzlePos, Vector2 aimTarget, int socketed, bool isLocked)
		{
			if (socketed <= 0)
				return;

			Vector2 beamDiff = aimTarget - muzzlePos;
			float beamLen = beamDiff.Length();
			if (beamLen < 8f)
				return;

			Vector2 beamDir = beamDiff / beamLen;
			Vector2 normal = new Vector2(-beamDir.Y, beamDir.X);

			// 1. Band of Regeneration: Small green hearts, little stars, and green light particles
			if (socketed == ItemID.BandofRegeneration)
			{
				// Small green heart particles floating along the beam
				if (Main.rand.NextBool(3))
				{
					float t = Main.rand.NextFloat(0.1f, 0.9f);
					Vector2 pos = Vector2.Lerp(muzzlePos, aimTarget, t) + normal * Main.rand.NextFloat(-6f, 6f);
					Dust d = Dust.NewDustDirect(pos - new Vector2(4, 4), 8, 8, ModContent.DustType<GreenHeartDust>());
					d.velocity = new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(0.6f, 1.4f)) + beamDir * 1.4f;
					d.scale = Main.rand.NextFloat(0.85f, 1.25f);
				}

				// Little sparkling stars along the beam
				if (Main.rand.NextBool(3))
				{
					float t = Main.rand.NextFloat(0.05f, 0.95f);
					Vector2 pos = Vector2.Lerp(muzzlePos, aimTarget, t) + normal * Main.rand.NextFloat(-4f, 4f);
					int starDust = Main.rand.NextBool() ? DustID.StarRoyale : DustID.YellowStarDust;
					Dust d = Dust.NewDustDirect(pos - new Vector2(2, 2), 4, 4, starDust);
					d.noGravity = true;
					d.velocity = beamDir * Main.rand.NextFloat(2f, 4.5f) + Main.rand.NextVector2Circular(0.8f, 0.8f);
					d.scale = Main.rand.NextFloat(0.55f, 0.85f);
				}

				// Green light particles along the beam
				if (Main.rand.NextBool(2))
				{
					float t = Main.rand.NextFloat(0.1f, 0.9f);
					Vector2 pos = Vector2.Lerp(muzzlePos, aimTarget, t);
					Dust d = Dust.NewDustDirect(pos - new Vector2(2, 2), 4, 4, DustID.GreenFairy);
					d.noGravity = true;
					d.velocity = beamDir * 2f + new Vector2(0f, -0.5f);
					d.scale = Main.rand.NextFloat(0.65f, 0.95f);
					Lighting.AddLight(pos, 0.08f, 0.65f, 0.22f);
				}

				// Rising green hearts from locked target
				if (isLocked && Main.rand.NextBool(3))
				{
					Vector2 offset = Main.rand.NextVector2Circular(16f, 16f);
					Dust d = Dust.NewDustDirect(aimTarget + offset, 4, 4, ModContent.DustType<GreenHeartDust>());
					d.velocity = new Vector2(0f, -Main.rand.NextFloat(0.8f, 1.6f));
					d.scale = Main.rand.NextFloat(0.9f, 1.3f);
				}
			}
			// 2. Band of Starpower: Blue starlight glitter and little stars trailing along the beam
			else if (socketed == ItemID.BandofStarpower)
			{
				if (Main.rand.NextBool(2))
				{
					float t = Main.rand.NextFloat(0.05f, 0.95f);
					Vector2 pos = Vector2.Lerp(muzzlePos, aimTarget, t) + normal * Main.rand.NextFloat(-6f, 6f);
					int dustType = Main.rand.Next(3) switch
					{
						0 => DustID.StarRoyale,
						1 => DustID.ManaRegeneration,
						_ => DustID.MagicMirror
					};
					Dust d = Dust.NewDustDirect(pos - new Vector2(2, 2), 4, 4, dustType);
					d.noGravity = true;
					d.velocity = beamDir * Main.rand.NextFloat(3f, 6f) + Main.rand.NextVector2Circular(0.5f, 0.5f);
					d.scale = Main.rand.NextFloat(0.65f, 1.05f);
					Lighting.AddLight(pos, 0.1f, 0.35f, 0.85f);
				}
			}
			// 3. Hand Warmer: Small snowflakes and warm amber glow along the beam
			else if (socketed == ItemID.HandWarmer)
			{
				// Small snowflakes swirling along the beam
				if (Main.rand.NextBool(2))
				{
					float t = Main.rand.NextFloat(0.05f, 0.95f);
					Vector2 pos = Vector2.Lerp(muzzlePos, aimTarget, t) + normal * Main.rand.NextFloat(-8f, 8f);
					Dust d = Dust.NewDustDirect(pos - new Vector2(2, 2), 4, 4, DustID.SnowflakeIce);
					d.noGravity = true;
					d.velocity = beamDir * Main.rand.NextFloat(1.5f, 3.5f) + new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(0.2f, 0.8f));
					d.scale = Main.rand.NextFloat(0.55f, 0.85f);
				}

				// Warm amber fiery glow along the beam
				if (Main.rand.NextBool(2))
				{
					float t = Main.rand.NextFloat(0.05f, 0.95f);
					Vector2 pos = Vector2.Lerp(muzzlePos, aimTarget, t) + normal * Main.rand.NextFloat(-4f, 4f);
					int dustType = Main.rand.NextBool() ? DustID.GoldFlame : DustID.GemAmber;
					Dust d = Dust.NewDustDirect(pos - new Vector2(3, 3), 6, 6, dustType);
					d.noGravity = true;
					d.velocity = beamDir * Main.rand.NextFloat(2f, 4f) + new Vector2(0f, -Main.rand.NextFloat(0.3f, 0.8f));
					d.scale = Main.rand.NextFloat(0.7f, 1.05f);
					Lighting.AddLight(pos, 0.80f, 0.42f, 0.12f);
				}
			}
			// 4. Bezoar: Emerald detoxification pulses
			else if (socketed == ItemID.Bezoar)
			{
				if (Main.rand.NextBool(2))
				{
					float t = Main.rand.NextFloat(0.1f, 0.9f);
					Vector2 pos = Vector2.Lerp(muzzlePos, aimTarget, t);
					Dust d = Dust.NewDustDirect(pos - new Vector2(2, 2), 4, 4, DustID.GemEmerald);
					d.noGravity = true;
					d.velocity = beamDir * Main.rand.NextFloat(4f, 7f);
					d.scale = Main.rand.NextFloat(0.8f, 1.2f);
					Lighting.AddLight(pos, 0.05f, 0.85f, 0.35f);
				}

				// Cleansing pulse rings expanding from target
				if (isLocked && Main.rand.NextBool(5))
				{
					float ringAngle = Main.rand.NextFloat(MathHelper.TwoPi);
					Vector2 offset = ringAngle.ToRotationVector2() * Main.rand.NextFloat(8f, 22f);
					Dust d = Dust.NewDustDirect(aimTarget + offset, 4, 4, DustID.GreenFairy);
					d.noGravity = true;
					d.velocity = offset.SafeNormalize(Vector2.Zero) * 2.2f;
					d.scale = 0.9f;
				}
			}
			// 5. Cobalt Shield: Faint blue energy rings shielding the tether
			else if (socketed == ItemID.CobaltShield)
			{
				if (Main.rand.NextBool(2))
				{
					float t = Main.rand.NextFloat(0.1f, 0.9f);
					Vector2 center = Vector2.Lerp(muzzlePos, aimTarget, t);
					float ringRadius = 12f;
					float ringAngle = Main.rand.NextFloat(MathHelper.TwoPi);
					Vector2 ringOffset = normal * ((float)Math.Cos(ringAngle) * ringRadius);
					Dust d = Dust.NewDustDirect(center + ringOffset - new Vector2(2, 2), 4, 4, DustID.Cobalt);
					d.noGravity = true;
					d.velocity = beamDir * 1.5f + normal * ((float)Math.Sin(ringAngle) * 1.2f);
					d.scale = Main.rand.NextFloat(0.7f, 1.0f);
					Lighting.AddLight(center, 0.1f, 0.3f, 0.7f);
				}
			}
			// 6. Shark Tooth Necklace: Crimson piercing sparks
			else if (socketed == ItemID.SharkToothNecklace)
			{
				if (Main.rand.NextBool(3))
				{
					float t = Main.rand.NextFloat(0.1f, 0.9f);
					Vector2 pos = Vector2.Lerp(muzzlePos, aimTarget, t);
					Dust d = Dust.NewDustDirect(pos - new Vector2(2, 2), 4, 4, DustID.GemRuby);
					d.noGravity = true;
					d.velocity = beamDir * Main.rand.NextFloat(3f, 6f);
					d.scale = Main.rand.NextFloat(0.7f, 1.0f);
				}
			}
			// 7. Philosopher's Stone: Mystical alchemical shimmer
			else if (socketed == ItemID.PhilosophersStone)
			{
				if (Main.rand.NextBool(3))
				{
					float t = Main.rand.NextFloat(0.1f, 0.9f);
					Vector2 pos = Vector2.Lerp(muzzlePos, aimTarget, t);
					Dust d = Dust.NewDustDirect(pos - new Vector2(2, 2), 4, 4, DustID.Enchanted_Pink);
					d.noGravity = true;
					d.velocity = beamDir * Main.rand.NextFloat(2f, 4f) + new Vector2(0f, -0.5f);
					d.scale = Main.rand.NextFloat(0.75f, 1.1f);
				}
			}
			// 8. Anklet of the Wind / Aglet: Rapid wind gusts and swiftness streaks
			else if (socketed == ItemID.AnkletoftheWind || socketed == ItemID.Aglet)
			{
				if (Main.rand.NextBool(3))
				{
					float t = Main.rand.NextFloat(0.1f, 0.9f);
					Vector2 pos = Vector2.Lerp(muzzlePos, aimTarget, t) + normal * Main.rand.NextFloat(-4f, 4f);
					Dust d = Dust.NewDustDirect(pos - new Vector2(2, 2), 4, 4, DustID.Cloud);
					d.noGravity = true;
					d.velocity = beamDir * Main.rand.NextFloat(4f, 7f);
					d.scale = Main.rand.NextFloat(0.6f, 0.9f);
				}
			}
			// 9. Feral Claws: Sharp golden claw gleams
			else if (socketed == ItemID.FeralClaws)
			{
				if (Main.rand.NextBool(3))
				{
					float t = Main.rand.NextFloat(0.1f, 0.9f);
					Vector2 pos = Vector2.Lerp(muzzlePos, aimTarget, t);
					Dust d = Dust.NewDustDirect(pos - new Vector2(2, 2), 4, 4, DustID.YellowStarDust);
					d.noGravity = true;
					d.velocity = beamDir * Main.rand.NextFloat(3f, 6f) + Main.rand.NextVector2Circular(1f, 1f);
					d.scale = Main.rand.NextFloat(0.7f, 1.0f);
				}
			}
			// 10. Shackle: Sturdy steel sparks
			else if (socketed == ItemID.Shackle)
			{
				if (Main.rand.NextBool(4))
				{
					float t = Main.rand.NextFloat(0.1f, 0.9f);
					Vector2 pos = Vector2.Lerp(muzzlePos, aimTarget, t);
					Dust d = Dust.NewDustDirect(pos - new Vector2(2, 2), 4, 4, DustID.SilverFlame);
					d.noGravity = true;
					d.velocity = beamDir * 2f;
					d.scale = 0.7f;
				}
			}
		}
	}
}
