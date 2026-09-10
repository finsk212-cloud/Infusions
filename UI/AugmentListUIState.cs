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
	// Clean, Terraria-style in-game Augment Codex & Inventory Slot List.
	// Displays augments in inventory-style slot boxes categorized by tier (Common, Rare, Epic, Legendary),
	// with an interactive inspector panel on the right showing full detailed info when clicked.
	public class AugmentListUIState : UIState
	{
		public static Asset<Texture2D> RarityStarAsset;

		private UIPanel backPanel;
		private UIList gridList;
		private UIScrollbar gridScrollbar;
		private AugmentDetailPanel detailPanel;
		private SupportClassTagElement supportTag;

		private string currentTab = "All";
		private Augment selectedAugment;
		private readonly List<CodexTabButton> tabButtons = new List<CodexTabButton>();

		private const float PanelWidth = 880f;
		private const float PanelHeight = 560f;
		private const int SlotsPerRow = 6;
		private const float SlotWidth = 74f;
		private const float SlotHeight = 84f;
		private const float SlotSpacing = 10f;

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

			// Title
			UIText title = new UIText("Infusions Codex", 1.05f)
			{
				HAlign = 0.5f
			};
			title.Top.Set(8f, 0f);
			backPanel.Append(title);

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

			// Left: Grid List Container for Inventory Slot Boxes
			gridList = new UIList();
			gridList.Top.Set(74f, 0f);
			gridList.Left.Set(12f, 0f);
			gridList.Width.Set(515f, 0f);
			gridList.Height.Set(-86f, 1f);
			gridList.ListPadding = 6f;
			backPanel.Append(gridList);

			// Scrollbar for Grid
			gridScrollbar = new UIScrollbar();
			gridScrollbar.Top.Set(74f, 0f);
			gridScrollbar.Height.Set(-86f, 1f);
			gridScrollbar.Left.Set(534f, 0f);
			gridList.SetScrollbar(gridScrollbar);
			backPanel.Append(gridScrollbar);

			// Right: Detailed Info Inspector Panel
			detailPanel = new AugmentDetailPanel();
			detailPanel.Top.Set(74f, 0f);
			detailPanel.Left.Set(568f, 0f);
			detailPanel.Width.Set(288f, 0f);
			detailPanel.Height.Set(-124f, 1f);
			backPanel.Append(detailPanel);

			// Bottom-Right: Support Class Tag
			supportTag = new SupportClassTagElement();
			supportTag.Left.Set(568f, 0f);
			supportTag.Width.Set(288f, 0f);
			supportTag.Height.Set(30f, 0f);
			supportTag.Top.Set(516f, 0f);
			backPanel.Append(supportTag);

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

		private void SwitchTab(string newTab)
		{
			currentTab = newTab;
			foreach (var btn in tabButtons)
				btn.IsActive = (btn.TabName == currentTab);

			PopulateGrid();
		}

		public void Refresh()
		{
			if (gridList == null)
				return;

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
			gridList.Clear();
			AugmentListEntry.HoveredAugment = null;

			var ap = Main.LocalPlayer.GetModPlayer<AugmentPlayer>();
			var all = AugmentDatabase.All;

			var filtered = new List<Augment>();
			foreach (var aug in all)
			{
				if (aug.Id == "debug_full_crit")
					continue;

				if (currentTab == "Equipped")
				{
					if (ap.HasAugment(aug.Id))
						filtered.Add(aug);
				}
				else if (currentTab == "All")
				{
					filtered.Add(aug);
				}
				else if (aug.Rarity.ToString().Equals(currentTab, StringComparison.OrdinalIgnoreCase))
				{
					filtered.Add(aug);
				}
			}

			// Sort by Rarity (Legendary -> Epic -> Rare -> Common) then Name
			filtered.Sort((a, b) =>
			{
				int rarityCompare = b.Rarity.CompareTo(a.Rarity);
				return rarityCompare != 0 ? rarityCompare : string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
			});

			if (filtered.Count == 0)
			{
				var emptyText = new UIText("No augments found in this tier.", 0.9f)
				{
					HAlign = 0.5f
				};
				emptyText.Top.Set(40f, 0f);
				gridList.Add(emptyText);
				return;
			}

			// Group into rows of SlotsPerRow
			UIElement currentRow = null;
			int slotIndexInRow = 0;

			for (int i = 0; i < filtered.Count; i++)
			{
				if (slotIndexInRow == 0)
				{
					currentRow = new UIElement();
					currentRow.Width.Set(0f, 1f);
					currentRow.Height.Set(SlotHeight + 5f, 0f);
					gridList.Add(currentRow);
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

				currentRow.Append(slot);

				slotIndexInRow++;
				if (slotIndexInRow >= SlotsPerRow)
					slotIndexInRow = 0;
			}
		}

		private void SelectAugment(Augment augment)
		{
			selectedAugment = augment;
			bool isOwned = Main.LocalPlayer.GetModPlayer<AugmentPlayer>().HasAugment(augment.Id);
			detailPanel.SetAugment(augment, isOwned);

			// Refresh grid selection state
			PopulateGrid();
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
			private Augment currentAugment;
			private bool currentIsOwned;

			public AugmentDetailPanel()
			{
				SetPadding(10f);
				BackgroundColor = new Color(18, 24, 46) * 0.95f;
				BorderColor = new Color(38, 50, 92);
			}

			public void SetAugment(Augment augment, bool isOwned)
			{
				currentAugment = augment;
				currentIsOwned = isOwned;
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

						// Glowing halo behind stars
						Color starGlow = starColor * 0.25f;
						if (currentAugment.Rarity == AugmentRarity.Epic)
						{
							float sPulse = (float)Math.Sin(time * 3f + s * 0.4f) * 0.5f + 0.5f;
							starGlow = new Color(190, 130, 255) * (0.25f + sPulse * 0.35f);
							spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)sx - 2, (int)y - 2, starTex.Width + 4, starTex.Height + 4), starGlow * 0.45f);
						}
						else if (currentAugment.Rarity == AugmentRarity.Legendary)
						{
							float sPulse = (float)Math.Sin(time * 4f + s * 0.5f) * 0.5f + 0.5f;
							starGlow = new Color(255, 180, 40) * (0.30f + sPulse * 0.45f);
							spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)sx - 3, (int)y - 3, starTex.Width + 6, starTex.Height + 6), starGlow * 0.50f);

							if (sPulse > 0.88f)
							{
								AugmentSlotElement.DrawStarSparkle(spriteBatch, (int)(sx + starTex.Width * 0.5f), (int)(y + starTex.Height * 0.5f), (sPulse - 0.88f) / 0.12f);
							}
						}
						else
						{
							spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)sx - 1, (int)y - 1, starTex.Width + 2, starTex.Height + 2), starGlow);
						}

						spriteBatch.Draw(starTex, starPos, starColor);
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
	}
}
