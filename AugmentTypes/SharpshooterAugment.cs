using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Augments.Core;

namespace Augments
{
    public class SharpshooterAugment : Augment
    {
        public override string Id => "sharpshooter";
        public override string DisplayName => "Sharpshooter";
        public override string Description =>
            $"Ranged hits deal increasing {AugmentText.BonusDamage("bonus damage")} the farther away the target is " +
            $"when hit, scaling up to {AugmentText.BonusDamage("+25% bonus damage")} at 400 pixels or more, based " +
            "on the weapon's base damage.";

        public override AugmentRarity Rarity => AugmentRarity.Rare;
        public override AugmentClass Class => AugmentClass.Ranged;
        public override string FamilyId => AugmentFamilyRegistry.MarksmanId;

        private const float MaxDistance = 400f;
        private const float MaxBonusDamagePercent = 0.25f;

        public override void ModifyHitNPCWithItem(Player player, Item item, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (item.DamageType != DamageClass.Ranged)
                return;

            float distance = Vector2.Distance(player.Center, target.Center);
            float scaledPercent = System.Math.Min(distance / MaxDistance, 1f) * MaxBonusDamagePercent;
            modifiers.FlatBonusDamage += item.damage * scaledPercent;
        }

        public override void ModifyHitNPCWithProj(Player player, Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (proj.DamageType != DamageClass.Ranged)
                return;

            float distance = Vector2.Distance(player.Center, target.Center);
            float scaledPercent = System.Math.Min(distance / MaxDistance, 1f) * MaxBonusDamagePercent;
            modifiers.FlatBonusDamage += proj.damage * scaledPercent;
        }
    }
}
