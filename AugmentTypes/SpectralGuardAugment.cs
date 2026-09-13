using Terraria;

namespace Augments
{
    public class SpectralGuardAugment : Augment
    {
        public override string Id => "spectral_guard";
        public override string DisplayName => "Spectral Guard";
        public override string Description =>
            $"While you have 3 or more minions or sentries active at once, gain {AugmentText.Defense("+8 defense")}.";

        public override AugmentRarity Rarity => AugmentRarity.Rare;
        public override AugmentClass Class => AugmentClass.Summon;

        private const int RequiredCount = 3;
        private const int DefenseBonus = 8;

        public override void UpdateEquips(Player player)
        {
            if (GetActiveMinionAndSentryCount(player) >= RequiredCount)
                player.statDefense += DefenseBonus;
        }

        // player.numMinions tracks the current minion count directly, but
        // sentries have no equivalent player-side counter - Player only
        // exposes maxTurrets (the cap), not how many are currently out - so
        // active sentries have to be counted by scanning for this player's
        // own sentry projectiles instead.
        private static int GetActiveMinionAndSentryCount(Player player)
        {
            int count = player.numMinions;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == player.whoAmI && proj.sentry)
                    count++;
            }

            return count;
        }
    }
}
