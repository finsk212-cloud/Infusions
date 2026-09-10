using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
    public class GlassSightAugment : Augment
    {
        public override string Id => "glass_sight";
        public override string DisplayName => "Glass Sight";
        public override string Description =>
            $"Ranged {AugmentText.Crit("crits")} deal {AugmentText.BonusDamage("15% increased damage")}.";
        public override AugmentRarity Rarity => AugmentRarity.Common;
        public override AugmentClass Class => AugmentClass.Ranged;

        private const float BonusPercent = 0.15f;

        // Applied in OnHit (post-finalization) so we know hit.Crit is true.
        // Uses a secondary silent strike for the bonus damage, same pattern as CriticalSurgeAugment.
        public override void OnHitNPCWithItem(Player player, Item item, NPC target, NPC.HitInfo hit)
        {
            if (!hit.Crit || item.DamageType != DamageClass.Ranged) return;
            ApplyBonus(player, target, (int)(hit.SourceDamage * BonusPercent));
        }

        public override void OnHitNPCWithProj(Player player, Projectile proj, NPC target, NPC.HitInfo hit)
        {
            if (!hit.Crit || proj.DamageType != DamageClass.Ranged) return;
            ApplyBonus(player, target, (int)(hit.SourceDamage * BonusPercent));
        }

        private static void ApplyBonus(Player player, NPC target, int bonus)
        {
            if (bonus <= 0) return;
            var extraHit = new NPC.HitInfo
            {
                Damage = bonus,
                SourceDamage = bonus,
                HitDirection = player.direction,
                DamageType = DamageClass.Ranged,
                HideCombatText = true
            };
            target.StrikeNPC(extraHit);
            // StrikeNPC does not sync itself. In multiplayer the damage would
            // only apply on the local client; the server's authoritative NPC
            // never takes it and re-syncs back to alive (looks like a "respawn").
            // Mirror SimpleStrikeNPC and relay the strike to the server/clients.
            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendStrikeNPC(target, in extraHit);
            CombatText.NewText(target.Hitbox, AugmentTextColors.BonusDamage, bonus);
        }
    }
}
