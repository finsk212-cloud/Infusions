using Terraria;
using Terraria.ModLoader;

namespace Augments
{
    public class VitalEchoAugment : Augment
    {
        public override string Id => "vital_echo";
        public override string DisplayName => "Vital Echo";
        public override string Description =>
            $"Being {AugmentText.Healing("healed")} by any source grants {AugmentText.Defense("+6 defense")} " +
            $"for {AugmentText.Duration("3 seconds")}.";

        public override AugmentRarity Rarity => AugmentRarity.Common;
        public override AugmentClass Class => AugmentClass.Universal;

        private const int DefenseBonus = 6;

        // Comparing statLife to its value last tick is the universal way to
        // catch ANY healing source (potion, Second Wind, natural regen,
        // anything) without needing a separate hook per source - an increase
        // from one tick to the next always means healing happened, no matter
        // what caused it.
        public override void OnUpdate(Player player)
        {
            player.GetModPlayer<AugmentPlayer>().TickVitalEcho();
        }

        public override void UpdateEquips(Player player)
        {
            if (player.GetModPlayer<AugmentPlayer>().HasVitalEchoDefense)
                player.statDefense += DefenseBonus;
        }
    }
}
