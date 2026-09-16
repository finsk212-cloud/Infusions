using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Augments.Items
{
	public class MediGunGlobalItem : GlobalItem
	{
		public override bool InstancePerEntity => true;

		public int SocketedAccessoryType { get; set; } = 0;

		public override bool AppliesToEntity(Item item, bool lateInstantiation)
		{
			return item.type == ModContent.ItemType<MediGunItem>()
				|| item.type == ModContent.ItemType<MediGunMK2Item>()
				|| item.type == ModContent.ItemType<MediGunMK3Item>()
				|| item.type == ModContent.ItemType<MediGunMK4Item>();
		}

		public static bool IsSupportedAccessory(int itemType)
		{
			return itemType == ItemID.BandofRegeneration;
		}

		public override void SaveData(Item item, TagCompound tag)
		{
			if (SocketedAccessoryType > 0)
			{
				tag["SocketedAccessoryType"] = SocketedAccessoryType;
			}
		}

		public override void LoadData(Item item, TagCompound tag)
		{
			SocketedAccessoryType = tag.GetInt("SocketedAccessoryType");
		}

		public override void NetSend(Item item, System.IO.BinaryWriter writer)
		{
			writer.Write(SocketedAccessoryType);
		}

		public override void NetReceive(Item item, System.IO.BinaryReader reader)
		{
			SocketedAccessoryType = reader.ReadInt32();
		}

		public override void OnCreated(Item item, ItemCreationContext context)
		{
			if (context is RecipeItemCreationContext recipeContext)
			{
				foreach (Item consumed in recipeContext.ConsumedItems)
				{
					if (consumed.TryGetGlobalItem<MediGunGlobalItem>(out var consumedMedi) && consumedMedi.SocketedAccessoryType > 0)
					{
						SocketedAccessoryType = consumedMedi.SocketedAccessoryType;
						break;
					}
				}
			}
		}

		public override bool CanRightClick(Item item)
		{
			return true;
		}

		public override bool ConsumeItem(Item item, Player player)
		{
			return false;
		}

		public override void RightClick(Item item, Player player)
		{
			if (SocketedAccessoryType > 0)
			{
				// Detach existing accessory
				int detachedType = SocketedAccessoryType;
				SocketedAccessoryType = 0;
				player.QuickSpawnItem(player.GetSource_ItemUse(item), detachedType, 1);
				SoundEngine.PlaySound(SoundID.Grab, player.Center);
				CombatText.NewText(player.getRect(), new Color(255, 200, 80), $"Detached: {Lang.GetItemNameValue(detachedType)}");
			}
			else
			{
				// Check cursor first, then inventory
				Item candidate = null;
				if (Main.mouseItem != null && !Main.mouseItem.IsAir && IsSupportedAccessory(Main.mouseItem.type))
				{
					candidate = Main.mouseItem;
				}
				else
				{
					for (int i = 0; i < 50; i++)
					{
						Item invItem = player.inventory[i];
						if (invItem != null && !invItem.IsAir && IsSupportedAccessory(invItem.type))
						{
							candidate = invItem;
							break;
						}
					}
				}

				if (candidate != null)
				{
					SocketedAccessoryType = candidate.type;
					candidate.stack--;
					if (candidate.stack <= 0)
						candidate.TurnToAir();

					SoundEngine.PlaySound(SoundID.Item37, player.Center);
					CombatText.NewText(player.getRect(), new Color(74, 222, 128), $"Socketed: {Lang.GetItemNameValue(SocketedAccessoryType)}");
				}
				else
				{
					CombatText.NewText(player.getRect(), new Color(200, 200, 200), "No compatible accessory in inventory!");
				}
			}
		}

		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
		{
			if (SocketedAccessoryType == ItemID.BandofRegeneration)
			{
				tooltips.Add(new TooltipLine(Mod, "SocketHeader", $"[c/FFC83B:Attached Accessory: {Lang.GetItemNameValue(SocketedAccessoryType)}]")
				{
					OverrideColor = new Color(255, 200, 59)
				});
				tooltips.Add(new TooltipLine(Mod, "SocketEffect1", "• 50% of beam healing is returned to the medic")
				{
					OverrideColor = new Color(74, 222, 128)
				});
				tooltips.Add(new TooltipLine(Mod, "SocketEffect2", "• Tethered ally gains Rapid Healing")
				{
					OverrideColor = new Color(74, 222, 128)
				});
				tooltips.Add(new TooltipLine(Mod, "SocketPrompt", "Right-Click in inventory to detach accessory")
				{
					OverrideColor = new Color(148, 163, 184)
				});
			}
			else
			{
				tooltips.Add(new TooltipLine(Mod, "EmptySocket", "[c/94A3B8:Empty Accessory Socket]")
				{
					OverrideColor = new Color(148, 163, 184)
				});
				tooltips.Add(new TooltipLine(Mod, "EmptySocketHint", "Right-Click in inventory with Band of Regeneration to socket")
				{
					OverrideColor = new Color(140, 230, 160)
				});
			}
		}
	}
}
