using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
    public class TimeWarpAugment : Augment
    {
        public override string Id => "time_warp";
        public override string DisplayName => "Time Warp";
        public override string Description =>
            $"Magic {AugmentText.Crit("crits")} {AugmentText.Immobilize("slow")} all nearby enemies by 40% for {AugmentText.Duration("3 seconds")}.";

        public override AugmentRarity Rarity => AugmentRarity.Epic;
        public override AugmentClass Class => AugmentClass.Magic;

        private const float SlowRange = 200f;
        private const int SlowDurationTicks = 180;
        private const float SlowPercent = 0.4f;

        public override void OnHitNPCWithItem(Player player, Item item, NPC target, NPC.HitInfo hit)
        {
            if (hit.Crit && item.DamageType == DamageClass.Magic)
                SlowNearbyTargets(target);
        }

        public override void OnHitNPCWithProj(Player player, Projectile proj, NPC target, NPC.HitInfo hit)
        {
            if (hit.Crit && proj.DamageType == DamageClass.Magic)
                SlowNearbyTargets(target);
        }

        private static void SlowNearbyTargets(NPC target)
        {
            foreach (NPC npc in Main.npc)
            {
                if (!npc.active || npc.friendly || npc.townNPC)
                    continue;

                if (npc.Distance(target.Center) <= SlowRange)
                {
                    npc.GetGlobalNPC<AugmentSlowNPC>().ApplySlow(SlowDurationTicks, SlowPercent);
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                        AugmentNet.SendApplyNPCEffectSlow(npc.whoAmI, SlowDurationTicks, SlowPercent);
                }
            }
        }
    }
}
