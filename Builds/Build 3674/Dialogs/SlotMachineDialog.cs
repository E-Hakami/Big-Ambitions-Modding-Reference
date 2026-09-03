using System.Collections;
using System.Collections.Generic;
using BigAmbitions.SoundSystem;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using UI.Dialog;
using UnityEngine;

namespace Dialogs;

public class SlotMachineDialog : Dialog
{
	public enum SlotElement
	{
		DoubleDiamond,
		Seven,
		Cherry,
		Orange,
		Apple,
		BarSingle,
		BarDouble,
		BarTriple
	}

	private const float MachineRowDelay = 0.3f;

	private const float JackpotMultiplier = 800f;

	private const float FullSevenMultiplier = 80f;

	private const float FullTripleBarMultiplier = 40f;

	private const float FullDoubleBarMultiplier = 25f;

	private const float FullSingleBarMultiplier = 10f;

	private const float FullCherryMultiplier = 10f;

	private const float AnyBarMultiplier = 5f;

	private const float TwoCherriesMultiplier = 5f;

	private const float OneCherryMultiplier = 2f;

	private const float FullOrangeMultiplier = 1f;

	private const float FullAppleMultiplier = 1f;

	private readonly DialogController _dialogController;

	private readonly SlotMachineController _slotMachineController;

	public SlotMachineDialog()
	{
		_dialogController = DialogController.current;
		npcNameKey = "ba:itemname_casinoslotmachine";
		_slotMachineController = (SlotMachineController)InstanceBehavior<GameManager>.Instance.playerController.Character.CurrentEntityController;
		_dialogController.ShowEntry(Start());
	}

	private DialogEntry Start()
	{
		return new DialogEntry
		{
			headerKey = npcNameKey,
			messageData = "dialog_slot_machine_start".Localize(),
			Template = DialogEntry.TemplateType.Text,
			ConfirmTextOverride = "dialog_slot_machine_spin_the_wheel_button".Localize(new
			{
				price = _slotMachineController.startingBet.ToShortCurrencyFormat()
			}),
			OnConfirm = SpinTheWheels,
			OnCancel = OnCancel,
			OnForcedCancel = ReleaseSlotMachine
		};
	}

	private DialogEntry SpinTheWheels()
	{
		if (InstanceBehavior<CasinoBoatManager>.Instance.kickOutSequenceStarted)
		{
			return null;
		}
		Dictionary<string, string> data = new Dictionary<string, string> { { "element", npcNameKey } };
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_casino", "ba:transactioncategory_casino", data);
		if (!GameManager.ChangeMoneySafe(-_slotMachineController.startingBet, transactionInfo, null, null, force: false, showNotification: true))
		{
			return null;
		}
		return new DialogEntry
		{
			Template = DialogEntry.TemplateType.Input,
			InputTemplate = DialogEntry.InputTemplateName.SlotMachine,
			OnVisible = OnVisibleSpinTheWheels,
			OnForcedCancel = OnForcedCancelWhileSpinningTheWheels
		};
	}

	private void OnCancel()
	{
		_dialogController.CancelDialog();
		ReleaseSlotMachine();
	}

	private void OnVisibleSpinTheWheels()
	{
		if (!InstanceBehavior<CasinoBoatManager>.Instance.kickOutSequenceStarted)
		{
			_slotMachineController.StartCoroutine(Gamble());
		}
	}

	private void OnForcedCancelWhileSpinningTheWheels()
	{
		_slotMachineController.StopAllCoroutines();
		ReturnBet();
		ReleaseSlotMachine();
	}

	private void ReturnBet()
	{
		Dictionary<string, string> data = new Dictionary<string, string> { { "element", npcNameKey } };
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_casino", "ba:transactioncategory_casino", data);
		GameManager.ChangeMoneySafe(_slotMachineController.startingBet, transactionInfo);
	}

	private void ReleaseSlotMachine()
	{
		_slotMachineController.ReleaseSlotMachine();
	}

	private DialogEntry Results(LanguageChangeEventDataHolder data)
	{
		HappinessHelper.AddModifier(_slotMachineController.GambledBalanceConfig.FinalType, _slotMachineController.GambledBalanceConfig.GetBoostHours(0));
		return new DialogEntry
		{
			messageData = data,
			Template = DialogEntry.TemplateType.Text,
			OnVisible = delegate
			{
				_dialogController.ShowEntry(Start());
			},
			OnForcedCancel = ReleaseSlotMachine
		};
	}

