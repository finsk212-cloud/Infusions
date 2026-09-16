using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
	public class OverclockMK3Buff : ModBuff
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
			player.moveSpeed += 0.25f;
			player.GetAttackSpeed(DamageClass.Generic) += 0.15f;
			player.GetCritChance(DamageClass.Generic) += 8f;

			// Cybernetic overdrive sparks
			if (player.velocity != Vector2.Zero && Main.rand.NextBool(2))
			{
				Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Electric, 0f, 0f, 100, default, 1.15f);
				d.noGravity = true;
				d.velocity *= 0.4f;
			}
		}
	}
}
