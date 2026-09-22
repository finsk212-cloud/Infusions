using Terraria.ModLoader;

namespace Augments
{
	// Marks an NPC as "last hit by this player's magic damage" - same
	// shared kill-detection shape as AugmentSwarmTacticsNPC/
	// AugmentVampiricEdgeNPC, just gated to magic instead of summon/melee.
	// Tagging on every magic hit and checking the tag on kill credit
	// (OnKillNPC) covers both direct-hit kills and DoT-finished kills alike.
	public class AugmentStormcallerNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		private int taggedPlayerIndex = -1;

		public void TagHit(int playerIndex)
		{
			taggedPlayerIndex = playerIndex;
		}

		public void TagMagicHit(int playerIndex)
		{
			TagHit(playerIndex);
		}

		public bool IsTaggedBy(int playerIndex)
		{
			return taggedPlayerIndex == playerIndex;
		}

		public void ClearTag()
		{
			taggedPlayerIndex = -1;
		}
	}
}
