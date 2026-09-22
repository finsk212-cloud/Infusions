using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace Augments
{
	public class AugmentUISystem : ModSystem
	{
		// --- Boss-kill choice popup ---
		private UserInterface augmentInterface;
		private AugmentChoiceUIState choiceState;

		// --- "Your Augments" browsable list ---
		private UserInterface listInterface;
		private AugmentListUIState listState;

		// --- Vendor shop panel ---
		private UserInterface shopInterface;
		private AugmentShopUIState shopState;

		// --- Vendor gacha decryption panel ---
		private UserInterface gachaInterface;
		private AugmentGachaUIState gachaState;

		private GameTime lastUpdateUiGameTime;

		public override void Load()
		{
			if (Main.dedServ)
				return; // dedicated server has no screen, skip UI setup entirely

			augmentInterface = new UserInterface();
			choiceState = new AugmentChoiceUIState();
			choiceState.Activate(); // forces OnInitialize now, so backPanel always exists

			listInterface = new UserInterface();
			listState = new AugmentListUIState();
			listState.Activate();

			shopInterface = new UserInterface();
			shopState = new AugmentShopUIState();
			shopState.Activate();

			gachaInterface = new UserInterface();
			gachaState = new AugmentGachaUIState();
			gachaState.Activate();
		}

		// Called every frame - keeps both panels' buttons/hover states responsive.
		public override void UpdateUI(GameTime gameTime)
		{
			lastUpdateUiGameTime = gameTime;

			if (augmentInterface?.CurrentState != null)
				augmentInterface.Update(gameTime);

			if (listInterface?.CurrentState != null)
				listInterface.Update(gameTime);

			if (shopInterface?.CurrentState != null)
				shopInterface.Update(gameTime);

			if (gachaInterface?.CurrentState != null)
				gachaInterface.Update(gameTime);

			AugmentFamilyHUD.Update(gameTime);
			AugmentAnalyticsHUD.Update(gameTime);
		}

		// Slots both panels into Terraria's actual draw order.
		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
		{
			int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
			if (mouseTextIndex == -1)
				return;

			// Insertion order matters here: each Insert places its layer
			// immediately before whatever currently sits at mouseTextIndex.
			// So the layer inserted FIRST in code ends up drawn LAST (on top).
			// We want: Auras (bottom) -> List UI -> Shop UI -> Choice UI -> Charges -> Cooldowns -> Family HUD -> Tooltip (top) -> Mouse Text.
			// So in code: insert Tooltip first, ..., List UI, then Auras last.
			layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
				"Augments: Sanitize Mouse Text",
				delegate
				{
					if (!string.IsNullOrEmpty(Main.hoverItemName))
					{
						if (Main.hoverItemName.Contains("[Augments]"))
							Main.hoverItemName = Main.hoverItemName.Replace("[Augments]", "").Trim();
						if (Main.hoverItemName.Contains("[augments]"))
							Main.hoverItemName = Main.hoverItemName.Replace("[augments]", "").Trim();
						if (Main.hoverItemName.EndsWith(" N P C"))
							Main.hoverItemName = Main.hoverItemName.Substring(0, Main.hoverItemName.Length - 6);
						else if (Main.hoverItemName.EndsWith(" NPC"))
							Main.hoverItemName = Main.hoverItemName.Substring(0, Main.hoverItemName.Length - 4);
					}
					return true;
				},
				InterfaceScaleType.UI)
			);

			layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
				"Augments: Tooltip",
				delegate
				{
					AugmentTooltipDrawer.DrawIfHovering(Main.spriteBatch);
					return true;
				},
				InterfaceScaleType.UI)
			);

			layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
				"Augments: Combat Analytics HUD",
				delegate
				{
					AugmentAnalyticsHUD.Draw(Main.spriteBatch);
					return true;
				},
				InterfaceScaleType.UI)
			);

			layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
				"Augments: Family HUD",
				delegate
				{
					AugmentFamilyHUD.Draw(Main.spriteBatch);
					return true;
				},
				InterfaceScaleType.UI)
			);

			layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
				"Augments: Cooldowns",
				delegate
				{
					AugmentCooldownDrawer.DrawCooldowns(Main.spriteBatch);
					return true;
				},
				InterfaceScaleType.UI)
			);

			layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
				"Augments: Charges",
				delegate
				{
					AugmentChargeDrawer.DrawCharges(Main.spriteBatch);
					return true;
				},
				InterfaceScaleType.UI)
			);

			layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
				"Augments: Medi Gun HUD",
				delegate
				{
					MediGunHUDOverlay.Draw(Main.spriteBatch);
					return true;
				},
				InterfaceScaleType.Game)
			);

			layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
				"Augments: Choice UI",
				delegate
				{
					if (lastUpdateUiGameTime != null && augmentInterface?.CurrentState != null)
						augmentInterface.Draw(Main.spriteBatch, lastUpdateUiGameTime);
					return true;
				},
				InterfaceScaleType.UI)
			);

			layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
				"Augments: Gacha UI",
				delegate
				{
					if (lastUpdateUiGameTime != null && gachaInterface?.CurrentState != null)
						gachaInterface.Draw(Main.spriteBatch, lastUpdateUiGameTime);
					return true;
				},
				InterfaceScaleType.UI)
			);

			layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
				"Augments: Shop UI",
				delegate
				{
					if (lastUpdateUiGameTime != null && shopInterface?.CurrentState != null)
						shopInterface.Draw(Main.spriteBatch, lastUpdateUiGameTime);
					return true;
				},
				InterfaceScaleType.UI)
			);

			layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
				"Augments: List UI",
				delegate
				{
					if (lastUpdateUiGameTime != null && listInterface?.CurrentState != null)
						listInterface.Draw(Main.spriteBatch, lastUpdateUiGameTime);
					return true;
				},
				InterfaceScaleType.UI)
			);

			// Inserted last = drawn first = beneath all UI panels but above the game world.
			layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
				"Augments: Auras",
				delegate
				{
					AugmentAuraDrawer.DrawAuras(Main.spriteBatch);
					return true;
				},
				InterfaceScaleType.UI)
			);
		}

		// --- Choice popup controls ---

		public void ShowChoices(List<Augment> choices, AugmentRarity rarity, RarityBracket bracket = RarityBracket.PreHardmode, bool networkReward = false, bool rerolled = false)
		{
			choiceState.SetChoices(choices, rarity, bracket, networkReward, rerolled);
			augmentInterface?.SetState(choiceState);
			if (!rerolled)
				SoundEngine.PlaySound(SoundID.Research);
		}

		public void HidePanel()
		{
			augmentInterface?.SetState(null);
		}

		public bool IsOpen => augmentInterface?.CurrentState != null;
		public bool IsChoiceOpenAndActive => augmentInterface?.CurrentState != null && choiceState != null && !choiceState.IsMinimized;

		public void RefreshOpenPlayerPanels()
		{
			if (IsListOpen)
				listState.Refresh();
			if (IsShopOpen)
				shopState.Refresh();
			if (IsGachaOpen)
				gachaState.Refresh();
		}

		// --- "Your Augments" list controls ---

		public void ShowList()
		{
			listState.Refresh();
			listInterface?.SetState(listState);
		}

		public void HideList()
		{
			listInterface?.SetState(null);
		}

		public void ToggleList()
		{
			if (IsListOpen)
				HideList();
			else
				ShowList();
		}

		public bool IsListOpen => listInterface?.CurrentState != null;

		// --- Vendor shop panel controls ---

		public void ShowShop()
		{
			if (shopState == null)
				return;

			if (IsGachaOpen)
				HideGacha();

			int essenceType = ModContent.ItemType<AugmentEssenceItem>();
			for (int i = 0; i < Main.maxItems; i++)
			{
				Item it = Main.item[i];
				if (it.active && it.type == essenceType && Vector2.Distance(it.Center, Main.LocalPlayer.Center) < 600f)
				{
					Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_FromThis(), essenceType, it.stack);
					it.active = false;
					it.type = ItemID.None;
					if (Main.netMode == NetmodeID.Server)
						NetMessage.SendData(MessageID.SyncItem, -1, -1, null, i);
				}
			}

			shopState.Refresh();
			shopInterface?.SetState(shopState);
		}

		public void HideShop()
		{
			shopInterface?.SetState(null);
		}

		public void ToggleShop()
		{
			if (IsShopOpen)
				HideShop();
			else
				ShowShop();
		}

		public bool IsShopOpen => shopInterface?.CurrentState != null;

		// --- Vendor gacha decryption panel controls ---

		public void ShowGacha()
		{
			if (gachaState == null)
				return;

			if (IsShopOpen)
				HideShop();

			int essenceType = ModContent.ItemType<AugmentEssenceItem>();
			for (int i = 0; i < Main.maxItems; i++)
			{
				Item it = Main.item[i];
				if (it.active && it.type == essenceType && Vector2.Distance(it.Center, Main.LocalPlayer.Center) < 600f)
				{
					Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_FromThis(), essenceType, it.stack);
					it.active = false;
					it.type = ItemID.None;
					if (Main.netMode == NetmodeID.Server)
						NetMessage.SendData(MessageID.SyncItem, -1, -1, null, i);
				}
			}

			gachaState.Refresh();
			gachaInterface?.SetState(gachaState);
		}

		public void HideGacha()
		{
			gachaInterface?.SetState(null);
		}

		public void ToggleGacha()
		{
			if (IsGachaOpen)
				HideGacha();
			else
				ShowGacha();
		}

		public bool IsGachaOpen => gachaInterface?.CurrentState != null;
	}
}
