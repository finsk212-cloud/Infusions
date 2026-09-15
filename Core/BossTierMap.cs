using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace Augments
{
	public enum RarityBracket
	{
		PreHardmode,
		EarlyHardmode,
		PostMechs,
		PostPlantera,
		EarlyPostMoonLord,
		LatePostMoonLord,
		FinalCalamity
	}

	// Maps a boss's NPC type to the bracket of rarities it should roll from.
	// Add new bosses here as you support more of them. Anything NOT in this
	// map falls back to PreHardmode, so an untiered boss still gives something
	// instead of silently doing nothing.
	public static class BossTierMap
	{
		public static readonly Dictionary<int, RarityBracket> Brackets = new Dictionary<int, RarityBracket>
		{
			// --- Pre-hardmode ---
			{ NPCID.KingSlime, RarityBracket.PreHardmode },
			{ NPCID.EyeofCthulhu, RarityBracket.PreHardmode },
			{ NPCID.EaterofWorldsHead, RarityBracket.PreHardmode },
			{ NPCID.BrainofCthulhu, RarityBracket.PreHardmode },
			{ NPCID.QueenBee, RarityBracket.PreHardmode },
			{ NPCID.SkeletronHead, RarityBracket.PreHardmode },
			{ NPCID.Deerclops, RarityBracket.PreHardmode },

			// --- Wall of Flesh ---
			{ NPCID.WallofFlesh, RarityBracket.EarlyHardmode },

			// --- Early/mid hardmode ---
			{ NPCID.QueenSlimeBoss, RarityBracket.EarlyHardmode },
			{ NPCID.Retinazer, RarityBracket.PostMechs },
			{ NPCID.Spazmatism, RarityBracket.PostMechs },
			{ NPCID.TheDestroyer, RarityBracket.PostMechs },
			{ NPCID.SkeletronPrime, RarityBracket.PostMechs },

			// --- Plantera and beyond ---
			{ NPCID.Plantera, RarityBracket.PostPlantera },
			{ NPCID.Golem, RarityBracket.PostPlantera },
			{ NPCID.DukeFishron, RarityBracket.PostPlantera },
			{ NPCID.HallowBoss, RarityBracket.PostPlantera }, // Empress of Light

			// --- Endgame ---
			{ NPCID.CultistBoss, RarityBracket.PostPlantera }, // Lunatic Cultist
			{ NPCID.MoonLordCore, RarityBracket.EarlyPostMoonLord },
		};

		public static RarityBracket GetBracket(int npcType)
		{
			if (Brackets.TryGetValue(npcType, out var bracket))
				return bracket;
			return RarityBracket.PreHardmode; // safe fallback for anything not yet mapped
		}

		public static RarityBracket GetCurrentWorldBracket()
		{
			if (NPC.downedMoonlord)
				return RarityBracket.EarlyPostMoonLord;

			if (NPC.downedPlantBoss || NPC.downedGolemBoss || NPC.downedFishron || NPC.downedEmpressOfLight || NPC.downedAncientCultist)
				return RarityBracket.PostPlantera;

			if (NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3)
				return RarityBracket.PostMechs;

			if (Main.hardMode || NPC.downedQueenSlime)
				return RarityBracket.EarlyHardmode;

			return RarityBracket.PreHardmode;
		}

		public static string GetBracketName(RarityBracket bracket)
		{
			return bracket switch
			{
				RarityBracket.PreHardmode => "Pre-Hardmode",
				RarityBracket.EarlyHardmode => "Early Hardmode",
				RarityBracket.PostMechs => "Post-Mechanical",
				RarityBracket.PostPlantera => "Post-Plantera",
				RarityBracket.EarlyPostMoonLord => "Post-Moon Lord",
				RarityBracket.LatePostMoonLord => "Late Endgame",
				RarityBracket.FinalCalamity => "Final Protocol",
				_ => "Standard"
			};
		}
	}
}
