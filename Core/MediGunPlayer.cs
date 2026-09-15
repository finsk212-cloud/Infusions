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
		public float UberCharge { get; set; } = 0f;
		public int UberActiveTimer { get; set; } = 0;
		public int UberDurationMax { get; set; } = 480;
		public int UberTier { get; set; } = 1;
		public int CurrentPatientWhoAmI { get; set; } = -1;

		public bool IsUberActive => UberActiveTimer > 0;

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

		public override void ResetEffects()
		{
			if (UberActiveTimer > 0)
			{
				UberActiveTimer--;

				// Self buff & invulnerability
				Player.AddBuff(ModContent.BuffType<UberChargeBuff>(), 10);

				// Tier-scaled mobility and combat power
				switch (UberTier)
				{
					case 1:
						Player.moveSpeed += 0.20f;
						break;
					case 2:
						Player.moveSpeed += 0.25f;
						Player.GetDamage(DamageClass.Generic) += 0.10f;
						break;
					case 3:
						Player.moveSpeed += 0.30f;
						Player.GetDamage(DamageClass.Generic) += 0.15f;
						Player.GetCritChance(DamageClass.Generic) += 15f;
						break;
					case 4:
						Player.moveSpeed += 0.35f;
						Player.GetDamage(DamageClass.Generic) += 0.25f;
						Player.GetCritChance(DamageClass.Generic) += 25f;
						Player.wingTime = Math.Max(Player.wingTime, 60);

						// Celestial electric sparks
						if (Main.rand.NextBool(3))
						{
							Dust d = Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.GoldFlame, 0f, 0f, 100, default, 1.4f);
							d.noGravity = true;
						}
						break;
				}

				// Apply to tethered patient if valid
				if (CurrentPatientWhoAmI >= 0 && CurrentPatientWhoAmI < Main.maxPlayers)
				{
					Player patient = Main.player[CurrentPatientWhoAmI];
					if (patient.active && !patient.dead)
					{
						patient.AddBuff(ModContent.BuffType<UberChargeBuff>(), 10);

						switch (UberTier)
						{
							case 1:
								patient.moveSpeed += 0.20f;
								break;
							case 2:
								patient.moveSpeed += 0.25f;
								patient.GetDamage(DamageClass.Generic) += 0.10f;
								break;
							case 3:
								patient.moveSpeed += 0.30f;
								patient.GetDamage(DamageClass.Generic) += 0.15f;
								patient.GetCritChance(DamageClass.Generic) += 15f;
								break;
							case 4:
								patient.moveSpeed += 0.35f;
								patient.GetDamage(DamageClass.Generic) += 0.25f;
								patient.GetCritChance(DamageClass.Generic) += 25f;
								patient.wingTime = Math.Max(patient.wingTime, 60);
								break;
						}
					}
				}

				if (UberActiveTimer <= 0)
				{
					CurrentPatientWhoAmI = -1;
				}
			}
		}

		public override void PostUpdate()
		{
			// Only the local player evaluates input and triggers activation
			if (Player.whoAmI != Main.myPlayer)
				return;

			if (IsHoldingMediGun(out int tier))
			{
				if (UberCharge >= 100f)
				{
					UberCharge = 100f;
					if (!playedReadySound && !IsUberActive)
					{
						playedReadySound = true;
						SoundEngine.PlaySound(SoundID.MaxMana, Player.Center);
						CombatText.NewText(Player.getRect(), new Color(255, 215, 64), "ÜBERCHARGE READY!");
					}

					// Right-click trigger check
					if (Main.mouseRight && rightClickReleased && !IsUberActive)
					{
						rightClickReleased = false;
						ActivateUber(tier);
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

		public void ActivateUber(int tier)
		{
			UberTier = tier;
			int duration = tier switch
			{
				1 => 480, // 8 seconds
				2 => 510, // 8.5 seconds
				3 => 540, // 9 seconds
				4 => 600, // 10 seconds
				_ => 480
			};

			UberDurationMax = duration;
			UberActiveTimer = duration;
			UberCharge = 0f;
			playedReadySound = false;

			SoundEngine.PlaySound(SoundID.Item119 with { Volume = 0.95f, Pitch = 0.2f }, Player.Center);
			SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.85f, Pitch = 0.4f }, Player.Center);
			CombatText.NewText(Player.getRect(), new Color(80, 240, 255), "ÜBERCHARGE ACTIVATED!");

			// Particle burst on activation
			for (int i = 0; i < 25; i++)
			{
				Dust d = Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.Electric, Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f), 100, default, 1.4f);
				d.noGravity = true;
			}
			for (int i = 0; i < 15; i++)
			{
				Dust d = Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.GoldFlame, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 100, default, 1.3f);
				d.noGravity = true;
			}

			// Broadcast to server in multiplayer
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = ModContent.GetInstance<Augments>().GetPacket();
				packet.Write((byte)AugmentPacketType.MediGunUberActivate);
				packet.Write((byte)tier);
				packet.Write((short)CurrentPatientWhoAmI);
				packet.Send();
			}
		}

		public override bool FreeDodge(Player.HurtInfo info)
		{
			// Absolute invulnerability during active ÜberCharge
			if (IsUberActive || Player.HasBuff<UberChargeBuff>())
			{
				Player.immune = true;
				Player.immuneTime = 30;
				return true;
			}
			return base.FreeDodge(info);
		}

		public override void ModifyHurt(ref Player.HurtModifiers modifiers)
		{
			if (IsUberActive || Player.HasBuff<UberChargeBuff>())
			{
				modifiers.SetMaxDamage(0);
				modifiers.FinalDamage *= 0f;
			}
		}

		public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
		{
			if (IsUberActive || Player.HasBuff<UberChargeBuff>())
			{
				Player.statLife = Math.Max(1, Player.statLife);
				Player.immune = true;
				Player.immuneTime = 60;
				return false;
			}
			return base.PreKill(damage, hitDirection, pvp, ref playSound, ref genGore, ref damageSource);
		}

		public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
		{
			UberCharge = 0f;
			UberActiveTimer = 0;
			CurrentPatientWhoAmI = -1;
			playedReadySound = false;
		}
	}
}
