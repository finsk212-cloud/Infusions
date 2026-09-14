using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
    public class AugmentEssenceItem : ModItem
    {
        public override string Texture => "Augments/Items/AugmentEssenceItem";

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 9999;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.buyPrice(silver: 50);
        }
    }
}
