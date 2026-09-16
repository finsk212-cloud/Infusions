using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
	public class HandWarmerBuff : ModBuff
	{
		public override string Texture => "Terraria/Images/Buff_124";

		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
			Main.debuff[Type] = false;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			player.buffImmune[BuffID.Chilled] = true;
			player.buffImmune[BuffID.Frozen] = true;
			player.ClearBuff(BuffID.Chilled);
			player.ClearBuff(BuffID.Frozen);
			player.resistCold = true;
		}
	}
}
