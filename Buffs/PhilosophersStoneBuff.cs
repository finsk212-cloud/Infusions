using System;
using Terraria;
using Terraria.ModLoader;

namespace Augments
{
	public class PhilosophersStoneBuff : ModBuff
	{
		public override string Texture => "Terraria/Images/Buff_58";

		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
			Main.debuff[Type] = false;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			// Shaves 20 seconds off standard 60-second Potion Sickness (reduced to 40s)
			if (player.potionDelay > 40 * 60)
			{
				player.potionDelay = Math.Min(player.potionDelay, 40 * 60);
			}
		}
	}
}
