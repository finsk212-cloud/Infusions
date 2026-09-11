using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;
using ReLogic.Content;

namespace Augments
{
	public enum AugmentSortMode
	{
		NameAZ,
		NameZA,
		RarityHighLow,
		RarityLowHigh,
		Class
	}

	public enum AugmentStatusFilter
	{
		All,
		Equipped,
		Unequipped
	}

	// Clean, Terraria-style in-game Augment Codex & Inventory Slot List.
	// Displays augments in inventory-style slot boxes categorized by tier (Common, Rare, Epic, Legendary),
	// with an interactive inspector panel on the right showing full detailed info when clicked.
	public class AugmentListUIState : UIState
	{
		public static bool IsDevMode = false;
		public static Asset<Texture2D> RarityStarAsset;

		private UIPanel backPanel;
		private UIList gridList;
		private UIScrollbar gridScrollbar;
		private AugmentDetailPanel detailPanel;
		private SupportClassTagElement supportTag;

		private UIElement devBarContainer;
		private DevBadgeElement devBadge;
		private int titleClickCount = 0;
		private double lastTitleClickTime = 0;

		private string currentTab = "All";
		private Augment selectedAugment;
		private readonly List<CodexTabButton> tabButtons = new List<CodexTabButton>();

		private AugmentClass? currentClassFilter = null;
		private AugmentRarity? currentRarityFilter = null;
		private AugmentStatusFilter currentStatusFilter = AugmentStatusFilter.All;
		private AugmentSortMode currentSortMode = AugmentSortMode.NameAZ;
		private bool isFilterMenuOpen = false;

		private CodexFilterButton classBtn;
		private CodexFilterButton sortBtn;
		private CodexFilterButton filterBtn;
		private AugmentFilterPanel filterPanel;

		private const float PanelWidth = 880f;
		private const float PanelHeight = 600f;
		private const int SlotsPerRow = 6;
		private const float SlotWidth = 74f;
		private const float SlotHeight = 84f;
		private const float SlotSpacing = 10f;

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (backPanel != null && backPanel.ContainsPoint(Main.MouseScreen))
			{
				Main.LocalPlayer.mouseInterface = true;
			}
			else if (filterPanel != null && isFilterMenuOpen && filterPanel.ContainsPoint(Main.MouseScreen))
			{
				Main.LocalPlayer.mouseInterface = true;
			}
		}

		public override void OnInitialize()
		{
			// Main Background Panel (Classic Terraria slate-blue panel)
			backPanel = new UIPanel();
			backPanel.Width.Set(PanelWidth, 0f);
			backPanel.Height.Set(PanelHeight, 0f);
			backPanel.HAlign = 0.5f;
			backPanel.VAlign = 0.5f;
			backPanel.BackgroundColor = new Color(28, 38, 70) * 0.96f;
			backPanel.BorderColor = new Color(14, 20, 42);

			// Title Header & Secret Dev Badge
			UIElement titleContainer = new UIElement();
			titleContainer.Width.Set(380f, 0f);
			titleContainer.Height.Set(28f, 0f);
			titleContainer.HAlign = 0.5f;
			titleContainer.Top.Set(8f, 0f);

			UIText title = new UIText("✦  Infusion List  ✦", 1.12f)
			{
				HAlign = 0.44f,
				VAlign = 0.5f,
				TextColor = new Color(255, 235, 175)
			};
			titleContainer.Append(title);

			devBadge = new DevBadgeElement(this);
			devBadge.Left.Set(295f, 0f);
			devBadge.Top.Set(3f, 0f);
			titleContainer.Append(devBadge);

			titleContainer.OnLeftClick += (evt, elem) => OnTitleClicked();
			title.OnLeftClick += (evt, elem) => OnTitleClicked();

			backPanel.Append(titleContainer);

			// Close Button in top-right corner
			var closeButton = new CloseButton();
			closeButton.Width.Set(24f, 0f);
			closeButton.Height.Set(24f, 0f);
			closeButton.HAlign = 1f;
			closeButton.Top.Set(8f, 0f);
			closeButton.Left.Set(-8f, 0f);
			closeButton.Clicked += () => ModContent.GetInstance<AugmentUISystem>().HideList();
			backPanel.Append(closeButton);

			// Tab Buttons Bar across top
			CreateTabButtons();

			// Filter & Sort Controls above Detail Panel (Left = 568f, Width = 288f, Top = 38f)
			CreateFilterBarControls();

			// Left: Grid List Container for Inventory Slot Boxes
			gridList = new UIList();
			gridList.ManualSortMethod = _ => { };
			gridList.Top.Set(74f, 0f);
			gridList.Left.Set(12f, 0f);
			gridList.Width.Set(515f, 0f);
			gridList.Height.Set(-125f, 1f);
			gridList.ListPadding = 6f;
			backPanel.Append(gridList);

			// Scrollbar for Grid
			gridScrollbar = new UIScrollbar();
			gridScrollbar.Top.Set(74f, 0f);
			gridScrollbar.Height.Set(-125f, 1f);
			gridScrollbar.Left.Set(534f, 0f);
			gridList.SetScrollbar(gridScrollbar);
			backPanel.Append(gridScrollbar);

			// Right: Detailed Info Inspector Panel
			detailPanel = new AugmentDetailPanel(this);
			detailPanel.Top.Set(74f, 0f);
			detailPanel.Left.Set(568f, 0f);
			detailPanel.Width.Set(288f, 0f);
			detailPanel.Height.Set(-125f, 1f);
			backPanel.Append(detailPanel);

			// Bottom-Right: Support Class Tag
			supportTag = new SupportClassTagElement();
			supportTag.Left.Set(568f, 0f);
			supportTag.Width.Set(288f, 0f);
			supportTag.Height.Set(30f, 0f);
			supportTag.Top.Set(556f, 0f);
			backPanel.Append(supportTag);

			// Bottom-Left: Dev Mode Action Bar
			CreateDevBarControls();
			if (IsDevMode)
				backPanel.Append(devBarContainer);

			Append(backPanel);
		}

		private void CreateTabButtons()
		{
			string[] tabs = { "All", "Common", "Rare", "Epic", "Legendary", "Equipped" };
			float startLeft = 14f;
			float buttonWidth = 82f;
			float gap = 5f;

			for (int i = 0; i < tabs.Length; i++)
			{
				string tab = tabs[i];
				var btn = new CodexTabButton(tab);
				btn.Width.Set(buttonWidth, 0f);
				btn.Height.Set(26f, 0f);
				btn.Top.Set(38f, 0f);
				btn.Left.Set(startLeft + i * (buttonWidth + gap), 0f);
				btn.IsActive = (tab == currentTab);
				btn.Clicked += SwitchTab;

				tabButtons.Add(btn);
				backPanel.Append(btn);
			}
		}

		private static readonly AugmentClass?[] AvailableClasses = new AugmentClass?[]
		{
			null,
			AugmentClass.Universal,
			AugmentClass.Melee,
			AugmentClass.Ranged,
			AugmentClass.Magic,
			AugmentClass.Summon,
			AugmentClass.Support
		};

