using Terraria;

namespace Augments
{
    public class AvatarOfBalanceAugment : Augment
    {
        public override string Id => "avatar_of_balance";
        public override string DisplayName => "Avatar of Balance";
        public override string Description =>
            $"Grants a smaller {AugmentText.BonusDamage("+10% bonus damage")} and {AugmentText.Defense("+10 defense")}, " +
            "always-on, with no permanent drawback.";

        public override AugmentRarity Rarity => AugmentRarity.Epic;
        public override AugmentClass Class => AugmentClass.Universal;

        public override string KeystoneFamily => "path_of_the_berserker";
        public override bool IsPermanent => true;

        private const int DefenseBonus = 10;
        private const float BonusDamagePercent = 0.10f;

        public override void UpdateEquips(Player player)
        {
            player.statDefense += DefenseBonus;
        }

        // Not restricted by DamageClass, same as the other two Avatars.
        public override void ModifyHitNPCWithItem(Player player, Item item, NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.FlatBonusDamage += (int)(item.damage * BonusDamagePercent);
        }

        public override void ModifyHitNPCWithProj(Player player, Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.FlatBonusDamage += (int)(proj.damage * BonusDamagePercent);
        }
    }
}
