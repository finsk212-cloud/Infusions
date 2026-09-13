using System;
using Terraria;

namespace Augments
{
    public class FortunesFavorAugment : Augment
    {
        public override string Id => "fortunes_favor";
        public override string DisplayName => "Fortune's Favor";
        public override string Description =>
            $"Grants {AugmentText.Crit("+10% Fortune")}, boosting the odds of all luck-based augments and world luck. " +
            $"Also slowly {AugmentText.Healing("regenerates 1 HP")} every {AugmentText.Duration("4 seconds")} on its own.";

        public override AugmentRarity Rarity => AugmentRarity.Common;
        public override AugmentClass Class => AugmentClass.Universal;

        public override bool IsLuckyThemed => true;

        public override float FortuneBonus => 0.10f;

        private const int RegenIntervalTicks = 240;
        private const int HealAmount = 1;

        public override void OnUpdate(Player player)
        {
            var ap = player.GetModPlayer<AugmentPlayer>();
            ap.FortunesFavorRegenTimer++;
            if (ap.FortunesFavorRegenTimer < RegenIntervalTicks)
                return;

            ap.FortunesFavorRegenTimer = 0;
            player.statLife = Math.Min(player.statLife + HealAmount, player.statLifeMax2);
            player.HealEffect(HealAmount);
        }
    }
}
