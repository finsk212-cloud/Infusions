using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
	public class AugmentMarksmanNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public int KineticMarkTimer;
		public int MarkedByPlayer = -1;

		public bool IsMarked => KineticMarkTimer > 0;

		public void ApplyMark(int playerIndex, int durationTicks = 240)
		{
			KineticMarkTimer = Math.Max(KineticMarkTimer, durationTicks);
			MarkedByPlayer = playerIndex;
		}

		public override void PostAI(NPC npc)
		{
			if (KineticMarkTimer <= 0)
			{
				MarkedByPlayer = -1;
				return;
			}

			KineticMarkTimer--;

			// Ambient ruby laser targeting particles
			if (!npc.friendly && !npc.townNPC)
			{
				if (Main.rand.NextBool(3))
				{
					Vector2 dustPos = npc.position + new Vector2(Main.rand.NextFloat(npc.width), Main.rand.NextFloat(npc.height));
					Dust d = Dust.NewDustPerfect(
						dustPos,
						DustID.GemRuby,
						Main.rand.NextVector2Circular(0.6f, 0.6f),
						100,
						new Color(244, 63, 94),
						Main.rand.NextFloat(0.7f, 1.1f)
					);
					d.noGravity = true;
				}

				// Periodic targeting reticle glint at NPC center
				if (KineticMarkTimer % 18 == 0)
				{
					for (int i = 0; i < 4; i++)
					{
						Vector2 offset = (i * MathHelper.PiOver2).ToRotationVector2() * 8f;
						Dust reticle = Dust.NewDustPerfect(
							npc.Center + offset,
							DustID.RedTorch,
							Vector2.Zero,
							120,
							new Color(255, 100, 130),
							1.1f
						);
						reticle.noGravity = true;
					}
				}
			}

			// Laser ruby ambient illumination
			Lighting.AddLight(npc.Center, 0.45f, 0.05f, 0.12f);
		}
	}
}
