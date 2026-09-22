using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Augments.Core;

namespace Augments
{
    public class DeadeyeAugment : Augment
    {
        public override string Id => "deadeye";
        public override string DisplayName => "Deadeye";
        public override string Description =>
            $"Consecutive ranged hits on the same enemy build {AugmentText.Crit("+2% crit chance")} per hit, " +
            $"up to {AugmentText.Crit("+16% crit chance")} at 8 stacks. Switching targets resets the stacks.";

        public override AugmentRarity Rarity => AugmentRarity.Epic;
        public override AugmentClass Class => AugmentClass.Ranged;
        public override string FamilyId => AugmentFamilyRegistry.MarksmanId;

        private const int MaxStacks = 8;
        private const float CritPerStack = 2f;

        // Shows "+X%" in the cooldown/status row while stacks are active,
        // same StatusValue mechanism ScavengersLuckAugment uses for its crit
        // buff, reusing the existing Crit color category.
        public override int? StatusValue => LocalPlayerState.DeadeyeHitStacks > 0 ? (int)(LocalPlayerState.DeadeyeHitStacks * CritPerStack) : (int?)null;
        public override Color StatusValueColor => AugmentTextColors.Crit;
        public override string StatusValueSuffix => "%";

        // Unlike MomentumSwingAugment, there's no time-based reset window
        // here - only switching targets resets the stacks. A missed shot
        // doesn't reduce hitStacks on its own; it simply means this method
        // never fires for that shot, leaving the stacks untouched.
        public override void OnHitNPCWithItem(Player player, Item item, NPC target, NPC.HitInfo hit)
        {
            if (item.DamageType == DamageClass.Ranged)
                ApplyStack(player, target);
        }

        public override void OnHitNPCWithProj(Player player, Projectile proj, NPC target, NPC.HitInfo hit)
        {
            if (proj.DamageType == DamageClass.Ranged)
                ApplyStack(player, target);
        }

        private void ApplyStack(Player player, NPC target)
        {
            var ap = player.GetModPlayer<AugmentPlayer>();
            if (target.whoAmI == ap.DeadeyeLastTargetWhoAmI)
            {
                if (ap.DeadeyeHitStacks < MaxStacks)
                    ap.DeadeyeHitStacks += HitEffectiveness;
            }
            else
            {
                ap.DeadeyeHitStacks = HitEffectiveness;
                ap.DeadeyeLastTargetWhoAmI = target.whoAmI;
            }
        }

        public override void ModifyWeaponCrit(Player player, Item item, ref float crit)
        {
            if (item.DamageType == DamageClass.Ranged)
                crit += player.GetModPlayer<AugmentPlayer>().DeadeyeHitStacks * CritPerStack;
        }
    }
}
