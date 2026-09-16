using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
	// Applied while tethered to a Medic with a socketed Band of Regeneration.
	// Replicates the exact Band of Regeneration accessory passive (+2 life regen).
	public class BandOfRegenBuff : ModBuff
	{
		public override string Texture => "Terraria/Images/Buff_2";

		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
			Main.debuff[Type] = false;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			player.lifeRegen += 2;
		}
	}
}
