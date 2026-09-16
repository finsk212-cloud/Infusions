using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Augments.Items;

namespace Augments
{
	public class MediGunPlayer : ModPlayer
	{
		public float OverclockCharge { get; set; } = 0f;
		public int CurrentPatientWhoAmI { get; set; } = -1;
		public int CurrentTargetNPCWhoAmI { get; set; } = -1;
		public int PhilosopherHealCooldown { get; set; } = 0;

		public float OverclockChargeRateMultiplier
		{
			get
			{
				if (Player.HeldItem?.TryGetGlobalItem<MediGunGlobalItem>(out var mg) == true)
				{
					if (mg.SocketedAccessoryType == ItemID.BandofStarpower)
						return 1.25f;
					if (mg.SocketedAccessoryType == ItemID.FeralClaws)
						return 1.15f;
				}
				return 1.0f;
			}
		}

		private bool rightClickReleased = true;
		private bool playedReadySound = false;

		public bool IsHoldingMediGun(out int tier)
		{
			tier = 1;
			if (Player.HeldItem == null || Player.HeldItem.IsAir)
				return false;

			int type = Player.HeldItem.type;
			if (type == ModContent.ItemType<Items.MediGunItem>())
			{
				tier = 1;
				return true;
			}
			if (type == ModContent.ItemType<Items.MediGunMK2Item>())
			{
				tier = 2;
				return true;
			}
			if (type == ModContent.ItemType<Items.MediGunMK3Item>())
			{
				tier = 3;
				return true;
			}
			if (type == ModContent.ItemType<Items.MediGunMK4Item>())
			{
				tier = 4;
				return true;
			}

			return false;
		}

		public override void PostUpdateEquips()
		{
			if (IsHoldingMediGun(out _) && Player.HeldItem?.TryGetGlobalItem<MediGunGlobalItem>(out var mg) == true)
			{
				bool isTethered = CurrentPatientWhoAmI >= 0 || CurrentTargetNPCWhoAmI >= 0;

				if (mg.SocketedAccessoryType == ItemID.AnkletoftheWind && isTethered)
				{
					Player.moveSpeed += 0.12f;
				}
				else if (mg.SocketedAccessoryType == ItemID.Aglet && isTethered)
				{
					Player.moveSpeed += 0.06f;
				}
				else if (mg.SocketedAccessoryType == ItemID.CobaltShield && isTethered)
				{
					Player.noKnockback = true;
				}
				else if (mg.SocketedAccessoryType == ItemID.Bezoar)
				{
					Player.buffImmune[BuffID.Poisoned] = true;
				}
				else if (mg.SocketedAccessoryType == ItemID.HandWarmer)
				{
					Player.buffImmune[BuffID.Chilled] = true;
					Player.buffImmune[BuffID.Frozen] = true;
					Player.resistCold = true;
				}
				else if (mg.SocketedAccessoryType == ItemID.SharkToothNecklace)
				{
					Player.GetArmorPenetration(DamageClass.Generic) += 3;
				}
				else if (mg.SocketedAccessoryType == ItemID.Shackle && isTethered)
				{
					Player.statDefense += 2;
				}
			}
		}

		public override void PostUpdate()
		{
			if (PhilosopherHealCooldown > 0)
				PhilosopherHealCooldown--;

			// Only local player processes input
			if (Player.whoAmI != Main.myPlayer)
				return;

			if (IsHoldingMediGun(out int tier))
			{
				if (OverclockCharge >= 100f)
				{
					OverclockCharge = 100f;
					if (!playedReadySound)
					{
						playedReadySound = true;
						SoundEngine.PlaySound(SoundID.MaxMana, Player.Center);
						CombatText.NewText(Player.getRect(), new Color(255, 215, 64), "OVERCLOCK READY!");
					}

					// Right-click activates Overclock on current tethered target (only when inventory is closed)
					if (Main.mouseRight && rightClickReleased && !Main.playerInventory)
					{
						rightClickReleased = false;

						if (CurrentPatientWhoAmI >= 0 || CurrentTargetNPCWhoAmI >= 0)
						{
							ActivateOverclock(tier);
						}
					}
				}
				else
				{
					playedReadySound = false;
				}

				if (!Main.mouseRight)
				{
					rightClickReleased = true;
				}
			}
			else
			{
				playedReadySound = false;
			}
		}

