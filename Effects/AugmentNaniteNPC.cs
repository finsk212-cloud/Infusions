using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
	public class AugmentNaniteNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public int NaniteTimer;
		public int InfestedByPlayer = -1;

		// Maps minion projectile identity to remaining tracking ticks (e.g. 120 ticks = 2s)
		private readonly Dictionary<int, int> recentMinionHits = new();

		public bool IsInfested => NaniteTimer > 0;

		public void ApplyNanites(int playerIndex, int minionIdentity, int durationTicks = 240)
		{
			NaniteTimer = Math.Max(NaniteTimer, durationTicks);
			InfestedByPlayer = playerIndex;

			if (minionIdentity >= 0)
			{
				recentMinionHits[minionIdentity] = 120;
			}
		}

		public int GetActiveMinionCount(Player player, NPC npc)
		{
			int count = recentMinionHits.Count;

			// Proximity check: also count active minion/sentry projectiles nearby
			int nearbyCount = 0;
			float maxRangeSq = 400f * 400f;
			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				Projectile p = Main.projectile[i];
				if (!p.active || p.owner != player.whoAmI)
					continue;

				if (p.minion || p.sentry || ProjectileID.Sets.MinionShot[p.type] || ProjectileID.Sets.SentryShot[p.type])
				{
					if (Vector2.DistanceSquared(p.Center, npc.Center) <= maxRangeSq)
						nearbyCount++;
				}
			}

			int effectiveCount = Math.Max(count, nearbyCount);
			if (player.numMinions > 0)
			{
				effectiveCount = Math.Min(effectiveCount, (int)Math.Ceiling((double)player.numMinions) + (player.maxTurrets > 0 ? 2 : 0));
			}

			return Math.Max(1, effectiveCount);
		}

		public override void PostAI(NPC npc)
		{
			if (NaniteTimer <= 0)
			{
				if (recentMinionHits.Count > 0)
					recentMinionHits.Clear();
				InfestedByPlayer = -1;
				return;
			}

			NaniteTimer--;

			// Decrement and prune expired minion hit tracking
			var keysToUpdate = new List<int>(recentMinionHits.Keys);
			foreach (int key in keysToUpdate)
			{
				int remaining = recentMinionHits[key] - 1;
				if (remaining <= 0)
					recentMinionHits.Remove(key);
				else
					recentMinionHits[key] = remaining;
			}

			// Ambient toxic neon-lime nanite particle crawl
			if (!npc.friendly && !npc.townNPC && Main.rand.NextBool(3))
			{
				Vector2 dustPos = npc.position + new Vector2(Main.rand.NextFloat(npc.width), Main.rand.NextFloat(npc.height));
				Dust d = Dust.NewDustPerfect(
					dustPos,
					DustID.TerraBlade,
					Main.rand.NextVector2Circular(0.8f, 0.8f),
					100,
					new Color(16, 185, 129),
					Main.rand.NextFloat(0.7f, 1.05f)
				);
				d.noGravity = true;
			}

			// Acid cyber-green ambient lighting
			Lighting.AddLight(npc.Center, 0.05f, 0.40f, 0.22f);
		}
	}
}