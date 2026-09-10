using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
	public class WhipCrackerAugment : Augment
	{
		public override string Id => "whip_cracker";
		public override string DisplayName => "Whip Cracker";
		public override string Description =>
			$"Whip hits apply a stacking debuff, increasing damage taken by {AugmentText.BonusDamage("2%")} per stack (max 5 stacks).";

		public override AugmentRarity Rarity => AugmentRarity.Rare;
		public override AugmentClass Class => AugmentClass.Summon;

		private const int StackDurationTicks = 240;

		public override void OnHitNPCWithProj(Player player, Projectile proj, NPC target, NPC.HitInfo hit)
		{
			if (proj.DamageType == DamageClass.SummonMeleeSpeed)
			{
				target.GetGlobalNPC<AugmentCrackedNPC>().ApplyStack(StackDurationTicks);
				if (Main.netMode == NetmodeID.MultiplayerClient)
					AugmentNet.SendApplyNPCEffectCracked(target.whoAmI, StackDurationTicks);
			}
		}
	}
}
