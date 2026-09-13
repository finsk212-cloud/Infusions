using Terraria;

namespace Augments
{
    public class DarkOmenAugment : Augment
    {
        public override string Id => "dark_omen";
        public override string DisplayName => "Dark Omen";
        public override string Description =>
            $"During a {AugmentText.BloodMoon("Blood Moon")}, all damage dealt is increased by " +
            $"{AugmentText.BonusDamage("+20%")} and {AugmentText.Healing("6% of damage dealt is returned as healing")}.\n" +
            $"During a {AugmentText.SolarEclipse("Solar Eclipse")}, {AugmentText.MovementSpeed("movement speed")} is increased by {AugmentText.MovementSpeed("20%")} and " +
            $"incoming damage is reduced by {AugmentText.Defense("12%")}.";

        public override AugmentRarity Rarity => AugmentRarity.Epic;
        public override AugmentClass Class => AugmentClass.Universal;

        private const float BloodMoonDamageBonusPercent = 0.20f;
        private const float BloodMoonLifestealPercent = 0.06f;
        private const float EclipseMovementSpeedBonus = 0.20f;
        private const float EclipseDamageReductionPercent = 0.12f;

        // Not restricted by DamageClass, unlike every other damage-boost
        // augment so far - Dark Omen is meant to buff every kind of damage
        // alike during a Blood Moon, melee/ranged/magic/summon included.
        public override void ModifyHitNPCWithItem(Player player, Item item, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Main.bloodMoon)
                modifiers.FlatBonusDamage += (int)(item.damage * BloodMoonDamageBonusPercent);
        }

        public override void ModifyHitNPCWithProj(Player player, Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Main.bloodMoon)
                modifiers.FlatBonusDamage += (int)(proj.damage * BloodMoonDamageBonusPercent);
        }

        public override void OnHitNPCWithItem(Player player, Item item, NPC target, NPC.HitInfo hit)
        {
            if (Main.bloodMoon)
                Lifesteal(player, hit.Damage, HitEffectiveness);
        }

        public override void OnHitNPCWithProj(Player player, Projectile proj, NPC target, NPC.HitInfo hit)
        {
            if (Main.bloodMoon)
                Lifesteal(player, hit.Damage, HitEffectiveness);
        }

        private static void Lifesteal(Player player, int damageDealt, float effectiveness)
        {
            int healAmount = (int)(damageDealt * BloodMoonLifestealPercent * effectiveness);
            if (healAmount <= 0)
                return;

            player.statLife = System.Math.Min(player.statLife + healAmount, player.statLifeMax2);
            player.HealEffect(healAmount);
        }

        public override void PostUpdateRunSpeeds(Player player)
        {
            if (!Main.eclipse)
                return;

            player.maxRunSpeed *= 1f + EclipseMovementSpeedBonus;
            player.accRunSpeed *= 1f + EclipseMovementSpeedBonus;
            player.runAcceleration *= 1f + EclipseMovementSpeedBonus;
        }

        public override void ModifyHurt(Player player, ref Player.HurtModifiers modifiers)
        {
            if (Main.eclipse)
                modifiers.FinalDamage *= 1f - EclipseDamageReductionPercent;
        }
    }
}
