using Terraria;
using Terraria.ModLoader;

namespace Augments
{
    public class GuardiansWrathAugment : Augment
    {
        public override string Id => "guardians_wrath";
        public override string DisplayName => "Guardian's Wrath";
        public override string Description =>
            $"While below {AugmentText.HP("30% HP")}, gain {AugmentText.Defense("+15 defense")} and reduce " +
            $"incoming damage by {AugmentText.Defense("20%")}, but outgoing damage is reduced by {AugmentText.BonusDamage("15%")} during that same window.";

        public override AugmentRarity Rarity => AugmentRarity.Epic;
        public override AugmentClass Class => AugmentClass.Universal;

        private const float TriggerThreshold = 0.3f;
        private const int DefenseBonus = 15;
        private const float IncomingDamageReductionPercent = 0.20f;
        private const float OutgoingDamageReductionPercent = 0.15f;

        private static bool IsActive(Player player) => player.statLife < player.statLifeMax2 * TriggerThreshold;

        public override void UpdateEquips(Player player)
        {
            if (IsActive(player))
                player.statDefense += DefenseBonus;
        }

        public override void ModifyHurt(Player player, ref Player.HurtModifiers modifiers)
        {
            if (IsActive(player))
                modifiers.FinalDamage *= 1f - IncomingDamageReductionPercent;
        }

        public override void ModifyHitNPCWithItem(Player player, Item item, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (IsActive(player))
                modifiers.FlatBonusDamage += -(int)(item.damage * OutgoingDamageReductionPercent);
        }

        public override void ModifyHitNPCWithProj(Player player, Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (IsActive(player))
                modifiers.FlatBonusDamage += -(int)(proj.damage * OutgoingDamageReductionPercent);
        }
    }
}
