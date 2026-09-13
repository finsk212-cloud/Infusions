using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
    public class BloodletterAugment : Augment
    {
        public override string Id => "bloodletter";
        public override string DisplayName => "Bloodletter";
        public override string Description =>
            $"Melee {AugmentText.Crit("crits")} make enemies {AugmentText.Bleed("bleed")}, dealing {AugmentText.BonusDamage("3 damage per second")} for {AugmentText.Duration("5s")}.";

        public override AugmentRarity Rarity => AugmentRarity.Common;
        public override AugmentClass Class => AugmentClass.Melee;

        private const int BleedDurationTicks = 300;
        private const int DamagePerSecond = 3;

        public override void OnHitNPCWithItem(Player player, Item item, NPC target, NPC.HitInfo hit)
        {
            if (hit.Crit && item.CountsAsClass(DamageClass.Melee))
                ApplyBleed(target);
        }

        public override void OnHitNPCWithProj(Player player, Projectile proj, NPC target, NPC.HitInfo hit)
        {
            if (hit.Crit && proj.CountsAsClass(DamageClass.Melee))
                ApplyBleed(target);
        }

        private static void ApplyBleed(NPC target)
        {
            target.GetGlobalNPC<AugmentBleedNPC>().ApplyBleed(BleedDurationTicks, DamagePerSecond);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                AugmentNet.SendApplyNPCEffectBleed(target.whoAmI, BleedDurationTicks, DamagePerSecond);
        }
    }
}
