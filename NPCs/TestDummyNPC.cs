using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
	public class TestDummyNPC : ModNPC
	{
		public override string Texture => "Terraria/Images/NPC_488";

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.TargetDummy];
			NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers()
			{
				Hide = true
			};
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
		}

		public override void SetDefaults()
		{
			NPC.width = 28;
			NPC.height = 40;
			NPC.damage = 0;
			NPC.defense = 0;
			NPC.lifeMax = 10000000;
			NPC.life = 10000000;
			NPC.HitSound = SoundID.NPCHit15;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.knockBackResist = 0f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.aiStyle = -1;
			NPC.immortal = false;
			NPC.dontTakeDamage = false;
			NPC.chaseable = true;
			NPC.friendly = false;
			NPC.townNPC = false;
		}

		public override bool CheckActive() => false;

		public override void AI()
		{
			NPC.velocity = Vector2.Zero;
			if (NPC.life < NPC.lifeMax)
			{
				NPC.life = NPC.lifeMax;
			}
			if (NPC.localAI[0] > 0f)
			{
				NPC.localAI[0]--;
			}
		}

		public override void FindFrame(int frameHeight)
		{
			if (NPC.localAI[0] > 0f)
			{
				int wobbleFrame = 1 + (int)((24f - NPC.localAI[0]) / 5f) % 5;
				NPC.frame.Y = wobbleFrame * frameHeight;
			}
			else
			{
				NPC.frame.Y = 0;
			}
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			NPC.localAI[0] = 24f;
		}

		public override bool CheckDead()
		{
			NPC.life = NPC.lifeMax;
			return false;
		}

		public override bool? CanBeHitByItem(Player player, Item item) => true;
		public override bool? CanBeHitByProjectile(Projectile projectile) => true;
		public override bool CanBeHitByNPC(NPC attacker) => true;
		public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
	}
}