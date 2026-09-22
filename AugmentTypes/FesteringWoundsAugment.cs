using Augments.Core;

namespace Augments
{
    public class FesteringWoundsAugment : Augment
    {
        public override string Id => "festering_wounds";
        public override string DisplayName => "Festering Wounds";
        public override string Description =>
            $"Damage Over Time effects deal {AugmentText.BonusDamage("50% more damage")}.";

        public override AugmentRarity Rarity => AugmentRarity.Rare;
        public override AugmentClass Class => AugmentClass.Universal;
        public override string FamilyId => AugmentFamilyRegistry.BloodhunterId;

        // This augment has no logic of its own - it's purely a flag read by
        // AugmentFesteringWoundsNPC.UpdateLifeRegen.
    }
}
