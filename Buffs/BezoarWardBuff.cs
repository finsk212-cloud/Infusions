using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
	public class BezoarWardBuff : ModBuff
	{
		public override string Texture => "Terraria/Images/Buff_113";

		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
			Main.debuff[Type] = false;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			player.buffImmune[BuffID.Poisoned] = true;
			player.buffImmune[BuffID.Venom] = true;
			player.ClearBuff(BuffID.Poisoned);
			player.ClearBuff(BuffID.Venom);
		}
	}
}
