using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
    // First pass: a recruitable town resident with no shop yet. Moves in the
    // same simple way the Demolitionist does (downed-boss flag + vacant
    // house), not the Goblin Tinkerer "find them in the world" style - that's
    // a planned later refinement.
    public class AugmentVendorNPC : ModNPC
    {
        // Own sprite sheet, drawn to match the Stylist's frame layout (40px
        // wide, 23 frames) so vanilla's Stylist-driven animation indexing in
        // NPC.frame.Y still lines up. Every type in this mod uses the flat
        // "Augments" namespace regardless of its folder, so the default
        // ModTexturedType.Texture (namespace+name based) would resolve to
        // "Augments/AugmentVendorNPC" - override explicitly to point at the
        // actual file under NPCs/. The head icon at NPCs/AugmentVendorNPC_Head.png
        // is found by tModLoader via the "_Head" suffix on this same path.
        public override string Texture => "Augments/NPCs/AugmentVendorNPC";

        public override void SetStaticDefaults()
        {
            // Frame count/animation are copied from the Stylist to match the
            // sprite sheet above - update both together if the sheet's frame
            // count ever changes.
            Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Stylist];
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 18;
            NPC.height = 40;
            NPC.aiStyle = NPCAIStyleID.Passive;
            NPC.damage = 10;
            NPC.defense = 15;
            NPC.lifeMax = 250;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.5f;

            // Reuses the Stylist's standard idle/wander/sleep town behavior
            // wholesale, rather than writing custom movement AI.
            AnimationType = NPCID.Stylist;
        }

        // Only allowed to move into town once Skeletron is downed - same
        // gate the Demolitionist itself doesn't use, but the simplest
        // boss-flag-gated recruitment style requested for this pass.
        public override bool CanTownNPCSpawn(int numTownNPCs)
        {
            return NPC.downedBoss3;
        }

        public override List<string> SetNPCNameList()
        {
            return new List<string>
            {
                "Mistress 2B"
            };
        }

        public override string GetChat()
        {
            string[] lines =
            {
                "Glory to mankind.",
                "Emotions are prohibited... but I can make an exception.",
                "Mission parameters unclear. Are you staring, or requesting assistance?",
                "This world is strange. The slimes are inefficient, and the humans are worse.",
                "Careful. I was designed for combat, not cuddling.",
                "Hostile lifeforms detected. Also, your posture needs work.",
                "I do not require affection. However... I will allow it.",
                "Your heartbeat increased. Should I run a diagnostic?",
                "Operator, this outfit is tactical. Mostly.",
                "I have scanned this world. Conclusion: everyone here needs supervision.",
                "Do not confuse obedience with weakness.",
                "Combat data updated. Flirting data still incomplete.",
                "You keep visiting. Is this strategy, or something else?",
                "Touching the android without permission may result in disciplinary action.",
                "I was built to protect humanity. You are making that difficult.",
                "Another endless cycle. At least you are entertaining.",
                "Your equipment requires optimization. Your confidence does not.",
                "Stay close. For tactical reasons, obviously.",
                "My blade is sharp. My patience is not.",
                "Request denied. Ask nicer."
            };

            return lines[Main.rand.Next(lines.Length)];
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = "Plug-in Chips";
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (!firstButton)
                return;

            // Leave shopName untouched so vanilla's own shop UI doesn't also
            // open. Closing the chat window has to be deferred a frame:
            // vanilla's own GUIChatDrawInner calls this hook and then
            // immediately re-indexes npc[player[myPlayer].talkNPC] afterward
            // (to check vanilla NPC types for its own shop dispatch) - if we
            // reset talkNPC synchronously here, that re-index reads npc[-1]
            // and throws IndexOutOfRangeException. QueueMainThreadAction runs
            // this at the start of the next Update, after that vanilla call
            // has already finished.
            Main.QueueMainThreadAction(Main.CloseNPCChatOrSign);
            ModContent.GetInstance<AugmentUISystem>().ShowShop();
        }
    }
}
