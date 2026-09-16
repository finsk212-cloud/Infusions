using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
	public class OverclockMK4Buff : ModBuff
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
			player.moveSpeed += 0.30f;
			player.GetAttackSpeed(DamageClass.Generic) += 0.20f;
			player.GetCritChance(DamageClass.Generic) += 12f;
			player.GetDamage(DamageClass.Generic) += 0.15f;

			// Celestial Overdrive sparks (Electric + Nebula / Stardust essence)
			if (player.velocity != Vector2.Zero && Main.rand.NextBool(2))
			{
				int dustType = Main.rand.NextBool() ? DustID.Electric : DustID.PurpleCrystalShard;
				Dust d = Dust.NewDustDirect(player.position, player.width, player.height, dustType, 0f, 0f, 100, default, 1.25f);
				d.noGravity = true;
				d.velocity *= 0.4f;
			}
		}
	}
}
