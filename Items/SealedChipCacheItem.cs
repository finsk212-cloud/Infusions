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

        public override bool CanRightClick() => true;

        public override void RightClick(Player player)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // Decryption audio chime
            SoundEngine.PlaySound(SoundID.Research with { Volume = 0.95f, Pitch = 0.1f }, player.Center);

            // Cybernetic particle burst
            for (int i = 0; i < 20; i++)
            {
                Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Electric, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 0, default, 1.2f);
                d.noGravity = true;
            }
            for (int i = 0; i < 12; i++)
            {
                Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.GoldFlame, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f), 0, default, 1.1f);
                d.noGravity = true;
            }

            // Roll and open 3-Card Decryption modal using current world progression bracket
            RarityBracket currentBracket = BossTierMap.GetCurrentWorldBracket();
            AugmentRewardLogic.GrantReward(player, currentBracket);
        }
    }
}
