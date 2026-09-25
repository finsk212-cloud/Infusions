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

namespace Augments
{
	public class AugmentChoiceUIState : UIState
	{
		private UIPanel backPanel;
		private readonly List<AugmentChoiceCard> cards = new List<AugmentChoiceCard>();
		private readonly HashSet<string> currentChoiceIds = new HashSet<string>();
		private RerollButton rerollButton;
		private RerollButton skipButton;
		private UIText capNoticeText;
		private ModalButton minimizeButton;
		private RestoreIcon restoreIcon;
		private bool isMinimized;
		public bool IsMinimized => isMinimized;

		private static readonly SoundStyle ChipInstallSound = new SoundStyle("Augments/Sounds/ChipInstall")
		{
			Volume = 0.90f,
			PitchVariance = 0.05f
		};

		// Rarity tier this popup's cards were rolled at - a reroll must stay
		// on this same tier, not re-roll a fresh (possibly different) one.
		private AugmentRarity currentRarity;

		// Exactly one reroll per popup, no matter how much Essence is
		// stockpiled - reset to false only when a fresh popup is shown.
		private bool rerollUsed;
		private bool rerollPending;
		private bool networkReward;

		// --- Keystone confirmation overlay ---
		// Appended/removed from `this` (the root UIState, not backPanel) so it
		// draws on top of and blocks clicks to everything behind it - cards,
		// reroll button, all of it - without needing a whole new interface
		// layer. It's still just part of the same "Augments: Choice UI" layer
		// AugmentUISystem already draws this state through.
		private UIPanel confirmOverlay;
		private UIText confirmMessageText;
		private Augment pendingKeystone;
		private bool pendingSkipConfirm;

		private const float PanelWidth = 880f;
		private const float CardWidth = 268f;
		private const float CardSpacing = 20f;
		private const float CardsTop = 104f;
		private const float BottomMargin = 18f;

		// Dedicated strip below the cards for the reroll and skip buttons
		private const float RerollGap = 16f;
		private const float RerollButtonHeight = 32f;

		private const float ConfirmBoxWidth = 520f;
		private const float ConfirmBoxHeight = 260f;
		private const float ConfirmButtonWidth = 180f;
		private const float ConfirmButtonHeight = 36f;

		public override void OnInitialize()
		{
			backPanel = new ChoiceBackPanel();
			backPanel.Width.Set(PanelWidth, 0f);
			backPanel.Height.Set(CardsTop + AugmentChoiceCard.MinCardHeight + RerollGap + RerollButtonHeight + BottomMargin, 0f);
			backPanel.HAlign = 0.5f;
			backPanel.VAlign = 0.5f;
			backPanel.SetPadding(0f);
			backPanel.BackgroundColor = new Color(16, 22, 44);
			backPanel.BorderColor = new Color(38, 52, 98);

			UIText titleText = new UIText("CHOOSE A PLUGIN", 1.22f)
			{
				HAlign = 0.5f,
				TextColor = new Color(255, 225, 150)
			};
			titleText.Top.Set(16f, 0f);
			backPanel.Append(titleText);

			UIText subtitle = new UIText("Select a plugin to install into your neural frame", 0.76f)
			{
				HAlign = 0.5f,
				TextColor = new Color(150, 170, 205)
			};
			subtitle.Top.Set(44f, 0f);
			backPanel.Append(subtitle);

			capNoticeText = new UIText("", 0.76f)
			{
				HAlign = 0.5f,
				TextColor = new Color(255, 95, 95)
			};
			capNoticeText.Top.Set(68f, 0f);
			backPanel.Append(capNoticeText);

			// Minimize Button (ends at 862px, exact 18px buffer from right edge)
			minimizeButton = new ModalButton("-", new Color(45, 52, 75), new Color(75, 88, 125));
			minimizeButton.Width.Set(24f, 0f);
			minimizeButton.Height.Set(24f, 0f);
			minimizeButton.Left.Set(838f, 0f);
			minimizeButton.Top.Set(10f, 0f);
			minimizeButton.Clicked += HandleMinimizeClicked;
			backPanel.Append(minimizeButton);

			// Reroll and Skip sit side by side on the same row, centered as a pair
			float rerollRowTop = CardsTop + AugmentChoiceCard.MinCardHeight + RerollGap;
			const float buttonGap = 16f;
			const float rerollWidth = 200f;
			const float skipWidth = 140f;
			float pairWidth = rerollWidth + buttonGap + skipWidth;

			rerollButton = new RerollButton();
			rerollButton.Width.Set(rerollWidth, 0f);
			rerollButton.Height.Set(RerollButtonHeight, 0f);
			rerollButton.Left.Set(-pairWidth / 2f, 0.5f);
			rerollButton.Top.Set(rerollRowTop, 0f);
			rerollButton.Clicked += HandleRerollClicked;
			backPanel.Append(rerollButton);

			skipButton = new RerollButton(new Color(90, 28, 28), new Color(140, 42, 42));
			skipButton.Width.Set(skipWidth, 0f);
			skipButton.Height.Set(RerollButtonHeight, 0f);
			skipButton.Left.Set(-pairWidth / 2f + rerollWidth + buttonGap, 0.5f);
			skipButton.Top.Set(rerollRowTop, 0f);
			skipButton.SetEnabled(true, "Skip");
			skipButton.Clicked += HandleSkipClicked;
			backPanel.Append(skipButton);

			Append(backPanel);

			BuildConfirmOverlay();
			BuildRestoreIcon();
		}

