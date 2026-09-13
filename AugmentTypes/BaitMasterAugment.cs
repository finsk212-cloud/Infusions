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

        // Adding to player.fishingSkill in UpdateEquips directly increases the player's
        // base fishing power, which is read by Fisherman's Pocket Guide (Player.GetFishingConditions)
        // and automatically applied to all fishing catch calculations.
        public override void UpdateEquips(Player player)
        {
            Item bait = FindActiveBait(player);
            if (bait != null && bait.bait > 0)
            {
                int bonus = (int)System.Math.Round(bait.bait * BonusPercent, System.MidpointRounding.AwayFromZero);
                if (bonus <= 0 && bait.bait > 0)
                    bonus = 1;

                player.fishingSkill += bonus;
            }
        }

        private static Item FindActiveBait(Player player)
        {
            // Check ammo slots first (54-57), matching Terraria's Fishing_GetBait priority
            for (int i = 54; i < 58; i++)
            {
                Item item = player.inventory[i];
                if (item != null && item.stack > 0 && item.bait > 0)
                    return item;
            }

            // Then check main inventory slots (0-49)
            for (int i = 0; i < 50; i++)
            {
                Item item = player.inventory[i];
                if (item != null && item.stack > 0 && item.bait > 0)
                    return item;
            }

            // Check Void Bag if open
            if (player.useVoidBag())
            {
                for (int i = 0; i < 40; i++)
                {
                    Item item = player.bank4.item[i];
                    if (item != null && item.stack > 0 && item.bait > 0)
                        return item;
                }
            }

            return null;
        }
    }
}
