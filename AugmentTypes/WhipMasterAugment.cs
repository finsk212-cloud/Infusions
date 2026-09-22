using Terraria;
using Terraria.ModLoader;
using Augments.Core;

namespace Augments
{
	public class WhipMasterAugment : Augment
	{
		public override string Id => "whip_master";
		public override string DisplayName => "Whip Master";
		public override string Description =>
			$"Whips gain {AugmentText.AttackSpeed("+15% attack speed")} and {AugmentText.BonusDamage("+20% range")}.";

		public override AugmentRarity Rarity => AugmentRarity.Common;
		public override AugmentClass Class => AugmentClass.Summon;
		public override string FamilyId => AugmentFamilyRegistry.LasherId;

		private const float AttackSpeedBonus = 0.15f;
		private const float RangeMultiplierBonus = 1.2f;

		public override void UpdateEquips(Player player)
		{
			player.GetAttackSpeed(DamageClass.SummonMeleeSpeed) += AttackSpeedBonus;
		}

		public override void OnProjectileSpawn(Player player, Projectile projectile)
		{
			if (projectile.DamageType == DamageClass.SummonMeleeSpeed)
				projectile.WhipSettings.RangeMultiplier *= RangeMultiplierBonus;
		}
	}
}
