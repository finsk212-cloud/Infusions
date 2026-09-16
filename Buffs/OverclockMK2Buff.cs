using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
	public class OverclockMK2Buff : ModBuff
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
			player.moveSpeed += 0.20f;
			player.GetAttackSpeed(DamageClass.Generic) += 0.10f;

			// Cybernetic speed & combat sparks while active
			if (player.velocity != Vector2.Zero && Main.rand.NextBool(3))
			{
				Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Electric, 0f, 0f, 100, default, 1.0f);
				d.noGravity = true;
				d.velocity *= 0.35f;
			}
		}
	}
}
