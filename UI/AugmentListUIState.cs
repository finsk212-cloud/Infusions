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

namespace Augments
{
	// Clean, Terraria-style in-game Augment Codex & Inventory Slot List.
	// Displays augments in inventory-style slot boxes categorized by tier (Common, Rare, Epic, Legendary),
	// with an interactive inspector panel on the right showing full detailed info when clicked.
	public class AugmentListUIState : UIState
	{
		private UIPanel backPanel;
		private UIList gridList;
		private UIScrollbar gridScrollbar;
		private AugmentDetailPanel detailPanel;
		private SupportClassTagElement supportTag;

		private string currentTab = "All";
		private Augment selectedAugment;
		private readonly List<CodexTabButton> tabButtons = new List<CodexTabButton>();

		private const float PanelWidth = 840f;
		private const float PanelHeight = 550f;
		private const int SlotsPerRow = 6;
		private const float SlotSize = 72f;
		private const float SlotSpacing = 11f;

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
			gridScrollbar.Left.Set(530f, 0f);
			gridList.SetScrollbar(gridScrollbar);
			backPanel.Append(gridScrollbar);

			// Right: Detailed Info Inspector Panel
			detailPanel = new AugmentDetailPanel();
			detailPanel.Top.Set(74f, 0f);
			detailPanel.Left.Set(556f, 0f);
			detailPanel.Width.Set(270f, 0f);
			detailPanel.Height.Set(-122f, 1f);
			backPanel.Append(detailPanel);

			// Bottom-Right: Support Class Tag
			supportTag = new SupportClassTagElement();
			supportTag.Left.Set(556f, 0f);
			supportTag.Width.Set(270f, 0f);
			supportTag.Height.Set(30f, 0f);
			supportTag.Top.Set(506f, 0f);
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
					currentRow.Height.Set(SlotSize + 4f, 0f);
					gridList.Add(currentRow);
				}

				Augment aug = filtered[i];
				var slot = new AugmentSlotElement(aug)
				{
					IsSelected = (selectedAugment != null && selectedAugment.Id == aug.Id),
					IsOwned = ap.HasAugment(aug.Id)
				};
				slot.Left.Set(slotIndexInRow * (SlotSize + SlotSpacing), 0f);
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
				base.DrawSelf(spriteBatch);

				if (currentAugment == null)
				{
					var emptyFont = FontAssets.MouseText.Value;
					string prompt = "Click any augment slot\nto inspect details.";
					Vector2 pSize = ChatManager.GetStringSize(emptyFont, prompt, new Vector2(0.85f));
					CalculatedStyle d = GetDimensions();
					Vector2 pPos = new Vector2(d.X + (d.Width - pSize.X) * 0.5f, d.Y + (d.Height - pSize.Y) * 0.4f);
					ChatManager.DrawColorCodedStringWithShadow(spriteBatch, emptyFont, prompt, pPos, Color.Gray, 0f, Vector2.Zero, new Vector2(0.85f));
					return;
				}

				CalculatedStyle dims = GetDimensions();
				var font = FontAssets.MouseText.Value;
				float x = dims.X + 10f;
				float y = dims.Y + 10f;
				float maxTextWidth = dims.Width - 20f;

