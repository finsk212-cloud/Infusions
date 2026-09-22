using Terraria;
using Terraria.ModLoader;
using Augments.Core;

namespace Augments
{
    public class RapidFireAugment : Augment
    {
        public override string Id => "rapid_fire";
        public override string DisplayName => "Rapid Fire";
        public override string Description =>
            $"Ranged weapons gain {AugmentText.AttackSpeed("+10% attack speed")}.";
        public override AugmentRarity Rarity => AugmentRarity.Common;
        public override AugmentClass Class => AugmentClass.Ranged;
        public override string FamilyId => AugmentFamilyRegistry.GunslingerId;

        public override void UpdateEquips(Player player)
        {
            player.GetAttackSpeed(DamageClass.Ranged) += 0.10f;
        }
    }
}
