using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
    public class CleansingStrikeAugment : Augment
    {
        public override string Id => "cleansing_strike";
        public override string DisplayName => "Cleansing Strike";
        public override string Description =>
            $"Melee {AugmentText.Crit("crits")} remove one active debuff from yourself, if you have any.";

        public override AugmentRarity Rarity => AugmentRarity.Rare;
        public override AugmentClass Class => AugmentClass.Melee;

        public override void OnHitNPCWithItem(Player player, Item item, NPC target, NPC.HitInfo hit)
        {
            if (hit.Crit && item.CountsAsClass(DamageClass.Melee))
                RemoveOneDebuff(player);
        }

        public override void OnHitNPCWithProj(Player player, Projectile proj, NPC target, NPC.HitInfo hit)
        {
            if (hit.Crit && proj.CountsAsClass(DamageClass.Melee))
                RemoveOneDebuff(player);
        }

        private static void RemoveOneDebuff(Player player)
        {
            for (int i = 0; i < player.buffType.Length; i++)
            {
                if (player.buffType[i] > 0 && Main.debuff[player.buffType[i]])
                {
                    player.DelBuff(i);
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                        NetMessage.SendData(MessageID.PlayerBuffs, number: player.whoAmI);
                    return;
                }
            }
        }
    }
}
