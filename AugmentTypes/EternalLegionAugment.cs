using Terraria;

namespace Augments
{
    public class EternalLegionAugment : Augment
    {
        public override string Id => "eternal_legion";
        public override string DisplayName => "Eternal Legion";
        public override string Description =>
            $"Permanently grants {AugmentText.BonusDamage("+1 max minion slot")} and {AugmentText.Defense("+1 max sentry slot")}.";

        public override AugmentRarity Rarity => AugmentRarity.Legendary;
        public override AugmentClass Class => AugmentClass.Summon;

        // player.maxMinions and player.maxTurrets ("sentries" internally) are
        // both reset to their base value of 1 in Player.ResetEffects() every
        // single frame, before equips/buffs re-add their bonuses on top - a
        // one-time +1 inside OnAcquire would just get wiped out on the very
        // next frame, the same lesson already learned with Ravenous Swarm's
        // maxMinions bonus. Re-apply both here every tick instead, same as
        // every other persistent stat bonus in this mod.
        public override void UpdateEquips(Player player)
        {
            player.maxMinions += 1;
            player.maxTurrets += 1;
        }
    }
}