		private void CreateFilterBarControls()
		{
			// 1. Class Button (Left = 568f, Width = 110f)
			classBtn = new CodexFilterButton(GetClassBtnText());
			classBtn.Top.Set(38f, 0f);
			classBtn.Left.Set(568f, 0f);
			classBtn.Width.Set(110f, 0f);
			classBtn.Height.Set(26f, 0f);
			classBtn.Clicked += CycleClassForward;
			classBtn.RightClicked += CycleClassBackward;
			backPanel.Append(classBtn);

			// 2. Sort Button (Left = 682f, Width = 110f)
			sortBtn = new CodexFilterButton(GetSortBtnText());
			sortBtn.Top.Set(38f, 0f);
			sortBtn.Left.Set(682f, 0f);
			sortBtn.Width.Set(110f, 0f);
			sortBtn.Height.Set(26f, 0f);
			sortBtn.Clicked += CycleSortForward;
			sortBtn.RightClicked += CycleSortBackward;
			backPanel.Append(sortBtn);

			// 3. Filter Button (Left = 796f, Width = 60f)
			filterBtn = new CodexFilterButton("⚙ Filter");
			filterBtn.Top.Set(38f, 0f);
			filterBtn.Left.Set(796f, 0f);
			filterBtn.Width.Set(60f, 0f);
			filterBtn.Height.Set(26f, 0f);
			filterBtn.Clicked += ToggleFilterMenu;
			backPanel.Append(filterBtn);

			// Floating Filter & Sort Panel
			filterPanel = new AugmentFilterPanel(this);
			filterPanel.Left.Set(540f, 0f);
			filterPanel.Top.Set(68f, 0f);
			filterPanel.Width.Set(320f, 0f);
			filterPanel.Height.Set(330f, 0f);
		}

		private void CreateDevBarControls()
		{
			devBarContainer = new UIElement();
			devBarContainer.Left.Set(14f, 0f);
			devBarContainer.Top.Set(556f, 0f);
			devBarContainer.Width.Set(515f, 0f);
			devBarContainer.Height.Set(28f, 0f);

			var clearAllBtn = new CodexFilterButton("🗑 Clear All", 0.72f);
			clearAllBtn.Left.Set(0f, 0f);
			clearAllBtn.Top.Set(0f, 0f);
			clearAllBtn.Width.Set(100f, 0f);
			clearAllBtn.Height.Set(26f, 0f);
			clearAllBtn.CustomActiveBorder = new Color(255, 100, 100);
			clearAllBtn.Clicked += () =>
			{
				if (Main.netMode == NetmodeID.MultiplayerClient)
					AugmentNet.SendDebugCommandRequest(DebugAugmentCommandType.Clear);
				else
					AugmentNet.ApplyDebugCommand(Main.LocalPlayer, DebugAugmentCommandType.Clear);

				SoundEngine.PlaySound(SoundID.Shatter);
				Main.NewText("✦ [DEV] All equipped augments cleared! ✦", Color.Yellow);
				PopulateGrid();
				if (selectedAugment != null)
					detailPanel.SetAugment(selectedAugment, false);
			};
			devBarContainer.Append(clearAllBtn);

			var resetCdsBtn = new CodexFilterButton("⏱ Reset CDs", 0.72f);
			resetCdsBtn.Left.Set(108f, 0f);
			resetCdsBtn.Top.Set(0f, 0f);
			resetCdsBtn.Width.Set(105f, 0f);
			resetCdsBtn.Height.Set(26f, 0f);
			resetCdsBtn.CustomActiveBorder = new Color(100, 220, 255);
			resetCdsBtn.Clicked += () =>
			{
				Main.LocalPlayer.GetModPlayer<AugmentPlayer>().ResetAllCooldowns();
				SoundEngine.PlaySound(SoundID.MaxMana, Main.LocalPlayer.Center);
				Main.NewText("✦ [DEV] All augment cooldowns have been reset to 0! ✦", Color.Cyan);
			};
			devBarContainer.Append(resetCdsBtn);

			var spawnDummyBtn = new CodexFilterButton("🎯 Spawn Dummy", 0.72f);
			spawnDummyBtn.Left.Set(221f, 0f);
			spawnDummyBtn.Top.Set(0f, 0f);
			spawnDummyBtn.Width.Set(125f, 0f);
			spawnDummyBtn.Height.Set(26f, 0f);
			spawnDummyBtn.CustomActiveBorder = new Color(120, 255, 120);
			spawnDummyBtn.Clicked += () =>
			{
				var player = Main.LocalPlayer;

				// Despawn any existing dummy so there is ONLY ever ONE dummy in the world
				int dummyType = ModContent.NPCType<TestDummyNPC>();
				for (int i = 0; i < Main.maxNPCs; i++)
				{
					if (Main.npc[i].active && Main.npc[i].type == dummyType)
					{
						Main.npc[i].active = false;
					}
				}

				int spawnX = (int)(player.Center.X + player.direction * 80);
				int spawnY = (int)player.Bottom.Y - 24;

				if (Main.netMode == NetmodeID.SinglePlayer)
				{
					int npcIndex = NPC.NewNPC(player.GetSource_FromThis(), spawnX, spawnY, dummyType);
					if (npcIndex >= 0 && npcIndex < Main.maxNPCs)
						Main.npc[npcIndex].netUpdate = true;
					SoundEngine.PlaySound(SoundID.Dig, player.Center);
					Main.NewText("✦ [DEV] Target Dummy spawned! (Right-click this button to remove) ✦", Color.LimeGreen);
				}
				else
				{
					Main.NewText("✦ [DEV] Dummy spawn is available in SinglePlayer! ✦", Color.Orange);
				}
			};
			spawnDummyBtn.RightClicked += () =>
			{
				int dummyType = ModContent.NPCType<TestDummyNPC>();
				bool removed = false;
				for (int i = 0; i < Main.maxNPCs; i++)
				{
					if (Main.npc[i].active && Main.npc[i].type == dummyType)
					{
						Main.npc[i].active = false;
						removed = true;
					}
				}
				if (removed)
				{
					SoundEngine.PlaySound(SoundID.NPCDeath1, Main.LocalPlayer.Center);
					Main.NewText("✦ [DEV] Target Dummy removed. ✦", Color.Orange);
				}
			};
			devBarContainer.Append(spawnDummyBtn);

			var testRollBtn = new CodexFilterButton("🎲 Test 3-Card Roll", 0.72f);
			testRollBtn.Left.Set(354f, 0f);
			testRollBtn.Top.Set(0f, 0f);
			testRollBtn.Width.Set(145f, 0f);
			testRollBtn.Height.Set(26f, 0f);
			testRollBtn.CustomActiveBorder = new Color(255, 215, 80);
			testRollBtn.Clicked += () =>
			{
				ModContent.GetInstance<AugmentUISystem>().HideList();
				if (Main.netMode == NetmodeID.SinglePlayer)
					AugmentRewardLogic.GrantReward(Main.LocalPlayer, RarityBracket.FinalCalamity);
				else if (Main.netMode == NetmodeID.MultiplayerClient)
					AugmentNet.SendDebugRewardRequest();

				SoundEngine.PlaySound(SoundID.MenuOpen);
			};
			devBarContainer.Append(testRollBtn);
		}

		private void OnTitleClicked()
		{
			double now = Main.gameTimeCache?.TotalGameTime.TotalSeconds ?? 0;
			if (now - lastTitleClickTime > 2.5)
			{
				titleClickCount = 0;
			}
			lastTitleClickTime = now;
			titleClickCount++;

			if (titleClickCount >= 5)
			{
				titleClickCount = 0;
				ToggleDevMode();
			}
			else
			{
				SoundEngine.PlaySound(SoundID.MenuTick);
			}
		}

