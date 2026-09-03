using System.Collections.Generic;
using Entities;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Dialog;
using UI.Notification;

namespace Dialogs;

public class BlackjackDialog : Dialog
{
	public const float PrizeMultiplier = 1f;

	public const float BlackjackMultiplier = 1.5f;

	private readonly DialogController _dialogController;

	private readonly CasinoGameController _casinoGameController;

	private float _betAmount;

	public BlackjackDialog()
	{
		_dialogController = DialogController.current;
		npcNameKey = "ba:itemname_casinoblackjacktable";
		_dialogController.ShowEntry(Start());
		_casinoGameController = (CasinoGameController)InstanceBehavior<GameManager>.Instance.playerController.Character.CurrentEntityController;
		_casinoGameController.ShowPlayingAnimation();
	}

	private DialogEntry Start()
	{
		return new DialogEntry
		{
			headerKey = npcNameKey,
			messageData = "dialog_blackjack_start".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = delegate
			{
				_dialogController.ShowEntry(AskForBet());
			},
			OnForcedCancel = ReleaseBlackjack
		};
	}

	private DialogEntry AskForBet()
	{
		return new DialogEntry
		{
			messageData = "dialog_bet_question_dealer".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = delegate
			{
				_dialogController.ShowEntry(BetInput());
			},
			OnForcedCancel = ReleaseBlackjack
		};
	}

	private DialogEntry BetInput()
	{
		return new DialogEntry
		{
			Template = DialogEntry.TemplateType.Input,
			InputTemplate = DialogEntry.InputTemplateName.BlackjackBetSettings,
			ConfirmTextOverride = "dialog_bet_button".Localize(),
			OnConfirm = Bet,
			OnCancel = OnCancel,
			OnForcedCancel = ReleaseBlackjack
		};
	}

	private DialogEntry Bet()
	{
		if (InstanceBehavior<CasinoBoatManager>.Instance.kickOutSequenceStarted)
		{
			return null;
		}
		if (!float.TryParse(_dialogController.GetInputTransform<TMP_InputField>("Amount").text, out _betAmount) || _betAmount <= 0f)
		{
			Notifications.ShowError("common_notification_invalid_amount", "common_notification_invalid_amount");
			return null;
		}
		Dictionary<string, string> data = new Dictionary<string, string> { { "element", npcNameKey } };
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_casino", "ba:transactioncategory_casino", data);
		if (!GameManager.ChangeMoneySafe(0f - _betAmount, transactionInfo, null, null, force: false, showNotification: true))
		{
			return null;
		}
		_casinoGameController.casinoGameEmployeeController.PlayDealAnimationDelayed(1f);
		return new DialogEntry
		{
			messageData = "dialog_bet_set_dealer".Localize(),
			OnVisible = delegate
			{
				_dialogController.ShowEntry(StartBlackjack());
			},
			OnForcedCancel = OnForcedCancelWhilePlayingBlackjack
		};
	}

	private DialogEntry StartBlackjack()
	{
		return new DialogEntry
		{
			Template = DialogEntry.TemplateType.Input,
			InputTemplate = DialogEntry.InputTemplateName.Blackjack,
			OnVisible = OnVisibleStartBlackjack,
			OnForcedCancel = OnForcedCancelWhilePlayingBlackjack
		};
	}

	private void OnVisibleStartBlackjack()
	{
		if (!InstanceBehavior<CasinoBoatManager>.Instance.kickOutSequenceStarted)
		{
			_dialogController.GetInputComponent<BlackjackUI>().PlayBlackjack(delegate(LanguageChangeEventDataHolder results)
			{
				_dialogController.ShowEntry(Results(results));
			}, _betAmount, _casinoGameController, IsActiveDialog);
		}
	}

	private void OnForcedCancelWhilePlayingBlackjack()
	{
		_dialogController.GetInputComponent<BlackjackUI>()?.StopAllCoroutines();
		ReturnBet();
		ReleaseBlackjack();
	}

	private void ReturnBet()
	{
		Dictionary<string, string> data = new Dictionary<string, string> { { "element", npcNameKey } };
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_casino", "ba:transactioncategory_casino", data);
		GameManager.ChangeMoneySafe(_betAmount, transactionInfo);
	}

	private DialogEntry Results(LanguageChangeEventDataHolder data)
	{
		HappinessHelper.AddModifier(_casinoGameController.GambledBalanceConfig.FinalType, _casinoGameController.GambledBalanceConfig.GetBoostHours(0));
		return new DialogEntry
		{
			messageData = data,
			Template = DialogEntry.TemplateType.Text,
			OnVisible = delegate
			{
				_dialogController.ShowEntry(AskForBet());
			},
			OnForcedCancel = ReleaseBlackjack
		};
	}

	private void OnCancel()
	{
		_dialogController.CancelDialog();
		ReleaseBlackjack();
	}

	private bool IsActiveDialog()
	{
		if (_dialogController != null)
		{
			return _dialogController.dialog == this;
		}
		return false;
	}

	private void ReleaseBlackjack()
	{
		_casinoGameController.OnExitGame();
	}
}
