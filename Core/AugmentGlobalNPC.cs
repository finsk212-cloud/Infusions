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

            // --- Pre-Hardmode Rare Encounters ---
            // Tim — rare skeleton wizard in caverns (100% drop).
            if (npc.type == NPCID.Tim)
                npcLoot.Add(ItemDropRule.Common(essenceType, 1, 1, 1));

            // Nymph — transformed cavern encounter (100% drop, belongs on Nymph, not LostGirl).
            if (npc.type == NPCID.Nymph)
                npcLoot.Add(ItemDropRule.Common(essenceType, 1, 1, 1));

            // Pinky — rare miniature slime encounter (100% drop).
            if (npc.type == NPCID.Pinky)
                npcLoot.Add(ItemDropRule.Common(essenceType, 1, 1, 1));

            // Undead Miner — cavern skeleton miner (50% drop, 1 in 2).
            if (npc.type == NPCID.UndeadMiner)
                npcLoot.Add(ItemDropRule.Common(essenceType, 2, 1, 1));

            // --- Hardmode Rare Encounters ---
            // Rune Wizard — rare cavern wizard (100% drop, 1 Essence).
            if (npc.type == NPCID.RuneWizard)
                npcLoot.Add(ItemDropRule.Common(essenceType, 1, 1, 1));

            // All mimic variants — early Hardmode risk-reward (33% drop, 1 in 3).
            // BigMimic* are the hardmode biome-key mimics (separate NPCIDs from base Mimic).
            if (npc.type == NPCID.Mimic          ||
                npc.type == NPCID.IceMimic        ||
                npc.type == NPCID.PresentMimic    ||
                npc.type == NPCID.BigMimicCorruption ||
                npc.type == NPCID.BigMimicCrimson    ||
                npc.type == NPCID.BigMimicHallow     ||
                npc.type == NPCID.BigMimicJungle)
            {
                npcLoot.Add(ItemDropRule.Common(essenceType, 3, 1, 1));
            }

            // Dungeon Spirit — late-game post-Plantera dungeon (25% drop, 1 in 4).
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
