namespace Augments
{
	public class IroncladAuraAugment : Augment
	{
		public override string Id => "ironclad_aura";
		public override string DisplayName => "Ironclad Aura";
		public override string Description =>
			$"Nearby teammates gain {AugmentText.Defense("+8 defense")} while you are within range.";

		public override AugmentRarity Rarity => AugmentRarity.Rare;
		public override AugmentClass Class => AugmentClass.Support;
		public override bool HasAuraEffect => true;

		// No hooks - the aura is pull-based. Each nearby player checks for this
		// augment in AugmentPlayer.UpdateEquips and applies the defense to themselves.
	}
}
