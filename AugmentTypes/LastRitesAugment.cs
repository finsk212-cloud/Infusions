namespace Augments
{
    public class LastRitesAugment : Augment
    {
        public override string Id => "last_rites";
        public override string DisplayName => "Last Rites";
        public override string Description =>
            $"When a nearby teammate drops below {AugmentText.HP("20% HP")}, grant them " +
            $"{AugmentText.Duration("3s")} of invulnerability.";
        public override AugmentRarity Rarity => AugmentRarity.Epic;
        public override AugmentClass Class => AugmentClass.Support;
        public override bool HasAuraEffect => true;
        public override string CooldownText => "90s cooldown per player";
        public override int CooldownRemaining => LocalPlayerState.LastRitesCooldown;
        // No hooks — pull check runs in AugmentPlayer.PostUpdate on the receiving player's client.
    }
}
