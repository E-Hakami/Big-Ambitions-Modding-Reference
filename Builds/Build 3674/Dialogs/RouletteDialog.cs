using System.Collections;
using System.Collections.Generic;
using BigAmbitions.SoundSystem;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using UI.Dialog;
using UI.Notification;
using UnityEngine;

namespace Dialogs;

public class RouletteDialog : Dialog
{
	public class RouletteSlot
	{
		public RouletteColor color;

		public int number;
	}

	public enum RouletteColor
	{
		Red,
		Black
	}

	private const float PlayRouletteTime = 3f;

	private const float PlayRouletteTimeDelay = 0.25f;

	private const float PrizeMultiplier = 34f;

	private const float AnyNumberPrizeMultiplier = 1.75f;

	private readonly DialogController _dialogController;

	private readonly CasinoGameController _casinoGameController;

	private float _betAmount;

	private RouletteColor _betColor;

	private int _betNumber;

	public static RouletteSlot[] slots = new RouletteSlot[36]
	{
		new RouletteSlot
		{
			color = RouletteColor.Red,
			number = 1
		},
		new RouletteSlot
		{
			color = RouletteColor.Black,
			number = 2
		},
		new RouletteSlot
		{
			color = RouletteColor.Red,
			number = 3
		},
		new RouletteSlot
		{
			color = RouletteColor.Black,
			number = 4
		},
		new RouletteSlot
		{
			color = RouletteColor.Red,
			number = 5
		},
		new RouletteSlot
		{
			color = RouletteColor.Black,
			number = 6
		},
		new RouletteSlot
		{
			color = RouletteColor.Red,
			number = 7
		},
		new RouletteSlot
		{
			color = RouletteColor.Black,
			number = 8
		},
		new RouletteSlot
		{
			color = RouletteColor.Red,
			number = 9
		},
		new RouletteSlot
		{
			color = RouletteColor.Black,
			number = 10
		},
		new RouletteSlot
		{
			color = RouletteColor.Black,
			number = 11
		},
		new RouletteSlot
		{
			color = RouletteColor.Red,
			number = 12
		},
		new RouletteSlot
		{
			color = RouletteColor.Black,
			number = 13
		},
		new RouletteSlot
		{
			color = RouletteColor.Red,
			number = 14
		},
		new RouletteSlot
		{
			color = RouletteColor.Black,
			number = 15
		},
		new RouletteSlot
		{
			color = RouletteColor.Red,
			number = 16
		},
		new RouletteSlot
		{
			color = RouletteColor.Black,
			number = 17
		},
		new RouletteSlot
		{
			color = RouletteColor.Red,
			number = 18
		},
		new RouletteSlot
		{
			color = RouletteColor.Red,
			number = 19
		},
		new RouletteSlot
		{
			color = RouletteColor.Black,
			number = 20
		},
		new RouletteSlot
		{
			color = RouletteColor.Red,
			number = 21
		},
		new RouletteSlot
		{
			color = RouletteColor.Black,
			number = 22
		},
		new RouletteSlot
		{
			color = RouletteColor.Red,
			number = 23
		},
		new RouletteSlot
		{
			color = RouletteColor.Black,
			number = 24
		},
		new RouletteSlot
		{
			color = RouletteColor.Red,
			number = 25
		},
		new RouletteSlot
		{
			color = RouletteColor.Black,
			number = 26
		},
		new RouletteSlot
		{
			color = RouletteColor.Red,
			number = 27
		},
		new RouletteSlot
		{
			color = RouletteColor.Black,
			number = 28
		},
		new RouletteSlot
		{
			color = RouletteColor.Black,
			number = 29
		},
		new RouletteSlot
		{
			color = RouletteColor.Red,
			number = 30
		},
		new RouletteSlot
		{
			color = RouletteColor.Black,
			number = 31
		},
		new RouletteSlot
		{
			color = RouletteColor.Red,
			number = 32
		},
		new RouletteSlot
		{
			color = RouletteColor.Black,
			number = 33
		},
		new RouletteSlot
		{
			color = RouletteColor.Red,
			number = 34
		},
		new RouletteSlot
		{
			color = RouletteColor.Black,
			number = 35
		},
		new RouletteSlot
		{
			color = RouletteColor.Red,
			number = 36
		}
	};

	public RouletteDialog()
	{
		_dialogController = DialogController.current;
		npcNameKey = "ba:itemname_casinoroulettetable";
		_casinoGameController = (CasinoGameController)InstanceBehavior<GameManager>.Instance.playerController.Character.CurrentEntityController;
		_casinoGameController.ShowPlayingAnimation();
		_dialogController.ShowEntry(Start());
	}

	private DialogEntry Start()
	{
		return new DialogEntry
		{
			headerKey = npcNameKey,
			messageData = "dialog_roulette_start".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = delegate
			{
				_dialogController.ShowEntry(AskForBet());
			},
			OnForcedCancel = ReleaseRoulette
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
			OnForcedCancel = ReleaseRoulette
		};
	}

	private DialogEntry BetInput()
	{
		return new DialogEntry
		{
			Template = DialogEntry.TemplateType.Input,
			InputTemplate = DialogEntry.InputTemplateName.RouletteBetSettings,
			ConfirmTextOverride = "dialog_bet_button".Localize(),
			OnConfirm = Bet,
			OnCancel = OnCancel,
			OnForcedCancel = ReleaseRoulette
		};
	}

