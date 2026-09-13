using Terraria;
using Terraria.ModLoader;

using Terraria.ID;

namespace Augments
{
    public class AugmentGlobalItem : GlobalItem
    {
        public override void OnConsumeItem(Item item, Player player)
        {
            foreach (var augment in player.GetModPlayer<AugmentPlayer>().Owned)
                augment.OnConsumeItem(player, item);
        }

        public override void RightClick(Item item, Player player)
        {
            foreach (var augment in player.GetModPlayer<AugmentPlayer>().Owned)
                augment.RightClickItem(player, item);
        }

        // Reforging always acts on Main.LocalPlayer - there is no other
        // player whose item could be Main.reforgeItem, so that's the correct
        // player to forward here (matching the same assumption the vanilla
        // reforge UI itself makes).
        public override bool ReforgePrice(Item item, ref int reforgePrice, ref bool canApplyDiscount)
        {
            bool result = true;
            foreach (var augment in Main.LocalPlayer.GetModPlayer<AugmentPlayer>().Owned)
                result &= augment.ReforgePrice(Main.LocalPlayer, item, ref reforgePrice, ref canApplyDiscount);
            return result;
        }

        public override void PreReforge(Item item)
        {
            foreach (var augment in Main.LocalPlayer.GetModPlayer<AugmentPlayer>().Owned)
                augment.PreReforge(Main.LocalPlayer, item);
        }

        public override void PostReforge(Item item)
        {
            foreach (var augment in Main.LocalPlayer.GetModPlayer<AugmentPlayer>().Owned)
                augment.PostReforge(Main.LocalPlayer, item);
        }

        public override void ModifyTooltips(Item item, System.Collections.Generic.List<TooltipLine> tooltips)
        {
            for (int i = tooltips.Count - 1; i >= 0; i--)
            {
                TooltipLine line = tooltips[i];
                if (line.Name == "ModName" && (line.Text.Contains("Augments") || line.Text.Contains("[Augments]")))
                {
                    tooltips.RemoveAt(i);
                    continue;
                }

                if (line.Text.Contains("[Augments]"))
                {
                    line.Text = line.Text.Replace("[Augments]", "").TrimEnd();
                }
            }

            if (Main.LocalPlayer == null)
                return;

            var ap = Main.LocalPlayer.GetModPlayer<AugmentPlayer>();
            if (ap == null)
                return;

            // 1. Tooltips for any Bait item in inventory when Bait Master is active
            if (item.bait > 0 && ap.HasAugment("bait_master"))
            {
                int bonus = BaitMasterAugment.GetBaitBonus(item.bait);
                tooltips.Add(new TooltipLine(Mod, "BaitMasterBonus",
                    $"[c/38B6FF:✦ Bait Master: +30% Fishing Power (+{bonus}%)]"));
                tooltips.Add(new TooltipLine(Mod, "BaitMasterEffective",
                    $"[c/88FFAA:  Effective Bait Power: {item.bait + bonus}%]"));
            }

            // 2. Tooltips for Fisherman's Pocket Guide, its upgrades, and Fishing Rods
            bool isFishingGuide = IsFishingGuideItem(item.type);
            bool isFishingRod = item.fishingPole > 0;
            if (isFishingGuide || isFishingRod)
            {
                bool hasMA = ap.HasAugment("master_angler");
                bool hasBM = ap.HasAugment("bait_master");

                if (hasMA || hasBM)
                {
                    tooltips.Add(new TooltipLine(Mod, "PlugInFishingHeader",
                        "[c/FFD700:── Plug-in Chip Fishing Bonuses ──]"));

                    if (hasMA)
                    {
                        tooltips.Add(new TooltipLine(Mod, "MasterAnglerBonus",
                            "[c/38B6FF:✦ Master Angler: +10 Fishing Power]"));
                    }

                    if (hasBM)
                    {
                        Item activeBait = BaitMasterAugment.FindActiveBait(Main.LocalPlayer);
                        if (activeBait != null && activeBait.bait > 0)
                        {
                            int baitBonus = BaitMasterAugment.GetBaitBonus(activeBait.bait);
                            tooltips.Add(new TooltipLine(Mod, "BaitMasterBonus",
                                $"[c/38B6FF:✦ Bait Master: +{baitBonus} Fishing Power ({activeBait.Name}: {activeBait.bait}% + 30%)]"));
                        }
                        else
                        {
                            tooltips.Add(new TooltipLine(Mod, "BaitMasterBonus",
                                "[c/38B6FF:✦ Bait Master: +30% Fishing Power (Requires bait in inventory)]"));
                        }
                    }
                }
            }
        }

        private static bool IsFishingGuideItem(int type)
        {
            return type == ItemID.FishermansGuide
                || type == ItemID.FishFinder
                || type == ItemID.PDA
                || type == ItemID.CellPhone
                || type == ItemID.Shellphone
                || type == ItemID.ShellphoneSpawn
                || type == ItemID.ShellphoneOcean
                || type == ItemID.ShellphoneHell;
        }
    }
}
