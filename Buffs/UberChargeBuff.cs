using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
	public class UberChargeBuff : ModBuff
	{
		public override string Texture => "Terraria/Images/Buff_150";

		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = false;
			Main.debuff[Type] = false;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			player.noKnockback = true;

			// Clear standard harmful debuffs while invulnerable
			int[] debuffsToClear =
			{
				BuffID.Poisoned,
				BuffID.OnFire,
				BuffID.OnFire3,
				BuffID.Frostburn,
				BuffID.Frostburn2,
				BuffID.Venom,
				BuffID.CursedInferno,
				BuffID.Ichor,
				BuffID.Bleeding,
				BuffID.Confused,
				BuffID.Slow,
				BuffID.Weak,
				BuffID.Silenced,
				BuffID.BrokenArmor,
				BuffID.Electrified
			};

			for (int i = 0; i < debuffsToClear.Length; i++)
			{
				player.ClearBuff(debuffsToClear[i]);
			}

			// Keep immunity timer refreshed as an extra safeguard
			player.immune = true;
			player.immuneTime = Math.Max(player.immuneTime, 30);

			// Invulnerability sparkling visuals
			if (Main.rand.NextBool(3))
			{
				Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Electric, 0f, 0f, 100, default, 1.1f);
				d.noGravity = true;
				d.velocity *= 0.5f;
			}
			if (Main.rand.NextBool(4))
			{
				Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.GoldFlame, 0f, 0f, 100, default, 1.2f);
				d.noGravity = true;
				d.velocity *= 0.4f;
			}
		}
	}
}