				// 1. Augment Display Name & Icon
				Color nameColor = AugmentListEntry.RarityColor(currentAugment.Rarity);
				if (currentAugment.Class == AugmentClass.Melee)
				{
					if (AugmentSlotElement.MeleeIconAsset == null)
						AugmentSlotElement.MeleeIconAsset = ModContent.Request<Texture2D>("Augments/UI/MeleeIcon", ReLogic.Content.AssetRequestMode.ImmediateLoad);

					if (AugmentSlotElement.MeleeIconAsset?.IsLoaded == true)
					{
						Texture2D sword = AugmentSlotElement.MeleeIconAsset.Value;
						Color iconColor = AugmentListEntry.RarityColor(currentAugment.Rarity);
						if (currentAugment.Rarity == AugmentRarity.Common)
							iconColor = new Color(225, 230, 240);

						spriteBatch.Draw(sword, new Vector2(x, y - 2f), iconColor);
						ChatManager.DrawColorCodedStringWithShadow(
							spriteBatch, font, currentAugment.DisplayName, new Vector2(x + sword.Width + 8f, y + 2f), nameColor, 0f, Vector2.Zero, new Vector2(0.95f)
						);
						y += Math.Max(sword.Height, ChatManager.GetStringSize(font, currentAugment.DisplayName, new Vector2(0.95f)).Y) + 4f;
					}
					else
					{
						ChatManager.DrawColorCodedStringWithShadow(
							spriteBatch, font, currentAugment.DisplayName, new Vector2(x, y), nameColor, 0f, Vector2.Zero, new Vector2(0.95f)
						);
						y += ChatManager.GetStringSize(font, currentAugment.DisplayName, new Vector2(0.95f)).Y + 3f;
					}
				}
				else if (currentAugment.Class == AugmentClass.Magic)
				{
					if (AugmentSlotElement.MagicIconAsset == null)
						AugmentSlotElement.MagicIconAsset = ModContent.Request<Texture2D>("Augments/UI/MagicIcon", ReLogic.Content.AssetRequestMode.ImmediateLoad);

					if (AugmentSlotElement.MagicIconAsset?.IsLoaded == true)
					{
						Texture2D flame = AugmentSlotElement.MagicIconAsset.Value;
						Color iconColor = AugmentListEntry.RarityColor(currentAugment.Rarity);
						if (currentAugment.Rarity == AugmentRarity.Common)
							iconColor = new Color(225, 230, 240);

						spriteBatch.Draw(flame, new Vector2(x, y - 2f), iconColor);
						ChatManager.DrawColorCodedStringWithShadow(
							spriteBatch, font, currentAugment.DisplayName, new Vector2(x + flame.Width + 8f, y + 2f), nameColor, 0f, Vector2.Zero, new Vector2(0.95f)
						);
						y += Math.Max(flame.Height, ChatManager.GetStringSize(font, currentAugment.DisplayName, new Vector2(0.95f)).Y) + 4f;
					}
					else
					{
						ChatManager.DrawColorCodedStringWithShadow(
							spriteBatch, font, currentAugment.DisplayName, new Vector2(x, y), nameColor, 0f, Vector2.Zero, new Vector2(0.95f)
						);
						y += ChatManager.GetStringSize(font, currentAugment.DisplayName, new Vector2(0.95f)).Y + 3f;
					}
				}
				else
				{
					ChatManager.DrawColorCodedStringWithShadow(
						spriteBatch, font, currentAugment.DisplayName, new Vector2(x, y), nameColor, 0f, Vector2.Zero, new Vector2(0.95f)
					);
					y += ChatManager.GetStringSize(font, currentAugment.DisplayName, new Vector2(0.95f)).Y + 3f;
				}

				// 2. Rarity Tier & Class
				string tierClassText = $"[{currentAugment.Rarity} Tier]  {currentAugment.Class}";
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, font, tierClassText, new Vector2(x, y), Color.LightGray, 0f, Vector2.Zero, new Vector2(0.78f)
				);
				y += ChatManager.GetStringSize(font, tierClassText, new Vector2(0.78f)).Y + 4f;

				// 3. Ownership Status
				string statusText = currentIsOwned ? "[ Equipped ]" : "[ Not Equipped ]";
				Color statusColor = currentIsOwned ? new Color(100, 255, 120) : new Color(140, 140, 150);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, font, statusText, new Vector2(x, y), statusColor, 0f, Vector2.Zero, new Vector2(0.78f)
				);
				y += ChatManager.GetStringSize(font, statusText, new Vector2(0.78f)).Y + 8f;

				// 4. Subtle Divider Line
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)x, (int)y, (int)maxTextWidth, 1), new Color(45, 60, 105));
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
