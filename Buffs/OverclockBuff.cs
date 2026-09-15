using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
	public class OverclockBuff : ModBuff
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

			// Cybernetic speed sparks while moving
			if (player.velocity != Vector2.Zero && Main.rand.NextBool(4))
			{
				Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Electric, 0f, 0f, 100, default, 0.9f);
				d.noGravity = true;
				d.velocity *= 0.3f;
			}
		}
	}
}
