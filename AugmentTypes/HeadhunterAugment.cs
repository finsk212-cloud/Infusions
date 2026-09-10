using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
    public class HeadhunterAugment : Augment
    {
        public override string Id => "headhunter";
        public override string DisplayName => "Headhunter";
        public override string Description =>
            $"Ranged {AugmentText.Crit("crits")} executes enemies below {AugmentText.HP("15% HP")}, " +
            $"regardless of the hit's normal damage. Does not work on bosses.";

        public override AugmentRarity Rarity => AugmentRarity.Rare;
        public override AugmentClass Class => AugmentClass.Ranged;

        private const float ExecuteThreshold = 0.15f;

        public override void OnHitNPCWithItem(Player player, Item item, NPC target, NPC.HitInfo hit)
        {
            if (hit.Crit && item.DamageType == DamageClass.Ranged && CanExecute(target) && PassesHitEffectivenessRoll())
                Execute(player, target);
        }

        public override void OnHitNPCWithProj(Player player, Projectile proj, NPC target, NPC.HitInfo hit)
        {
            if (hit.Crit && proj.DamageType == DamageClass.Ranged && CanExecute(target) && PassesHitEffectivenessRoll())
                Execute(player, target);
        }

        private static bool CanExecute(NPC target)
        {
            return !target.boss && target.life > 0 && target.life <= target.lifeMax * ExecuteThreshold;
        }

        private static void Execute(Player player, NPC target)
        {
            // Built manually (instead of SimpleStrikeNPC) so the combat text can be
            // forced to white - HideCombatText suppresses the auto popup and we
            // spawn our own via CombatText.NewText.
            var hit = new NPC.HitInfo
            {
                Damage = 9999,
                SourceDamage = 9999,
                HitDirection = player.direction,
                DamageType = DamageClass.Ranged,
                HideCombatText = true
            };

            target.StrikeNPC(hit);
            // StrikeNPC does not sync itself. In multiplayer the damage would
            // only apply on the local client; the server's authoritative NPC
            // never takes it and re-syncs back to alive (looks like a "respawn").
            // Mirror SimpleStrikeNPC and relay the strike to the server/clients.
            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendStrikeNPC(target, in hit);
            CombatText.NewText(target.Hitbox, Color.White, hit.Damage);
        }
    }
}
