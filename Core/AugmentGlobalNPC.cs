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

            // --- Pre-Hardmode Rare Monsters (100% Drop) ---
            // Tim — rare skeleton wizard in caverns.
            if (npc.type == NPCID.Tim)
                npcLoot.Add(ItemDropRule.Common(essenceType, 1, 1, 1));

            // Nymph — transformed cavern encounter (belongs on Nymph, not LostGirl).
            if (npc.type == NPCID.Nymph)
                npcLoot.Add(ItemDropRule.Common(essenceType, 1, 1, 1));

            // Pinky — rare high-defense mini-slime.
            if (npc.type == NPCID.Pinky)
                npcLoot.Add(ItemDropRule.Common(essenceType, 1, 1, 1));

            // Doctor Bones — rare underground jungle zombie.
            if (npc.type == NPCID.DoctorBones)
                npcLoot.Add(ItemDropRule.Common(essenceType, 1, 1, 1));

            // Golden Slime — rare underground gold slime.
            if (npc.type == NPCID.GoldenSlime)
                npcLoot.Add(ItemDropRule.Common(essenceType, 1, 1, 1));

            // --- Pre-Hardmode Events & Cavern Encounters (Chance Drops) ---
            // The Groom & The Bride — rare Blood Moon zombies (50% drop).
            if (npc.type == NPCID.TheGroom || npc.type == NPCID.TheBride)
                npcLoot.Add(ItemDropRule.Common(essenceType, 2, 1, 1));

            // Undead Miner — rare cavern enemy (33% drop).
            if (npc.type == NPCID.UndeadMiner)
                npcLoot.Add(ItemDropRule.Common(essenceType, 3, 1, 1));

            // Blood Moon Fishing Enemies — Wandering Eye Fish & Zombie Merman (25% drop).
            if (npc.type == NPCID.EyeballFlyingFish || npc.type == NPCID.ZombieMerman)
                npcLoot.Add(ItemDropRule.Common(essenceType, 4, 1, 1));

            // --- Hardmode Rare Spawns & Mini-Bosses ---
            // Rune Wizard — rare cavern wizard; drops 2 Essence (100% drop).
            if (npc.type == NPCID.RuneWizard)
                npcLoot.Add(ItemDropRule.Common(essenceType, 1, 2, 2));

            // Dreadnautilus — Blood Moon fishing mini-boss; drops 2 Essence (100% drop).
            if (npc.type == NPCID.BloodNautilus)
                npcLoot.Add(ItemDropRule.Common(essenceType, 1, 2, 2));

            // Moth — rare underground jungle spawn (100% drop).
            if (npc.type == NPCID.Moth)
                npcLoot.Add(ItemDropRule.Common(essenceType, 1, 1, 1));

            // All mimic variants — 50% drop.
            // BigMimic* are the hardmode biome-key mimics (separate NPCIDs from base Mimic).
            if (npc.type == NPCID.Mimic          ||
                npc.type == NPCID.IceMimic        ||
                npc.type == NPCID.PresentMimic    ||
                npc.type == NPCID.BigMimicCorruption ||
                npc.type == NPCID.BigMimicCrimson    ||
                npc.type == NPCID.BigMimicHallow     ||
                npc.type == NPCID.BigMimicJungle)
            {
                npcLoot.Add(ItemDropRule.Common(essenceType, 2, 1, 1));
            }

            // Dungeon Spirit — hardmode post-Plantera dungeon enemy; 50% drop.
            if (npc.type == NPCID.DungeonSpirit)
                npcLoot.Add(ItemDropRule.Common(essenceType, 2, 1, 1));

            // Ice Golem (Blizzard) & Sand Elemental (Sandstorm) — mini-bosses (50% drop).
            if (npc.type == NPCID.IceGolem || npc.type == NPCID.SandElemental)
                npcLoot.Add(ItemDropRule.Common(essenceType, 2, 1, 1));

            // Goblin Summoner (Goblin Army) & Pirate Captain (Pirate Invasion) — (50% drop).
            if (npc.type == NPCID.GoblinSummoner || npc.type == NPCID.PirateCaptain || npc.type == NPCID.PirateShip)
                npcLoot.Add(ItemDropRule.Common(essenceType, 2, 1, 1));
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
