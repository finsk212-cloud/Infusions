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

        public const float BonusPercent = 0.3f;

        // Adding to player.fishingSkill in UpdateEquips directly increases the player's
        // base fishing power, which is read by Fisherman's Pocket Guide (Player.GetFishingConditions)
        // and automatically applied to all fishing catch calculations.
        public override void UpdateEquips(Player player)
        {
            Item bait = FindActiveBait(player);
            if (bait != null && bait.bait > 0)
            {
                int bonus = GetBaitBonus(bait.bait);
                player.fishingSkill += bonus;
            }
        }

        public static int GetBaitBonus(int rawBait)
        {
            if (rawBait <= 0)
                return 0;

            int bonus = (int)System.Math.Round(rawBait * BonusPercent, System.MidpointRounding.AwayFromZero);
            return bonus > 0 ? bonus : 1;
        }

        public static Item FindActiveBait(Player player)
        {
            if (player == null || player.inventory == null)
                return null;

            // 1. Mouse cursor item (if player is dragging or holding bait on mouse)
            if (Main.mouseItem != null && !Main.mouseItem.IsAir && Main.mouseItem.stack > 0 && Main.mouseItem.bait > 0)
                return Main.mouseItem;

            // 2. Ammo slots first (54-57), matching Terraria's Fishing_GetBait priority
            for (int i = 54; i < 58; i++)
            {
                Item item = player.inventory[i];
                if (item != null && !item.IsAir && item.stack > 0 && item.bait > 0)
                    return item;
            }

            // 3. Main inventory slots (0-49)
            for (int i = 0; i < 50; i++)
            {
                Item item = player.inventory[i];
                if (item != null && !item.IsAir && item.stack > 0 && item.bait > 0)
                    return item;
            }

            // 4. Void Bag (bank4)
            if (player.bank4?.item != null)
            {
                for (int i = 0; i < player.bank4.item.Length; i++)
                {
                    Item item = player.bank4.item[i];
                    if (item != null && !item.IsAir && item.stack > 0 && item.bait > 0)
                        return item;
                }
            }

            return null;
        }
    }
}
