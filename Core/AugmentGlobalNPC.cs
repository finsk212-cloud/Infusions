using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace Augments
{
    public class AugmentGlobalNPC : GlobalNPC
    {
        private const float TauntRadius = 800f;

        // Redirect hostile NPCs toward any nearby Support player who owns Taunt.
        // Runs before vanilla AI so the target index is set before movement is calculated.
        // Note: boss AI often re-overrides npc.target mid-AI; Taunt is best-effort for non-bosses.
        public override bool PreAI(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return true;

            if (!npc.friendly && !npc.townNPC && !npc.dontTakeDamage)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (!player.active || player.dead) continue;

                    var ap = player.GetModPlayer<AugmentPlayer>();
                    if (!ap.HasAugment("taunt")) continue;
                    if (Vector2.Distance(npc.Center, player.Center) > TauntRadius) continue;

                    npc.target = i;
                    break;
                }
            }

            return true;
        }

        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            int essenceType = ModContent.ItemType<AugmentEssenceItem>();

            // --- Pre-Hardmode Thematic Constructs & Bosses ---
            // Meteor Head — extraterrestrial scrap (5% drop, 1 in 20).
            if (npc.type == NPCID.MeteorHead)
                npcLoot.Add(ItemDropRule.Common(essenceType, 20, 1, 1));

            // Granite Golem & Granite Flyer — energized automaton constructs (20% drop, 1 in 5).
            if (npc.type == NPCID.GraniteGolem || npc.type == NPCID.GraniteFlyer)
                npcLoot.Add(ItemDropRule.Common(essenceType, 5, 1, 1));

            // Skeletron Head — dungeon guardian (100% drop, 2-3 cores).
            if (npc.type == NPCID.SkeletronHead)
                npcLoot.Add(ItemDropRule.Common(essenceType, 1, 2, 3));

            // --- Hardmode & Invasion Mechanical Units ---
            // Martian Saucer — mothership construct (100% drop, 3-5 cores).
            if (npc.type == NPCID.MartianSaucerCore || npc.type == NPCID.MartianSaucer)
                npcLoot.Add(ItemDropRule.Common(essenceType, 1, 3, 5));

            // Scutlix Gunner — cybernetic rider (50% drop, 1-2 cores).
            if (npc.type == NPCID.ScutlixRider)
                npcLoot.Add(ItemDropRule.Common(essenceType, 2, 1, 2));

            // Martian Walker — heavy bipedal mech (50% drop, 1-2 cores).
            if (npc.type == NPCID.MartianWalker)
                npcLoot.Add(ItemDropRule.Common(essenceType, 2, 1, 2));

            // Dungeon Spirit — late-game post-Plantera dungeon (25% drop, 1 core).
            if (npc.type == NPCID.DungeonSpirit)
                npcLoot.Add(ItemDropRule.Common(essenceType, 4, 1, 1));
        }

        public override void OnKill(NPC npc)
        {
            if (npc.friendly || npc.lastInteraction < 0 || npc.lastInteraction >= Main.maxPlayers)
                return;

            Player player = Main.player[npc.lastInteraction];

            if (!player.active)
                return;

            foreach (var augment in player.GetModPlayer<AugmentPlayer>().Owned)
                augment.OnKillNPC(player, npc);
        }
    }
}
