namespace Augments
{
    public class CombatMedicAugment : Augment
    {
        public override string Id => "combat_medic";
        public override string DisplayName => "Combat Medic";
        public override string Description =>
            $"Nearby teammates regenerate {AugmentText.Healing("1.5 HP per second")}.";
        public override AugmentRarity Rarity => AugmentRarity.Common;
        public override AugmentClass Class => AugmentClass.Support;
        public override string FamilyId => "field_medic";
        public override bool HasAuraEffect => true;
        // No hooks — pull-based architecture (AugmentPlayer.UpdateEquips)
    }
}
