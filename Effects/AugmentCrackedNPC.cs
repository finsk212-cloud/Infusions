using Terraria;
using Terraria.ModLoader;

namespace Augments
{
	// Tracks the Whip Cracker "Cracked" debuff per NPC - an invisible stacking
	// damage-taken amplifier with no vanilla buff icon, refreshed (not stacked)
	// in duration on every new whip hit.
	public class AugmentCrackedNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public const int MaxStacks = 5;
		public const float DamageIncreasePerStack = 0.02f;

		private int ticksRemaining;
		private int stacks;
		public int SonicCrackCooldown;

		public int Stacks => ticksRemaining > 0 ? stacks : 0;

		// Call on every whip hit - adds a stack (up to MaxStacks) and resets
		// the duration to its full length rather than extending it.
		public void ApplyStack(int durationTicks)
		{
			if (ticksRemaining <= 0)
				stacks = 0;

			if (stacks < MaxStacks)
				stacks++;

			ticksRemaining = durationTicks;
		}

		public override void PostAI(NPC npc)
		{
			if (SonicCrackCooldown > 0)
				SonicCrackCooldown--;

			if (ticksRemaining <= 0)
				return;

			ticksRemaining--;
			if (ticksRemaining <= 0)
				stacks = 0;
		}

		public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
		{
			if (Stacks > 0)
				modifiers.FinalDamage *= 1f + Stacks * DamageIncreasePerStack;
		}
	}
}
