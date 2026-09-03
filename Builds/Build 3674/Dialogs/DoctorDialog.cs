using System.Collections;
using BigAmbitions.DayNightCycle;
using CameraControllers;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using UI;
using UnityEngine;

namespace Dialogs;

public class DoctorDialog : Dialog
{
	private const float InitialOperationPrice = 1E+09f;

	private const float PriceAdditionPerOperation = 0.25f;

	public const int YearsRevertedByOperation = 10;

	private const int MinimumAgeForOperation = 60;

	public DoctorDialog()
	{
		npcNameKey = "dialog_doctor_npc_name";
		bool flag = TimeHelper.GetYearsByDays(PlayerHelper.CharacterData.ageInDays) < 60;
		DialogController.current.ShowEntry(flag ? StartYoung() : Start());
	}

	private DialogEntry Start()
	{
		return new DialogEntry
		{
			headerKey = npcNameKey,
			messageData = "dialog_doctor_start".Localize(),
			Template = DialogEntry.TemplateType.Text,
			ConfirmTextOverride = "dialog_doctor_extended_warranty".Localize(),
			OnConfirm = ExtendedWarrantyExplanation1,
			OnSecondOption = PlasticSurgeryExplanation,
			SecondOptionTextOverride = "plastic_surgery_title".GetLocalization(),
			OnCancel = DialogController.current.CancelDialog
		};
	}

	private DialogEntry StartYoung()
	{
		return new DialogEntry
		{
			headerKey = npcNameKey,
			messageData = "dialog_doctor_start".Localize(),
			Template = DialogEntry.TemplateType.Text,
			ConfirmTextOverride = "plastic_surgery_title".Localize(),
			OnConfirm = PlasticSurgeryExplanation,
			OnCancel = DialogController.current.CancelDialog
		};
	}

	private DialogEntry ExtendedWarrantyExplanation1()
	{
		return new DialogEntry
		{
			messageData = "dialog_doctor_operation_explanation_1".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = delegate
			{
				DialogController.current.ShowEntry(ExtendedWarrantyExplanation2());
			}
		};
	}

	private DialogEntry ExtendedWarrantyExplanation2()
	{
		return new DialogEntry
		{
			messageData = "dialog_doctor_operation_explanation_2".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = delegate
			{
				DialogController.current.ShowEntry(ExtendedWarrantyExplanation3());
			}
		};
	}

	private DialogEntry ExtendedWarrantyExplanation3()
	{
		return new DialogEntry
		{
			messageData = "dialog_doctor_operation_explanation_3".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = delegate
			{
				DialogController.current.ShowEntry(ExtendedWarrantyExplanationProceed());
			}
		};
	}

	private DialogEntry ExtendedWarrantyExplanationProceed()
	{
		return new DialogEntry
		{
			messageData = "dialog_doctor_operation_explanation_proceed".Localize(),
			Template = DialogEntry.TemplateType.Text,
			ConfirmTextOverride = "dialog_doctor_proceed".Localize(new
			{
				price = GetCurrentOperationPrice().ToShortCurrencyFormat()
			}),
			OnConfirm = DoOperation,
			OnCancel = DialogController.current.CancelDialog
		};
	}

	private DialogEntry DoOperation()
	{
		float currentOperationPrice = GetCurrentOperationPrice();
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_doctorappointment");
		if (!GameManager.ChangeMoneySafe(0f - currentOperationPrice, transactionInfo, null, null, force: false, showNotification: true))
		{
			return null;
		}
		SaveGameManager.Current.numberOfDoctorOperations++;
		PlayerHelper.CharacterData.ageInDays -= TimeHelper.GetDaysByYears(10f);
		ThirdPersonCharacter.permanentZombieWalking = PlayerHelper.ShouldEnablePermanentZombieWalking();
		InstanceBehavior<GameManager>.Instance.StartCoroutine(PerformOperation());
		DialogController.current.CancelDialog();
		GameEvent.Invoke("ba:gameevent_undergonemrternitysurgery");
		return null;
	}

	private float GetCurrentOperationPrice()
	{
		float num = 1E+09f;
		for (int i = 0; i < SaveGameManager.Current.numberOfDoctorOperations; i++)
		{
			num += num * 0.25f;
		}
		return num * SaveGameManager.Current.gameVariables.marketPriceMultiplier;
	}

	private IEnumerator PerformOperation()
	{
		EnergyHelper.goingToHospital = true;
		InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
		InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.DoctorOperation);
		InstanceBehavior<GameManager>.Instance.playerController.Character.ResetZombieState();
		yield return UiFader.Fade();
		InstanceBehavior<GameManager>.Instance.playerController.Character.ResetAnimator();
		InstanceBehavior<GameManager>.Instance.playerController.Character.appearanceSetter.UpdateVisualAge();
		InstanceBehavior<UIs>.Instance.playerHUD.dialogUI.CancelDialog();
		EnergyHelper.SetCurrentEnergyRegen(EnergyRegen.Hospital);
		InstanceBehavior<GameManager>.Instance.indoorCamera.GetComponent<PedestrianCam>().angle = -15f;
		InstanceBehavior<GameManager>.Instance.pedestrianCamera.GetComponent<PedestrianCam>().angle = -15f;
		InstanceBehavior<GameManager>.Instance.playerController.Character.Reset();
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.DoctorOperation);
		InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
		Timestamp timestamp = TimeHelper.Now();
		timestamp.AddHours(24);
		InstanceBehavior<UIs>.Instance.timeMachine.StartTimeMachine(timestamp, disableCancel: true, "hospital_operation_info");
		GameEvent.Invoke(string.Empty);
		yield return new WaitUntil(() => !InstanceBehavior<UIs>.Instance.timeMachine.isRunning);
		yield return UiFader.UnFade();
		SaveGameManager.Current.Energy = 100f;
		SaveGameManager.Current.Hunger = 100f;
		EnergyHelper.SetCurrentEnergyRegen(EnergyRegen.None);
		EnergyHelper.goingToHospital = false;
		SaveGameManager.Current.achievementsData.doctorsAppointments++;
		GameEvent.Invoke("ba:gameevent_rejuvenationsurgerydone");
	}

	private DialogEntry PlasticSurgeryExplanation()
	{
		return new DialogEntry
		{
			messageData = "dialog_doctor_plastic_surgery_prompt".Localize(new
			{
				hours = 2
			}),
			Template = DialogEntry.TemplateType.Text,
			ConfirmTextOverride = "menu_continue".Localize(),
			OnConfirm = OpenPlasticSurgeryUI,
			OnCancel = DialogController.current.CancelDialog
		};
	}

	private DialogEntry OpenPlasticSurgeryUI()
	{
		DialogController.current.CancelDialog();
		InstanceBehavior<UIs>.Instance.plasticSurgeryUI.Show();
		return null;
	}
}