		// Built once and kept around, but only appended to the root UIState
		// while the panel is minimized - the underlying choices/reroll state
		// live on regardless of whether backPanel or restoreIcon is showing.
		private void BuildRestoreIcon()
		{
			restoreIcon = new RestoreIcon();
			restoreIcon.Width.Set(48f, 0f);
			restoreIcon.Height.Set(48f, 0f);
			restoreIcon.HAlign = 0.5f;
			restoreIcon.VAlign = 0.85f;
			restoreIcon.Clicked += HandleRestoreClicked;
		}

		// Built once and kept around, but only appended to the root UIState
		// (and so only drawn/clickable) while a Keystone pick is actually
		// pending confirmation.
		private void BuildConfirmOverlay()
		{
			confirmOverlay = new UIPanel();
			confirmOverlay.Width.Set(0f, 1f);
			confirmOverlay.Height.Set(0f, 1f);
			confirmOverlay.SetPadding(0f);
			confirmOverlay.BackgroundColor = Color.Black * 0.6f;
			confirmOverlay.BorderColor = Color.Transparent;

			var confirmBox = new UIPanel();
			confirmBox.Width.Set(ConfirmBoxWidth, 0f);
			confirmBox.Height.Set(ConfirmBoxHeight, 0f);
			confirmBox.HAlign = 0.5f;
			confirmBox.VAlign = 0.5f;
			confirmBox.BackgroundColor = new Color(33, 43, 79) * 0.95f;
			confirmBox.BorderColor = new Color(220, 60, 60);
			confirmOverlay.Append(confirmBox);

			UIText confirmTitleText = new UIText("Confirm", 1.05f)
			{
				HAlign = 0.5f,
				TextColor = new Color(220, 60, 60)
			};
			confirmTitleText.Top.Set(14f, 0f);
			confirmBox.Append(confirmTitleText);

			confirmMessageText = new UIText("", 0.8f)
			{
				HAlign = 0.5f,
				IsWrapped = true
			};
			confirmMessageText.Width.Set(-30f, 1f);
			confirmMessageText.Height.Set(120f, 0f);
			confirmMessageText.Top.Set(50f, 0f);
			confirmBox.Append(confirmMessageText);

			var confirmButton = new ModalButton("Confirm", new Color(110, 40, 40), new Color(160, 60, 60));
			confirmButton.Width.Set(ConfirmButtonWidth, 0f);
			confirmButton.Height.Set(ConfirmButtonHeight, 0f);
			confirmButton.HAlign = 0.25f;
			confirmButton.VAlign = 1f;
			confirmButton.Top.Set(-16f, 0f);
			confirmButton.Clicked += HandleConfirmOverlayConfirmed;
			confirmBox.Append(confirmButton);

			var cancelButton = new ModalButton("Cancel", new Color(60, 70, 110), new Color(90, 105, 160));
			cancelButton.Width.Set(ConfirmButtonWidth, 0f);
			cancelButton.Height.Set(ConfirmButtonHeight, 0f);
			cancelButton.HAlign = 0.75f;
			cancelButton.VAlign = 1f;
			cancelButton.Top.Set(-16f, 0f);
			cancelButton.Clicked += HandleConfirmOverlayCanceled;
			confirmBox.Append(cancelButton);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			RefreshRerollButton();

			if (!isMinimized)
			{
				Main.LocalPlayer.mouseInterface = true;
			}
			else if (restoreIcon != null && restoreIcon.ContainsPoint(Main.MouseScreen))
			{
				Main.LocalPlayer.mouseInterface = true;
			}
		}

