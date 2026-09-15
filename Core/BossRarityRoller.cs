using Terraria;

namespace Augments
{
	public readonly struct RarityRollChances
	{
		public readonly int Common;
		public readonly int Rare;
		public readonly int Epic;
		public readonly int Legendary;

		public RarityRollChances(int common, int rare, int epic, int legendary)
		{
			Common = common;
			Rare = rare;
			Epic = epic;
			Legendary = legendary;
		}
	}

	// Rolls a weighted AugmentRarity for a given boss bracket. Weights are
	// per-mille... well, per-cent - each bracket's weights sum to 100.
	public static class BossRarityRoller
	{
		public static AugmentRarity Roll(RarityBracket bracket)
		{
			RarityRollChances chances = GetChancesForBracket(bracket);
			int roll = Main.rand.Next(100);
			if (roll < chances.Common)
				return AugmentRarity.Common;
			roll -= chances.Common;
			if (roll < chances.Rare)
				return AugmentRarity.Rare;
			roll -= chances.Rare;
			if (roll < chances.Epic)
				return AugmentRarity.Epic;
			return AugmentRarity.Legendary;
		}

		public static RarityRollChances GetChancesForBracket(RarityBracket bracket)
		{
			return bracket switch
			{
				RarityBracket.PreHardmode => new RarityRollChances(80, 15, 5, 0),
				RarityBracket.EarlyHardmode => new RarityRollChances(50, 38, 10, 2),
				RarityBracket.PostMechs => new RarityRollChances(34, 30, 30, 6),
				RarityBracket.PostPlantera => new RarityRollChances(25, 25, 35, 15),
				RarityBracket.EarlyPostMoonLord => new RarityRollChances(15, 20, 40, 25),
				RarityBracket.LatePostMoonLord => new RarityRollChances(10, 15, 40, 35),
				RarityBracket.FinalCalamity => new RarityRollChances(5, 10, 35, 50),
				_ => new RarityRollChances(80, 15, 5, 0)
			};
		}
	}
}
