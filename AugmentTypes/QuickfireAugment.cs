using Terraria;

namespace Augments
{
    public class QuickfireAugment : Augment
    {
        public override string Id => "quickfire";
        public override string DisplayName => "Quickfire";
        public override string Description =>
            $"Ranged weapons have a {AugmentText.Trigger("25% chance")} to not consume ammo.";

        public override AugmentRarity Rarity => AugmentRarity.Common;
        public override AugmentClass Class => AugmentClass.Ranged;

        private const float ProcChance = 0.25f;

        public override bool CanConsumeAmmo(Player player, Item weapon, Item ammo)
        {
            if (Main.rand.NextFloat() < ProcChance)
                return false;

            return true;
        }
    }
}