		public bool IsMouseOverRestore => isMinimized && restoreIcon != null && restoreIcon.ContainsPoint(Main.MouseScreen);

		private RarityBracket currentBracket;

		// Call this right before showing the panel - replaces whatever cards
		// were there with a fresh set built from the given augments, and resets
		// this popup's reroll allowance.
		public void SetChoices(List<Augment> choices, AugmentRarity rarity, RarityBracket bracket = RarityBracket.PreHardmode, bool networkReward = false, bool rerolled = false)
		{
			currentRarity = rarity;
			currentBracket = bracket;
			rerollUsed = rerolled;
			rerollPending = false;
			this.networkReward = networkReward;
			pendingSkipConfirm = false;
			HideKeystoneConfirm();

			// Fresh popup always starts un-minimized, regardless of how the
			// previous one was left.
			isMinimized = false;
			RemoveChild(restoreIcon);
			if (backPanel.Parent == null)
				Append(backPanel);

			RebuildCards(choices);
			RefreshCapNotice();
			RefreshRerollButton();
		}

		private void RebuildCards(List<Augment> choices)
		{
			foreach (var card in cards)
				backPanel.RemoveChild(card);
			cards.Clear();
			currentChoiceIds.Clear();

			int count = choices.Count;
			float totalWidth = count * CardWidth + (count - 1) * CardSpacing;
			float startX = (PanelWidth - totalWidth) / 2f;

			// Dynamically determine the maximum required height across all cards in this roll
			float maxRequired = AugmentChoiceCard.MinCardHeight;
			for (int i = 0; i < count; i++)
			{
				float reqH = AugmentChoiceCard.CalculateRequiredHeight(choices[i], CardWidth);
				if (reqH > maxRequired)
					maxRequired = reqH;
			}

			float cardHeight = maxRequired;
			float desiredPanelHeight = CardsTop + cardHeight + RerollGap + RerollButtonHeight + BottomMargin;

			// Responsive clamp for 720p / high UI scale
			float maxAllowedHeight = Math.Max(480f, Main.screenHeight - 36f);
			float finalPanelHeight = Math.Min(desiredPanelHeight, maxAllowedHeight);
			backPanel.Height.Set(finalPanelHeight, 0f);

			// Position Reroll and Skip buttons dynamically beneath the cards
			float rerollRowTop = CardsTop + cardHeight + RerollGap;
			if (rerollRowTop + RerollButtonHeight + BottomMargin > finalPanelHeight)
				rerollRowTop = finalPanelHeight - BottomMargin - RerollButtonHeight;

			rerollButton.Top.Set(rerollRowTop, 0f);
			skipButton.Top.Set(rerollRowTop, 0f);

			for (int i = 0; i < count; i++)
			{
				var card = new AugmentChoiceCard(choices[i], CardWidth, cardHeight, i);
				card.Left.Set(startX + i * (CardWidth + CardSpacing), 0f);
				card.Top.Set(CardsTop, 0f);
				card.OnAugmentChosen += HandleAugmentChosen;

				cards.Add(card);
				currentChoiceIds.Add(choices[i].Id);
				backPanel.Append(card);
			}
		}