	private IEnumerator Gamble()
	{
		SlotMachineUI slotMachineUI = _dialogController.GetInputComponent<SlotMachineUI>();
		InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.SlotMachineStart, _slotMachineController.transform.position);
		yield return new WaitForSeconds(0.3f);
		if (!IsActiveDialog())
		{
			yield break;
		}
		SlotElement firstRow = _slotMachineController.GetRandomElement(1);
		slotMachineUI.SetIcon(0, firstRow);
		yield return new WaitForSeconds(0.3f);
		if (!IsActiveDialog())
		{
			yield break;
		}
		SlotElement secondRow = _slotMachineController.GetRandomElement(2);
		slotMachineUI.SetIcon(1, secondRow);
		yield return new WaitForSeconds(0.3f);
		if (IsActiveDialog())
		{
			SlotElement randomElement = _slotMachineController.GetRandomElement(3);
			slotMachineUI.SetIcon(2, randomElement);
			bool flag = IsJackpot(firstRow, secondRow, randomElement);
			float prize = GetPrize(flag, firstRow, secondRow, randomElement);
			LanguageChangeEventDataHolder data;
			if (prize == 0f)
			{
				data = "dialog_casino_no_prize_dialog".Localize();
				InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.SlotMachineLoose, _slotMachineController.transform.position);
			}
			else if (flag)
			{
				data = "dialog_slot_machine_jackpot_prize".Localize(new
				{
					prize = prize.ToShortCurrencyFormat()
				});
				InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.SlotMachineJackpot, _slotMachineController.transform.position);
			}
			else
			{
				data = "dialog_casino_regular_prize_dialog".Localize(new
				{
					prize = prize.ToShortCurrencyFormat()
				});
				InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.SlotMachineWin, _slotMachineController.transform.position);
			}
			if (prize > 0f)
			{
				Dictionary<string, string> data2 = new Dictionary<string, string> { { "element", npcNameKey } };
				TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_casino", "ba:transactioncategory_casino", data2);
				GameManager.ChangeMoneySafe(prize, transactionInfo);
				_slotMachineController.ShowVictoryAnimation();
			}
			else
			{
				_slotMachineController.ShowDefeatAnimation();
			}
			_dialogController.ShowEntry(Results(data));
		}
	}

	private bool IsActiveDialog()
	{
		if (_dialogController != null)
		{
			return _dialogController.dialog == this;
		}
		return false;
	}

	private bool IsJackpot(SlotElement firstRow, SlotElement secondRow, SlotElement thirdRow)
	{
		if (firstRow == SlotElement.DoubleDiamond && secondRow == SlotElement.DoubleDiamond)
		{
			return thirdRow == SlotElement.DoubleDiamond;
		}
		return false;
	}

	private float GetPrize(bool jackpot, SlotElement first, SlotElement second, SlotElement third)
	{
		if (jackpot)
		{
			return (float)_slotMachineController.startingBet * 800f;
		}
		if (CountElements(first, second, third, SlotElement.Seven) == 3)
		{
			return (float)_slotMachineController.startingBet * 80f;
		}
		if (CountElements(first, second, third, SlotElement.BarTriple) == 3)
		{
			return (float)_slotMachineController.startingBet * 40f;
		}
		if (CountElements(first, second, third, SlotElement.BarDouble) == 3)
		{
			return (float)_slotMachineController.startingBet * 25f;
		}
		if (CountElements(first, second, third, SlotElement.BarSingle) == 3)
		{
			return (float)_slotMachineController.startingBet * 10f;
		}
		if (IsBar(first) && IsBar(second) && IsBar(third))
		{
			return (float)_slotMachineController.startingBet * 5f;
		}
		switch (CountElements(first, second, third, SlotElement.Cherry))
		{
		case 3:
			return (float)_slotMachineController.startingBet * 10f;
		case 2:
			return (float)_slotMachineController.startingBet * 5f;
		case 1:
			return (float)_slotMachineController.startingBet * 2f;
		default:
			if (CountElements(first, second, third, SlotElement.Orange) == 3)
			{
				return (float)_slotMachineController.startingBet * 1f;
			}
			if (CountElements(first, second, third, SlotElement.Apple) == 3)
			{
				return (float)_slotMachineController.startingBet * 1f;
			}
			return 0f;
		}
	}

	private static int CountElements(SlotElement first, SlotElement second, SlotElement third, SlotElement element)
	{
		int num = 0;
		if (first == element)
		{
			num++;
		}
		if (second == element)
		{
			num++;
		}
		if (third == element)
		{
			num++;
		}
		return num;
	}

	private static bool IsBar(SlotElement element)
	{
		if (element != SlotElement.BarTriple && element != SlotElement.BarDouble)
		{
			return element == SlotElement.BarSingle;
		}
		return true;
	}
}
