using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
	internal static class SupportEffects
	{
		public const float AuraRadius = 600f;

		public static bool AreAllies(Player owner, Player target)
		{
			if (owner == null || target == null || !owner.active || !target.active)
				return false;
			return owner.team == 0 || target.team == 0 || owner.team == target.team;
		}

		public static bool IsAllyInRange(Player owner, Player target, float radius, bool includeOwner = false)
		{
			if (!AreAllies(owner, target) || target.dead)
				return false;
			if (!includeOwner && owner.whoAmI == target.whoAmI)
				return false;
			return Vector2.DistanceSquared(owner.Center, target.Center) <= radius * radius;
		}

		public static bool TryFindSupportOwner(Player target, string augmentId, float radius, out Player owner)
		{
			owner = null;
			float bestDistanceSquared = radius < 0f ? float.MaxValue : radius * radius;
			for (int i = 0; i < Main.maxPlayers; i++)
			{
				Player candidate = Main.player[i];
				if (!candidate.active || candidate.dead || candidate.whoAmI == target.whoAmI)
					continue;
				if (!AreAllies(candidate, target) || !candidate.GetModPlayer<AugmentPlayer>().HasAugment(augmentId))
					continue;

				float distanceSquared = Vector2.DistanceSquared(candidate.Center, target.Center);
				if (distanceSquared > bestDistanceSquared)
					continue;

				bestDistanceSquared = distanceSquared;
				owner = candidate;
			}

			return owner != null;
		}

		public static int ServerHealPlayer(Player target, int healAmount)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient || target == null || !target.active || target.dead || healAmount <= 0)
				return 0;

			int oldLife = target.statLife;
			target.statLife = Math.Min(target.statLifeMax2, target.statLife + healAmount);
			int actualHeal = target.statLife - oldLife;
			if (actualHeal <= 0)
				return 0;

			if (Main.netMode == NetmodeID.Server)
			{
				NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, target.whoAmI);
				ModPacket packet = ModContent.GetInstance<Augments>().GetPacket();
				packet.Write((byte)AugmentPacketType.SupportHealVisual);
				packet.Write((byte)target.whoAmI);
				packet.Write(actualHeal);
				packet.Send();
			}
			else
			{
				target.HealEffect(actualHeal, false);
			}

			ModContent.GetInstance<Augments>().Logger.Info($"Server healed target={target.name} amount={actualHeal}");
			return actualHeal;
		}

		public static void ServerClearDebuffs(Player target)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient || target == null || !target.active)
				return;

			for (int i = target.buffType.Length - 1; i >= 0; i--)
			{
				if (target.buffType[i] > 0 && Main.debuff[target.buffType[i]])
					target.DelBuff(i);
			}

			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.PlayerBuffs, -1, -1, null, target.whoAmI);
		}

		public static void HandleSoulLinkRequest(int whoAmI, int requestedHeal)
		{
			if (!TryGetRequestOwner(whoAmI, "soul_link", out Player owner))
				return;
			if (!owner.GetModPlayer<AugmentPlayer>().TryAuthorizeSoulLinkRequest())
				return;

			int healAmount = Math.Clamp(requestedHeal, 1, 200);
			foreach (Player target in Main.player)
			{
				if (IsAllyInRange(owner, target, AuraRadius))
					ServerHealPlayer(target, healAmount);
			}
		}

		public static void HandleCleanseRequest(int whoAmI)
		{
			if (!TryGetRequestOwner(whoAmI, "cleanse", out Player owner))
				return;
			owner.GetModPlayer<AugmentPlayer>().TryTriggerCleanseServer();
		}

		public static void HandleLifelineRequest(int whoAmI)
		{
			if (Main.netMode != NetmodeID.Server || whoAmI < 0 || whoAmI >= Main.maxPlayers)
				return;

			Player target = Main.player[whoAmI];
			if (target.active)
				target.GetModPlayer<AugmentPlayer>().TryConsumeLifelineServer();
		}

		public static void HandleUndyingBondRequest(int whoAmI)
		{
			if (Main.netMode != NetmodeID.Server || whoAmI < 0 || whoAmI >= Main.maxPlayers)
				return;

			Player target = Main.player[whoAmI];
			if (target.active && target.dead)
				target.GetModPlayer<AugmentPlayer>().TryApplyUndyingBondRedirectServer();
		}

		public static void BroadcastRevitalizingWaveVisual(Player owner)
		{
			if (Main.netMode != NetmodeID.Server)
				return;

			ModPacket packet = ModContent.GetInstance<Augments>().GetPacket();
			packet.Write((byte)AugmentPacketType.RevitalizingWaveVisual);
			packet.Write((byte)owner.whoAmI);
			packet.Send();
		}

		public static void SendUndyingBondRedirect(Player target, int spawnX, int spawnY)
		{
			if (Main.netMode != NetmodeID.Server)
				return;

			ModPacket packet = ModContent.GetInstance<Augments>().GetPacket();
			packet.Write((byte)AugmentPacketType.UndyingBondRedirect);
			packet.Write(spawnX);
			packet.Write(spawnY);
			packet.Send(target.whoAmI);
		}

		public static void HandleRevitalizingWaveRequest(int whoAmI)
		{
			if (!TryGetRequestOwner(whoAmI, "revitalizing_wave", out Player owner))
				return;

			BroadcastRevitalizingWaveVisual(owner);

			foreach (Player target in Main.player)
			{
				if (IsAllyInRange(owner, target, AuraRadius))
					ServerHealPlayer(target, 25);
			}
		}

		public static void HandleRevitalizingWaveVisual(int ownerIndex)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient && ownerIndex >= 0 && ownerIndex < Main.maxPlayers)
				RevitalizingWaveAugment.SpawnBurst(Main.player[ownerIndex]);
		}

		public static void HandleHealVisual(int targetIndex, int amount)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient || targetIndex < 0 || targetIndex >= Main.maxPlayers || amount <= 0)
				return;

			Player player = Main.player[targetIndex];
			if (!player.active || player.dead)
				return;

			// Floating combat text for all nearby clients
			player.HealEffect(amount, false);

			// If this client is the target being healed, we MUST update our own local statLife!
			// In vanilla Terraria, a client's own statLife is client-authoritative and ignores
			// incoming PlayerLifeMana (packet 16) from the server when whoAmI == Main.myPlayer.
			// Without this local update, the client would report their old HP on next sync,
			// causing the heal to rubberband backwards.
			if (targetIndex == Main.myPlayer)
			{
				player.statLife = Math.Min(player.statLifeMax2, player.statLife + amount);
				NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, Main.myPlayer);
			}
		}

		public static void HandleUndyingBondRedirect(int spawnX, int spawnY)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			Main.LocalPlayer.GetModPlayer<AugmentPlayer>().ApplyUndyingBondRedirectClient(spawnX, spawnY);
		}

		private static bool TryGetRequestOwner(int whoAmI, string augmentId, out Player owner)
		{
			owner = null;
			if (Main.netMode != NetmodeID.Server || whoAmI < 0 || whoAmI >= Main.maxPlayers)
				return false;

			owner = Main.player[whoAmI];
			return owner.active && !owner.dead && owner.GetModPlayer<AugmentPlayer>().HasAugment(augmentId);
		}
	}
}