		public void ActivateOverclock(int tier)
		{
			OverclockCharge = 0f;
			playedReadySound = false;

			int healAmount = tier == 4 ? 100 : (tier == 3 ? 75 : (tier == 2 ? 50 : 30));
			int buffDuration = 360; // 6 seconds

			// Target player heal & buff (NO BUFFS TO MEDIC)
			if (CurrentPatientWhoAmI >= 0 && CurrentPatientWhoAmI < Main.maxPlayers)
			{
				Player patient = Main.player[CurrentPatientWhoAmI];
				if (patient.active && !patient.dead)
				{
					if (Main.netMode == NetmodeID.SinglePlayer)
					{
						SupportEffects.ServerHealPlayer(patient, healAmount);
						if (tier == 4)
						{
							patient.AddBuff(ModContent.BuffType<OverclockMK4Buff>(), buffDuration);
						}
						else if (tier == 3)
						{
							patient.AddBuff(ModContent.BuffType<OverclockMK3Buff>(), buffDuration);
						}
						else if (tier == 2)
						{
							patient.AddBuff(ModContent.BuffType<OverclockMK2Buff>(), buffDuration);
						}
						else
						{
							patient.AddBuff(ModContent.BuffType<OverclockBuff>(), buffDuration);
						}
					}
					else if (Main.netMode == NetmodeID.MultiplayerClient)
					{
						ModPacket packet = ModContent.GetInstance<Augments>().GetPacket();
						packet.Write((byte)AugmentPacketType.MediGunOverclockActivate);
						packet.Write((byte)tier);
						packet.Write((byte)CurrentPatientWhoAmI);
						packet.Send();
					}

					// Audio & visual burst on ally
					SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.85f, Pitch = 0.2f }, patient.Center);
					SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.95f, Pitch = 0.4f }, patient.Center);
					CombatText.NewText(patient.getRect(), new Color(80, 240, 255), $"+{healAmount} HP OVERCLOCK!");

					// Kinetic burst dusts around the ally
					for (int i = 0; i < 20; i++)
					{
						Dust d = Dust.NewDustDirect(patient.position, patient.width, patient.height, DustID.Electric, Main.rand.NextFloat(-3.5f, 3.5f), Main.rand.NextFloat(-3.5f, 3.5f), 100, default, 1.2f);
						d.noGravity = true;
					}
				}
			}
			else if (CurrentTargetNPCWhoAmI >= 0 && CurrentTargetNPCWhoAmI < Main.maxNPCs)
			{
				NPC npc = Main.npc[CurrentTargetNPCWhoAmI];
				if (npc.active)
				{
					npc.life = Math.Min(npc.lifeMax, npc.life + healAmount);
					npc.HealEffect(healAmount);
					if (Main.netMode == NetmodeID.Server)
					{
						NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
					}
					SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.85f, Pitch = 0.2f }, npc.Center);
					CombatText.NewText(npc.getRect(), new Color(80, 240, 255), $"+{healAmount} HP OVERCLOCK!");
				}
			}
		}

		public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
		{
			OverclockCharge = 0f;
			CurrentPatientWhoAmI = -1;
			CurrentTargetNPCWhoAmI = -1;
			playedReadySound = false;
			PhilosopherHealCooldown = 0;
		}

		public override bool HoverSlot(Item[] inventory, int context, int slot)
		{
			if (inventory == null || slot < 0 || slot >= inventory.Length)
				return false;

			Item targetItem = inventory[slot];
			if (targetItem == null || targetItem.IsAir || !targetItem.TryGetGlobalItem<MediGunGlobalItem>(out var mediGun))
				return false;

			if (context != ItemSlot.Context.InventoryItem &&
				context != ItemSlot.Context.ChestItem &&
				context != ItemSlot.Context.BankItem &&
				context != ItemSlot.Context.VoidItem)
			{
				return false;
			}

			if (Main.mouseRight && Main.mouseRightRelease)
			{
				// 1. Holding a supported accessory on cursor -> socket/swap it
				if (Main.mouseItem != null && !Main.mouseItem.IsAir && MediGunGlobalItem.IsSupportedAccessory(Main.mouseItem.type))
				{
					int oldType = mediGun.SocketedAccessoryType;
					mediGun.SocketedAccessoryType = Main.mouseItem.type;

					if (oldType > 0)
					{
						Main.mouseItem.SetDefaults(oldType);
					}
					else
					{
						Main.mouseItem.stack--;
						if (Main.mouseItem.stack <= 0)
							Main.mouseItem.TurnToAir();
					}

					SoundEngine.PlaySound(SoundID.Item37, Player.Center);
					CombatText.NewText(Player.getRect(), new Color(74, 222, 128), $"Socketed: {Lang.GetItemNameValue(mediGun.SocketedAccessoryType)}");

					if (Main.netMode == NetmodeID.MultiplayerClient && context == ItemSlot.Context.ChestItem && Player.chest >= 0)
					{
						NetMessage.SendData(MessageID.SyncChestItem, -1, -1, null, Player.chest, slot);
					}

					Main.mouseRightRelease = false;
					Recipe.FindRecipes();
					return true;
				}

				// 2. Empty cursor hand -> detach attached accessory directly into hand
				if ((Main.mouseItem == null || Main.mouseItem.IsAir) && mediGun.SocketedAccessoryType > 0)
				{
					int detachedType = mediGun.SocketedAccessoryType;
					mediGun.SocketedAccessoryType = 0;

					Main.mouseItem = new Item();
					Main.mouseItem.SetDefaults(detachedType);

					SoundEngine.PlaySound(SoundID.Grab, Player.Center);
					CombatText.NewText(Player.getRect(), new Color(255, 200, 80), $"Detached: {Lang.GetItemNameValue(detachedType)}");

					if (Main.netMode == NetmodeID.MultiplayerClient && context == ItemSlot.Context.ChestItem && Player.chest >= 0)
					{
						NetMessage.SendData(MessageID.SyncChestItem, -1, -1, null, Player.chest, slot);
					}

					Main.mouseRightRelease = false;
					Recipe.FindRecipes();
					return true;
				}
			}

			return false;
		}
	}
}
