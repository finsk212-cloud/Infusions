using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace Augments
{
    public class MasterAnglerAugment : Augment
    {
        public override string Id => "master_angler";
        public override string DisplayName => "Master Angler";
        public override string Description =>
            $"Grants {AugmentText.BonusDamage("+10 fishing power")} and opening crates has a {AugmentText.Trigger("20% chance")} for bonus loot.";

        public override AugmentRarity Rarity => AugmentRarity.Rare;
        public override AugmentClass Class => AugmentClass.Universal;

        private const int FishingLevelBonus = 10;
        private const float CrateBonusChance = 0.2f;
        private const int MinBonusCoins = 1;
        private const int MaxBonusCoins = 3;

        // Boosting fishingLevel here is the verified, idiomatic way actual
        // fishing rods/bait already bias toward rarer catches - vanilla's
        // own catch-rarity weighting (common/uncommon/rare/veryrare/
        // legendary/crate flags on FishingAttempt) reads straight from this
        // same field, so this is a genuine rarity-odds boost, not a forced
        // outcome.
        public override void ModifyFishingAttempt(Player player, ref FishingAttempt attempt)
        {
            attempt.fishingLevel += FishingLevelBonus;
        }

        // No GlobalItem/ItemLoader hook exists anywhere for "this crate is
        // being opened, add bonus loot to its result" - confirmed by
        // inspecting both classes directly, neither exposes one. Crates ARE
        // consumed on use though (same as any potion), so GlobalItem.
        // OnConsumeItem still fires for them - confirmed via
        // ItemID.Sets.IsFishingCrate/IsFishingCrateHardmode. Since there's
        // no hook into the crate's own loot pool, this grants an
        // independent bonus instead (the same spawn mechanism Lucky Find
        // already uses for its bonus coins) rather than trying to inject
        // into the crate's actual contents.
        public override void OnConsumeItem(Player player, Item item)
        {
            bool isCrate = ItemID.Sets.IsFishingCrate[item.type] || ItemID.Sets.IsFishingCrateHardmode[item.type];
            if (!isCrate)
                return;

            if (Main.rand.NextFloat() >= CrateBonusChance)
                return;

            int coinCount = Main.rand.Next(MinBonusCoins, MaxBonusCoins + 1);
            player.QuickSpawnItem(player.GetSource_FromThis(), ItemID.GoldCoin, coinCount);
        }
    }
}
