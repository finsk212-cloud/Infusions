using Terraria;

namespace Augments
{
    public class DebugFullCritAugment : Augment
    {
        public override string Id => "debug_full_crit";
        public override string DisplayName => "[DEBUG] Full Crit";
        public override string Description =>
            $"Testing only. Guarantees {AugmentText.Crit("100% crit chance")} on all weapons.";

        public override AugmentRarity Rarity => AugmentRarity.Common;
        public override AugmentClass Class => AugmentClass.Universal;

        public override bool IsDebugOnly => true;

        public override void ModifyWeaponCrit(Player player, Item item, ref float crit)
        {
            crit += 100f;
        }
    }
}
