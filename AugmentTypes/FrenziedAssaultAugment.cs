using Terraria;
using Terraria.ModLoader;

namespace Augments
{
	public class FrenziedAssaultAugment : Augment
	{
		public override string Id => "frenzied_assault";
		public override string DisplayName => "Frenzied Assault";
		public override string Description =>
			$"Melee {AugmentText.Crit("crits")} grant a stacking attack speed buff: {AugmentText.AttackSpeed("+7% per stack")}, up to " +
			$"{AugmentText.AttackSpeed("+35% at 5 stacks")}. Stacks reset after {AugmentText.Duration("3 seconds")}.";

		public override AugmentRarity Rarity => AugmentRarity.Epic;
		public override AugmentClass Class => AugmentClass.Melee;

		private const int ResetWindowTicks = 180;
		private const int MaxStacks = 5;
		private const float AttackSpeedPerStack = 0.07f;

		public override void OnUpdate(Player player)
		{
			var ap = player.GetModPlayer<AugmentPlayer>();
			if (ap.FrenziedAssaultResetTimer <= 0)
				return;

			ap.FrenziedAssaultResetTimer--;
			if (ap.FrenziedAssaultResetTimer == 0)
				ap.FrenziedAssaultStacks = 0;
		}

		public override void UpdateEquips(Player player)
		{
			var ap = player.GetModPlayer<AugmentPlayer>();
			if (ap.FrenziedAssaultStacks > 0)
				player.GetAttackSpeed(DamageClass.Melee) += ap.FrenziedAssaultStacks * AttackSpeedPerStack;
		}

		public override void OnHitNPCWithItem(Player player, Item item, NPC target, NPC.HitInfo hit)
		{
			if (item.CountsAsClass(DamageClass.Melee) && hit.Crit)
				RegisterCrit(player);
		}

		public override void OnHitNPCWithProj(Player player, Projectile proj, NPC target, NPC.HitInfo hit)
		{
			if (proj.CountsAsClass(DamageClass.Melee) && hit.Crit)
				RegisterCrit(player);
		}

		private static void RegisterCrit(Player player)
		{
			var ap = player.GetModPlayer<AugmentPlayer>();
			if (ap.FrenziedAssaultStacks < MaxStacks)
				ap.FrenziedAssaultStacks++;

			ap.FrenziedAssaultResetTimer = ResetWindowTicks;
		}
	}
}
