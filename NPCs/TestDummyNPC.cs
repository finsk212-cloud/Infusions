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
			Main.npcFrameCount[Type] = 1;
			NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers()
			{
				Hide = true
			};
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
		}

		public override void SetDefaults()
		{
			NPC.width = 32;
			NPC.height = 48;
			NPC.damage = 0;
			NPC.defense = 0;
			NPC.lifeMax = 1000000;
			NPC.life = 1000000;
			NPC.HitSound = SoundID.NPCHit15;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.knockBackResist = 0f;
			NPC.noGravity = false;
			NPC.noTileCollide = false;
			NPC.aiStyle = -1;
			NPC.immortal = false;
			NPC.dontTakeDamage = false;
			NPC.chaseable = true;
		}

		public override bool CheckActive()
		{
			return false;
		}

		public override void AI()
		{
			NPC.velocity.X = 0f;
			if (NPC.life < NPC.lifeMax)
			{
				NPC.life = NPC.lifeMax;
			}
		}

		public override bool CanChat()
		{
			return true;
		}

		public override void SetChatButtons(ref string button, ref string button2)
		{
			button = "Remove Dummy";
		}

		public override void OnChatButtonClicked(bool firstButton, ref string shopName)
		{
			if (firstButton)
			{
				NPC.active = false;
				SoundEngine.PlaySound(SoundID.NPCDeath1, NPC.Center);
				Main.NewText("Target Dummy removed.", Color.Orange);
			}
		}
	}
}