using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
	public class RevitalizingWaveAugment : Augment
	{
		private const int PulseTicks = 1200;

		public override string Id => "revitalizing_wave";
		public override string DisplayName => "Revitalizing Wave";
		public override string Description =>
			$"Every {AugmentText.Cooldown("20 seconds")}, you start charging a magical cloud. At max 20 stacks heal all nearby teammates for {AugmentText.Healing("25 HP")}.";
		public override AugmentRarity Rarity => AugmentRarity.Epic;
		public override AugmentClass Class => AugmentClass.Support;
		public override bool HasAuraEffect => true;
		public override int CooldownRemaining => Main.LocalPlayer.GetModPlayer<AugmentPlayer>().RevitalizingWaveTimer;
		public override Texture2D Icon => TextureAssets.Item[ItemID.BandofRegeneration].Value;

		public override void OnUpdate(Player player)
		{
			if (Main.netMode == NetmodeID.Server)
				return;

			int timer = player.GetModPlayer<AugmentPlayer>().RevitalizingWaveTimer;
			float progress = 1f - timer / (float)PulseTicks;
			Vector2 orbCenter = player.Top + new Vector2(0f, -18f - progress * 14f);
			float orbRadius = 4f + progress * 16f;
			int dustCount = timer == 0 ? 2 : Main.rand.NextBool(3) ? 1 : 0;

			for (int i = 0; i < dustCount; i++)
			{
				float angle = Main.rand.NextFloat() * MathHelper.TwoPi;
				float cos = (float)Math.Cos(angle);
				float sin = (float)Math.Sin(angle);
				Dust dust = Dust.NewDustDirect(orbCenter + new Vector2(cos, sin) * orbRadius, 0, 0, DustID.GemEmerald);
				dust.noGravity = true;
				dust.velocity = new Vector2(-sin, cos) * (1.5f + progress * 2.5f);
				dust.scale = 0.7f + progress * 1.1f;
				dust.alpha = 30 + (int)(progress * 60f);
			}

			int coreCount = timer == 0 ? 4 : (int)(1f + progress * 2f);
			for (int i = 0; i < coreCount; i++)
			{
				float angle = Main.rand.NextFloat() * MathHelper.TwoPi;
				float distance = Main.rand.NextFloat() * orbRadius * 0.35f;
				Vector2 position = orbCenter + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * distance;
				Dust dust = Dust.NewDustDirect(position, 0, 0, DustID.GemEmerald);
				dust.noGravity = true;
				dust.velocity = Vector2.Zero;
				dust.scale = 0.6f + progress;
			}
		}

		internal static void SpawnBurst(Player player)
		{
			Vector2 center = player.Top + new Vector2(0f, -32f);
			for (int i = 0; i < 36; i++)
			{
				float angle = i / 36f * MathHelper.TwoPi;
				Dust dust = Dust.NewDustDirect(center, 0, 0, DustID.GemEmerald);
				dust.noGravity = true;
				dust.velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * Main.rand.NextFloat(4f, 8f);
				dust.scale = 1.5f + Main.rand.NextFloat(0.6f);
			}
		}
	}
}