		// Keystone picks need confirmation first (see ShowKeystoneConfirm) since
		// they permanently lock their family's siblings out of every future
		// popup - everything else still grants immediately, exactly as before.
		private void HandleAugmentChosen(Augment augment)
		{
			if (augment.KeystoneFamily != null)
				ShowKeystoneConfirm(augment);
			else
				GrantAndClose(augment);
		}

		private void ShowKeystoneConfirm(Augment augment)
		{
			pendingKeystone = augment;

			var siblingNames = new List<string>();
			foreach (var other in AugmentDatabase.All)
			{
				if (other.KeystoneFamily == augment.KeystoneFamily && other.Id != augment.Id)
					siblingNames.Add(other.DisplayName);
			}

			confirmMessageText.SetText(
				$"Choosing {augment.DisplayName} will permanently exclude {JoinWithAnd(siblingNames)} " +
				"from ever being offered to you again. This cannot be undone.");

			Append(confirmOverlay);
		}

		private void HideKeystoneConfirm()
		{
			pendingKeystone = null;
			RemoveChild(confirmOverlay);
		}

		private void HandleSkipClicked()
		{
			SoundEngine.PlaySound(SoundID.MenuTick);
			pendingSkipConfirm = true;
			pendingKeystone = null;

			confirmMessageText.SetText(
				"Are you sure you want to skip this plugin selection? " +
				"This reward will be forfeited and cannot be recovered.");

			Append(confirmOverlay);
		}

		private void HandleConfirmOverlayConfirmed()
		{
			if (pendingSkipConfirm)
			{
				pendingSkipConfirm = false;
				RemoveChild(confirmOverlay);
				SoundEngine.PlaySound(SoundID.MenuClose);
				ModContent.GetInstance<AugmentUISystem>().HidePanel();
				return;
			}

			if (pendingKeystone == null)
				return;

			GrantAndClose(pendingKeystone);
		}

		// Dismisses the confirmation and returns to the original 3-card
		// choice without granting anything - the cards underneath were never
		// touched, so a different card can still be picked instead.
		private void HandleConfirmOverlayCanceled()
		{
			SoundEngine.PlaySound(SoundID.MenuClose);
			pendingSkipConfirm = false;
			HideKeystoneConfirm();
		}

		private void HandleMinimizeClicked()
		{
			// Don't allow minimizing while a Keystone/Skip confirmation is up -
			// it's modal and meant to block interaction with everything behind
			// it, so swapping the panel out from under it would look broken.
			if (confirmOverlay.Parent != null)
				return;

			SoundEngine.PlaySound(SoundID.MenuClose);
			isMinimized = true;
			RemoveChild(backPanel);
			Append(restoreIcon);
		}

		private void HandleRestoreClicked()
		{
			SoundEngine.PlaySound(SoundID.MenuOpen);
			isMinimized = false;
			RemoveChild(restoreIcon);
			Append(backPanel);
		}

		private void GrantAndClose(Augment augment)
		{
			var player = Main.LocalPlayer;
			var ap = player.GetModPlayer<AugmentPlayer>();

			ap.ChooseReward(augment);

			pendingKeystone = null;
			RemoveChild(confirmOverlay);
			SoundEngine.PlaySound(ChipInstallSound);
			ModContent.GetInstance<AugmentUISystem>().HidePanel();
		}

