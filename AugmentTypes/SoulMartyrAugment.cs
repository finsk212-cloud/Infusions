using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace Augments
{
    public class SoulMartyrAugment : Augment
    {
        public override string Id => "soul_martyr";
        public override string DisplayName => "Soul Martyr";
        public override string Description =>
            $"Teammates inside your aura cannot die. Any lethal damage they take is absorbed and transferred to you instead (cannot drop you below {AugmentText.HP("1 HP")}).";

        public override AugmentRarity Rarity => AugmentRarity.Legendary;
        public override AugmentClass Class => AugmentClass.Support;
        public override bool HasAuraEffect => true;

        public override Texture2D Icon => TextureAssets.Item[ItemID.PaladinsShield].Value;
    }
}
