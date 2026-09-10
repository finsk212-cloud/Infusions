using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
    public class ApexHunterAugment : Augment
    {
        public override string Id => "apex_hunter";
        public override string DisplayName => "Apex Hunter";
        public override string Description =>
            "Ranged hits against a boss build a mark; once it reaches 10 hits, the next hit triggers a " +
            $"bonus burst dealing {AugmentText.BonusDamage("15% of the boss's max HP")}, then the mark resets.";

        public override AugmentRarity Rarity => AugmentRarity.Legendary;
        public override AugmentClass Class => AugmentClass.Ranged;

        private const int MaxMarkStacks = 10;
        private const float BurstPercentOfMaxHP = 0.15f;

        // Tracked per-player (on AugmentPlayer), not per-target - the mark only
        // ever matters against whichever single boss is currently being fought,
        // so there's no need to key it by target.whoAmI.

        public override void OnHitNPCWithItem(Player player, Item item, NPC target, NPC.HitInfo hit)
        {
            if (target.boss && item.DamageType == DamageClass.Ranged)
                HandleMark(player, target);
        }

        public override void OnHitNPCWithProj(Player player, Projectile proj, NPC target, NPC.HitInfo hit)
        {
            if (target.boss && proj.DamageType == DamageClass.Ranged)
                HandleMark(player, target);
        }

        private void HandleMark(Player player, NPC target)
        {
            var ap = player.GetModPlayer<AugmentPlayer>();
            if (ap.ApexHunterMarkStacks >= MaxMarkStacks)
            {
                ap.ApexHunterMarkStacks = 0;
                Strike(player, target, HitEffectiveness);
            }
            else
            {
                ap.ApexHunterMarkStacks += HitEffectiveness;
            }
        }

        // Built manually (instead of SimpleStrikeNPC) so the combat text can be
        // forced to yellow - HideCombatText suppresses the auto popup and we
        // spawn our own via CombatText.NewText, same as MeteorStrikeAugment.
        private static void Strike(Player player, NPC target, float effectiveness)
        {
            int damage = System.Math.Max(1, (int)(target.lifeMax * BurstPercentOfMaxHP * effectiveness));

            var hit = new NPC.HitInfo
            {
                Damage = damage,
                SourceDamage = damage,
                HitDirection = player.direction,
                HideCombatText = true
            };

            target.StrikeNPC(hit);
            // StrikeNPC does not sync itself. In multiplayer the damage would
            // only apply on the local client; the server's authoritative NPC
            // never takes it and re-syncs back to alive (looks like a "respawn").
            // Mirror SimpleStrikeNPC and relay the strike to the server/clients.
            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendStrikeNPC(target, in hit);
            CombatText.NewText(target.Hitbox, Color.Yellow, damage);
        }
    }
}
