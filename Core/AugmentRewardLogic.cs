using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Augments
{
	public static class AugmentRewardLogic
	{
		// Flat, one-shot chance (not per-slot) for an Epic/Legendary roll to
		// present a whole Keystone family instead of 3 normal picks.
		private const float KeystoneFamilyChance = 0.15f;

		// Rolls a rarity for the bracket, tries that tier, falls back to
		public static bool TryRollRewardChoices(AugmentPlayer augmentPlayer, RarityBracket bracket, HashSet<string> excludedIds, out List<Augment> choices, out AugmentRarity finalRarity)
		{
			AugmentRarity rolledRarity = BossRarityRoller.Roll(bracket);

			// Protocol Partner Bias (20%): If player has 1/2 of an active protocol, 20% chance to bias the reward tier towards the partner chip's rarity
			var missingPartners = augmentPlayer.GetMissingProtocolPartners();
			if (missingPartners.Count > 0 && Main.rand.NextFloat() < 0.20f)
			{
				var targetPartner = missingPartners[Main.rand.Next(missingPartners.Count)];
				rolledRarity = targetPartner.Rarity;
			}

			if (TryRollKeystoneFamily(augmentPlayer, rolledRarity, out List<Augment> keystoneChoices))
			{
				choices = keystoneChoices;
				finalRarity = rolledRarity;
				return true;
			}

			foreach (AugmentRarity rarity in GetRarityFallbackOrder(rolledRarity))
			{
				choices = augmentPlayer.RollChoices(3, rarity, excludedIds);
				if (choices.Count > 0)
				{
					finalRarity = rarity;
					return true;
				}
			}

			choices = new List<Augment>();
			finalRarity = rolledRarity;
			return false;
		}

		public static void GrantReward(Player player, RarityBracket bracket)
		{
			AugmentPlayer augmentPlayer = player.GetModPlayer<AugmentPlayer>();

			if (TryRollRewardChoices(augmentPlayer, bracket, null, out List<Augment> choices, out AugmentRarity rarity))
			{
				ShowRewardChoices(player, choices, rarity, bracket);
				return;
			}

			const string message = "No plug-in chips left to offer.";
			if (Main.netMode == NetmodeID.Server)
				ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral(message), new Color(255, 100, 100), player.whoAmI);
			else
				Main.NewText(message, 255, 100, 100);
		}

		public static IEnumerable<AugmentRarity> GetRarityFallbackOrder(AugmentRarity rolledRarity)
		{
			yield return rolledRarity;

			for (int rarity = (int)rolledRarity - 1; rarity >= (int)AugmentRarity.Common; rarity--)
				yield return (AugmentRarity)rarity;

			for (int rarity = (int)rolledRarity + 1; rarity <= (int)AugmentRarity.Legendary; rarity++)
				yield return (AugmentRarity)rarity;
		}

		private static void ShowRewardChoices(Player player, List<Augment> choices, AugmentRarity rarity, RarityBracket bracket)
		{
			if (Main.netMode == NetmodeID.Server)
				AugmentNet.SendRewardChoices(player.whoAmI, choices, rarity, bracket);
			else
				ModContent.GetInstance<AugmentUISystem>().ShowChoices(choices, rarity, bracket);
		}

		// Only Epic/Legendary rolls get a shot at this - on success, an
		// entire unlocked Keystone family (all of its members, regardless of
		// each one's own exact rarity) replaces the popup outright, completely
		// bypassing AugmentPlayer.RollChoices for this kill. Keystones are
		// never mixed in 1-at-a-time with normal cards.
		private static bool TryRollKeystoneFamily(AugmentPlayer augmentPlayer, AugmentRarity rolledRarity, out List<Augment> choices)
		{
			choices = null;

			if (rolledRarity != AugmentRarity.Epic && rolledRarity != AugmentRarity.Legendary)
				return false;

			if (Main.rand.NextFloat() >= KeystoneFamilyChance)
				return false;

			var eligibleFamilies = new List<string>();
			foreach (var augment in AugmentDatabase.All)
			{
				if (augment.KeystoneFamily == null || eligibleFamilies.Contains(augment.KeystoneFamily))
					continue;

				if (augmentPlayer.LockedKeystoneFamilies.Contains(augment.KeystoneFamily))
					continue;

				bool hasMemberAtRolledRarity = false;
				foreach (var member in AugmentDatabase.All)
				{
					if (member.KeystoneFamily == augment.KeystoneFamily && member.Rarity == rolledRarity)
					{
						hasMemberAtRolledRarity = true;
						break;
					}
				}

				if (hasMemberAtRolledRarity)
					eligibleFamilies.Add(augment.KeystoneFamily);
			}

			if (eligibleFamilies.Count == 0)
				return false;

			string chosenFamily = eligibleFamilies[Main.rand.Next(eligibleFamilies.Count)];

			choices = new List<Augment>();
			foreach (var augment in AugmentDatabase.All)
			{
				if (augment.KeystoneFamily == chosenFamily)
					choices.Add(augment);
			}

			return true;
		}
	}
}