		private void RefreshCapNotice()
		{
			var ap = Main.LocalPlayer.GetModPlayer<AugmentPlayer>();
			capNoticeText.SetText(ap.Owned.Count >= AugmentPlayer.MaxOwnedAugments
				? "⚠ NOTICE: Neural Frame full (5/5) — Selection will be archived at Mistress 2B & dismantled for Machine Cores."
				: "");
		}

		private static string JoinWithAnd(List<string> names)
		{
			if (names.Count == 0)
				return "";
			if (names.Count == 1)
				return names[0];

			return string.Join(", ", names.GetRange(0, names.Count - 1)) + " and " + names[names.Count - 1];
		}

		private void HandleRerollClicked()
		{
			if (rerollPending)
				return;

			var player = Main.LocalPlayer;
			int essenceType = ModContent.ItemType<AugmentEssenceItem>();

			if (rerollUsed)
			{
				if (player.CountItem(essenceType, 1) < 1)
				{
					Main.NewText("Not enough Machine Cores to reroll.", 255, 80, 80);
					SoundEngine.PlaySound(SoundID.MenuClose);
					RefreshRerollButton();
					return;
				}
			}

			if (networkReward)
			{
				rerollPending = true;
				SoundEngine.PlaySound(SoundID.Item37);
				AugmentNet.SendRerollRequest(player);
				RefreshRerollButton();
				return;
			}

			var augmentPlayer = player.GetModPlayer<AugmentPlayer>();
			if (!AugmentRewardLogic.TryRollRewardChoices(augmentPlayer, currentBracket, currentChoiceIds, out List<Augment> newChoices, out AugmentRarity newRarity))
			{
				Main.NewText("No other plugins are available.", 255, 100, 100);
				RefreshRerollButton();
				return;
			}

			if (rerollUsed)
			{
				player.ConsumeItem(essenceType);
			}
			else
			{
				rerollUsed = true;
			}

			currentRarity = newRarity;
			SoundEngine.PlaySound(SoundID.Item37);
			RebuildCards(newChoices);
			RefreshRerollButton();
		}

		private void RefreshRerollButton()
		{
			if (rerollPending)
			{
				rerollButton.SetEnabled(false, "Rerolling...");
				return;
			}

			if (!rerollUsed)
			{
				rerollButton.SetEnabled(true, "Reroll (Free)");
				return;
			}

			int essenceType = ModContent.ItemType<AugmentEssenceItem>();
			int essenceCount = Main.LocalPlayer.CountItem(essenceType);

			if (essenceCount >= 1)
			{
				rerollButton.SetEnabled(true, "Reroll (1 Core)");
			}
			else
			{
				rerollButton.SetEnabled(false, "Need 1 Core");
			}
		}

		// Small standalone clickable panel - same manual hover/click approach
		// AugmentChoiceCard/AugmentShopEntry use, to match this mod's existing
		// UI button style. "Disabled" here is just a grey visual hint - the
		// real gating lives in AugmentChoiceUIState.HandleRerollClicked.
		private class RerollButton : UIPanel
		{
			public event Action Clicked;

			private static readonly Color DefaultIdleColor = new Color(20, 28, 54);
			private static readonly Color DefaultHoverColor = new Color(34, 48, 88);
			private static readonly Color DisabledColor = new Color(16, 20, 34);

			private readonly Color idleColor;
			private readonly Color hoverColor;
			private readonly UIText labelText;
			private bool enabledState = true;
			private bool isHovered = false;

			public RerollButton() : this(DefaultIdleColor, DefaultHoverColor)
			{
			}

			public RerollButton(Color idleColor, Color hoverColor)
			{
				this.idleColor = idleColor;
				this.hoverColor = hoverColor;

				SetPadding(0f);
				BackgroundColor = idleColor;
				BorderColor = new Color(50, 75, 130) * 0.8f;

				labelText = new UIText("Reroll (1 Core)", 0.82f)
				{
					HAlign = 0.5f,
					VAlign = 0.5f,
					TextColor = new Color(230, 235, 245)
				};
				Append(labelText);
			}