	private DialogEntry Bet()
	{
		if (InstanceBehavior<CasinoBoatManager>.Instance.kickOutSequenceStarted)
		{
			return null;
		}
		RouletteBetSettings inputComponent = _dialogController.GetInputComponent<RouletteBetSettings>();
		if (!inputComponent.hasColorSelected)
		{
			Notifications.ShowError("common_notification_select_color", "common_notification_select_color");
			return null;
		}
		if (inputComponent.selectedNumber == -1)
		{
			Notifications.ShowError("common_notification_select_number", "common_notification_select_number");
			return null;
		}
		if (!float.TryParse(inputComponent.amountInput.text, out _betAmount) || _betAmount <= 0f)
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
		_betColor = inputComponent.selectedColor;
		_betNumber = inputComponent.selectedNumber;
		_casinoGameController.casinoGameEmployeeController.PlayDealAnimationDelayed(1f);
		return new DialogEntry
		{
			messageData = "dialog_bet_set_dealer".Localize(),
			OnVisible = delegate
			{
				_dialogController.ShowEntry(StartRoulette());
			},
			OnForcedCancel = OnForcedCancelWhilePlayingRoulette
		};
	}

	private DialogEntry StartRoulette()
	{
		return new DialogEntry
		{
			Template = DialogEntry.TemplateType.Input,
			InputTemplate = DialogEntry.InputTemplateName.Roulette,
			OnVisible = OnVisibleStartRoulette,
			OnForcedCancel = OnForcedCancelWhilePlayingRoulette
		};
	}

	private void OnVisibleStartRoulette()
	{
		if (!InstanceBehavior<CasinoBoatManager>.Instance.kickOutSequenceStarted)
		{
			_casinoGameController.StartCoroutine(PlayRoulette());
		}
	}

	private void OnForcedCancelWhilePlayingRoulette()
	{
		_casinoGameController.StopAllCoroutines();
		ReturnBet();
		ReleaseRoulette();
	}

	private void ReturnBet()
	{
		Dictionary<string, string> data = new Dictionary<string, string> { { "element", npcNameKey } };
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_casino", "ba:transactioncategory_casino", data);
		GameManager.ChangeMoneySafe(_betAmount, transactionInfo);
	}

	private DialogEntry ShowWhereTheBallLanded(string color, int number)
	{
		return new DialogEntry
		{
			messageData = "dialog_roulette_ball_land_position".Localize(new
			{
				color = color,
				number = $"{number}"
			}),
			Template = DialogEntry.TemplateType.Text,
			OnForcedCancel = OnForcedCancelWhilePlayingRoulette
		};
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
			OnForcedCancel = ReleaseRoulette
		};
	}

	private IEnumerator PlayRoulette()
	{
		InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.RouletteStart, _casinoGameController.transform.position);
		_dialogController.GetInputComponent<RouletteUI>().Spin(3f, IsActiveDialog);
		yield return new WaitForSeconds(3.25f);
		if (IsActiveDialog())
		{
			RouletteColor rouletteColor = ((Random.Range(0, 2) == 0) ? RouletteColor.Black : RouletteColor.Red);
			int random = GetNumbersOnColor(rouletteColor).GetRandom();
			bool num = rouletteColor == _betColor;
			bool flag = random == _betNumber || _betNumber == 0;
			_dialogController.ShowEntry(ShowWhereTheBallLanded((rouletteColor == RouletteColor.Red) ? "colors_red" : "colors_black", random));
			float num2;
			LanguageChangeEventDataHolder data;
			if (num & flag)
			{
				num2 = ((_betNumber != 0) ? (_betAmount * 34f) : (_betAmount * 1.75f));
				data = "dialog_casino_regular_prize_dialog".Localize(new
				{
					prize = num2.ToShortCurrencyFormat()
				});
				_casinoGameController.ShowVictoryAnimation();
			}
			else
			{
				data = "dialog_casino_no_prize_dialog".Localize();
				_casinoGameController.ShowDefeatAnimation();
				num2 = 0f;
			}
			if (num2 > 0f)
			{
				Dictionary<string, string> data2 = new Dictionary<string, string> { { "element", npcNameKey } };
				TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_casino", "ba:transactioncategory_casino", data2);
				GameManager.ChangeMoneySafe(num2, transactionInfo);
			}
			_dialogController.ShowEntry(Results(data));
		}
	}

	public static List<int> GetNumbersOnColor(RouletteColor color)
	{
		List<int> list = new List<int>(slots.Length / 2);
		RouletteSlot[] array = slots;
		foreach (RouletteSlot rouletteSlot in array)
		{
			if (rouletteSlot.color == color)
			{
				list.Add(rouletteSlot.number);
			}
		}
		return list;
	}

	private void OnCancel()
	{
		_dialogController.CancelDialog();
		ReleaseRoulette();
	}

	private bool IsActiveDialog()
	{
		if ((bool)_dialogController)
		{
			return _dialogController.dialog == this;
		}
		return false;
	}

	private void ReleaseRoulette()
	{
		_casinoGameController.OnExitGame();
	}
}
