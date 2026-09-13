using Terraria;
using Terraria.ModLoader;

namespace Augments
{
	public class QuickcastAugment : Augment
	{
		public override string Id => "quickcast";
		public override string DisplayName => "Quickcast";
		public override string Description =>
			$"Magic weapons gain {AugmentText.AttackSpeed("+20% attack speed")}.";

		public override AugmentRarity Rarity => AugmentRarity.Common;
		public override AugmentClass Class => AugmentClass.Magic;

		private const float AttackSpeedBonus = 0.20f;

		public override void UpdateEquips(Player player)
		{
			player.GetAttackSpeed(DamageClass.Magic) += AttackSpeedBonus;
		}
	}
}