			public void SetEnabled(bool enabled, string label)
			{
				labelText.SetText(label);

				if (enabledState == enabled)
					return;

				enabledState = enabled;
				BackgroundColor = enabled ? (isHovered ? hoverColor : idleColor) : DisabledColor;
				BorderColor = enabled ? (isHovered ? new Color(100, 160, 255) : new Color(50, 75, 130) * 0.8f) : new Color(35, 45, 65) * 0.6f;
				labelText.TextColor = enabled ? (isHovered ? Color.White : new Color(230, 235, 245)) : new Color(110, 120, 140);
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
				isHovered = true;
				if (enabledState)
				{
					BackgroundColor = hoverColor;
					BorderColor = new Color(100, 160, 255);
					labelText.TextColor = Color.White;
					SoundEngine.PlaySound(SoundID.MenuTick);
				}
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				isHovered = false;
				if (enabledState)
				{
					BackgroundColor = idleColor;
					BorderColor = new Color(50, 75, 130) * 0.8f;
					labelText.TextColor = new Color(230, 235, 245);
				}
			}
		}

		// Generic small clickable panel for the keystone confirm overlay's
		// Confirm/Cancel buttons - same manual hover/click approach as
		// RerollButton, just without a disabled state since both buttons are
		// always actionable for as long as the overlay is shown.
		private class ModalButton : UIPanel
		{
			public event Action Clicked;

			private readonly Color idleColor;
			private readonly Color hoverColor;

			public ModalButton(string label, Color idleColor, Color hoverColor)
			{
				this.idleColor = idleColor;
				this.hoverColor = hoverColor;

				SetPadding(0f);
				BackgroundColor = idleColor;
				BorderColor = Color.White * 0.4f;

				UIText labelText = new UIText(label, 0.85f)
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
				BackgroundColor = hoverColor;
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				BackgroundColor = idleColor;
			}
		}

		// The minimized-state stand-in for backPanel - small, always-visible
		// reminder that a reward selection is still pending. Pulses a gold
		// glow using the same sine-based approach as AugmentChoiceCard's
		// rarity border pulse, just with a single fixed speed/strength
		// instead of per-rarity tiers.
		private class RestoreIcon : UIPanel
		{
			public event Action Clicked;

			private static readonly Color BaseBorderColor = new Color(180, 150, 60);
			private const float PulseSpeed = 2.2f;
			private const float PulseStrength = 0.4f;

			private float pulseTimer;

			public RestoreIcon()
			{
				SetPadding(0f);
				BackgroundColor = new Color(33, 43, 79) * 0.95f;
				BorderColor = BaseBorderColor;

				var label = new UIText("?", 1.1f) { HAlign = 0.5f, VAlign = 0.5f };
				Append(label);
			}

			public override void Update(GameTime gameTime)
			{
				base.Update(gameTime);
				pulseTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				base.DrawSelf(spriteBatch);
			}

			public override void MouseOver(UIMouseEvent evt)
			{
				base.MouseOver(evt);
				BorderColor = Color.White;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				BorderColor = BaseBorderColor;
			}

			public override void LeftClick(UIMouseEvent evt)
			{
				base.LeftClick(evt);
				Clicked?.Invoke();
			}
		}

		private class ChoiceBackPanel : UIPanel
		{
			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				base.DrawSelf(spriteBatch);

				CalculatedStyle dims = GetDimensions();

				// Header horizontal divider spanning X = 18f to X = 862f (exact 18px bilateral symmetry)
				int divY = (int)dims.Y + 86;
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)dims.X + 18, divY, (int)dims.Width - 36, 1), new Color(45, 62, 105) * 0.75f);

				// Center diamond node
				int midX = (int)dims.X + (int)(dims.Width * 0.5f);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(midX - 1, divY - 1, 3, 3), new Color(80, 160, 240) * 0.85f);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(midX, divY, 1, 1), Color.White * 0.9f);
			}
		}
	}
}
