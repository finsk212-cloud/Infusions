using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
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

		public static bool TryFindSupportOwner(Player target, string augmentId, float radius, out Player owner, bool includeSelf = false)
		{
			owner = null;
			float bestDistanceSquared = radius < 0f ? float.MaxValue : radius * radius;
			for (int i = 0; i < Main.maxPlayers; i++)
			{
				Player candidate = Main.player[i];
				if (!candidate.active || candidate.dead)
					continue;
				if (!includeSelf && candidate.whoAmI == target.whoAmI)
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

			int maxLife = Math.Max(target.statLifeMax, target.statLifeMax2);
			if (maxLife <= 0) maxLife = 500;

			int oldLife = target.statLife;
			target.statLife = Math.Min(maxLife, target.statLife + healAmount);
			int actualHeal = Math.Max(healAmount, target.statLife - oldLife);
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

		public static bool IsCleansableDebuff(int buffType)
		{
			if (buffType <= 0 || !Main.debuff[buffType])
				return false;
			if (buffType == BuffID.PotionSickness || buffType == BuffID.ManaSickness || buffType == BuffID.ChaosState)
				return false;
			if (buffType == ModContent.BuffType<LifelineCooldownBuff>() || buffType == ModContent.BuffType<LastRitesCooldownBuff>())
				return false;
			return true;
		}

		public static void ServerClearDebuffs(Player target)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient || target == null || !target.active)
				return;

			for (int i = target.buffType.Length - 1; i >= 0; i--)
			{
				if (IsCleansableDebuff(target.buffType[i]))
					target.DelBuff(i);
			}

			if (Main.netMode == NetmodeID.Server)
			{
				ModPacket packet = ModContent.GetInstance<Augments>().GetPacket();
				packet.Write((byte)AugmentPacketType.CleanseClearDebuffs);
				packet.Send(target.whoAmI);
			}
		}

		public static void HandleCleanseClearDebuffs()
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			Player player = Main.LocalPlayer;
			if (!player.active || player.dead)
				return;

			bool removedAny = false;
			for (int i = player.buffType.Length - 1; i >= 0; i--)
			{
				if (IsCleansableDebuff(player.buffType[i]))
				{
					player.DelBuff(i);
					removedAny = true;
				}
			}

			SoundEngine.PlaySound(SoundID.Item4, player.Center);
			for (int i = 0; i < 25; i++)
			{
				Vector2 speed = Main.rand.NextVector2Circular(5f, 5f);
				Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.PurificationPowder, speed.X, speed.Y);
				d.noGravity = true;
			}

			if (removedAny)
			{
				CombatText.NewText(player.getRect(), Color.LightCyan, "Cleansed!");
			}

			NetMessage.SendData(MessageID.PlayerBuffs, -1, -1, null, player.whoAmI);
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

		public static void HandleSoulMartyrTrigger(int sourcePlayerIndex, byte martyrIndex, int damage)
		{
			if (Main.netMode != NetmodeID.Server || martyrIndex >= Main.maxPlayers)
				return;

			Player martyr = Main.player[martyrIndex];
			if (!martyr.active || martyr.dead || !martyr.GetModPlayer<AugmentPlayer>().HasAugment("soul_martyr"))
				return;

			// Server applies damage to martyr, clamped so they never drop below 1 HP
			martyr.statLife = Math.Max(1, martyr.statLife - damage);
			NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, martyr.whoAmI);

			// Notify martyr's client so it updates local statLife and triggers invulnerability
			ModPacket packet = ModContent.GetInstance<Augments>().GetPacket();
			packet.Write((byte)AugmentPacketType.SoulMartyrDamage);
			packet.Write(damage);
			packet.Send(martyr.whoAmI);
		}

		public static void HandleSoulMartyrDamage(int damage)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			Player player = Main.LocalPlayer;
			player.statLife = Math.Max(1, player.statLife - damage);
			player.immune = true;
			player.immuneTime = 40;
			SoundEngine.PlaySound(SoundID.Item29, player.Center);
			NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, player.whoAmI);
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
				int maxHp = player.statLifeMax2 > 0 ? player.statLifeMax2 : player.statLifeMax;
				player.statLife = Math.Min(maxHp, player.statLife + amount);
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
