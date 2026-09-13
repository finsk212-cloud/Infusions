using Terraria;

namespace Augments
{
    public class BaitMasterAugment : Augment
    {
        public override string Id => "bait_master";
        public override string DisplayName => "Bait Master";
        public override string Description =>
            $"Fishing bait grants {AugmentText.BonusDamage("+30% more fishing power")} than normal.";

        public override AugmentRarity Rarity => AugmentRarity.Rare;
        public override AugmentClass Class => AugmentClass.Universal;

        private const float BonusPercent = 0.3f;

        // GetFishingLevel hands us the actual bait Item being used
        // separately from the rod, and Item.bait is that bait's own raw
        // power stat - so the bonus here is genuinely isolated to bait's
        // contribution specifically, not a blanket boost to the whole
        // fishingLevel calculation (which would also include the rod).
        public override void GetFishingLevel(Player player, Item fishingRod, Item bait, ref float fishingLevel)
        {
            if (bait != null && bait.bait > 0)
                fishingLevel += bait.bait * BonusPercent;
        }
    }
}
