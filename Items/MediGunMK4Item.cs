using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments.Items
{
    public class MediGunMK4Item : ModItem
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
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ItemRarityID.Red;
            Item.shoot = ModContent.ProjectileType<Projectiles.MediGunMK4BeamProjectile>();
            Item.shootSpeed = 1f;
            Item.autoReuse = false;
        }

        public override bool CanUseItem(Player player)
        {
            // Only allow 1 channeled beam projectile at a time
            return player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.MediGunMK4BeamProjectile>()] <= 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Cleanly eliminate any active beam projectiles across all MK tiers for this player
            int mk1Type = ModContent.ProjectileType<Projectiles.MediGunBeamProjectile>();
            int mk2Type = ModContent.ProjectileType<Projectiles.MediGunMK2BeamProjectile>();
            int mk3Type = ModContent.ProjectileType<Projectiles.MediGunMK3BeamProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && (p.type == type || p.type == mk1Type || p.type == mk2Type || p.type == mk3Type))
                {
                    p.Kill();
                }
            }
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<MediGunMK3Item>(), 1);
            recipe.AddIngredient(ItemID.LunarBar, 10);
            recipe.AddIngredient(ItemID.FragmentNebula, 10);
            recipe.AddIngredient(ItemID.FragmentStardust, 10);
            recipe.AddTile(TileID.LunarCraftingStation);
            recipe.Register();
        }
    }
}
