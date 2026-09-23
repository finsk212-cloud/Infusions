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
		private UIScrollbar listScrollbar;

		private UIText dpsValueText;
		private UIText totalDmgValueText;
		private UIText durationValueText;
		private UIText modeValueText;

		private AnalyticsActionButton modeButton;
		private AnalyticsActionButton pauseButton;
		private AnalyticsActionButton resetButton;

		private readonly UIParticleSystem particles = new UIParticleSystem(50);

		private const float PanelWidth = 840f;
		private const float PanelHeight = 570f;

		private int updateCounter = 0;
		private long lastSeenTotalDamage = -1;
		private AnalyticsViewMode lastSeenMode = AnalyticsViewMode.Last10Minutes;

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

			// 2. Title & Subtitle Header
			UIText titleText = new UIText("✦  Combat Analytics & DPS  ✦", 1.15f)
			{
				HAlign = 0.5f,
				TextColor = new Color(255, 235, 175)
			};
			titleText.Top.Set(12f, 0f);
			backPanel.Append(titleText);

			UIText subtitle = new UIText("Real-time neural telemetry and plug-in chip performance metrics", 0.76f)
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
			closeButton.Left.Set(-10f, 0f);
			closeButton.Clicked += () => ModContent.GetInstance<AugmentUISystem>().HideAnalytics();
			backPanel.Append(closeButton);

			// 3. Top Action Buttons (Mode, Pause, Reset)
			float btnTop = 48f;

			modeButton = new AnalyticsActionButton("★  Mode: Last 10 Mins  ★", new Color(56, 189, 248), new Color(20, 42, 65));
			modeButton.Left.Set(16f, 0f);
			modeButton.Top.Set(btnTop, 0f);
			modeButton.Width.Set(185f, 0f);
			modeButton.Height.Set(24f, 0f);
			modeButton.Clicked += () =>
			{
				AugmentDamageTracker.ViewMode = AugmentDamageTracker.ViewMode == AnalyticsViewMode.Last10Minutes
					? AnalyticsViewMode.TotalSession
					: AnalyticsViewMode.Last10Minutes;
				UpdateControlLabels();
				PopulateRecords();
			};
			backPanel.Append(modeButton);

			pauseButton = new AnalyticsActionButton("⏸  Pause Feed", new Color(250, 204, 21), new Color(48, 42, 20));
			pauseButton.Left.Set(209f, 0f);
			pauseButton.Top.Set(btnTop, 0f);
			pauseButton.Width.Set(120f, 0f);
			pauseButton.Height.Set(24f, 0f);
			pauseButton.Clicked += () =>
			{
				AugmentDamageTracker.TogglePause();
				UpdateControlLabels();
			};
			backPanel.Append(pauseButton);

			resetButton = new AnalyticsActionButton("↺  Reset Data", new Color(248, 113, 113), new Color(50, 24, 24));
			resetButton.Left.Set(337f, 0f);
			resetButton.Top.Set(btnTop, 0f);
			resetButton.Width.Set(110f, 0f);
			resetButton.Height.Set(24f, 0f);
			resetButton.Clicked += () =>
			{
				AugmentDamageTracker.Reset();
				SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.6f, Pitch = 0.2f });
				PopulateRecords();
			};
			backPanel.Append(resetButton);

			// 4. Summary Metric Cards (4 Cards across)
			CreateMetricCards();

			// 5. Table Column Header Bar
			CreateTableHeaders();

			// 6. Scrollable Records List & Scrollbar
			recordsList = new UIList();
			recordsList.ManualSortMethod = _ => { };
			recordsList.Top.Set(176f, 0f);
			recordsList.Left.Set(14f, 0f);
			recordsList.Width.Set(PanelWidth - 48f, 0f);
			recordsList.Height.Set(PanelHeight - 192f, 0f);
			recordsList.ListPadding = 4f;
			backPanel.Append(recordsList);

			listScrollbar = new UIScrollbar();
			listScrollbar.Top.Set(176f, 0f);
			listScrollbar.Height.Set(PanelHeight - 192f, 0f);
			listScrollbar.Left.Set(PanelWidth - 28f, 0f);
			recordsList.SetScrollbar(listScrollbar);
			backPanel.Append(listScrollbar);
		}

		private void CreateMetricCards()
		{
			float cardTop = 84f;
			float cardHeight = 52f;
			float totalW = PanelWidth - 32f;
			float cardSpacing = 8f;
			float cardW = (totalW - (cardSpacing * 3f)) / 4f;

			// Card 1: Live DPS
			var dpsCard = CreateSingleCard(16f + 0 * (cardW + cardSpacing), cardTop, cardW, cardHeight, new Color(74, 222, 128), "LIVE DPS (3S)", out dpsValueText);
			backPanel.Append(dpsCard);

			// Card 2: Total Damage
			var dmgCard = CreateSingleCard(16f + 1 * (cardW + cardSpacing), cardTop, cardW, cardHeight, new Color(56, 189, 248), "RECORDED DAMAGE", out totalDmgValueText);
			backPanel.Append(dmgCard);

			// Card 3: Session Duration
			var durCard = CreateSingleCard(16f + 2 * (cardW + cardSpacing), cardTop, cardW, cardHeight, new Color(168, 85, 247), "COMBAT TIME", out durationValueText);
			backPanel.Append(durCard);

			// Card 4: Active Mode
			var modeCard = CreateSingleCard(16f + 3 * (cardW + cardSpacing), cardTop, cardW, cardHeight, new Color(250, 204, 21), "CURRENT VIEW", out modeValueText);
			backPanel.Append(modeCard);
		}

		private UIPanel CreateSingleCard(float left, float top, float width, float height, Color accent, string label, out UIText valueOutput)
		{
			UIPanel card = new UIPanel();
			card.Left.Set(left, 0f);
			card.Top.Set(top, 0f);
			card.Width.Set(width, 0f);
			card.Height.Set(height, 0f);
			card.SetPadding(0f);
			card.BackgroundColor = new Color(14, 20, 38) * 0.95f;
			card.BorderColor = accent * 0.65f;

			UIText labelText = new UIText(label, 0.65f)
			{
				HAlign = 0.5f,
				Top = new StyleDimension(5f, 0f),
				TextColor = new Color(148, 163, 184)
			};
			card.Append(labelText);

			valueOutput = new UIText("--", 0.95f)
			{
				HAlign = 0.5f,
				Top = new StyleDimension(22f, 0f),
				TextColor = accent
			};
			card.Append(valueOutput);

			return card;
		}

		private void CreateTableHeaders()
		{
			UIPanel header = new UIPanel();
			header.Left.Set(14f, 0f);
			header.Top.Set(144f, 0f);
			header.Width.Set(PanelWidth - 48f, 0f);
			header.Height.Set(26f, 0f);
			header.SetPadding(0f);
			header.BackgroundColor = new Color(16, 22, 42) * 0.85f;
			header.BorderColor = new Color(34, 48, 86);

			var col1 = new UIText("PLUG-IN CHIP / DAMAGE SOURCE", 0.72f) { Top = new StyleDimension(5f, 0f), Left = new StyleDimension(14f, 0f), TextColor = new Color(180, 200, 230) };
			var col2 = new UIText("HITS & CRITS", 0.72f) { Top = new StyleDimension(5f, 0f), Left = new StyleDimension(350f, 0f), TextColor = new Color(180, 200, 230) };
			var col3 = new UIText("MAX HIT", 0.72f) { Top = new StyleDimension(5f, 0f), Left = new StyleDimension(515f, 0f), TextColor = new Color(180, 200, 230) };
			var col4 = new UIText("DAMAGE (% SHARE)", 0.72f) { Top = new StyleDimension(5f, 0f), Left = new StyleDimension(635f, 0f), TextColor = new Color(180, 200, 230) };

			header.Append(col1);
			header.Append(col2);
			header.Append(col3);
			header.Append(col4);
			backPanel.Append(header);
		}

		public void Refresh()
		{
			UpdateControlLabels();
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

			if (modeValueText != null)
			{
				modeValueText.SetText(AugmentDamageTracker.ViewMode == AnalyticsViewMode.Last10Minutes ? "Last 10 Mins" : "Total Session");
			}
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (backPanel != null && backPanel.ContainsPoint(Main.MouseScreen))
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
				var (viewDmg, _) = AugmentDamageTracker.GetCurrentViewData(ap);

				if (totalDmgValueText != null)
					totalDmgValueText.SetText($"{viewDmg:N0}");

				if (viewDmg != lastSeenTotalDamage || AugmentDamageTracker.ViewMode != lastSeenMode)
				{
					lastSeenTotalDamage = viewDmg;
					lastSeenMode = AugmentDamageTracker.ViewMode;
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
			var (viewDamage, sortedRecords) = AugmentDamageTracker.GetCurrentViewData(ap);

			if (totalDmgValueText != null)
				totalDmgValueText.SetText($"{viewDamage:N0}");

			if (sortedRecords.Count == 0)
			{
				UIPanel emptyRow = new UIPanel();
				emptyRow.Width.Set(0f, 1f);
				emptyRow.Height.Set(48f, 0f);
				emptyRow.BackgroundColor = new Color(16, 22, 42) * 0.8f;
				emptyRow.BorderColor = new Color(30, 42, 70);

				UIText emptyLabel = new UIText("No combat damage recorded yet. Strike enemies to gather neural telemetry.", 0.80f)
				{
					HAlign = 0.5f,
					VAlign = 0.5f,
					TextColor = new Color(148, 163, 184)
				};
				emptyRow.Append(emptyLabel);
				recordsList.Add(emptyRow);
				return;
			}

			foreach (var rec in sortedRecords)
			{
				var row = new AnalyticsRowEntry(rec, viewDamage);
				recordsList.Add(row);
			}
		}

		private class AnalyticsBackPanel : UIPanel
		{
			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				base.DrawSelf(spriteBatch);

				CalculatedStyle dims = GetDimensions();

				// Header horizontal divider
				int divY = (int)dims.Y + 74;
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)dims.X + 20, divY, (int)dims.Width - 40, 1), new Color(45, 62, 105) * 0.75f);

				// Center diamond node
				int midX = (int)dims.X + (int)(dims.Width * 0.5f);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(midX - 2, divY - 2, 5, 5), new Color(56, 189, 248) * 0.85f);
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
	}

	// Single styled row in the Analytics telemetry list
	public class AnalyticsRowEntry : UIPanel
	{
		private readonly DamageSourceRecord record;
		private readonly long totalSessionDamage;
		private readonly Augment augmentRef;
		private bool isHovered;

		public AnalyticsRowEntry(DamageSourceRecord record, long totalSessionDamage)
		{
			this.record = record;
			this.totalSessionDamage = totalSessionDamage;
			this.augmentRef = AugmentDatabase.GetById(record.Id);

			SetPadding(0f);
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
			Rectangle iconBox = new Rectangle(rect.X + 8, rect.Y + 8, 32, 32);
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
			else
			{
				string iconChar = record.IsWeapon ? "⚔" : "✦";
				Vector2 cSz = ChatManager.GetStringSize(font, iconChar, new Vector2(0.85f));
				Vector2 cPos = new Vector2(iconBox.X + (iconBox.Width - cSz.X) * 0.5f, iconBox.Y + (iconBox.Height - cSz.Y) * 0.5f);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, iconChar, cPos, record.Color, 0f, Vector2.Zero, new Vector2(0.85f));
			}

			// 2. Name & Category Tag
			float textX = rect.X + 48f;
			string nameText = record.DisplayName;
			if (nameText.Length > 28)
				nameText = nameText.Substring(0, 26) + "...";

			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, nameText, new Vector2(textX, rect.Y + 6f), record.Color, 0f, Vector2.Zero, new Vector2(0.80f));

			string subTag;
			if (augmentRef != null)
			{
				string rHex = AugmentListEntry.RarityColor(augmentRef.Rarity).Hex3();
				subTag = $"[c/38BDF8:{augmentRef.Class}]  •  [c/{rHex}:{augmentRef.Rarity} Chip]";
			}
			else if (record.IsProtocol)
				subTag = "[c/FF3EA5:Protocol Synergy]";
			else if (record.IsWeapon)
				subTag = "[c/CBD5E1:Primary Weapon]";
			else
				subTag = "[c/94A3B8:Combat Proc]";

			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, subTag, new Vector2(textX, rect.Y + 26f), Color.White, 0f, Vector2.Zero, new Vector2(0.65f));

			// 3. Hits & Crits
			float critRate = record.HitCount > 0 ? ((float)record.CritCount / record.HitCount) * 100f : 0f;
			string hitsText = $"{record.HitCount:N0} Hits  •  {record.CritCount:N0} Crits ({critRate:0.0}%)";
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, hitsText, new Vector2(rect.X + 350f, rect.Y + 16f), new Color(200, 215, 235), 0f, Vector2.Zero, new Vector2(0.72f));

			// 4. Max Hit
			string maxHitText = record.MaxHit > 0 ? $"{record.MaxHit:N0}" : "--";
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, maxHitText, new Vector2(rect.X + 515f, rect.Y + 16f), new Color(253, 224, 71), 0f, Vector2.Zero, new Vector2(0.74f));

			// 5. Total Damage & Share Percentage
			float sharePercent = totalSessionDamage > 0 ? (float)record.TotalDamage / totalSessionDamage : 0f;
			string dmgText = $"{record.TotalDamage:N0}";
			string shareText = $"({sharePercent * 100f:0.0}%)";

			Vector2 dmgSz = ChatManager.GetStringSize(font, dmgText, new Vector2(0.82f));
			Vector2 shareSz = ChatManager.GetStringSize(font, shareText, new Vector2(0.72f));

			float rightColX = rect.Right - dmgSz.X - shareSz.X - 22f;
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, dmgText, new Vector2(rightColX, rect.Y + 14f), new Color(245, 248, 255), 0f, Vector2.Zero, new Vector2(0.82f));
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, shareText, new Vector2(rightColX + dmgSz.X + 6f, rect.Y + 16f), record.Color, 0f, Vector2.Zero, new Vector2(0.72f));

			// 6. Sleek 3px Progress Fill Bar across the bottom of the row
			Rectangle barBg = new Rectangle(rect.X + 2, rect.Bottom - 4, rect.Width - 4, 3);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, barBg, new Color(10, 14, 26, 220));

			int fillW = Math.Max(record.TotalDamage > 0 ? 3 : 0, (int)((rect.Width - 4) * Math.Min(1f, sharePercent)));
			Rectangle barFill = new Rectangle(rect.X + 2, rect.Bottom - 4, fillW, 3);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, barFill, record.Color);

			// Tooltip for non-chip entries when hovered
			if (isHovered && augmentRef == null)
			{
				string tip = $"{record.DisplayName}\nTotal Damage: {record.TotalDamage:N0} ({sharePercent * 100f:0.0}% of output)\nHits: {record.HitCount:N0}  •  Crits: {record.CritCount:N0}  •  Max Hit: {record.MaxHit:N0}";
				Main.instance.MouseText(tip);
			}
		}
	}
}
