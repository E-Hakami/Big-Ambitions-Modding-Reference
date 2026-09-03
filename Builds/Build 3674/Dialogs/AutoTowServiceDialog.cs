using System.Collections;
using System.Collections.Generic;
using BigAmbitions.DayNightCycle;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Streets;
using UI;
using UI.Dialog;
using UI.Notification;
using Vehicles;

namespace Dialogs;

public class AutoTowServiceDialog : Dialog
{
	public AutoTowServiceDialog()
	{
		DialogController.current.ShowEntry(VehicleHelper.IsInsideMotorVehicle() ? Start() : NoVehicle());
	}

	private DialogEntry Start()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_auto_tow_start", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "dialog_auto_tow_start".Localize(),
			Template = DialogEntry.TemplateType.Text,
			ConfirmTextOverride = "dialog_import_create_new_partnership".Localize(),
			OnVisible = delegate
			{
				DialogController.current.ShowEntry(StartService());
			}
		};
	}

	private DialogEntry NoVehicle()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_auto_tow_service_no_vehicle", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "dialog_auto_tow_service_no_vehicle".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = DialogController.current.FinishDialog
		};
	}

	private DialogEntry StartService()
	{
		return new DialogEntry
		{
			Template = DialogEntry.TemplateType.Input,
			ConfirmTextOverride = "dialog_accept_button".Localize(),
			InputTemplate = DialogEntry.InputTemplateName.AutoTowServiceSettings,
			OnConfirm = OnAutoTowServiceSettingsSet,
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	private DialogEntry OnAutoTowServiceSettingsSet()
	{
		AutoTowServiceSettings inputComponent = DialogController.current.GetInputComponent<AutoTowServiceSettings>();
		TowDestinationData data = TowDestinationHelper.GetData(inputComponent.optionSelected);
		VehicleInstance currentVehicle = VehicleHelper.GetCurrentVehicle();
		Dictionary<string, string> data2 = new Dictionary<string, string>();
		bool taxDeductible = currentVehicle.VehicleType.taxDeductible;
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_autotowservice", data2);
		if (taxDeductible)
		{
			transactionInfo.SetTaxDeductibleName("ba:transaction_autotowservice_label");
		}
		if (!GameManager.ChangeMoneySafe(-data.servicePrice, transactionInfo, null, null, force: false, showNotification: true))
		{
			return null;
		}
		Dictionary<string, string> messageData = new Dictionary<string, string> { { "autoTowServiceOption", inputComponent.optionSelected } };
		DialogController.current.contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_auto_tow_service_settings_set_player", messageData, read: true));
		Dictionary<string, string> messageData2 = new Dictionary<string, string> { 
		{
			"amount",
			data.servicePrice.ToShortCurrencyFormat()
		} };
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_auto_tow_service_settings_set", messageData2, read: true));
		InstanceBehavior<UIs>.Instance.StartCoroutine(TowVehicle(currentVehicle, inputComponent.optionSelected));
		InstanceBehavior<UIs>.Instance.fullMenu.Toggle(show: false);
		return null;
	}

	private IEnumerator TowVehicle(VehicleInstance vehicleToTow, string towDestination)
	{
		yield return UiFader.Fade();
		for (int num = vehicleToTow.cargoInstances.Count - 1; num >= 0; num--)
		{
			if (!vehicleToTow.cargoInstances[num].paid)
			{
				vehicleToTow.RemoveFromCargo(vehicleToTow.cargoInstances[num]);
			}
		}
		InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetVehicle(InstanceBehavior<GameManager>.Instance.selectedVehicle);
		Address towAddress = VehicleHelper.TowVehicle(vehicleToTow, towDestination);
		if (towAddress.IsUndefined())
		{
			Notifications.Show(NotificationType.Error, "autotowservicedialog_notification_no_free_space");
			yield return UiFader.UnFade();
			yield break;
		}
		yield return null;
		Timestamp timestamp = TimeHelper.Now();
		timestamp.AddHours(2);
		InstanceBehavior<UIs>.Instance.timeMachine.StartTimeMachine(timestamp, disableCancel: true);
		Dictionary<string, string> notificationData = new Dictionary<string, string> { 
		{
			"towAddress",
			towAddress.ToFormattedString()
		} };
		Notifications.Show(NotificationType.Success, "autotowservicedialog_notification_vehicle_towed", notificationData);
		CarController component = InstanceBehavior<GameManager>.Instance.selectedVehicle.GetComponent<CarController>();
		if (component != null)
		{
			component.isCheckedForParkingZone = false;
		}
		yield return UiFader.UnFade();
	}
}
