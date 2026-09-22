using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Augments.Core;

namespace Augments
{
	// Tracks a custom movement-slow effect per NPC. Vanilla's BuffID.Slow is
	// hardcoded to only affect players - AddBuff will happily attach it to an
	// NPC, but it's a no-op - so Time Warp needs its own tracked slow instead.
	public class AugmentSlowNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		private int ticksRemaining;
		private float slowPercent;

		public bool IsActive => ticksRemaining > 0;

		// Call this to start (or refresh) a slow on this NPC.
		public void ApplySlow(int durationTicks, float percent)
		{
			ticksRemaining = Math.Max(ticksRemaining, durationTicks);
			slowPercent = Math.Max(slowPercent, percent);
		}

		public override void PostAI(NPC npc)
		{
			// Cryo Protocol (Absolute Zero): Enemies afflicted with Frostburn are slowed by 30%
			bool isFrostburned = npc.HasBuff(BuffID.Frostburn) || npc.HasBuff(BuffID.Frostburn2);
			if (isFrostburned && !npc.friendly && !npc.townNPC)
			{
				bool cryoActive = false;
				if (Main.netMode == NetmodeID.SinglePlayer)
				{
					var ap = Main.LocalPlayer?.GetModPlayer<AugmentPlayer>();
					cryoActive = ap != null && AugmentFamilyRegistry.GetOwnedCount(ap, AugmentFamilyRegistry.CryoId) >= 2;
				}
				else
				{
					for (int i = 0; i < Main.maxPlayers; i++)
					{
						Player p = Main.player[i];
						if (p.active && AugmentFamilyRegistry.GetOwnedCount(p.GetModPlayer<AugmentPlayer>(), AugmentFamilyRegistry.CryoId) >= 2)
						{
							cryoActive = true;
							break;
						}
					}
				}

				if (cryoActive)
				{
					// Smooth non-compounding displacement slow: reduce movement by 20% without crushing velocity
					npc.position -= npc.velocity * 0.20f;
					if (Main.rand.NextBool(6))
					{
						Dust d = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.IceTorch, 0f, 0f, 100, default, 1.1f);
						d.noGravity = true;
						d.velocity *= 0.4f;
					}
				}
			}

			if (ticksRemaining <= 0)
				return;

			ticksRemaining--;
			npc.position -= npc.velocity * slowPercent;
		}
	}
}
