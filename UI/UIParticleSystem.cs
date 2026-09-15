using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace Augments
{
	public class UIParticle
	{
		public Vector2 Position;
		public Vector2 Velocity;
		public Color BaseColor;
		public float Scale;
		public float Alpha;
		public int Life;
		public int MaxLife;
		public bool HasGravity;
		public float SinOffset;
		public float SinSpeed;
	}

	public class UIParticleSystem
	{
		private readonly List<UIParticle> particles = new List<UIParticle>();
		private readonly int maxParticles;

		public UIParticleSystem(int maxParticles = 140)
		{
			this.maxParticles = maxParticles;
		}

		public void Clear()
		{
			particles.Clear();
		}

		public void SpawnAmbient(Rectangle bounds, Color color, float scale = 2.2f)
		{
			if (particles.Count >= maxParticles)
				return;

			particles.Add(new UIParticle
			{
				Position = new Vector2(
					bounds.X + Main.rand.NextFloat(bounds.Width),
					bounds.Y + bounds.Height - Main.rand.NextFloat(16f)
				),
				Velocity = new Vector2(0f, -Main.rand.NextFloat(0.35f, 0.95f)),
				BaseColor = color,
				Scale = scale * Main.rand.NextFloat(0.8f, 1.3f),
				Alpha = 0f,
				Life = 0,
				MaxLife = Main.rand.Next(70, 150),
				HasGravity = false,
				SinOffset = Main.rand.NextFloat(MathHelper.TwoPi),
				SinSpeed = Main.rand.NextFloat(0.025f, 0.06f)
			});
		}

		public void SpawnBurst(Vector2 center, Color color, int count = 45, float maxSpeed = 4.5f)
		{
			for (int i = 0; i < count; i++)
			{
				if (particles.Count >= maxParticles)
					break;

				float angle = Main.rand.NextFloat(MathHelper.TwoPi);
				float spd = Main.rand.NextFloat(maxSpeed * 0.25f, maxSpeed);
				particles.Add(new UIParticle
				{
					Position = center,
					Velocity = new Vector2((float)Math.Cos(angle) * spd, (float)Math.Sin(angle) * spd),
					BaseColor = color,
					Scale = Main.rand.NextFloat(2.2f, 4f),
					Alpha = 1f,
					Life = 0,
					MaxLife = Main.rand.Next(40, 85),
					HasGravity = true,
					SinOffset = 0f,
					SinSpeed = 0f
				});
			}
		}

		public void SpawnLaserSpark(Vector2 pos, Color color)
		{
			if (particles.Count >= maxParticles)
				return;

			particles.Add(new UIParticle
			{
				Position = pos,
				Velocity = new Vector2(Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-2f, 2f)),
				BaseColor = color,
				Scale = Main.rand.NextFloat(1.5f, 2.8f),
				Alpha = 1f,
				Life = 0,
				MaxLife = Main.rand.Next(16, 32),
				HasGravity = false,
				SinOffset = 0f,
				SinSpeed = 0f
			});
		}

		public void SpawnVortex(Vector2 center, Color color, float radius = 120f)
		{
			if (particles.Count >= maxParticles)
				return;

			float angle = Main.rand.NextFloat(MathHelper.TwoPi);
			Vector2 pos = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
			Vector2 toCenter = Vector2.Normalize(center - pos);
			Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X);
			Vector2 vel = toCenter * Main.rand.NextFloat(1.6f, 2.8f) + tangent * Main.rand.NextFloat(0.6f, 1.6f);

			particles.Add(new UIParticle
			{
				Position = pos,
				Velocity = vel,
				BaseColor = color,
				Scale = Main.rand.NextFloat(1.8f, 3.2f),
				Alpha = 1f,
				Life = 0,
				MaxLife = Main.rand.Next(35, 55),
				HasGravity = false,
				SinOffset = 0f,
				SinSpeed = 0f
			});
		}

		public void Update()
		{
			for (int i = particles.Count - 1; i >= 0; i--)
			{
				var p = particles[i];
				p.Life++;
				if (p.Life >= p.MaxLife)
				{
					particles.RemoveAt(i);
					continue;
				}

				if (p.HasGravity)
				{
					p.Position += p.Velocity;
					p.Velocity.Y += 0.07f;
					p.Velocity.X *= 0.985f;
					p.Alpha = 1f - (p.Life / (float)p.MaxLife);
				}
				else
				{
					p.Position += p.Velocity;
					p.Position.X += (float)Math.Sin(p.Life * p.SinSpeed + p.SinOffset) * 0.35f;

					float progress = p.Life / (float)p.MaxLife;
					if (progress < 0.2f)
						p.Alpha = progress / 0.2f;
					else if (progress > 0.7f)
						p.Alpha = (1f - progress) / 0.3f;
					else
						p.Alpha = 1f;
				}
			}
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			foreach (var p in particles)
			{
				Color drawColor = p.BaseColor * (p.Alpha * 0.85f);
				int size = (int)Math.Max(1f, p.Scale);
				spriteBatch.Draw(pixel, new Rectangle((int)p.Position.X - size / 2, (int)p.Position.Y - size / 2, size, size), drawColor);
			}
		}
	}
}
