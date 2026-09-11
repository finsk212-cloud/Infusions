using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments.Items
{
    public class MediGunItem : ModItem
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.LaserRifle;

        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 18;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.noMelee = true;
            Item.channel = true;
            Item.damage = 0;
            Item.knockBack = 0f;
            Item.DamageType = DamageClass.Generic;
            Item.value = Item.buyPrice(gold: 1, silver: 50);
            Item.rare = ItemRarityID.Orange;
            Item.shoot = ModContent.ProjectileType<Projectiles.MediGunBeamProjectile>();
            Item.shootSpeed = 1f;
            Item.autoReuse = true;
        }

        public override bool CanUseItem(Player player)
        {
            // Only allow 1 channeled beam projectile at a time
            return player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.MediGunBeamProjectile>()] <= 0;
        }

        public override void AddRecipes()
        {
            // Gold Bar recipe
            Recipe recipeGold = CreateRecipe();
            recipeGold.AddIngredient(ItemID.LifeCrystal, 1);
            recipeGold.AddIngredient(ItemID.GoldBar, 10);
            recipeGold.AddIngredient(ItemID.FallenStar, 5);
            recipeGold.AddTile(TileID.Anvils);
            recipeGold.Register();

            // Platinum Bar recipe
            Recipe recipePlatinum = CreateRecipe();
            recipePlatinum.AddIngredient(ItemID.LifeCrystal, 1);
            recipePlatinum.AddIngredient(ItemID.PlatinumBar, 10);
            recipePlatinum.AddIngredient(ItemID.FallenStar, 5);
            recipePlatinum.AddTile(TileID.Anvils);
            recipePlatinum.Register();
        }
    }
}
