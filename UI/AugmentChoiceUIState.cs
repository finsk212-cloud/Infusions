using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
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

		private const float PanelWidth = 860f;
		private const float CardWidth = 270f;
		private const float CardSpacing = 14f;
		private const float CardsTop = 68f;
		private const float BottomMargin = 24f;

		// Dedicated strip below the cards for the reroll button - kept as its
		// own gap + button height rather than folded into BottomMargin, so the
		// button always has clear, non-overlapping space under the card row.
		private const float RerollGap = 20f;
		private const float RerollButtonHeight = 34f;

		private const float ConfirmBoxWidth = 520f;
		private const float ConfirmBoxHeight = 260f;
		private const float ConfirmButtonWidth = 180f;
		private const float ConfirmButtonHeight = 36f;

		public override void OnInitialize()
		{
			backPanel = new UIPanel();
			backPanel.Width.Set(PanelWidth, 0f);
			// Driven by AugmentChoiceCard.MinCardHeight (the cards' actual fixed
			// height) rather than a separate hardcoded number, plus room for the
			// reroll strip below the cards.
			backPanel.Height.Set(CardsTop + AugmentChoiceCard.MinCardHeight + RerollGap + RerollButtonHeight + BottomMargin, 0f);
			backPanel.HAlign = 0.5f;
			backPanel.VAlign = 0.5f;
			backPanel.BackgroundColor = new Color(33, 43, 79) * 0.9f;

			UIText titleText = new UIText("Choose an augment", 1.2f)
			{
				HAlign = 0.5f,
				TextColor = new Color(255, 170, 40)
			};
			titleText.Top.Set(15f, 0f);
			backPanel.Append(titleText);

			capNoticeText = new UIText("", 0.8f)
			{
				HAlign = 0.5f,
				TextColor = AugmentTextColors.Cooldown
			};
			capNoticeText.Top.Set(42f, 0f);
			backPanel.Append(capNoticeText);

			// Reroll and Skip sit side by side on the same row, centered as a
			// pair with a small gap between them. Left is set in pixel terms
			// relative to the pair's own total width (not the 0.5f anchor
			// alone), since anchoring both buttons off 50% without accounting
			// for their widths made them overlap.
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

			skipButton = new RerollButton(new Color(120, 30, 30), new Color(170, 45, 45));
			skipButton.Width.Set(skipWidth, 0f);
			skipButton.Height.Set(RerollButtonHeight, 0f);
			skipButton.Left.Set(-pairWidth / 2f + rerollWidth + buttonGap, 0.5f);
			skipButton.Top.Set(rerollRowTop, 0f);
			skipButton.SetEnabled(true, "Skip");
			skipButton.Clicked += HandleSkipClicked;
			backPanel.Append(skipButton);

			minimizeButton = new ModalButton("-", new Color(60, 60, 70), new Color(90, 90, 105));
			minimizeButton.Width.Set(24f, 0f);
			minimizeButton.Height.Set(24f, 0f);
			minimizeButton.Left.Set(-30f, 1f);
			minimizeButton.Top.Set(6f, 0f);
			minimizeButton.Clicked += HandleMinimizeClicked;
			backPanel.Append(minimizeButton);

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
		}

		// Call this right before showing the panel - replaces whatever cards
		// were there with a fresh set built from the given augments, and resets
		// this popup's reroll allowance.
		public void SetChoices(List<Augment> choices, AugmentRarity rarity, bool networkReward = false, bool rerolled = false)
		{
			currentRarity = rarity;
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
			// Center against the panel's actual current inner width (post-padding),
			// not the raw PanelWidth constant - children are positioned relative to
			// GetInnerDimensions(), so centering against the outer width leaves the
			// row off-center by the panel's padding.
			float panelInnerWidth = backPanel.GetInnerDimensions().Width;
			float totalWidth = count * CardWidth + (count - 1) * CardSpacing;
			float startX = (panelInnerWidth - totalWidth) / 2f;

			for (int i = 0; i < count; i++)
			{
				var card = new AugmentChoiceCard(choices[i], CardWidth);
				card.Left.Set(startX + i * (CardWidth + CardSpacing), 0f);
				card.Top.Set(CardsTop, 0f);
				card.OnAugmentChosen += HandleAugmentChosen;

				backPanel.Append(card);
				cards.Add(card);
				currentChoiceIds.Add(choices[i].Id);
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
			pendingSkipConfirm = true;
			pendingKeystone = null;

			confirmMessageText.SetText(
				"Are you sure you want to skip this augment selection? " +
				"This reward will be forfeited and cannot be recovered.");

			Append(confirmOverlay);
		}

		private void HandleConfirmOverlayConfirmed()
		{
			if (pendingSkipConfirm)
			{
				pendingSkipConfirm = false;
				RemoveChild(confirmOverlay);
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

			isMinimized = true;
			RemoveChild(backPanel);
			Append(restoreIcon);
		}

		private void HandleRestoreClicked()
		{
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
			ModContent.GetInstance<AugmentUISystem>().HidePanel();
		}

		private void RefreshCapNotice()
		{
			var ap = Main.LocalPlayer.GetModPlayer<AugmentPlayer>();
			capNoticeText.SetText(ap.Owned.Count >= AugmentPlayer.MaxOwnedAugments
				? "Plug-in Chip slots full - selection will be transferred to Mistress 2B."
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
			// Hard gate - this is re-checked here regardless of the button's
			// own visual enabled state, since this bool is the entire point
			// of the feature and must hold no matter how many times the
			// button is clicked.
			if (rerollUsed || rerollPending)
				return;

			var player = Main.LocalPlayer;

			if (networkReward)
			{
				rerollPending = true;
				AugmentNet.SendRerollRequest(player);
				RefreshRerollButton();
				return;
			}

			rerollUsed = true;

			var newChoices = player.GetModPlayer<AugmentPlayer>().RollChoices(3, currentRarity, currentChoiceIds);
			if (newChoices.Count == 0)
			{
				rerollUsed = false;
				Main.NewText("No other plug-in chips are available at this rarity.", 255, 100, 100);
				RefreshRerollButton();
				return;
			}

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

			if (rerollUsed)
			{
				rerollButton.SetEnabled(false, "Reroll Used");
				return;
			}

			rerollButton.SetEnabled(true, "Reroll (Free)");
		}

		// Small standalone clickable panel - same manual hover/click approach
		// AugmentChoiceCard/AugmentShopEntry use, to match this mod's existing
		// UI button style. "Disabled" here is just a grey visual hint - the
		// real gating lives in AugmentChoiceUIState.HandleRerollClicked.
		private class RerollButton : UIPanel
		{
			public event Action Clicked;

			private static readonly Color DefaultIdleColor = new Color(60, 70, 110);
			private static readonly Color DefaultHoverColor = new Color(90, 105, 160);
			private static readonly Color DisabledColor = new Color(45, 45, 45);

			private readonly Color idleColor;
			private readonly Color hoverColor;
			private readonly UIText labelText;
			private bool enabledState = true;

			public RerollButton() : this(DefaultIdleColor, DefaultHoverColor)
			{
			}

			public RerollButton(Color idleColor, Color hoverColor)
			{
				this.idleColor = idleColor;
				this.hoverColor = hoverColor;

				SetPadding(0f);
				BackgroundColor = idleColor;
				BorderColor = Color.White * 0.4f;

				labelText = new UIText("Reroll (1 Essence)", 0.8f)
				{
					HAlign = 0.5f,
					VAlign = 0.5f
				};
				Append(labelText);
			}

			public void SetEnabled(bool enabled, string label)
			{
				labelText.SetText(label);

				if (enabledState == enabled)
					return;

				enabledState = enabled;
				BackgroundColor = enabled ? idleColor : DisabledColor;
			}

			public override void LeftClick(UIMouseEvent evt)
			{
				base.LeftClick(evt);
				// Always fires - HandleRerollClicked does the real rerollUsed/Essence
				// checks and shows the insufficient-funds message itself; the grey
				// styling here is just a visual hint, not the actual gate.
				Clicked?.Invoke();
			}

			public override void MouseOver(UIMouseEvent evt)
			{
				base.MouseOver(evt);
				if (enabledState)
					BackgroundColor = hoverColor;
			}

			public override void MouseOut(UIMouseEvent evt)
			{
				base.MouseOut(evt);
				if (enabledState)
					BackgroundColor = idleColor;
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
				float pulse = (float)Math.Sin(pulseTimer * PulseSpeed) * 0.5f + 0.5f;

				CalculatedStyle dims = GetDimensions();
				float glowSize = 4f + pulse * PulseStrength * 18f;
				Rectangle glowRect = new Rectangle(
					(int)(dims.X - glowSize),
					(int)(dims.Y - glowSize),
					(int)(dims.Width + glowSize * 2f),
					(int)(dims.Height + glowSize * 2f));

				spriteBatch.Draw(TextureAssets.MagicPixel.Value, glowRect, BaseBorderColor * (pulse * PulseStrength * 0.5f));
				BorderColor = Color.Lerp(BaseBorderColor, Color.White, pulse * PulseStrength);

				base.DrawSelf(spriteBatch);
			}

			public override void LeftClick(UIMouseEvent evt)
			{
				base.LeftClick(evt);
				Clicked?.Invoke();
			}
		}
	}
}
