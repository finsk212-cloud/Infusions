using Terraria;

namespace Augments
{
    public class AvatarOfTheWallAugment : Augment
    {
        public override string Id => "avatar_of_the_wall";
        public override string DisplayName => "Avatar of the Wall";
        public override string Description =>
            $"Grants {AugmentText.Defense("+20 defense")} and reduces incoming damage by {AugmentText.Defense("30%")}, but permanently " +
            $"reduces your own outgoing damage by {AugmentText.BonusDamage("20%")}. Always-on, no trigger condition.";

        public override AugmentRarity Rarity => AugmentRarity.Epic;
        public override AugmentClass Class => AugmentClass.Universal;

        public override string KeystoneFamily => "path_of_the_berserker";
        public override bool IsPermanent => true;

        private const int DefenseBonus = 20;
        private const float IncomingDamageReductionPercent = 0.30f;
        private const float OutgoingDamageReductionPercent = 0.20f;

        public override void UpdateEquips(Player player)
        {
            player.statDefense += DefenseBonus;
        }

        // Same incoming-damage-reduction mechanism Dark Omen's Eclipse half
        // already uses.
        public override void ModifyHurt(Player player, ref Player.HurtModifiers modifiers)
        {
            modifiers.FinalDamage *= 1f - IncomingDamageReductionPercent;
        }

        // Not restricted by DamageClass - same -(value) pattern Guardian's
        // Wrath already uses to reduce outgoing damage.
        public override void ModifyHitNPCWithItem(Player player, Item item, NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.FlatBonusDamage += -(int)(item.damage * OutgoingDamageReductionPercent);
        }

        public override void ModifyHitNPCWithProj(Player player, Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.FlatBonusDamage += -(int)(proj.damage * OutgoingDamageReductionPercent);
        }
    }
}