		public void ToggleDevMode()
		{
			IsDevMode = !IsDevMode;
			if (IsDevMode)
			{
				SoundEngine.PlaySound(SoundID.Item4);
				Main.NewText("✦ [DEV MODE ACTIVATED] ✦ Right-click any augment to toggle equip!", Color.Gold);
			}
			else
			{
				SoundEngine.PlaySound(SoundID.MenuClose);
				Main.NewText("✦ [DEV MODE DEACTIVATED] ✦", Color.Orange);
			}

			UpdateDevModeVisuals();
			PopulateGrid();
		}

		public void UpdateDevModeVisuals()
		{
			if (backPanel == null)
				return;

			if (IsDevMode)
			{
				if (devBarContainer != null && !backPanel.HasChild(devBarContainer))
					backPanel.Append(devBarContainer);
			}
			else
			{
				if (devBarContainer != null && backPanel.HasChild(devBarContainer))
					backPanel.RemoveChild(devBarContainer);
			}

			if (selectedAugment != null && detailPanel != null)
			{
				bool isOwned = Main.LocalPlayer.GetModPlayer<AugmentPlayer>().HasAugment(selectedAugment.Id);
				detailPanel.SetAugment(selectedAugment, isOwned);
			}
		}

		private void CycleClassForward()
		{
			int idx = Array.IndexOf(AvailableClasses, currentClassFilter);
			int next = (idx + 1) % AvailableClasses.Length;
			SetClassFilter(AvailableClasses[next]);
		}

		private void CycleClassBackward()
		{
			int idx = Array.IndexOf(AvailableClasses, currentClassFilter);
			int prev = (idx - 1 + AvailableClasses.Length) % AvailableClasses.Length;
			SetClassFilter(AvailableClasses[prev]);
		}

		public void SetClassFilter(AugmentClass? newClass)
		{
			currentClassFilter = newClass;
			UpdateFilterControlStates();
			PopulateGrid();
		}

		private void CycleSortForward()
		{
			currentSortMode = (AugmentSortMode)(((int)currentSortMode + 1) % 5);
			UpdateFilterControlStates();
			PopulateGrid();
		}

		private void CycleSortBackward()
		{
			currentSortMode = (AugmentSortMode)(((int)currentSortMode - 1 + 5) % 5);
			UpdateFilterControlStates();
			PopulateGrid();
		}

		public void SetSortMode(AugmentSortMode mode)
		{
			currentSortMode = mode;
			UpdateFilterControlStates();
			PopulateGrid();
		}

		public void SetRarityFilter(AugmentRarity? rarity)
		{
			currentRarityFilter = rarity;
			currentStatusFilter = AugmentStatusFilter.All;
			currentTab = rarity.HasValue ? rarity.Value.ToString() : "All";
			UpdateTabButtons();
			UpdateFilterControlStates();
			PopulateGrid();
		}

		public void SetStatusFilter(AugmentStatusFilter status)
		{
			currentStatusFilter = status;
			if (status == AugmentStatusFilter.Equipped)
			{
				currentRarityFilter = null;
				currentTab = "Equipped";
			}
			else if (status == AugmentStatusFilter.All)
			{
				currentTab = currentRarityFilter.HasValue ? currentRarityFilter.Value.ToString() : "All";
			}
			else
			{
				currentTab = "";
			}
			UpdateTabButtons();
			UpdateFilterControlStates();
			PopulateGrid();
		}

		public void ResetAllFilters()
		{
			currentClassFilter = null;
			currentRarityFilter = null;
			currentStatusFilter = AugmentStatusFilter.All;
			currentSortMode = AugmentSortMode.NameAZ;
			currentTab = "All";
			UpdateTabButtons();
			UpdateFilterControlStates();
			PopulateGrid();
		}

		public void ToggleFilterMenu()
		{
			isFilterMenuOpen = !isFilterMenuOpen;
			if (isFilterMenuOpen)
			{
				if (!backPanel.HasChild(filterPanel))
					backPanel.Append(filterPanel);
				filterPanel.SyncUI();
			}
			else
			{
				if (backPanel.HasChild(filterPanel))
					backPanel.RemoveChild(filterPanel);
			}
			UpdateFilterControlStates();
		}

		public void UpdateFilterControlStates()
		{
			classBtn?.SetText(GetClassBtnText());
			sortBtn?.SetText(GetSortBtnText());

			bool isFiltered = currentClassFilter != null || currentRarityFilter != null || currentStatusFilter != AugmentStatusFilter.All;
			if (filterBtn != null)
			{
				filterBtn.IsActiveHighlight = isFiltered || isFilterMenuOpen;
			}

			if (classBtn != null)
			{
				classBtn.IsActiveHighlight = currentClassFilter != null;
			}

			if (filterPanel != null && isFilterMenuOpen)
			{
				filterPanel.SyncUI();
			}
		}

		private void UpdateTabButtons()
		{
			foreach (var btn in tabButtons)
			{
				if (currentStatusFilter == AugmentStatusFilter.Equipped)
				{
					btn.IsActive = (btn.TabName == "Equipped");
				}
				else if (currentRarityFilter.HasValue)
				{
					btn.IsActive = btn.TabName.Equals(currentRarityFilter.Value.ToString(), StringComparison.OrdinalIgnoreCase);
				}
				else if (currentStatusFilter == AugmentStatusFilter.All && !currentRarityFilter.HasValue)
				{
					btn.IsActive = (btn.TabName == "All");
				}
				else
				{
					btn.IsActive = false;
				}
			}
		}

		private string GetClassBtnText()
		{
			if (currentClassFilter == null)
				return "Class: All ▾";
			return $"Class: {currentClassFilter.Value} ▾";
		}

		private string GetSortBtnText()
		{
			return currentSortMode switch
			{
				AugmentSortMode.NameAZ => "Sort: A → Z ▾",
				AugmentSortMode.NameZA => "Sort: Z → A ▾",
				AugmentSortMode.RarityHighLow => "Sort: Rarity ▼",
				AugmentSortMode.RarityLowHigh => "Sort: Rarity ▲",
				AugmentSortMode.Class => "Sort: Class ▾",
				_ => "Sort: A → Z ▾"
			};
		}

		private void SwitchTab(string newTab)
		{
			currentTab = newTab;
			if (newTab == "All")
			{
				currentRarityFilter = null;
				currentStatusFilter = AugmentStatusFilter.All;
			}
			else if (newTab == "Equipped")
			{
				currentRarityFilter = null;
				currentStatusFilter = AugmentStatusFilter.Equipped;
			}
			else if (Enum.TryParse<AugmentRarity>(newTab, true, out var parsedRarity))
			{
				currentRarityFilter = parsedRarity;
				currentStatusFilter = AugmentStatusFilter.All;
			}

			UpdateTabButtons();
			UpdateFilterControlStates();
			PopulateGrid();
		}

