using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
	public class MediGunPlayer : ModPlayer
	{
		public float OverclockCharge { get; set; } = 0f;
		public int CurrentPatientWhoAmI { get; set; } = -1;
		public int CurrentTargetNPCWhoAmI { get; set; } = -1;

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

		public override void PostUpdate()
		{
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

					// Right-click activates Overclock on current tethered target
					if (Main.mouseRight && rightClickReleased)
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
		}
	}
}
