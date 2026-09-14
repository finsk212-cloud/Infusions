using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments.Items
{
    public class MediGunMK2Item : ModItem
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.LaserMachinegun;

        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.noMelee = true;
            Item.channel = true;
            Item.damage = 0;
            Item.knockBack = 0f;
            Item.DamageType = DamageClass.Generic;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Pink;
            Item.shoot = ModContent.ProjectileType<Projectiles.MediGunMK2BeamProjectile>();
            Item.shootSpeed = 1f;
            Item.autoReuse = false;
        }

        public override bool CanUseItem(Player player)
        {
            // Only allow 1 channeled beam projectile at a time
            return player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.MediGunMK2BeamProjectile>()] <= 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Cleanly eliminate any existing beam projectiles for this player
            int mk1Type = ModContent.ProjectileType<Projectiles.MediGunBeamProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && (p.type == type || p.type == mk1Type))
                {
                    p.Kill();
                }
            }
            return true;
        }

        public override void AddRecipes()
        {
            // Mythril Bar recipe
            Recipe recipeMythril = CreateRecipe();
            recipeMythril.AddIngredient(ModContent.ItemType<MediGunItem>(), 1);
            recipeMythril.AddIngredient(ItemID.MythrilBar, 10);
            recipeMythril.AddIngredient(ItemID.CrystalShard, 15);
            recipeMythril.AddIngredient(ItemID.SoulofLight, 5);
            recipeMythril.AddTile(TileID.MythrilAnvil);
            recipeMythril.Register();

            // Orichalcum Bar recipe
            Recipe recipeOrichalcum = CreateRecipe();
            recipeOrichalcum.AddIngredient(ModContent.ItemType<MediGunItem>(), 1);
            recipeOrichalcum.AddIngredient(ItemID.OrichalcumBar, 10);
            recipeOrichalcum.AddIngredient(ItemID.CrystalShard, 15);
            recipeOrichalcum.AddIngredient(ItemID.SoulofLight, 5);
            recipeOrichalcum.AddTile(TileID.MythrilAnvil);
            recipeOrichalcum.Register();
        }
    }
}
