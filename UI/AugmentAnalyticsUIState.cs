using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;
using ReLogic.Graphics;
using Augments.Core;

namespace Augments
{
	// Full Terraria-styled Combat Analytics & Live DPS UIState.
	// Matches the exact visual polish, navy/slate palette, ambient particles, and layout of AugmentListUIState and AugmentShopUIState.
	public class AugmentAnalyticsUIState : UIState
	{
		private AnalyticsBackPanel backPanel;
		private UIList recordsList;
		private AnalyticsScrollbar listScrollbar;

		private UIText dpsValueText;
		private UIText totalDmgValueText;
		private UIText blockedValueText;
		private UIText durationValueText;
		private UIText hitsValueText;

		public enum AnalyticsSourceFilter
		{
			All,
			PluginsOnly,
			WeaponsOnly,
			ProtocolsOnly
		}

		private AnalyticsSourceFilter currentSourceFilter = AnalyticsSourceFilter.All;
		private AugmentClass? currentClassFilter = null;
		private AugmentRarity? currentRarityFilter = null;
		private string currentSearchQuery = "";
		private bool isFilterMenuOpen = false;
		private AnalyticsFilterPanel filterPanel;

		private AnalyticsActionButton modeButton;
		private AnalyticsActionButton pauseButton;
		private AnalyticsActionButton resetButton;
		private AnalyticsActionButton filterButton;
		private AnalyticsSearchBar searchBar;
		private UIPanel statusPill;
		private UIText statusPillText;

		private readonly UIParticleSystem particles = new UIParticleSystem(40);

		private const float PanelWidth = 860f;
		private const float PanelHeight = 600f;

		private int updateCounter = 0;
		private long lastSeenTotalDamage = -1;
		private long lastSeenBlockedDamage = -1;
		private AnalyticsViewMode lastSeenMode = AnalyticsViewMode.Last10Minutes;
		private int lastSeenOwnedCount = -1;

		public override void OnInitialize()
		{
			// 1. Main Background Panel (Classic Terraria slate-navy styling)
			backPanel = new AnalyticsBackPanel();
			backPanel.Width.Set(PanelWidth, 0f);
			backPanel.Height.Set(PanelHeight, 0f);
			backPanel.HAlign = 0.5f;
			backPanel.VAlign = 0.5f;
			backPanel.BackgroundColor = new Color(20, 28, 54) * 0.98f;
			backPanel.BorderColor = new Color(38, 52, 98);
			Append(backPanel);

			// 2. Title & Subtitle Header (Cleanly separated at Y = 12 & Y = 38)
			UIText titleText = new UIText("✦  Combat Analytics & DPS  ✦", 1.15f)
			{
				HAlign = 0.5f,
				TextColor = new Color(255, 235, 175)
			};
			titleText.Top.Set(12f, 0f);
			backPanel.Append(titleText);

			UIText subtitle = new UIText("Real-time neural telemetry and plugin performance metrics", 0.76f)
			{
				HAlign = 0.5f,
				TextColor = new Color(150, 170, 205)
			};
			subtitle.Top.Set(38f, 0f);
			backPanel.Append(subtitle);

			// Close Button in top-right corner
			var closeButton = new CloseButton();
			closeButton.Width.Set(24f, 0f);
			closeButton.Height.Set(24f, 0f);
			closeButton.HAlign = 1f;
			closeButton.Top.Set(10f, 0f);
			closeButton.Left.Set(-16f, 0f);
			closeButton.Clicked += () => ModContent.GetInstance<AugmentUISystem>().HideAnalytics();
			backPanel.Append(closeButton);

			// 3. Control Action Toolbar (Y = 76f to 104f - completely clear of header divider at 66f)
			float barTop = 76f;
			float barHeight = 28f;

			modeButton = new AnalyticsActionButton("★  Mode: Last 10 Mins  ★", new Color(56, 189, 248), new Color(20, 42, 65));
			modeButton.Left.Set(16f, 0f);
			modeButton.Top.Set(barTop, 0f);
			modeButton.Width.Set(180f, 0f);
			modeButton.Height.Set(barHeight, 0f);
			modeButton.Clicked += () =>
			{
				AugmentDamageTracker.ViewMode = AugmentDamageTracker.ViewMode == AnalyticsViewMode.Last10Minutes
					? AnalyticsViewMode.TotalSession
					: AnalyticsViewMode.Last10Minutes;
				UpdateControlLabels();
				PopulateRecords();
			};
			backPanel.Append(modeButton);

			pauseButton = new AnalyticsActionButton("⏸  Pause", new Color(250, 204, 21), new Color(48, 42, 20));
			pauseButton.Left.Set(202f, 0f);
			pauseButton.Top.Set(barTop, 0f);
			pauseButton.Width.Set(108f, 0f);
			pauseButton.Height.Set(barHeight, 0f);
			pauseButton.Clicked += () =>
			{
				AugmentDamageTracker.TogglePause();
				UpdateControlLabels();
			};
			backPanel.Append(pauseButton);

			resetButton = new AnalyticsActionButton("↺  Reset", new Color(248, 113, 113), new Color(50, 24, 24));
			resetButton.Left.Set(316f, 0f);
			resetButton.Top.Set(barTop, 0f);
			resetButton.Width.Set(95f, 0f);
			resetButton.Height.Set(barHeight, 0f);
			resetButton.Clicked += () =>
			{
				AugmentDamageTracker.Reset();
				SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.6f, Pitch = 0.2f });
				PopulateRecords();
			};
			backPanel.Append(resetButton);

			filterButton = new AnalyticsActionButton("⚙  Filters", new Color(168, 85, 247), new Color(38, 22, 58));
			filterButton.Left.Set(417f, 0f);
			filterButton.Top.Set(barTop, 0f);
			filterButton.Width.Set(125f, 0f);
			filterButton.Height.Set(barHeight, 0f);
			filterButton.Clicked += ToggleFilterMenu;
			backPanel.Append(filterButton);

			searchBar = new AnalyticsSearchBar();
			searchBar.Left.Set(548f, 0f);
			searchBar.Top.Set(barTop, 0f);
			searchBar.Width.Set(154f, 0f);
			searchBar.Height.Set(barHeight, 0f);
			searchBar.OnSearchChanged += (text) =>
			{
				currentSearchQuery = text;
				PopulateRecords();
			};
			backPanel.Append(searchBar);

			// Status indicator pill on the far right (ends at 844f, exact 6px gap from searchBar)
			statusPill = new StatusIndicatorPill();
			statusPill.Left.Set(708f, 0f);
			statusPill.Top.Set(barTop, 0f);
			statusPill.Width.Set(136f, 0f);
			statusPill.Height.Set(barHeight, 0f);
			statusPill.SetPadding(0f);
			statusPill.BackgroundColor = new Color(14, 20, 36) * 0.95f;
			statusPill.BorderColor = new Color(74, 222, 128) * 0.7f;

			statusPillText = new UIText("● Live Feed", 0.70f)
			{
				HAlign = 0.5f,
				VAlign = 0.5f,
				TextColor = new Color(74, 222, 128)
			};
			statusPill.Append(statusPillText);
			backPanel.Append(statusPill);

			// 4. Summary Metric Cards (Y = 114f to 166f)
			CreateMetricCards();

