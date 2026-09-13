using Terraria;
using Terraria.ModLoader;

namespace Augments
{
    public class LuckyStrikeAugment : Augment
    {
        public override string Id => "lucky_strike";
        public override string DisplayName => "Lucky Strike";
        public override string Description =>
            $"Grants {AugmentText.Crit("+5% Fortune")}. Any {AugmentText.Crit("crit")} has a 15% chance " +
            $"to deal a second strike for the same {AugmentText.BonusDamage("damage")} (scales with Fortune).";

        public override AugmentRarity Rarity => AugmentRarity.Rare;
        public override AugmentClass Class => AugmentClass.Universal;

        public override bool IsLuckyThemed => true;
        public override float FortuneBonus => 0.05f;

        private const float ProcChance = 0.15f;

        public override void OnHitNPCWithItem(Player player, Item item, NPC target, NPC.HitInfo hit)
        {
            if (hit.Crit && Main.rand.NextFloat() < ProcChance * (1f + player.GetModPlayer<AugmentPlayer>().TotalFortune))
                target.SimpleStrikeNPC(ScaleHitEffect(hit.Damage), player.direction);
        }

        public override void OnHitNPCWithProj(Player player, Projectile proj, NPC target, NPC.HitInfo hit)
        {
            if (hit.Crit && Main.rand.NextFloat() < ProcChance * (1f + player.GetModPlayer<AugmentPlayer>().TotalFortune))
                target.SimpleStrikeNPC(ScaleHitEffect(hit.Damage), player.direction);
        }
    }
}
