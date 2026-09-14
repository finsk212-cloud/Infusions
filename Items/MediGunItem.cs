using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments.Items
{
    public class MediGunItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 56;
            Item.height = 26;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.noMelee = true;
            Item.channel = true;
            Item.damage = 0;
            Item.knockBack = 0f;
            Item.DamageType = DamageClass.Generic;
            Item.value = Item.buyPrice(gold: 1, silver: 50);
            Item.rare = ItemRarityID.Orange;
            Item.shoot = ModContent.ProjectileType<Projectiles.MediGunBeamProjectile>();
            Item.shootSpeed = 1f;
            Item.autoReuse = false;
        }

        public override bool CanUseItem(Player player)
        {
            // Only allow 1 channeled beam projectile at a time
            return player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.MediGunBeamProjectile>()] <= 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Ensure any existing beam projectiles for this player are cleanly eliminated
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == type)
                {
                    p.Kill();
                }
            }
            return true;
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
