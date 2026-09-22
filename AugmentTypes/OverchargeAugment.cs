using Terraria;
using Terraria.ModLoader;
using Augments.Core;

namespace Augments
{
    public class OverchargeAugment : Augment
    {
        public override string Id => "overcharge";
        public override string DisplayName => "Overcharge";
        public override string Description =>
            $"Magic weapons cost {AugmentText.Mana("double mana")} per cast, but deal {AugmentText.BonusDamage("+40% bonus damage")}.";

        public override AugmentRarity Rarity => AugmentRarity.Rare;
        public override AugmentClass Class => AugmentClass.Magic;
        public override string FamilyId => AugmentFamilyRegistry.ArcaneSurgeId;

        private const float ManaCostMultiplier = 2f;
        private const float BonusDamagePercent = 0.40f;

        public override void ModifyManaCost(Player player, Item item, ref float reduce, ref float mult)
        {
            mult *= ManaCostMultiplier;
        }

        public override void ModifyHitNPCWithItem(Player player, Item item, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (item.DamageType == DamageClass.Magic)
                modifiers.FlatBonusDamage += (int)(item.damage * BonusDamagePercent);
        }

        public override void ModifyHitNPCWithProj(Player player, Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (proj.DamageType == DamageClass.Magic)
                modifiers.FlatBonusDamage += (int)(proj.damage * BonusDamagePercent);
        }
    }
}
