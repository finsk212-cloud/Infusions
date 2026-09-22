using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
	public class SynchronizerRepairBuff : ModBuff
	{
		public override string Texture => "Terraria/Images/Buff_" + BuffID.Regeneration;

		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = false;
			Main.debuff[Type] = false;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			var ap = player.GetModPlayer<AugmentPlayer>();
			if (!ap.HasAugment("type_s_synchronizer_protocol"))
			{
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}
}