		public void Refresh()
		{
			if (gridList == null)
				return;

			UpdateDevModeVisuals();
			UpdateFilterControlStates();
			PopulateGrid();

			if (selectedAugment == null)
			{
				var owned = Main.LocalPlayer.GetModPlayer<AugmentPlayer>().Owned;
				if (owned.Count > 0)
					SelectAugment(owned[0]);
				else if (AugmentDatabase.All.Count > 0)
					SelectAugment(AugmentDatabase.All[0]);
			}
			else
			{
				bool isOwned = Main.LocalPlayer.GetModPlayer<AugmentPlayer>().HasAugment(selectedAugment.Id);
				detailPanel.SetAugment(selectedAugment, isOwned);
			}
		}

		private void PopulateGrid()
		{
			float prevScroll = gridScrollbar?.ViewPosition ?? 0f;
			gridList.Clear();
			AugmentListEntry.HoveredAugment = null;

			var ap = Main.LocalPlayer.GetModPlayer<AugmentPlayer>();
			var all = AugmentDatabase.All;

			var filtered = new List<Augment>();
			foreach (var aug in all)
			{
				if (aug.Id == "debug_full_crit")
					continue;

				// Status filter (Equipped / Unequipped)
				if (currentStatusFilter == AugmentStatusFilter.Equipped && !ap.HasAugment(aug.Id))
					continue;
				if (currentStatusFilter == AugmentStatusFilter.Unequipped && ap.HasAugment(aug.Id))
					continue;

				// Rarity filter
				if (currentRarityFilter.HasValue && aug.Rarity != currentRarityFilter.Value)
					continue;

				// Class filter
				if (currentClassFilter.HasValue && aug.Class != currentClassFilter.Value)
					continue;

				filtered.Add(aug);
			}

			// Sort
			filtered.Sort((a, b) =>
			{
				return currentSortMode switch
				{
					AugmentSortMode.NameAZ => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase),
					AugmentSortMode.NameZA => string.Compare(b.DisplayName, a.DisplayName, StringComparison.OrdinalIgnoreCase),
					AugmentSortMode.RarityHighLow => ((int)b.Rarity).CompareTo((int)a.Rarity) != 0
						? ((int)b.Rarity).CompareTo((int)a.Rarity)
						: string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase),
					AugmentSortMode.RarityLowHigh => ((int)a.Rarity).CompareTo((int)b.Rarity) != 0
						? ((int)a.Rarity).CompareTo((int)b.Rarity)
						: string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase),
					AugmentSortMode.Class => a.Class.CompareTo(b.Class) != 0
						? a.Class.CompareTo(b.Class)
						: string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase),
					_ => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase)
				};
			});

			if (filtered.Count == 0)
			{
				var emptyText = new UIText("No augments match the current filters.", 0.9f)
				{
					HAlign = 0.5f
				};
				emptyText.Top.Set(40f, 0f);
				gridList.Add(emptyText);
				return;
			}

			// Group into rows of SlotsPerRow
			var rows = new List<UIElement>();
			UIElement currentRow = null;
			int slotIndexInRow = 0;

			for (int i = 0; i < filtered.Count; i++)
			{
				if (slotIndexInRow == 0)
				{
					currentRow = new UIElement();
					currentRow.Width.Set(0f, 1f);
					currentRow.Height.Set(SlotHeight + 5f, 0f);
					rows.Add(currentRow);
				}

				Augment aug = filtered[i];
				var slot = new AugmentSlotElement(aug)
				{
					IsSelected = (selectedAugment != null && selectedAugment.Id == aug.Id),
					IsOwned = ap.HasAugment(aug.Id)
				};
				slot.Left.Set(slotIndexInRow * (SlotWidth + SlotSpacing), 0f);
				slot.Top.Set(0f, 0f);
				slot.Clicked += SelectAugment;
				slot.RightClicked += OnSlotRightClicked;

				currentRow.Append(slot);

				slotIndexInRow++;
				if (slotIndexInRow >= SlotsPerRow)
					slotIndexInRow = 0;
			}

			gridList.AddRange(rows);

			if (gridScrollbar != null)
				gridScrollbar.ViewPosition = prevScroll;
		}

		private void SelectAugment(Augment augment)
		{
			selectedAugment = augment;
			bool isOwned = Main.LocalPlayer.GetModPlayer<AugmentPlayer>().HasAugment(augment.Id);
			detailPanel.SetAugment(augment, isOwned);

			// Refresh grid selection state
			PopulateGrid();
		}

		private void OnSlotRightClicked(Augment aug)
		{
			if (!IsDevMode)
				return;

			var ap = Main.LocalPlayer.GetModPlayer<AugmentPlayer>();
			bool isOwned = ap.HasAugment(aug.Id);
			var cmd = isOwned ? DebugAugmentCommandType.Remove : DebugAugmentCommandType.Add;

			if (Main.netMode == NetmodeID.MultiplayerClient)
				AugmentNet.SendDebugCommandRequest(cmd, aug.Id);
			else
				AugmentNet.ApplyDebugCommand(Main.LocalPlayer, cmd, aug.Id);

			SoundEngine.PlaySound(isOwned ? SoundID.MenuClose : SoundID.Item4);
			SelectAugment(aug);
		}

		// Tab button for tier category filters
		private class CodexTabButton : UIPanel
		{
			public readonly string TabName;
			public bool IsActive { get; set; }
			public event Action<string> Clicked;

			private bool isHovered;

			public CodexTabButton(string tabName)
			{
				TabName = tabName;
				SetPadding(0f);

				UIText label = new UIText(tabName, 0.75f)
				{
					HAlign = 0.5f,
					VAlign = 0.5f
				};
				Append(label);
			}

			public override void MouseOver(UIMouseEvent evt)
			{
				base.MouseOver(evt);
				isHovered = true;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				isHovered = false;
			}

			public override void LeftClick(UIMouseEvent evt)
			{
				base.LeftClick(evt);
				SoundEngine.PlaySound(SoundID.MenuTick);
				Clicked?.Invoke(TabName);
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				BackgroundColor = IsActive ? new Color(42, 58, 110) : (isHovered ? new Color(34, 46, 88) : new Color(20, 28, 54));
				BorderColor = IsActive ? new Color(255, 220, 80) : (isHovered ? Color.White * 0.7f : new Color(45, 60, 105));

				base.DrawSelf(spriteBatch);
			}
		}

		// Right-hand Detail Inspector Panel
		private class AugmentDetailPanel : UIPanel
		{
			private readonly AugmentListUIState parentState;
			private Augment currentAugment;
			private bool currentIsOwned;
			private CodexFilterButton devEquipBtn;

			public AugmentDetailPanel(AugmentListUIState parentState)
			{
				this.parentState = parentState;
				SetPadding(10f);
				BackgroundColor = new Color(18, 24, 46) * 0.95f;
				BorderColor = new Color(38, 50, 92);

				devEquipBtn = new CodexFilterButton("[ + Equip ]", 0.72f);
				devEquipBtn.Width.Set(95f, 0f);
				devEquipBtn.Height.Set(20f, 0f);
				devEquipBtn.Left.Set(-9999f, 0f);
				devEquipBtn.Top.Set(48f, 0f);
				devEquipBtn.Clicked += OnDevEquipClicked;
				Append(devEquipBtn);
			}

			private void OnDevEquipClicked()
			{
				if (currentAugment == null || !IsDevMode)
					return;

				var ap = Main.LocalPlayer.GetModPlayer<AugmentPlayer>();
				bool isOwned = ap.HasAugment(currentAugment.Id);
				var cmd = isOwned ? DebugAugmentCommandType.Remove : DebugAugmentCommandType.Add;

				if (Main.netMode == NetmodeID.MultiplayerClient)
					AugmentNet.SendDebugCommandRequest(cmd, currentAugment.Id);
				else
					AugmentNet.ApplyDebugCommand(Main.LocalPlayer, cmd, currentAugment.Id);

				SoundEngine.PlaySound(isOwned ? SoundID.MenuClose : SoundID.Item4);
				currentIsOwned = !isOwned;
				UpdateDevEquipBtnText();
				parentState?.PopulateGrid();
			}

			public void SetAugment(Augment augment, bool isOwned)
			{
				currentAugment = augment;
				currentIsOwned = isOwned;
				UpdateDevEquipBtnText();
			}

			public void UpdateDevEquipBtnText()
			{
				if (!IsDevMode || currentAugment == null)
				{
					devEquipBtn.Left.Set(-9999f, 0f);
				}
				else
				{
					devEquipBtn.Left.Set(176f, 0f);
					devEquipBtn.Top.Set(48f, 0f);
					devEquipBtn.SetText(currentIsOwned ? "[ - Unequip ]" : "[ + Equip ]");
					devEquipBtn.BorderColor = currentIsOwned ? new Color(255, 100, 100) : new Color(100, 255, 140);
					devEquipBtn.SetTextColor(currentIsOwned ? new Color(255, 130, 130) : new Color(130, 255, 160));
				}
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				CalculatedStyle dims = GetDimensions();
				var rect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);
				float time = (float)Main.GlobalTimeWrappedHourly;
				float epicPulse = (float)Math.Sin(time * 3f + (rect.X + rect.Y) * 0.02f) * 0.5f + 0.5f;
				float legPulse = (float)Math.Sin(time * 4f + (rect.X + rect.Y) * 0.02f) * 0.5f + 0.5f;

				if (currentAugment != null)
				{
					// Outer Aura Glow behind description card for Epic & Legendary
					if (currentAugment.Rarity == AugmentRarity.Epic)
					{
						int glowDist = 2 + (int)(epicPulse * 4f);
						Color epicGlow = new Color(170, 90, 255) * (0.10f + epicPulse * 0.18f);
						Rectangle auraRect = new Rectangle(rect.X - glowDist, rect.Y - glowDist, rect.Width + glowDist * 2, rect.Height + glowDist * 2);
						spriteBatch.Draw(TextureAssets.MagicPixel.Value, auraRect, epicGlow);

						BorderColor = Color.Lerp(new Color(155, 115, 225), new Color(215, 180, 255), epicPulse * 0.45f);
					}
					else if (currentAugment.Rarity == AugmentRarity.Legendary)
					{
						int glowDist = 3 + (int)(legPulse * 5f);
						Color legGlow = new Color(255, 170, 30) * (0.15f + legPulse * 0.25f);
						Rectangle auraRect = new Rectangle(rect.X - glowDist, rect.Y - glowDist, rect.Width + glowDist * 2, rect.Height + glowDist * 2);
						spriteBatch.Draw(TextureAssets.MagicPixel.Value, auraRect, legGlow);

						BorderColor = Color.Lerp(new Color(255, 160, 20), new Color(255, 210, 60), legPulse * 0.45f);
					}
					else if (currentAugment.Rarity == AugmentRarity.Common)
					{
						BorderColor = new Color(225, 230, 240);
					}
					else
					{
						BorderColor = Color.SkyBlue;
					}
				}
				else
				{
					BorderColor = new Color(38, 50, 92);
				}

				base.DrawSelf(spriteBatch);

				if (currentAugment == null)
				{
					var emptyFont = FontAssets.MouseText.Value;
					string prompt = "Click any augment slot\nto inspect details.";
					Vector2 pSize = ChatManager.GetStringSize(emptyFont, prompt, new Vector2(0.85f));
					Vector2 pPos = new Vector2(dims.X + (dims.Width - pSize.X) * 0.5f, dims.Y + (dims.Height - pSize.Y) * 0.4f);
					ChatManager.DrawColorCodedStringWithShadow(spriteBatch, emptyFont, prompt, pPos, Color.Gray, 0f, Vector2.Zero, new Vector2(0.85f));
					return;
				}

				// Diagonal Light Gleam (Shine Sweep) across card for Legendary tier
				if (currentAugment.Rarity == AugmentRarity.Legendary)
				{
					float sweepPeriod = 4.0f;
					float sweepProgress = (time * 0.6f + (rect.X + rect.Y) * 0.003f) % sweepPeriod;
					if (sweepProgress < 1.8f)
					{
						float t = sweepProgress / 1.8f;
						float sweepCenter = (rect.Width + rect.Height) * t;
						int beamWidth = 22;
						Color beamColor = new Color(255, 245, 215);

						for (int py = 4; py < rect.Height - 4; py += 3)
						{
							int centerPx = (int)(sweepCenter - py);
							int startPx = Math.Max(4, centerPx - beamWidth / 2);
							int endPx = Math.Min(rect.Width - 4, centerPx + beamWidth / 2);
							if (endPx > startPx)
							{
								float dist = Math.Abs((startPx + endPx) * 0.5f - centerPx);
								float beamA = (1f - dist / (beamWidth * 0.6f)) * 0.26f;
								if (beamA > 0.03f)
								{
									spriteBatch.Draw(TextureAssets.MagicPixel.Value,
										new Rectangle(rect.X + startPx, rect.Y + py, endPx - startPx, 3),
										beamColor * beamA);
								}
							}
						}
					}
				}

				// Corner Ornaments & Trims for description card
				if (currentAugment.Rarity == AugmentRarity.Epic)
				{
					// 4 Luminous Amethyst Corner Studs (4x4 pixels)
					Color gemColor = new Color(225, 185, 255) * (0.8f + epicPulse * 0.2f);
					spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X - 1, rect.Y - 1, 4, 4), gemColor);
					spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 3, rect.Y - 1, 4, 4), gemColor);
					spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X - 1, rect.Bottom - 3, 4, 4), gemColor);
					spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 3, rect.Bottom - 3, 4, 4), gemColor);
				}
				else if (currentAugment.Rarity == AugmentRarity.Legendary)
				{
					// Inner Gold Hairline
					Color innerGold = new Color(255, 225, 90) * (0.75f + legPulse * 0.25f);
					spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, 1), innerGold);
					spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X + 2, rect.Y + 2, 1, rect.Height - 4), innerGold);

					// 4 Ornate Royal Gold Corner Brackets (8x2 and 2x8 L-shapes)
					Color cornerGold = new Color(255, 220, 80);
					// Top-Left
					spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X - 2, rect.Y - 2, 8, 2), cornerGold);
					spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X - 2, rect.Y - 2, 2, 8), cornerGold);
					// Top-Right
					spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 6, rect.Y - 2, 8, 2), cornerGold);
					spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right, rect.Y - 2, 2, 8), cornerGold);
					// Bottom-Left
					spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X - 2, rect.Bottom, 8, 2), cornerGold);
					spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X - 2, rect.Bottom - 6, 2, 8), cornerGold);
					// Bottom-Right
					spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 6, rect.Bottom, 8, 2), cornerGold);
					spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right, rect.Bottom - 6, 2, 8), cornerGold);

					// Multi-Point Twinkling Star Sparkles along the card borders
					(int x, int y, float offset)[] spPoints = new (int, int, float)[]
					{
						(rect.Right - 2, rect.Y, 0.0f),
						(rect.X + 2, rect.Bottom - 2, 1.0f),
						(rect.X, rect.Y + (int)(rect.Height * 0.35f), 2.0f),
						(rect.Right - 1, rect.Y + (int)(rect.Height * 0.65f), 3.0f),
						(rect.X + (int)(rect.Width * 0.7f), rect.Y + 1, 1.5f),
						(rect.X + (int)(rect.Width * 0.3f), rect.Bottom - 1, 2.5f)
					};

					foreach (var sp in spPoints)
					{
						float spPhase = (time * 1.0f + sp.offset + (rect.X * 0.02f)) % 4.0f;
						if (spPhase < 1.4f)
						{
							float prog = spPhase / 1.4f;
							float intensity = (float)Math.Sin(prog * MathHelper.Pi);
							AugmentSlotElement.DrawStarSparkle(spriteBatch, sp.x, sp.y, intensity);
						}
					}
				}

				var font = FontAssets.MouseText.Value;
				float x = dims.X + 10f;
				float y = dims.Y + 10f;
				float maxTextWidth = dims.Width - 20f;

				// 1. Augment Display Name & Icon
				Color nameColor = AugmentListEntry.RarityColor(currentAugment.Rarity);
				Texture2D classIcon = AugmentSlotElement.GetClassIcon(currentAugment.Class);
				if (classIcon != null)
				{
					Color iconColor = AugmentListEntry.RarityColor(currentAugment.Rarity);
					if (currentAugment.Rarity == AugmentRarity.Common)
						iconColor = new Color(225, 230, 240);

					spriteBatch.Draw(classIcon, new Vector2(x, y - 2f), iconColor);
					ChatManager.DrawColorCodedStringWithShadow(
						spriteBatch, font, currentAugment.DisplayName, new Vector2(x + classIcon.Width + 8f, y + 2f), nameColor, 0f, Vector2.Zero, new Vector2(0.95f)
					);
					y += Math.Max(classIcon.Height, ChatManager.GetStringSize(font, currentAugment.DisplayName, new Vector2(0.95f)).Y) + 4f;
				}
				else
				{
					ChatManager.DrawColorCodedStringWithShadow(
						spriteBatch, font, currentAugment.DisplayName, new Vector2(x, y), nameColor, 0f, Vector2.Zero, new Vector2(0.95f)
					);
					y += ChatManager.GetStringSize(font, currentAugment.DisplayName, new Vector2(0.95f)).Y + 3f;
				}

				// 2. Rarity Tier Stars & Class Name
				int starCount = currentAugment.Rarity switch
				{
					AugmentRarity.Common => 1,
					AugmentRarity.Rare => 2,
					AugmentRarity.Epic => 3,
					AugmentRarity.Legendary => 4,
					_ => 1
				};

				Color starColor = AugmentListEntry.RarityColor(currentAugment.Rarity);
				if (currentAugment.Rarity == AugmentRarity.Common)
					starColor = new Color(225, 230, 240);

				if (RarityStarAsset == null)
					RarityStarAsset = ModContent.Request<Texture2D>("Augments/UI/RarityStar", ReLogic.Content.AssetRequestMode.ImmediateLoad);

				if (RarityStarAsset?.IsLoaded == true)
				{
					Texture2D starTex = RarityStarAsset.Value;
					float starSpacing = 17f;

					for (int s = 0; s < starCount; s++)
					{
						float sx = x + s * starSpacing;
						Vector2 starPos = new Vector2(sx, y);

						Color drawColor = starColor;

						if (currentAugment.Rarity == AugmentRarity.Epic)
						{
							float sPulse = (float)Math.Sin(time * 3f + s * 0.4f) * 0.5f + 0.5f;
							drawColor = Color.Lerp(starColor, new Color(255, 215, 255), sPulse * 0.4f);

							// Subtle star-shaped halo using the star texture itself (no square boxes)
							Color halo = new Color(190, 130, 255) * (0.20f + sPulse * 0.25f);
							spriteBatch.Draw(starTex, starPos + new Vector2(-1f, 0f), halo);
							spriteBatch.Draw(starTex, starPos + new Vector2(1f, 0f), halo);
							spriteBatch.Draw(starTex, starPos + new Vector2(0f, -1f), halo);
							spriteBatch.Draw(starTex, starPos + new Vector2(0f, 1f), halo);
						}
						else if (currentAugment.Rarity == AugmentRarity.Legendary)
						{
							float sPulse = (float)Math.Sin(time * 4f + s * 0.5f) * 0.5f + 0.5f;
							drawColor = Color.Lerp(starColor, new Color(255, 255, 220), sPulse * 0.45f);

							// Subtle star-shaped halo using the star texture itself (no square boxes)
							Color halo = new Color(255, 180, 40) * (0.25f + sPulse * 0.30f);
							spriteBatch.Draw(starTex, starPos + new Vector2(-1f, 0f), halo);
							spriteBatch.Draw(starTex, starPos + new Vector2(1f, 0f), halo);
							spriteBatch.Draw(starTex, starPos + new Vector2(0f, -1f), halo);
							spriteBatch.Draw(starTex, starPos + new Vector2(0f, 1f), halo);

							if (sPulse > 0.88f)
							{
								AugmentSlotElement.DrawStarSparkle(spriteBatch, (int)(sx + starTex.Width * 0.5f), (int)(y + starTex.Height * 0.5f), (sPulse - 0.88f) / 0.12f);
							}
						}

						spriteBatch.Draw(starTex, starPos, drawColor);
					}

					float classX = x + starCount * starSpacing + 6f;
					Color classColor = new Color(200, 210, 230);
					ChatManager.DrawColorCodedStringWithShadow(
						spriteBatch, font, currentAugment.Class.ToString(), new Vector2(classX, y - 1f), classColor, 0f, Vector2.Zero, new Vector2(0.82f)
					);
					y += Math.Max(starTex.Height, ChatManager.GetStringSize(font, currentAugment.Class.ToString(), new Vector2(0.82f)).Y) + 6f;
				}
				else
				{
					string tierClassText = $"[{currentAugment.Rarity} Tier]  {currentAugment.Class}";
					ChatManager.DrawColorCodedStringWithShadow(
						spriteBatch, font, tierClassText, new Vector2(x, y), Color.LightGray, 0f, Vector2.Zero, new Vector2(0.78f)
					);
					y += ChatManager.GetStringSize(font, tierClassText, new Vector2(0.78f)).Y + 4f;
				}

				// 3. Ownership Status
				string statusText = currentIsOwned ? "[ Equipped ]" : "[ Not Equipped ]";
				Color statusColor = currentIsOwned ? new Color(100, 255, 120) : new Color(140, 140, 150);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, font, statusText, new Vector2(x, y), statusColor, 0f, Vector2.Zero, new Vector2(0.78f)
				);
				y += ChatManager.GetStringSize(font, statusText, new Vector2(0.78f)).Y + 8f;

				// 4. Subtle Divider Line (matches rarity tier)
				Color divColor = AugmentListEntry.RarityColor(currentAugment.Rarity) * 0.45f;
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)x, (int)y, (int)maxTextWidth, 1), divColor);
				y += 8f;

				// 5. Description Header
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, font, "Effect:", new Vector2(x, y), Color.Gold, 0f, Vector2.Zero, new Vector2(0.78f)
				);
				y += ChatManager.GetStringSize(font, "Effect:", new Vector2(0.78f)).Y + 4f;

				// 6. Wrapped Description with chat colors
				var lines = AugmentColorText.Wrap(font, currentAugment.Description, maxTextWidth, new Vector2(0.82f));
				foreach (var line in lines)
				{
					ChatManager.DrawColorCodedStringWithShadow(
						spriteBatch, font, line, new Vector2(x, y), Color.White * 0.95f, 0f, Vector2.Zero, new Vector2(0.82f)
					);
					y += ChatManager.GetStringSize(font, line, new Vector2(0.82f)).Y + 2f;
				}

				// 7. Active Keybind note if any
				var kb = currentAugment.ActiveModKeybind;
				if (kb != null)
				{
					y += 8f;
					string kbText = kb.GetAssignedKeys().Count > 0 ? $"Keybind: {string.Join(", ", kb.GetAssignedKeys())}" : "No key bound to this augment";
					Color kbColor = kb.GetAssignedKeys().Count > 0 ? Color.SkyBlue : new Color(255, 120, 100);
					ChatManager.DrawColorCodedStringWithShadow(
						spriteBatch, font, kbText, new Vector2(x, y), kbColor, 0f, Vector2.Zero, new Vector2(0.75f)
					);
				}
			}
		}

		// Secret Dev Badge displayed next to Title when Dev Mode is active
		private class DevBadgeElement : UIPanel
		{
			private readonly AugmentListUIState parent;
			private readonly UIText label;
			private bool isHovered;

			public DevBadgeElement(AugmentListUIState parent)
			{
				this.parent = parent;
				SetPadding(0f);
				Width.Set(52f, 0f);
				Height.Set(22f, 0f);

				label = new UIText("[DEV]", 0.72f)
				{
					HAlign = 0.5f,
					VAlign = 0.5f,
					TextColor = new Color(255, 215, 80)
				};
				Append(label);
			}

			public override void MouseOver(UIMouseEvent evt)
			{
				base.MouseOver(evt);
				if (IsDevMode)
				{
					isHovered = true;
					SoundEngine.PlaySound(SoundID.MenuTick);
				}
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				isHovered = false;
			}

			public override void LeftClick(UIMouseEvent evt)
			{
				base.LeftClick(evt);
				if (IsDevMode)
					parent.ToggleDevMode();
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				if (!IsDevMode)
					return;

				float time = (float)Main.GlobalTimeWrappedHourly;
				float pulse = (float)Math.Sin(time * 5f) * 0.5f + 0.5f;

				BackgroundColor = new Color(32, 20, 10) * (isHovered ? 0.98f : 0.88f);
				BorderColor = Color.Lerp(new Color(255, 160, 25), new Color(255, 235, 120), pulse);

				base.DrawSelf(spriteBatch);

				if (isHovered)
				{
					Main.instance.MouseText("Click to disable Dev Mode");
				}
			}

			protected override void DrawChildren(SpriteBatch spriteBatch)
			{
				if (IsDevMode)
					base.DrawChildren(spriteBatch);
			}
		}

		// Close button (matching AugmentShopUIState)
		private class CloseButton : UIPanel
		{
			public event Action Clicked;

			private static readonly Color IdleColor = new Color(110, 40, 40);
			private static readonly Color HoverColor = new Color(160, 60, 60);

			public CloseButton()
			{
				SetPadding(0f);
				BackgroundColor = IdleColor;
				BorderColor = Color.White * 0.4f;

				UIText labelText = new UIText("x", 0.85f)
				{
					HAlign = 0.5f,
					VAlign = 0.5f
				};
				Append(labelText);
			}

			public override void LeftClick(UIMouseEvent evt)
			{
				base.LeftClick(evt);
				Clicked?.Invoke();
			}

			public override void MouseOver(UIMouseEvent evt)
			{
				base.MouseOver(evt);
				BackgroundColor = HoverColor;
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				BackgroundColor = IdleColor;
			}
		}

		// Reusable interactive button/pill for filter bar and popup chips
		private class CodexFilterButton : UIPanel
		{
			public readonly UIText Label;
			public event Action Clicked;
			public event Action RightClicked;
			private bool isHovered;
			public bool IsActiveHighlight { get; set; }
			public Color CustomActiveBg { get; set; } = new Color(38, 54, 105);
			public Color CustomActiveBorder { get; set; } = new Color(255, 215, 75);

			public CodexFilterButton(string initialText, float textScale = 0.72f)
			{
				SetPadding(0f);
				BackgroundColor = new Color(20, 28, 54);
				BorderColor = new Color(45, 60, 105);

				Label = new UIText(initialText, textScale)
				{
					HAlign = 0.5f,
					VAlign = 0.5f
				};
				Append(Label);
			}

			public void SetText(string text)
			{
				Label.SetText(text);
			}

			public void SetTextColor(Color color)
			{
				Label.TextColor = color;
			}

			public override void MouseOver(UIMouseEvent evt)
			{
				base.MouseOver(evt);
				isHovered = true;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				isHovered = false;
			}

			public override void LeftClick(UIMouseEvent evt)
			{
				base.LeftClick(evt);
				SoundEngine.PlaySound(SoundID.MenuTick);
				Clicked?.Invoke();
			}

			public override void RightClick(UIMouseEvent evt)
			{
				base.RightClick(evt);
				SoundEngine.PlaySound(SoundID.MenuTick);
				RightClicked?.Invoke();
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				if (IsActiveHighlight)
				{
					BackgroundColor = isHovered ? Color.Lerp(CustomActiveBg, Color.White, 0.2f) : CustomActiveBg;
					BorderColor = CustomActiveBorder;
				}
				else
				{
					BackgroundColor = isHovered ? new Color(34, 46, 88) : new Color(20, 28, 54);
					BorderColor = isHovered ? Color.White * 0.7f : new Color(45, 60, 105);
				}

				base.DrawSelf(spriteBatch);
			}
		}

		// Floating filter & sort modal panel
		private class AugmentFilterPanel : UIPanel
		{
			private readonly AugmentListUIState state;
			private readonly List<(AugmentClass? cls, CodexFilterButton btn)> classButtons = new();
			private readonly List<(AugmentRarity? rar, CodexFilterButton btn)> rarityButtons = new();
			private readonly List<(AugmentSortMode sort, CodexFilterButton btn)> sortButtons = new();
			private readonly List<(AugmentStatusFilter status, CodexFilterButton btn)> statusButtons = new();

			public AugmentFilterPanel(AugmentListUIState state)
			{
				this.state = state;
				SetPadding(8f);
				BackgroundColor = new Color(14, 18, 38) * 0.98f;
				BorderColor = new Color(255, 215, 75);

				// Title
				UIText title = new UIText("Codex Filters & Sorting", 0.85f)
				{
					TextColor = Color.Gold,
					Top = { Pixels = 2f },
					Left = { Pixels = 4f }
				};
				Append(title);

				// Close button [X]
				var closeBtn = new CloseButton();
				closeBtn.Width.Set(20f, 0f);
				closeBtn.Height.Set(20f, 0f);
				closeBtn.HAlign = 1f;
				closeBtn.Top.Set(2f, 0f);
				closeBtn.Left.Set(-2f, 0f);
				closeBtn.Clicked += state.ToggleFilterMenu;
				Append(closeBtn);

				float curY = 28f;

				// 1. CLASS FILTER
				AddSectionLabel("CLASS FILTER", curY);
				curY += 16f;

				(AugmentClass? cls, string name, float w)[] classDefsRow1 = {
					(null, "All", 68f),
					(AugmentClass.Universal, "Universal", 76f),
					(AugmentClass.Melee, "Melee", 72f),
					(AugmentClass.Ranged, "Ranged", 74f)
				};
				float rowX = 4f;
				foreach (var def in classDefsRow1)
				{
					var btn = CreatePill(def.name, def.w, 22f, rowX, curY);
					btn.Clicked += () => state.SetClassFilter(def.cls);
					classButtons.Add((def.cls, btn));
					rowX += def.w + 4f;
				}
				curY += 25f;

				(AugmentClass? cls, string name, float w)[] classDefsRow2 = {
					(AugmentClass.Magic, "Magic", 94f),
					(AugmentClass.Summon, "Summon", 98f),
					(AugmentClass.Support, "Support", 98f)
				};
				rowX = 4f;
				foreach (var def in classDefsRow2)
				{
					var btn = CreatePill(def.name, def.w, 22f, rowX, curY);
					btn.Clicked += () => state.SetClassFilter(def.cls);
					classButtons.Add((def.cls, btn));
					rowX += def.w + 6f;
				}
				curY += 30f;

				// 2. RARITY TIER
				AddSectionLabel("RARITY TIER", curY);
				curY += 16f;

				(AugmentRarity? rar, string name, float w)[] rarityDefs = {
					(null, "All", 52f),
					(AugmentRarity.Common, "Common", 64f),
					(AugmentRarity.Rare, "Rare", 54f),
					(AugmentRarity.Epic, "Epic", 54f),
					(AugmentRarity.Legendary, "Legendary", 70f)
				};
				rowX = 4f;
				foreach (var def in rarityDefs)
				{
					var btn = CreatePill(def.name, def.w, 22f, rowX, curY);
					if (def.rar.HasValue)
					{
						Color rColor = AugmentListEntry.RarityColor(def.rar.Value);
						if (def.rar.Value == AugmentRarity.Common) rColor = new Color(225, 230, 240);
						btn.CustomActiveBorder = rColor;
					}
					btn.Clicked += () => state.SetRarityFilter(def.rar);
					rarityButtons.Add((def.rar, btn));
					rowX += def.w + 2f;
				}
				curY += 30f;

				// 3. SORT ORDER
				AddSectionLabel("SORT BY", curY);
				curY += 16f;

				(AugmentSortMode sort, string name, float w)[] sortDefsRow1 = {
					(AugmentSortMode.NameAZ, "A → Z", 96f),
					(AugmentSortMode.NameZA, "Z → A", 96f),
					(AugmentSortMode.Class, "Class", 98f)
				};
				rowX = 4f;
				foreach (var def in sortDefsRow1)
				{
					var btn = CreatePill(def.name, def.w, 22f, rowX, curY);
					btn.Clicked += () => state.SetSortMode(def.sort);
					sortButtons.Add((def.sort, btn));
					rowX += def.w + 6f;
				}
				curY += 25f;

				(AugmentSortMode sort, string name, float w)[] sortDefsRow2 = {
					(AugmentSortMode.RarityHighLow, "Rarity ▼ (High)", 146f),
					(AugmentSortMode.RarityLowHigh, "Rarity ▲ (Low)", 146f)
				};
				rowX = 4f;
				foreach (var def in sortDefsRow2)
				{
					var btn = CreatePill(def.name, def.w, 22f, rowX, curY);
					btn.Clicked += () => state.SetSortMode(def.sort);
					sortButtons.Add((def.sort, btn));
					rowX += def.w + 10f;
				}
				curY += 30f;

				// 4. STATUS
				AddSectionLabel("STATUS", curY);
				curY += 16f;

				(AugmentStatusFilter st, string name, float w)[] statusDefs = {
					(AugmentStatusFilter.All, "All", 86f),
					(AugmentStatusFilter.Equipped, "Equipped Only", 110f),
					(AugmentStatusFilter.Unequipped, "Unequipped", 98f)
				};
				rowX = 4f;
				foreach (var def in statusDefs)
				{
					var btn = CreatePill(def.name, def.w, 22f, rowX, curY);
					btn.Clicked += () => state.SetStatusFilter(def.st);
					statusButtons.Add((def.st, btn));
					rowX += def.w + 4f;
				}
				curY += 32f;

				// Bottom Action Buttons
				var resetBtn = new CodexFilterButton("Reset All", 0.75f);
				resetBtn.Width.Set(146f, 0f);
				resetBtn.Height.Set(24f, 0f);
				resetBtn.Left.Set(4f, 0f);
				resetBtn.Top.Set(curY, 0f);
				resetBtn.BackgroundColor = new Color(60, 25, 35);
				resetBtn.BorderColor = new Color(180, 70, 80);
				resetBtn.Clicked += state.ResetAllFilters;
				Append(resetBtn);

				var closeApplyBtn = new CodexFilterButton("Close / Apply", 0.75f);
				closeApplyBtn.Width.Set(146f, 0f);
				closeApplyBtn.Height.Set(24f, 0f);
				closeApplyBtn.Left.Set(156f, 0f);
				closeApplyBtn.Top.Set(curY, 0f);
				closeApplyBtn.BackgroundColor = new Color(25, 60, 40);
				closeApplyBtn.BorderColor = new Color(60, 160, 90);
				closeApplyBtn.Clicked += state.ToggleFilterMenu;
				Append(closeApplyBtn);
			}

			private void AddSectionLabel(string text, float y)
			{
				UIText label = new UIText(text, 0.72f)
				{
					TextColor = new Color(180, 190, 210),
					Top = { Pixels = y },
					Left = { Pixels = 4f }
				};
				Append(label);
			}

			private CodexFilterButton CreatePill(string text, float width, float height, float x, float y)
			{
				var btn = new CodexFilterButton(text, 0.70f);
				btn.Width.Set(width, 0f);
				btn.Height.Set(height, 0f);
				btn.Left.Set(x, 0f);
				btn.Top.Set(y, 0f);
				Append(btn);
				return btn;
			}

			public void SyncUI()
			{
				foreach (var (cls, btn) in classButtons)
				{
					btn.IsActiveHighlight = (cls == state.currentClassFilter);
				}

				foreach (var (rar, btn) in rarityButtons)
				{
					btn.IsActiveHighlight = (rar == state.currentRarityFilter);
				}

				foreach (var (sort, btn) in sortButtons)
				{
					btn.IsActiveHighlight = (sort == state.currentSortMode);
				}

				foreach (var (status, btn) in statusButtons)
				{
					btn.IsActiveHighlight = (status == state.currentStatusFilter);
				}
			}

			public override void LeftClick(UIMouseEvent evt)
			{
				base.LeftClick(evt);
			}
		}
	}
}