			// 5. Table Column Header Bar (Y = 176f to 202f)
			CreateTableHeaders();

			// 6. Scrollable Records List & Scrollbar (Y = 208f to 556f)
			float listTop = 208f;
			float listHeight = PanelHeight - listTop - 42f;

			recordsList = new UIList();
			recordsList.ManualSortMethod = _ => { };
			recordsList.Top.Set(listTop, 0f);
			recordsList.Left.Set(16f, 0f);
			recordsList.Width.Set(814f, 0f);
			recordsList.Height.Set(listHeight, 0f);
			recordsList.ListPadding = 4f;
			backPanel.Append(recordsList);

			listScrollbar = new AnalyticsScrollbar();
			listScrollbar.Top.Set(listTop, 0f);
			listScrollbar.Height.Set(listHeight, 0f);
			listScrollbar.Left.Set(834f, 0f);
			listScrollbar.Width.Set(8f, 0f);
			recordsList.SetScrollbar(listScrollbar);
			backPanel.Append(listScrollbar);

			PopulateRecords();
		}

		public override void OnActivate()
		{
			base.OnActivate();
			PopulateRecords();
		}

		private void CreateMetricCards()
		{
			float cardTop = 112f;
			float cardHeight = 56f;

			// Card 1: Live DPS (Width = 150f)
			var dpsCard = CreateSingleCard(16f, cardTop, 150f, cardHeight, new Color(74, 222, 128), "LIVE DPS (3S)", PinnedStatType.LiveDPS, out dpsValueText);
			backPanel.Append(dpsCard);

			// Card 2: Total Damage (Width = 155f)
			var dmgCard = CreateSingleCard(178f, cardTop, 155f, cardHeight, new Color(56, 189, 248), "RECORDED DAMAGE", PinnedStatType.TotalDamage, out totalDmgValueText);
			backPanel.Append(dmgCard);

			// Card 3: Damage Blocked (Width = 155f)
			var blockCard = CreateSingleCard(345f, cardTop, 155f, cardHeight, new Color(52, 211, 153), "DAMAGE BLOCKED", PinnedStatType.DamageBlocked, out blockedValueText);
			backPanel.Append(blockCard);

			// Card 4: Combat Time (Width = 140f)
			var durCard = CreateSingleCard(512f, cardTop, 140f, cardHeight, new Color(192, 132, 252), "COMBAT TIME", PinnedStatType.CombatTime, out durationValueText);
			backPanel.Append(durCard);

			// Card 5: Hits & Crits Summary (Width = 180f)
			var hitsCard = CreateSingleCard(664f, cardTop, 180f, cardHeight, new Color(250, 204, 21), "HITS & CRIT RATE", PinnedStatType.HitsAndCrits, out hitsValueText);
			backPanel.Append(hitsCard);
		}

		private UIPanel CreateSingleCard(float left, float top, float width, float height, Color accent, string label, PinnedStatType statType, out UIText valueOutput)
		{
			UIPanel card = new UIPanel();
			card.Left.Set(left, 0f);
			card.Top.Set(top, 0f);
			card.Width.Set(width, 0f);
			card.Height.Set(height, 0f);
			card.SetPadding(0f);
			card.BackgroundColor = new Color(12, 18, 36) * 0.96f;
			card.BorderColor = accent * 0.70f;

			UIText labelText = new UIText(label, 0.62f)
			{
				Left = new StyleDimension(10f, 0f),
				Top = new StyleDimension(6f, 0f),
				HAlign = 0f,
				TextColor = new Color(150, 168, 195)
			};
			card.Append(labelText);

			valueOutput = new UIText("--", 0.90f)
			{
				HAlign = 0.5f,
				Top = new StyleDimension(25f, 0f),
				TextColor = accent
			};
			card.Append(valueOutput);

			var pinBtn = new CardPinButton(statType);
			card.Append(pinBtn);

			return card;
		}

		private class CardPinButton : UIPanel
		{
			public readonly PinnedStatType StatType;
			private bool isHovered;

			public CardPinButton(PinnedStatType type)
			{
				this.StatType = type;
				SetPadding(0f);
				Width.Set(18f, 0f);
				Height.Set(18f, 0f);
				Left.Set(-24f, 1f);
				Top.Set(5f, 0f);
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
				AugmentPinnedHUD.TogglePin(StatType);
				SoundEngine.PlaySound(AugmentPinnedHUD.IsPinned(StatType) ? SoundID.Research : SoundID.MenuClose);
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				bool isPinned = AugmentPinnedHUD.IsPinned(StatType);

				if (isPinned)
				{
					BackgroundColor = isHovered ? new Color(60, 48, 20) : new Color(38, 30, 14);
					BorderColor = isHovered ? Color.White : new Color(255, 215, 75);
				}
				else
				{
					BackgroundColor = isHovered ? new Color(28, 38, 68) : new Color(16, 22, 40);
					BorderColor = isHovered ? Color.White * 0.8f : new Color(42, 58, 92);
				}

				base.DrawSelf(spriteBatch);

				CalculatedStyle dims = GetDimensions();
				int cx = (int)dims.X + 9;
				int cy = (int)dims.Y + 8;

				Color headCol = isPinned ? new Color(255, 215, 75) : (isHovered ? Color.White : new Color(160, 180, 210));
				Color needleCol = isPinned ? new Color(255, 245, 200) : new Color(210, 225, 245);
				Color shadowCol = new Color(0, 0, 0, 160);

				// 1px Drop Shadow for 3D depth
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - 3, cy - 4, 7, 2), shadowCol);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - 1, cy - 2, 3, 3), shadowCol);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - 3, cy + 1, 7, 2), shadowCol);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx, cy + 3, 1, 5), shadowCol);

				// Crisp Pixel Pushpin:
				// 1. Top Rim / Cap
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - 3, cy - 5, 7, 2), headCol);
				// 2. Middle Stem
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - 1, cy - 3, 3, 3), headCol * 0.85f);
				// 3. Lower Flange / Collar
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - 3, cy, 7, 2), headCol);
				// 4. Needle
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx, cy + 2, 1, 5), needleCol);

				if (isHovered)
				{
					string tip = isPinned
						? "Pinned to in-game HUD (Click to unpin)"
						: "Pin stat to in-game HUD (Click to pin)\n[Hold Left Alt in combat to drag anywhere]";
					Main.instance.MouseText(tip);
				}
			}
		}

		private class StatusIndicatorPill : UIPanel
		{
			public StatusIndicatorPill()
			{
				SetPadding(0f);
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				base.DrawSelf(spriteBatch);

				CalculatedStyle dims = GetDimensions();
				Point mouse = new Point(Main.mouseX, Main.mouseY);
				if (dims.ToRectangle().Contains(mouse))
				{
					if (AugmentDamageTracker.IsPaused)
					{
						Main.instance.MouseText("Telemetry Feed: PAUSED\nLive combat hits are temporarily frozen. Click 'Resume Feed' to restart recording.");
					}
					else
					{
						Main.instance.MouseText("Telemetry Feed: LIVE FEED (Online)\nGreen indicator confirms combat damage telemetry is actively recording in real-time.");
					}
				}
			}
		}

		private class AnalyticsScrollbar : UIScrollbar
		{
			public AnalyticsScrollbar()
			{
				Width.Set(8f, 0f);
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				if (!CanScroll)
					return;

				CalculatedStyle dims = GetDimensions();
				Rectangle trackRect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);

				// Dark sleek cybernetic track backing
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, trackRect, new Color(10, 16, 32) * 0.92f);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(trackRect.X, trackRect.Y, 1, trackRect.Height), new Color(34, 48, 86) * 0.6f);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(trackRect.Right - 1, trackRect.Y, 1, trackRect.Height), new Color(34, 48, 86) * 0.6f);

				base.DrawSelf(spriteBatch);
			}
		}

		private void CreateTableHeaders()
		{
			UIPanel header = new UIPanel();
			header.Left.Set(16f, 0f);
			header.Top.Set(176f, 0f);
			header.Width.Set(828f, 0f);
			header.Height.Set(26f, 0f);
			header.SetPadding(0f);
			header.BackgroundColor = new Color(14, 20, 40) * 0.92f;
			header.BorderColor = new Color(34, 48, 86);

			var col1 = new UIText("PLUGIN / DAMAGE SOURCE", 0.70f) { Top = new StyleDimension(5f, 0f), Left = new StyleDimension(16f, 0f), TextColor = new Color(180, 200, 230) };
			var col2 = new UIText("HITS & CRITS", 0.70f) { Top = new StyleDimension(5f, 0f), Left = new StyleDimension(330f, 0f), TextColor = new Color(180, 200, 230) };
			var col3 = new UIText("MAX HIT", 0.70f) { Top = new StyleDimension(5f, 0f), Left = new StyleDimension(500f, 0f), TextColor = new Color(180, 200, 230) };
			var col4 = new UIText("DAMAGE (% SHARE)", 0.70f) { Top = new StyleDimension(5f, 0f), Left = new StyleDimension(630f, 0f), TextColor = new Color(180, 200, 230) };

			header.Append(col1);
			header.Append(col2);
			header.Append(col3);
			header.Append(col4);
			backPanel.Append(header);
		}

		public bool HasActiveFilters => currentSourceFilter != AnalyticsSourceFilter.All || currentClassFilter.HasValue || currentRarityFilter.HasValue;

		public AnalyticsSourceFilter CurrentSourceFilter => currentSourceFilter;
		public AugmentClass? CurrentClassFilter => currentClassFilter;
		public AugmentRarity? CurrentRarityFilter => currentRarityFilter;

		public void SetSourceFilter(AnalyticsSourceFilter filter)
		{
			currentSourceFilter = filter;
			UpdateFilterButtonDisplay();
			PopulateRecords();
		}

		public void SetClassFilter(AugmentClass? cls)
		{
			currentClassFilter = cls;
			UpdateFilterButtonDisplay();
			PopulateRecords();
		}

		public void SetRarityFilter(AugmentRarity? rar)
		{
			currentRarityFilter = rar;
			UpdateFilterButtonDisplay();
			PopulateRecords();
		}

		public void ResetFilters()
		{
			currentSourceFilter = AnalyticsSourceFilter.All;
			currentClassFilter = null;
			currentRarityFilter = null;
			if (searchBar != null)
				searchBar.Text = "";
			currentSearchQuery = "";
			UpdateFilterButtonDisplay();
			PopulateRecords();
		}

		public void UpdateFilterButtonDisplay()
		{
			if (filterButton == null)
				return;

			if (!HasActiveFilters)
			{
				filterButton.SetLabel("⚙  Filters");
				filterButton.SetAccent(new Color(168, 85, 247));
			}
			else
			{
				string filterSummary = "⚙ ";
				if (currentClassFilter.HasValue)
					filterSummary += $"{currentClassFilter.Value} ";
				else if (currentSourceFilter != AnalyticsSourceFilter.All)
				{
					filterSummary += currentSourceFilter switch
					{
						AnalyticsSourceFilter.PluginsOnly => "Plugins ",
						AnalyticsSourceFilter.WeaponsOnly => "Weapons ",
						AnalyticsSourceFilter.ProtocolsOnly => "Protocols ",
						_ => ""
					};
				}
				else if (currentRarityFilter.HasValue)
					filterSummary += $"{currentRarityFilter.Value} ";

				filterButton.SetLabel(filterSummary.TrimEnd() + " ★");
				filterButton.SetAccent(new Color(255, 215, 75));
			}

			filterPanel?.UpdatePillStates();
		}

		public void ToggleFilterMenu()
		{
			isFilterMenuOpen = !isFilterMenuOpen;
			if (isFilterMenuOpen)
			{
				if (filterPanel == null)
				{
					filterPanel = new AnalyticsFilterPanel(this);
					filterPanel.Left.Set(240f, 0f);
					filterPanel.Top.Set(108f, 0f);
					filterPanel.Width.Set(520f, 0f);
					filterPanel.Height.Set(210f, 0f);
				}
				filterPanel.UpdatePillStates();
				backPanel.Append(filterPanel);
			}
			else if (filterPanel != null && backPanel.HasChild(filterPanel))
			{
				backPanel.RemoveChild(filterPanel);
			}
		}

		public override void OnDeactivate()
		{
			base.OnDeactivate();
			if (searchBar != null)
				searchBar.IsFocused = false;
			if (isFilterMenuOpen)
				ToggleFilterMenu();
		}

		public void Refresh()
		{
			UpdateControlLabels();
			UpdateFilterButtonDisplay();
			PopulateRecords();
		}

		private void UpdateControlLabels()
		{
			if (modeButton != null)
			{
				modeButton.SetLabel(AugmentDamageTracker.ViewMode == AnalyticsViewMode.Last10Minutes
					? "★  Mode: Last 10 Mins  ★"
					: "★  Mode: Total Session  ★");
			}

			if (pauseButton != null)
			{
				pauseButton.SetLabel(AugmentDamageTracker.IsPaused ? "▶  Resume Feed" : "⏸  Pause Feed");
				pauseButton.SetAccent(AugmentDamageTracker.IsPaused ? new Color(74, 222, 128) : new Color(250, 204, 21));
			}

			if (statusPill != null && statusPillText != null)
			{
				if (AugmentDamageTracker.IsPaused)
				{
					statusPill.BorderColor = new Color(250, 204, 21) * 0.7f;
					statusPillText.SetText("❚❚ Feed Paused");
					statusPillText.TextColor = new Color(250, 204, 21);
				}
				else
				{
					statusPill.BorderColor = new Color(74, 222, 128) * 0.7f;
					statusPillText.SetText("● Live Feed");
					statusPillText.TextColor = new Color(74, 222, 128);
				}
			}
		}

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

			particles.Update();

			if (backPanel != null && Main.rand.NextBool(6))
			{
				CalculatedStyle dims = backPanel.GetDimensions();
				if (dims.Width > 0)
				{
					Rectangle rect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);
					Color emberCol = Main.rand.NextBool(2) ? new Color(56, 189, 248) : new Color(96, 165, 250);
					particles.SpawnAmbient(rect, emberCol, 1.6f);
				}
			}

			// Live metrics updates
			float dps = AugmentDamageTracker.GetCurrentDPS();
			if (dpsValueText != null)
				dpsValueText.SetText($"{Math.Round(dps):N0} /s");

			if (durationValueText != null)
			{
				int s = (int)AugmentDamageTracker.SessionDuration;
				durationValueText.SetText($"{s / 60:D2}:{s % 60:D2}");
			}

			// Periodic or event-driven row refresh
			updateCounter++;
			if (updateCounter % 15 == 0)
			{
				var player = Main.LocalPlayer;
				var ap = player?.GetModPlayer<AugmentPlayer>();
				var (viewDmg, viewBlocked, sortedRecords) = AugmentDamageTracker.GetCurrentViewData(ap);

				if (totalDmgValueText != null)
					totalDmgValueText.SetText($"{viewDmg:N0}");

				if (blockedValueText != null)
					blockedValueText.SetText($"{viewBlocked:N0}");

				int totalHits = 0;
				int totalCrits = 0;
				foreach (var r in sortedRecords)
				{
					totalHits += r.HitCount;
					totalCrits += r.CritCount;
				}
				float critRate = totalHits > 0 ? ((float)totalCrits / totalHits) * 100f : 0f;
				if (hitsValueText != null)
					hitsValueText.SetText($"{totalHits:N0} ({critRate:0.0}%)");

				int ownedCount = ap?.Owned?.Count ?? 0;
				if (viewDmg != lastSeenTotalDamage || viewBlocked != lastSeenBlockedDamage || AugmentDamageTracker.ViewMode != lastSeenMode || ownedCount != lastSeenOwnedCount)
				{
					lastSeenTotalDamage = viewDmg;
					lastSeenBlockedDamage = viewBlocked;
					lastSeenMode = AugmentDamageTracker.ViewMode;
					lastSeenOwnedCount = ownedCount;
					PopulateRecords();
				}
			}
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			base.Draw(spriteBatch);
			particles.Draw(spriteBatch);
		}

		private void PopulateRecords()
		{
			if (recordsList == null)
				return;

			recordsList.Clear();

			var player = Main.LocalPlayer;
			if (player == null || !player.active)
				return;

			var ap = player.GetModPlayer<AugmentPlayer>();
			var (viewDamage, viewBlocked, sortedRecords) = AugmentDamageTracker.GetCurrentViewData(ap);

			if (totalDmgValueText != null)
				totalDmgValueText.SetText($"{viewDamage:N0}");

			if (blockedValueText != null)
				blockedValueText.SetText($"{viewBlocked:N0}");

			int totalHits = 0;
			int totalCrits = 0;
			foreach (var r in sortedRecords)
			{
				totalHits += r.HitCount;
				totalCrits += r.CritCount;
			}
			float critRate = totalHits > 0 ? ((float)totalCrits / totalHits) * 100f : 0f;
			if (hitsValueText != null)
				hitsValueText.SetText($"{totalHits:N0} ({critRate:0.0}%)");

			// Apply active filters
			var filteredRecords = new List<DamageSourceRecord>();
			foreach (var rec in sortedRecords)
			{
				// Source type filter
				if (currentSourceFilter == AnalyticsSourceFilter.PluginsOnly && (rec.IsWeapon || rec.IsProtocol))
					continue;
				if (currentSourceFilter == AnalyticsSourceFilter.WeaponsOnly && !rec.IsWeapon)
					continue;
				if (currentSourceFilter == AnalyticsSourceFilter.ProtocolsOnly && !rec.IsProtocol)
					continue;

				// Combat class filter
				if (currentClassFilter.HasValue && rec.SourceClass != currentClassFilter.Value)
					continue;

				// Chip rarity filter (only applies to chips)
				if (currentRarityFilter.HasValue)
				{
					if (rec.IsWeapon || rec.IsProtocol)
						continue;
					if (rec.Rarity != currentRarityFilter.Value)
						continue;
				}

				// Text search query
				if (!string.IsNullOrWhiteSpace(currentSearchQuery) &&
				    !rec.DisplayName.Contains(currentSearchQuery, StringComparison.OrdinalIgnoreCase))
					continue;

				filteredRecords.Add(rec);
			}

			if (filteredRecords.Count == 0)
			{
				UIPanel emptyRow = new UIPanel();
				emptyRow.Width.Set(0f, 1f);
				emptyRow.Height.Set(48f, 0f);
				emptyRow.BackgroundColor = new Color(16, 22, 42) * 0.8f;
				emptyRow.BorderColor = new Color(30, 42, 70);

				bool hasFilters = HasActiveFilters || !string.IsNullOrWhiteSpace(currentSearchQuery);
				string emptyMsg = hasFilters
					? "No telemetry records match your active filters. Click here to reset filters."
					: "No combat damage recorded yet. Strike enemies to gather neural telemetry.";

				UIText emptyLabel = new UIText(emptyMsg, 0.78f)
				{
					HAlign = 0.5f,
					VAlign = 0.5f,
					TextColor = hasFilters ? new Color(255, 215, 75) : new Color(148, 163, 184)
				};
				if (hasFilters)
				{
					emptyRow.OnLeftClick += (evt, elem) =>
					{
						ResetFilters();
						SoundEngine.PlaySound(SoundID.MenuTick);
					};
				}
				emptyRow.Append(emptyLabel);
				recordsList.Add(emptyRow);
				return;
			}

			foreach (var rec in filteredRecords)
			{
				var row = new AnalyticsRowEntry(rec, viewDamage, viewBlocked);
				recordsList.Add(row);
			}
		}

		private class AnalyticsBackPanel : UIPanel
		{
			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				base.DrawSelf(spriteBatch);

				CalculatedStyle dims = GetDimensions();

				// 1. Cyber Corner Accents on Dialog
				Color cornerAccent = new Color(56, 189, 248) * 0.75f;
				const int clen = 8;
				const int cthk = 2;
				Rectangle bRect = dims.ToRectangle();

				// Top-left
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(bRect.X + 2, bRect.Y + 2, clen, cthk), cornerAccent);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(bRect.X + 2, bRect.Y + 2, cthk, clen), cornerAccent);
				// Top-right
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(bRect.Right - clen - 2, bRect.Y + 2, clen, cthk), cornerAccent);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(bRect.Right - cthk - 2, bRect.Y + 2, cthk, clen), cornerAccent);
				// Bottom-left
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(bRect.X + 2, bRect.Bottom - cthk - 2, clen, cthk), cornerAccent);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(bRect.X + 2, bRect.Bottom - clen - 2, cthk, clen), cornerAccent);
				// Bottom-right
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(bRect.Right - clen - 2, bRect.Bottom - cthk - 2, clen, cthk), cornerAccent);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(bRect.Right - cthk - 2, bRect.Bottom - clen - 2, cthk, clen), cornerAccent);

				// 2. Header horizontal divider (at Y = 66)
				int divY = (int)dims.Y + 66;
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)dims.X + 20, divY, (int)dims.Width - 40, 1), new Color(45, 62, 105) * 0.75f);
				int midX = (int)dims.X + (int)(dims.Width * 0.5f);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(midX - 3, divY - 2, 7, 5), new Color(56, 189, 248) * 0.85f);

				// 3. Cybernetic Background Radar / Telemetry Reticle Watermark
				Vector2 center = new Vector2(dims.X + dims.Width * 0.5f, dims.Y + 380f);
				Color watermarkCol = new Color(56, 189, 248) * 0.04f;

				DrawRadarCircle(spriteBatch, center, 50f, watermarkCol);
				DrawRadarCircle(spriteBatch, center, 110f, watermarkCol);
				DrawRadarCircle(spriteBatch, center, 170f, watermarkCol);

				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)center.X - 180, (int)center.Y, 360, 1), watermarkCol);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)center.X, (int)center.Y - 180, 1, 360), watermarkCol);

				// 4. Footer Horizontal Divider & Status Line (at Y = bRect.Bottom - 32)
				int footerY = bRect.Bottom - 32;
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(bRect.X + 20, footerY, bRect.Width - 40, 1), new Color(35, 48, 80) * 0.75f);

				var font = FontAssets.MouseText.Value;
				string footerLeft = "✦ TELEMETRY ENGINE v2.0  •  [Hold Left Alt] To Drag Pinned HUD Widgets";
				string footerRight = "[L] Toggle  •  [ESC] Close";
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, footerLeft, new Vector2(bRect.X + 20f, footerY + 8f), new Color(130, 150, 185) * 0.70f, 0f, Vector2.Zero, new Vector2(0.58f));

				Vector2 rSz = ChatManager.GetStringSize(font, footerRight, new Vector2(0.58f));
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, footerRight, new Vector2(bRect.Right - 20f - rSz.X, footerY + 8f), new Color(100, 125, 160) * 0.65f, 0f, Vector2.Zero, new Vector2(0.58f));
			}

			private static void DrawRadarCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
			{
				const int segments = 36;
				float step = MathHelper.TwoPi / segments;
				for (int i = 0; i < segments; i++)
				{
					float angle = i * step;
					int x = (int)(center.X + (float)Math.Cos(angle) * radius);
					int y = (int)(center.Y + (float)Math.Sin(angle) * radius);
					spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(x, y, 2, 2), color);
				}
			}
		}

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
				SoundEngine.PlaySound(SoundID.MenuTick);
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				BackgroundColor = IdleColor;
			}
		}

		private class AnalyticsActionButton : UIPanel
		{
			public event Action Clicked;

			private UIText labelText;
			private Color accentColor;
			private Color idleBg;
			private Color hoverBg;

			public AnalyticsActionButton(string label, Color accent, Color bg)
			{
				this.accentColor = accent;
				this.idleBg = bg;
				this.hoverBg = bg * 1.4f;

				SetPadding(0f);
				BackgroundColor = idleBg;
				BorderColor = accent * 0.7f;

				labelText = new UIText(label, 0.74f)
				{
					HAlign = 0.5f,
					VAlign = 0.5f,
					TextColor = Color.White
				};
				Append(labelText);
			}

			public void SetLabel(string label)
			{
				labelText?.SetText(label);
			}

			public void SetAccent(Color accent)
			{
				accentColor = accent;
				BorderColor = accent * 0.7f;
			}

			public override void LeftClick(UIMouseEvent evt)
			{
				base.LeftClick(evt);
				SoundEngine.PlaySound(SoundID.MenuTick);
				Clicked?.Invoke();
			}

			public override void MouseOver(UIMouseEvent evt)
			{
				base.MouseOver(evt);
				BackgroundColor = hoverBg;
				BorderColor = Color.White;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				BackgroundColor = idleBg;
				BorderColor = accentColor * 0.7f;
			}
		}

		// Reusable interactive button/pill for filter panel
		private class FilterPillButton : UIPanel
		{
			public readonly UIText Label;
			public event Action Clicked;
			private bool isHovered;
			public bool IsActiveHighlight { get; set; }
			public Color CustomActiveBg { get; set; } = new Color(38, 54, 105);
			public Color CustomActiveBorder { get; set; } = new Color(255, 215, 75);

			public FilterPillButton(string initialText, float textScale = 0.70f)
			{
				SetPadding(0f);
				BackgroundColor = new Color(20, 28, 54);
				BorderColor = new Color(45, 60, 105);

				Label = new UIText(initialText, textScale)
				{
					HAlign = 0.5f,
					VAlign = 0.5f,
					TextColor = new Color(200, 215, 235)
				};
				Append(Label);
			}

			public void SetText(string text) => Label.SetText(text);
			public void SetTextColor(Color color) => Label.TextColor = color;

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

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				if (IsActiveHighlight)
				{
					BackgroundColor = isHovered ? Color.Lerp(CustomActiveBg, Color.White, 0.2f) : CustomActiveBg;
					BorderColor = CustomActiveBorder;
					Label.TextColor = Color.White;
				}
				else
				{
					BackgroundColor = isHovered ? new Color(32, 44, 82) : new Color(18, 24, 46);
					BorderColor = isHovered ? Color.White * 0.7f : new Color(42, 54, 95);
					Label.TextColor = isHovered ? Color.White : new Color(180, 195, 220);
				}

				base.DrawSelf(spriteBatch);
			}
		}

		// Search input bar with live text input, placeholder, clear button, and focus styling
		private class AnalyticsSearchBar : UIPanel
		{
			public string Text { get; set; } = "";
			public bool IsFocused { get; set; }
			public event Action<string> OnSearchChanged;

			private bool isHovered;
			private int _textBlinkerCount;
			private int _textBlinkerState;

			public AnalyticsSearchBar()
			{
				SetPadding(0f);
				BackgroundColor = new Color(16, 22, 44) * 0.95f;
				BorderColor = new Color(45, 60, 105);
			}

			public void Clear()
			{
				if (!string.IsNullOrEmpty(Text))
				{
					Text = "";
					OnSearchChanged?.Invoke(Text);
				}
			}

			public override void Update(GameTime gameTime)
			{
				base.Update(gameTime);

				if (IsFocused)
				{
					PlayerInput.WritingText = true;
					Main.CurrentInputTextTakerOverride = this;
				}

				Vector2 mousePoint = new Vector2(Main.mouseX, Main.mouseY);
				if (IsFocused && !ContainsPoint(mousePoint) && Main.mouseLeft)
				{
					IsFocused = false;
				}
			}

			public override void LeftClick(UIMouseEvent evt)
			{
				base.LeftClick(evt);

				var dims = GetDimensions();
				if (!string.IsNullOrEmpty(Text) && evt.MousePosition.X >= dims.X + dims.Width - 22f)
				{
					Clear();
					SoundEngine.PlaySound(SoundID.MenuTick);
					return;
				}

				if (!IsFocused)
				{
					Main.clrInput();
					IsFocused = true;
					SoundEngine.PlaySound(SoundID.MenuTick);
				}
			}

			public override void RightClick(UIMouseEvent evt)
			{
				base.RightClick(evt);
				if (!string.IsNullOrEmpty(Text))
				{
					Clear();
					SoundEngine.PlaySound(SoundID.MenuTick);
				}
			}

			public override void MouseOver(UIMouseEvent evt)
			{
				base.MouseOver(evt);
				isHovered = true;
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				isHovered = false;
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				if (IsFocused)
				{
					PlayerInput.WritingText = true;
					Main.CurrentInputTextTakerOverride = this;
					Main.instance.HandleIME();

					string inputText = Main.GetInputText(Text);
					inputText = inputText.Replace("\r", "").Replace("\n", "");

					if (Main.inputTextEscape)
					{
						Main.inputTextEscape = false;
						IsFocused = false;
					}
					else if (Main.inputTextEnter)
					{
						Main.inputTextEnter = false;
						IsFocused = false;
					}

					if (!inputText.Equals(Text))
					{
						Text = inputText;
						OnSearchChanged?.Invoke(Text);
					}

					if (++_textBlinkerCount >= 20)
					{
						_textBlinkerState = (_textBlinkerState + 1) % 2;
						_textBlinkerCount = 0;
					}

					BackgroundColor = new Color(18, 26, 56) * 0.98f;
					BorderColor = new Color(110, 185, 255);
				}
				else if (isHovered)
				{
					BackgroundColor = new Color(22, 30, 60) * 0.95f;
					BorderColor = new Color(75, 115, 185);
				}
				else
				{
					BackgroundColor = new Color(16, 22, 44) * 0.95f;
					BorderColor = new Color(45, 60, 105);
				}

				base.DrawSelf(spriteBatch);

				var dims = GetDimensions();
				Vector2 scale = new Vector2(0.70f);
				var font = FontAssets.MouseText.Value;
				float textYOffset = 5f;

				if (string.IsNullOrEmpty(Text))
				{
					if (IsFocused)
					{
						if (_textBlinkerState == 1)
						{
							Vector2 cursorSize = ChatManager.GetStringSize(font, "|", scale);
							float textX = dims.X + 8f;
							float textY = dims.Y + (dims.Height - cursorSize.Y) / 2f + textYOffset;
							ChatManager.DrawColorCodedStringWithShadow(
								spriteBatch,
								font,
								"|",
								new Vector2(textX, textY),
								new Color(110, 185, 255),
								0f,
								Vector2.Zero,
								scale
							);
						}
					}
					else
					{
						string placeholder = "Search sources...";
						Vector2 textSize = ChatManager.GetStringSize(font, placeholder, scale);
						float textX = dims.X + 8f;
						float textY = dims.Y + (dims.Height - textSize.Y) / 2f + textYOffset;
						ChatManager.DrawColorCodedStringWithShadow(
							spriteBatch,
							font,
							placeholder,
							new Vector2(textX, textY),
							new Color(130, 148, 175) * 0.75f,
							0f,
							Vector2.Zero,
							scale
						);
					}
				}
				else
				{
					string display = Text;
					if (IsFocused && _textBlinkerState == 1)
					{
						display += "|";
					}

					float clearBtnReserve = 20f;
					float maxTextWidth = dims.Width - 12f - clearBtnReserve;
					while (display.Length > 0 && ChatManager.GetStringSize(font, display, scale).X > maxTextWidth)
					{
						display = display.Substring(1);
					}

					Vector2 textSize = ChatManager.GetStringSize(font, display, scale);
					float textX = dims.X + 8f;
					float textY = dims.Y + (dims.Height - textSize.Y) / 2f + textYOffset;

					ChatManager.DrawColorCodedStringWithShadow(
						spriteBatch,
						font,
						display,
						new Vector2(textX, textY),
						Color.White,
						0f,
						Vector2.Zero,
						scale
					);

					// Draw Clear [✕] button
					bool clearHover = isHovered && Main.MouseScreen.X >= dims.X + dims.Width - 20f;
					Color clearColor = clearHover ? new Color(255, 110, 110) : new Color(160, 175, 205) * 0.8f;
					Vector2 xSize = ChatManager.GetStringSize(font, "✕", scale);
					float xX = dims.X + dims.Width - 16f;
					float xY = dims.Y + (dims.Height - xSize.Y) / 2f + textYOffset;

					ChatManager.DrawColorCodedStringWithShadow(
						spriteBatch,
						font,
						"✕",
						new Vector2(xX, xY),
						clearColor,
						0f,
						Vector2.Zero,
						scale
					);
				}

				if (isHovered && !string.IsNullOrEmpty(Text) && Main.MouseScreen.X >= dims.X + dims.Width - 20f)
				{
					Main.instance.MouseText("Clear search");
				}
			}
		}

		// Floating filter popup panel
		private class AnalyticsFilterPanel : UIPanel
		{
			private readonly AugmentAnalyticsUIState state;
			private readonly List<(AnalyticsSourceFilter filter, FilterPillButton btn)> sourceButtons = new();
			private readonly List<(AugmentClass? cls, FilterPillButton btn)> classButtons = new();
			private readonly List<(AugmentRarity? rar, FilterPillButton btn)> rarityButtons = new();
			private UIText summaryText;

			public AnalyticsFilterPanel(AugmentAnalyticsUIState state)
			{
				this.state = state;
				SetPadding(8f);
				BackgroundColor = new Color(14, 20, 42) * 0.98f;
				BorderColor = new Color(255, 215, 75);

				// Title
				UIText title = new UIText("⚙ Combat Telemetry Filters", 0.84f)
				{
					TextColor = new Color(255, 215, 75),
					Top = { Pixels = 2f },
					Left = { Pixels = 4f }
				};
				Append(title);

				// Close button [x]
				var closeBtn = new CloseButton();
				closeBtn.Width.Set(20f, 0f);
				closeBtn.Height.Set(20f, 0f);
				closeBtn.HAlign = 1f;
				closeBtn.Top.Set(2f, 0f);
				closeBtn.Left.Set(-2f, 0f);
				closeBtn.Clicked += state.ToggleFilterMenu;
				Append(closeBtn);

				float curY = 26f;

				// 1. SOURCE CATEGORY
				AddSectionLabel("SOURCE CATEGORY", curY);
				curY += 16f;

				(AnalyticsSourceFilter filter, string name, float w)[] sourceDefs = {
					(AnalyticsSourceFilter.All, "All Sources", 86f),
					(AnalyticsSourceFilter.PluginsOnly, "Plugins", 76f),
					(AnalyticsSourceFilter.WeaponsOnly, "Weapons", 82f),
					(AnalyticsSourceFilter.ProtocolsOnly, "Protocols", 82f)
				};
				float rowX = 4f;
				foreach (var def in sourceDefs)
				{
					var btn = CreatePill(def.name, def.w, 22f, rowX, curY);
					btn.Clicked += () => state.SetSourceFilter(def.filter);
					sourceButtons.Add((def.filter, btn));
					rowX += def.w + 6f;
				}
				curY += 28f;

				// 2. COMBAT CLASS
				AddSectionLabel("COMBAT CLASS (WEAPONS & PLUGINS)", curY);
				curY += 16f;

				(AugmentClass? cls, string name, float w)[] classDefs = {
					(null, "All", 52f),
					(AugmentClass.Melee, "Melee", 66f),
					(AugmentClass.Ranged, "Ranged", 70f),
					(AugmentClass.Magic, "Magic", 66f),
					(AugmentClass.Summon, "Summon", 74f),
					(AugmentClass.Universal, "Universal", 78f)
				};
				rowX = 4f;
				foreach (var def in classDefs)
				{
					var btn = CreatePill(def.name, def.w, 22f, rowX, curY);
					btn.Clicked += () => state.SetClassFilter(def.cls);
					classButtons.Add((def.cls, btn));
					rowX += def.w + 6f;
				}
				curY += 28f;

				// 3. RARITY
				AddSectionLabel("PLUGIN RARITY", curY);
				curY += 16f;

				(AugmentRarity? rar, string name, float w)[] rarityDefs = {
					(null, "All", 52f),
					(AugmentRarity.Common, "Common", 72f),
					(AugmentRarity.Rare, "Rare", 66f),
					(AugmentRarity.Epic, "Epic", 66f),
					(AugmentRarity.Legendary, "Legendary", 84f)
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
					rowX += def.w + 6f;
				}
				curY += 32f;

				// Bottom Action Bar: Reset + Close + Summary
				var resetBtn = new FilterPillButton("↺ Clear Filters", 0.72f);
				resetBtn.Width.Set(110f, 0f);
				resetBtn.Height.Set(24f, 0f);
				resetBtn.Left.Set(4f, 0f);
				resetBtn.Top.Set(curY, 0f);
				resetBtn.BackgroundColor = new Color(60, 25, 35);
				resetBtn.BorderColor = new Color(180, 70, 80);
				resetBtn.Clicked += state.ResetFilters;
				Append(resetBtn);

				var closeApplyBtn = new FilterPillButton("✔ Close Menu", 0.72f);
				closeApplyBtn.Width.Set(110f, 0f);
				closeApplyBtn.Height.Set(24f, 0f);
				closeApplyBtn.Left.Set(120f, 0f);
				closeApplyBtn.Top.Set(curY, 0f);
				closeApplyBtn.BackgroundColor = new Color(25, 45, 80);
				closeApplyBtn.BorderColor = new Color(70, 130, 210);
				closeApplyBtn.Clicked += state.ToggleFilterMenu;
				Append(closeApplyBtn);

				summaryText = new UIText("", 0.70f)
				{
					Left = { Pixels = 240f },
					Top = { Pixels = curY + 4f },
					TextColor = new Color(200, 215, 235)
				};
				Append(summaryText);
			}

			private void AddSectionLabel(string text, float top)
			{
				UIText label = new UIText(text, 0.65f)
				{
					TextColor = new Color(56, 189, 248),
					Top = { Pixels = top },
					Left = { Pixels = 4f }
				};
				Append(label);
			}

			private FilterPillButton CreatePill(string label, float width, float height, float left, float top)
			{
				var pill = new FilterPillButton(label)
				{
					Width = { Pixels = width },
					Height = { Pixels = height },
					Left = { Pixels = left },
					Top = { Pixels = top }
				};
				Append(pill);
				return pill;
			}

			public void UpdatePillStates()
			{
				foreach (var (filter, btn) in sourceButtons)
				{
					btn.IsActiveHighlight = state.CurrentSourceFilter == filter;
				}

				foreach (var (cls, btn) in classButtons)
				{
					btn.IsActiveHighlight = state.CurrentClassFilter == cls;
				}

				foreach (var (rar, btn) in rarityButtons)
				{
					btn.IsActiveHighlight = state.CurrentRarityFilter == rar;
				}

				if (summaryText != null)
				{
					int count = 0;
					if (state.CurrentSourceFilter != AnalyticsSourceFilter.All) count++;
					if (state.CurrentClassFilter.HasValue) count++;
					if (state.CurrentRarityFilter.HasValue) count++;

					if (count > 0)
						summaryText.SetText($"[c/FDE047:{count} filter{(count > 1 ? "s" : "")} active]");
					else
						summaryText.SetText("[c/94A3B8:Showing all telemetry]");
				}
			}
		}
	}

	// Single styled row in the Analytics telemetry list
	public class AnalyticsRowEntry : UIPanel
	{
		private readonly DamageSourceRecord record;
		private readonly long totalSessionDamage;
		private readonly long totalSessionBlocked;
		private readonly Augment augmentRef;
		private bool isHovered;

		public AnalyticsRowEntry(DamageSourceRecord record, long totalSessionDamage, long totalSessionBlocked)
		{
			this.record = record;
			this.totalSessionDamage = totalSessionDamage;
			this.totalSessionBlocked = totalSessionBlocked;
			this.augmentRef = AugmentDatabase.GetById(record.Id);

			SetPadding(0f);
			Width.Set(0f, 1f);
			Height.Set(48f, 0f);
		}

		public override void MouseOver(UIMouseEvent evt)
		{
			base.MouseOver(evt);
			isHovered = true;
			if (augmentRef != null)
			{
				AugmentListEntry.HoveredAugment = augmentRef;
			}
			SoundEngine.PlaySound(SoundID.MenuTick);
		}

		public override void MouseOut(UIMouseEvent evt)
		{
			base.MouseOut(evt);
			isHovered = false;
			if (augmentRef != null && AugmentListEntry.HoveredAugment == augmentRef)
			{
				AugmentListEntry.HoveredAugment = null;
			}
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			CalculatedStyle dims = GetDimensions();
			Rectangle rect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);

			Color baseBorder = record.Color;
			Color bgColor = isHovered ? new Color(30, 42, 76) * 0.98f : new Color(18, 26, 48) * 0.94f;
			Color borderColor = isHovered ? baseBorder : baseBorder * 0.5f;

			// Row Background
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, bgColor);

			// Borders
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, rect.Width, 1), borderColor);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), borderColor);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, 1, rect.Height), borderColor);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), borderColor);

			DynamicSpriteFont font = FontAssets.MouseText.Value;

			// 1. Icon Box on the Left
			Rectangle iconBox = new Rectangle(rect.X + 12, rect.Y + 8, 32, 32);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, iconBox, new Color(12, 18, 34) * 0.95f);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(iconBox.X, iconBox.Y, iconBox.Width, 1), borderColor * 0.7f);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(iconBox.X, iconBox.Bottom - 1, iconBox.Width, 1), borderColor * 0.7f);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(iconBox.X, iconBox.Y, 1, iconBox.Height), borderColor * 0.7f);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(iconBox.Right - 1, iconBox.Y, 1, iconBox.Height), borderColor * 0.7f);

			if (augmentRef != null)
			{
				Texture2D classIcon = AugmentSlotElement.GetClassIcon(augmentRef.Class);
				if (classIcon != null)
				{
					Rectangle targetIcon = new Rectangle(iconBox.X + 4, iconBox.Y + 4, 24, 24);
					spriteBatch.Draw(classIcon, targetIcon, Color.White);
				}
			}
			else if (record.IsWeapon)
			{
				string weaponGlyph = record.SourceClass switch
				{
					AugmentClass.Melee => "⚔",
					AugmentClass.Ranged => "⌖",
					AugmentClass.Magic => "★",
					AugmentClass.Summon => "❖",
					_ => "⚔"
				};
				Color weaponCol = record.SourceClass switch
				{
					AugmentClass.Melee => new Color(249, 115, 22),
					AugmentClass.Ranged => new Color(74, 222, 128),
					AugmentClass.Magic => new Color(56, 189, 248),
					AugmentClass.Summon => new Color(192, 132, 252),
					_ => new Color(225, 230, 240)
				};
				Vector2 cSz = ChatManager.GetStringSize(font, weaponGlyph, new Vector2(0.85f));
				Vector2 cPos = new Vector2(iconBox.X + (iconBox.Width - cSz.X) * 0.5f, iconBox.Y + (iconBox.Height - cSz.Y) * 0.5f);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, weaponGlyph, cPos, weaponCol, 0f, Vector2.Zero, new Vector2(0.85f));
			}
			else
			{
				string iconChar = record.IsProtocol ? "◆" : "✦";
				Vector2 cSz = ChatManager.GetStringSize(font, iconChar, new Vector2(0.85f));
				Vector2 cPos = new Vector2(iconBox.X + (iconBox.Width - cSz.X) * 0.5f, iconBox.Y + (iconBox.Height - cSz.Y) * 0.5f);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, iconChar, cPos, record.Color, 0f, Vector2.Zero, new Vector2(0.85f));
			}

			// 2. Name & Category Tag
			float textX = rect.X + 52f;
			string nameText = record.DisplayName;
			if (nameText.Length > 28)
				nameText = nameText.Substring(0, 26) + "...";

			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, nameText, new Vector2(textX, rect.Y + 6f), record.Color, 0f, Vector2.Zero, new Vector2(0.80f));

			string subTag;
			if (augmentRef != null)
			{
				string rHex = AugmentListEntry.RarityColor(augmentRef.Rarity).Hex3();
				subTag = $"[c/38BDF8:{augmentRef.Class}]  •  [c/{rHex}:{augmentRef.Rarity} Plugin]";
			}
			else if (record.IsProtocol)
			{
				string pClass = record.SourceClass.HasValue ? record.SourceClass.Value.ToString() : "Universal";
				subTag = $"[c/38BDF8:{pClass}]  •  [c/FF3EA5:Protocol Synergy]";
			}
			else if (record.IsWeapon)
			{
				string wClass = record.SourceClass.HasValue ? record.SourceClass.Value.ToString() : "Melee";
				subTag = $"[c/38BDF8:{wClass} Weapon]  •  [c/CBD5E1:Primary]";
			}
			else
			{
				subTag = "[c/94A3B8:Combat Proc]";
			}

			if (record.DamageBlocked > 0)
			{
				subTag += $"  •  [c/34D399:◈ {record.DamageBlocked:N0} Blocked]";
			}

			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, subTag, new Vector2(textX, rect.Y + 26f), Color.White, 0f, Vector2.Zero, new Vector2(0.65f));

			// 3. Hits & Crits (aligned to 330f)
			float critRate = record.HitCount > 0 ? ((float)record.CritCount / record.HitCount) * 100f : 0f;
			string hitsText;
			Color hitsCol;
			if (record.HitCount > 0 && record.BlockCount > 0)
			{
				hitsText = $"{record.HitCount:N0} Hits  •  {record.BlockCount:N0} Blocked";
				hitsCol = new Color(200, 215, 235);
			}
			else if (record.HitCount > 0)
			{
				hitsText = $"{record.HitCount:N0} Hits  •  {record.CritCount:N0} Crits ({critRate:0.0}%)";
				hitsCol = new Color(200, 215, 235);
			}
			else if (record.BlockCount > 0)
			{
				hitsText = $"{record.BlockCount:N0} Attacks Blocked";
				hitsCol = new Color(52, 211, 153);
			}
			else
			{
				hitsText = "--";
				hitsCol = new Color(148, 163, 184);
			}
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, hitsText, new Vector2(rect.X + 330f, rect.Y + 16f), hitsCol, 0f, Vector2.Zero, new Vector2(0.72f));

			// 4. Max Hit (aligned to 500f)
			string maxHitText = record.MaxHit > 0 ? $"{record.MaxHit:N0}" : "--";
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, maxHitText, new Vector2(rect.X + 500f, rect.Y + 16f), new Color(253, 224, 71), 0f, Vector2.Zero, new Vector2(0.74f));

			// 5. Total Damage & Share Percentage (aligned to 630f)
			float rightColX = rect.X + 630f;
			if (record.TotalDamage == 0 && record.DamageBlocked > 0)
			{
				float blockShare = totalSessionBlocked > 0 ? (float)record.DamageBlocked / totalSessionBlocked : 0f;
				string blockText = $"{record.DamageBlocked:N0}";
				string blockShareText = $"(Blocked {blockShare * 100f:0.0}%)";
				Vector2 bSz = ChatManager.GetStringSize(font, blockText, new Vector2(0.82f));
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, blockText, new Vector2(rightColX, rect.Y + 14f), new Color(52, 211, 153), 0f, Vector2.Zero, new Vector2(0.82f));
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, blockShareText, new Vector2(rightColX + bSz.X + 6f, rect.Y + 16f), new Color(52, 211, 153) * 0.9f, 0f, Vector2.Zero, new Vector2(0.72f));
			}
			else
			{
				float sharePercent = totalSessionDamage > 0 ? (float)record.TotalDamage / totalSessionDamage : 0f;
				string dmgText = $"{record.TotalDamage:N0}";
				string shareText = $"({sharePercent * 100f:0.0}%)";
				Vector2 dmgSz = ChatManager.GetStringSize(font, dmgText, new Vector2(0.82f));
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, dmgText, new Vector2(rightColX, rect.Y + 14f), new Color(245, 248, 255), 0f, Vector2.Zero, new Vector2(0.82f));
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, shareText, new Vector2(rightColX + dmgSz.X + 6f, rect.Y + 16f), (record.Color == Color.White ? new Color(56, 189, 248) : record.Color), 0f, Vector2.Zero, new Vector2(0.72f));
			}

			// 6. Sleek 2px Progress Fill Bar across the bottom of the row
			Color barAccent = (record.TotalDamage == 0 && record.DamageBlocked > 0)
				? new Color(52, 211, 153)
				: (record.Color == Color.White ? new Color(56, 189, 248) : record.Color);

			float fillRatio = 0f;
			if (record.TotalDamage > 0 && totalSessionDamage > 0)
				fillRatio = (float)record.TotalDamage / totalSessionDamage;
			else if (record.DamageBlocked > 0 && totalSessionBlocked > 0)
				fillRatio = (float)record.DamageBlocked / totalSessionBlocked;

			Rectangle barBg = new Rectangle(rect.X + 6, rect.Bottom - 3, rect.Width - 12, 2);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, barBg, new Color(10, 14, 26, 200));

			int fillW = Math.Max((record.TotalDamage > 0 || record.DamageBlocked > 0) ? 3 : 0, (int)((rect.Width - 12) * Math.Min(1f, fillRatio)));
			Rectangle barFill = new Rectangle(rect.X + 6, rect.Bottom - 3, fillW, 2);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, barFill, barAccent * 0.85f);

			// Tooltip for non-plugin entries when hovered
			if (isHovered && augmentRef == null)
			{
				float sharePercent = totalSessionDamage > 0 ? (float)record.TotalDamage / totalSessionDamage : 0f;
				string tip = $"{record.DisplayName}\nTotal Damage: {record.TotalDamage:N0} ({sharePercent * 100f:0.0}% of output)\nHits: {record.HitCount:N0}  •  Crits: {record.CritCount:N0}  •  Max Hit: {record.MaxHit:N0}";
				if (record.DamageBlocked > 0)
				{
					tip += $"\nDamage Blocked: {record.DamageBlocked:N0} ({record.BlockCount:N0} blocks)";
				}
				Main.instance.MouseText(tip);
			}
		}
	}
}
