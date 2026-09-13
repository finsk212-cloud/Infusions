using Terraria;
using Terraria.ModLoader;

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
        }
    }
}
