using Terraria;

namespace Augments
{
    public class MartyrsResolveAugment : Augment
    {
        public override string Id => "martyrs_resolve";
        public override string DisplayName => "Martyr's Resolve";
        public override string Description =>
            $"Nearby teammates take {AugmentText.Defense("15% reduced damage")}. You take {AugmentText.BonusDamage("15% increased damage")}.";
        public override AugmentRarity Rarity => AugmentRarity.Legendary;
        public override AugmentClass Class => AugmentClass.Support;
        public override bool HasAuraEffect => true;

        // Self-penalty: Support player takes 15% more damage while in Support stance.
        // The teammates' 15% damage reduction is applied in AugmentPlayer.ModifyHurt
        // via pull loop, since it checks OTHER players' augment lists.
        public override void ModifyHurt(Player player, ref Player.HurtModifiers modifiers)
        {
            // Self-penalty applies whenever the augment is owned — no stance requirement,
            // consistent with the teammates' damage reduction working from 1 augment.
            modifiers.FinalDamage *= 1.15f;
        }
    }
}
