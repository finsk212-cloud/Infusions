using Terraria;
using Terraria.ModLoader;

namespace Augments
{
	public class FeralClawsBuff : ModBuff
	{
		public override string Texture => "Terraria/Images/Buff_117";

		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
			Main.debuff[Type] = false;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			player.GetAttackSpeed(DamageClass.Melee) += 0.15f;
			player.autoReuseGlove = true;
		}
	}
}
