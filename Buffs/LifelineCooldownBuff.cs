using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
    // Display-only buff shown on the saved player while Lifeline is on cooldown.
    // No gameplay effect — the actual cooldown is tracked in AugmentPlayer.LifelineCooldown.
    public class LifelineCooldownBuff : ModBuff
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.CrossNecklace;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }
    }
}
