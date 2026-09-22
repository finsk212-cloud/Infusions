using Augments.Core;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
    public class FrostboundAugment : Augment
    {
        public override string Id => "frostbound";
        public override string DisplayName => "Frostbound";
        public override string Description =>
            $"Magic hits against an already {AugmentText.Frostburn("Frostburned")} enemy deal " +
            $"{AugmentText.BonusDamage("+25% bonus damage")}, based on the weapon's base damage. " +
            $"Requires the enemy to have {AugmentText.Frostburn("Frostburn")} applied.";

        public override AugmentRarity Rarity => AugmentRarity.Rare;
        public override AugmentClass Class => AugmentClass.Magic;
        public override string FamilyId => AugmentFamilyRegistry.CryoId;

        private const float BonusDamagePercent = 0.25f;

        public override void ModifyHitNPCWithItem(Player player, Item item, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (item.DamageType == DamageClass.Magic && target.HasBuff(BuffID.Frostburn))
                modifiers.FlatBonusDamage += (int)(item.damage * BonusDamagePercent);
        }

        public override void ModifyHitNPCWithProj(Player player, Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (proj.DamageType == DamageClass.Magic && target.HasBuff(BuffID.Frostburn))
                modifiers.FlatBonusDamage += (int)(proj.damage * BonusDamagePercent);
        }
    }
}
