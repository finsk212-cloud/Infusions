using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
    public class SealedChipCacheItem : ModItem
    {
        public override string Texture => "Augments/Items/SealedChipCacheItem";

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 99;
            Item.rare = ItemRarityID.LightRed;
            Item.value = Item.buyPrice(gold: 1);
            Item.consumable = true;
        }

        // Disabled for now
        public override bool CanRightClick() => false;

        public override void RightClick(Player player)
        {
            // Disabled
        }
    }
}
