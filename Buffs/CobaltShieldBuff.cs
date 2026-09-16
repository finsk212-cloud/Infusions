using Terraria;
using Terraria.ModLoader;

namespace Augments
{
	public class CobaltShieldBuff : ModBuff
	{
		public override string Texture => "Terraria/Images/Buff_62";

		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
			Main.debuff[Type] = false;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			player.noKnockback = true;
			player.statDefense += 4;
		}
	}
}
