using Terraria;

namespace Augments
{
    public class FinalStandAugment : Augment
    {
        public override string Id => "final_stand";
        public override string DisplayName => "Final Stand";
        public override string Description =>
            $"Dropping below {AugmentText.HP("20% HP")} grants {AugmentText.Defense("+40 defense")} and " +
            $"full knockback immunity for {AugmentText.Duration("5 seconds")}. " +
            $"{AugmentText.Cooldown("60 second cooldown")}.";

        public override AugmentRarity Rarity => AugmentRarity.Legendary;
        public override AugmentClass Class => AugmentClass.Universal;

        private const float TriggerThreshold = 0.20f;
        private const int DefenseBonus = 40;
        private const int ActiveTicks = 300;   // 5 seconds
        private const int CooldownTicks = 3600; // 60 seconds

        public override int CooldownRemaining => LocalPlayerState.FinalStandCooldown;

        // Self-only trigger (own HP threshold) - no netMode guard needed,
        // same reasoning as Mending Aura/Phoenix Heart/Last Stand.
        public override void OnUpdate(Player player)
        {
            var ap = player.GetModPlayer<AugmentPlayer>();

            if (ap.FinalStandCooldown > 0)
                ap.FinalStandCooldown--;

            // Cooldown gate doubles as the "only once per drop below the
            // threshold" guard: the instant this triggers, FinalStandCooldown
            // is set to the full 60s, so the HP check below can't re-fire
            // again until the cooldown fully elapses - including throughout
            // the entire 5-second active window itself.
            if (ap.FinalStandActiveTicks == 0 && ap.FinalStandCooldown == 0 &&
                player.statLife <= (int)(player.statLifeMax2 * TriggerThreshold))
            {
                ap.FinalStandCooldown = CooldownTicks;
                ap.FinalStandActiveTicks = ActiveTicks;
            }

            if (ap.FinalStandActiveTicks > 0)
                ap.FinalStandActiveTicks--;
        }

        public override void UpdateEquips(Player player)
        {
            var ap = player.GetModPlayer<AugmentPlayer>();
            if (ap.FinalStandActiveTicks <= 0)
                return;

            player.statDefense += DefenseBonus;
            player.noKnockback = true;
        }
    }
}
