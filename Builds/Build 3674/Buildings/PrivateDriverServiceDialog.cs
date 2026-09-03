using System;
using System.Collections.Generic;
using Buildings.BuildingTypes.Special.PrivateDriverService;
using Dialogs;
using Entities;
using Helpers;
using Localizor;
using UI;

namespace Buildings;

public class PrivateDriverServiceDialog : Dialog
{
	private static Contact CurrentContact => DialogController.current.contact;

	private static string BusinessName => BuildingHelper.GetBuildingRegistration(CurrentContact.Address).BusinessName;

	public PrivateDriverServiceDialog()
	{
		npcNameKey = "dialog_private_driver_service_npc_name";
		DialogController.current.ShowEntry(Start());
	}

	private DialogEntry Start()
	{
		Dictionary<string, string> messageData = new Dictionary<string, string> { { "businessName", BusinessName } };
		PrivateDriverContract activeContract = PrivateDriverHelpers.GetActiveContract();
		string text = (activeContract ? "dialog_private_driver_start_existing" : "dialog_private_driver_start");
		CurrentContact.SendMessage(new TextMessage(text, messageData, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = text.Localize(new
			{
				businessName = BusinessName
			}),
			OnCancel = DialogController.current.CancelDialog,
			OnConfirm = ManageContract,
			ConfirmTextOverride = GetManageButtonKey().Localize(),
			OnSecondOption = (activeContract ? new Func<DialogEntry>(ManageVehicles) : null),
			SecondOptionTextOverride = (activeContract ? "dialog_private_driver_manage_vehicles" : null)
		};
	}

	private DialogEntry ManageVehicles()
	{
		if (!PrivateDriverHelpers.GetActiveContract())
		{
			DialogController.current.CancelDialog();
			return null;
		}
		List<VehicleInstance> privateDriverVehicleInstances = SaveGameManager.Current.privateDriverVehicleInstances;
		if (privateDriverVehicleInstances == null || privateDriverVehicleInstances.Count <= 0)
		{
			return new DialogEntry
			{
				messageData = "ba:private_driver_no_vehicles".Localize(new
				{
					businessName = BusinessName
				}),
				OnConfirm = OnMoreHelpOffered
			};
		}
		return new DialogEntry
		{
			Template = DialogEntry.TemplateType.Input,
			InputTemplate = DialogEntry.InputTemplateName.PrivateDriverManageVehicles,
			OnCancel = delegate
			{
				OnMoreHelpOffered().ShowEntry();
			},
			CancelTextOverride = "common_cancel",
			OnSecondOption = UnassignAllVehicles,
			SecondOptionTextOverride = "ba:private_driver_unassign_all"
		};
	}

	public DialogEntry OnUnassignVehicle(VehicleInstance vehicleInstance)
	{
		PrivateDriverVehicleList inputTransformSafe = DialogController.current.GetInputTransformSafe<PrivateDriverVehicleList>();
		if ((bool)inputTransformSafe)
		{
			inputTransformSafe.Disable();
		}
		if (!InstanceBehavior<CityManager>.Instance.FindCityBuildingController(CurrentContact.Address).GetComponent<PrivateDriverParkingLot>().TryGetRandomFreeSpotForPlayerVehicle(out var spotPosition, out var spotRotation))
		{
			return new DialogEntry
			{
				messageData = "ba:private_driver_no_parking_lot_space".Localize(new
				{
					businessName = BusinessName
				}),
				OnConfirm = OnMoreHelpOffered
			};
		}
		vehicleInstance.parkingState = ParkingState.Legal;
		VehicleHelper.CreateAndSpawnVehicle(vehicleInstance, spotPosition, spotRotation);
		SaveGameManager.Current.privateDriverVehicleInstances.Remove(vehicleInstance);
		InstanceBehavior<UIs>.Instance.smartphoneUI.DismissPrivateDriver(force: false, vehicleInstance, instantRemove: true);
		InstanceBehavior<UIs>.Instance.smartphoneUI.RebuildPrivateDriverUI();
		Dictionary<string, string> messageData = new Dictionary<string, string>
		{
			{ "vehicleName", vehicleInstance.vehicleTypeName },
			{ "businessName", BusinessName }
		};
		CurrentContact.SendMessage(new TextMessage("ba:private_driver_vehicle_unassigned", messageData, read: true));
		var arguments = new
		{
			vehicleName = vehicleInstance.vehicleTypeName,
			businessName = BusinessName
		};
		return new DialogEntry
		{
			messageData = "ba:private_driver_vehicle_unassigned".Localize(arguments),
			OnConfirm = OnMoreHelpOffered
		};
	}

	private DialogEntry ManageContract()
	{
		bool flag = PrivateDriverHelpers.GetActiveContract() != null;
		return new DialogEntry
		{
			Template = DialogEntry.TemplateType.Input,
			InputTemplate = DialogEntry.InputTemplateName.PrivateDriverManageContract,
			OnConfirm = () => OnContractSet(),
			OnCancel = delegate
			{
				OnMoreHelpOffered().ShowEntry();
			},
			CancelTextOverride = "common_cancel",
			OnSecondOption = (flag ? new Func<DialogEntry>(PromptConfirmTerminateContract) : null),
			SecondOptionTextOverride = (flag ? "dialog_private_driver_terminate" : null)
		};
	}

	private DialogEntry OnContractSet(bool confirmDowngrade = false)
	{
		PrivateDriverContractSettings inputTransformSafe = DialogController.current.GetInputTransformSafe<PrivateDriverContractSettings>();
		inputTransformSafe.Disable();
		PrivateDriverContract selectedContract = inputTransformSafe.GetSelectedContract();
		if (!selectedContract)
		{
			return OnMoreHelpOffered();
		}
		PrivateDriverContract activeContract = PrivateDriverHelpers.GetActiveContract();
		if (activeContract == selectedContract)
		{
			return OnMoreHelpOffered();
		}
		if ((bool)activeContract && selectedContract.maxCars < activeContract.maxCars && !confirmDowngrade)
		{
			List<VehicleInstance> privateDriverVehicleInstances = SaveGameManager.Current.privateDriverVehicleInstances;
			if (privateDriverVehicleInstances != null && privateDriverVehicleInstances.Count > 0)
			{
				return new DialogEntry
				{
					messageData = "dialog_private_driver_confirm_downgrade".Localize(new
					{
						businessName = BusinessName
					}),
					OnCancel = delegate
					{
						OnMoreHelpOffered().ShowEntry();
					},
					CancelTextOverride = "common_cancel",
					OnConfirm = () => OnContractSet(confirmDowngrade: true)
				};
			}
		}
		InstanceBehavior<UIs>.Instance.smartphoneUI.DismissPrivateDriver(force: false, null, instantRemove: true);
		if (confirmDowngrade && !TryReturnVehicles(GetAssignedVehiclesNotFittingContract(selectedContract)))
		{
			return new DialogEntry
			{
				messageData = "ba:private_driver_no_parking_lot_space".Localize(new
				{
					businessName = BusinessName
				}),
				OnConfirm = OnMoreHelpOffered
			};
		}
		PrivateDriverHelpers.SetActiveContract(selectedContract);
		PrivateDriverHelpers.PayForActiveContract();
		if (!activeContract)
		{
			List<VehicleInstance> privateDriverVehicleInstances = SaveGameManager.Current.privateDriverVehicleInstances;
			if (privateDriverVehicleInstances == null || privateDriverVehicleInstances.Count <= 0)
			{
				Dictionary<string, string> messageData = new Dictionary<string, string> { { "businessName", BusinessName } };
				CurrentContact.SendMessage(new TextMessage("dialog_private_driver_new_contract_reply_dropoff", messageData, read: true));
				return new DialogEntry
				{
					messageData = "dialog_private_driver_new_contract_reply_dropoff".Localize(new
					{
						businessName = BusinessName
					}),
					OnConfirm = OnMoreHelpOffered
				};
			}
		}
		CurrentContact.SendMessage(new TextMessage("dialog_private_driver_new_contract_reply", null, read: true));
		return OnMoreHelpOffered("dialog_private_driver_new_contract_reply");
	}

	private DialogEntry OnMoreHelpOffered()
	{
		return OnMoreHelpOffered("dialog_private_driver_action_reply");
	}

	private DialogEntry OnMoreHelpOffered(string key)
	{
		PrivateDriverContract activeContract = PrivateDriverHelpers.GetActiveContract();
		PrivateDriverVehicleList inputTransformSafe = DialogController.current.GetInputTransformSafe<PrivateDriverVehicleList>();
		if ((bool)inputTransformSafe)
		{
			inputTransformSafe.Disable();
		}
		return new DialogEntry
		{
			messageData = key.Localize(),
			OnCancel = DialogController.current.CancelDialog,
			OnConfirm = ManageContract,
			ConfirmTextOverride = GetManageButtonKey().Localize(),
			OnSecondOption = (activeContract ? new Func<DialogEntry>(ManageVehicles) : null),
			SecondOptionTextOverride = (activeContract ? "dialog_private_driver_manage_vehicles" : null)
		};
	}

	private static string GetManageButtonKey()
	{
		if (!(PrivateDriverHelpers.GetActiveContract() != null))
		{
			return "dialog_private_driver_hire_driver";
		}
		return "dialog_private_driver_manage_driver";
	}

	private DialogEntry UnassignAllVehicles()
	{
		InstanceBehavior<UIs>.Instance.smartphoneUI.DismissPrivateDriver(force: false, null, instantRemove: true);
		if (!TryReturnVehicles())
		{
			return new DialogEntry
			{
				messageData = "ba:private_driver_no_parking_lot_space".Localize(new
				{
					businessName = BusinessName
				}),
				OnConfirm = OnMoreHelpOffered
			};
		}
		return new DialogEntry
		{
			messageData = "ba:private_driver_vehicle_unassigned_all".Localize(new
			{
				businessName = BusinessName
			}),
			OnConfirm = OnMoreHelpOffered
		};
	}

	private DialogEntry PromptConfirmTerminateContract()
	{
		return new DialogEntry
		{
			messageData = "dialog_private_driver_terminate_ask_confirm".Localize(new
			{
				businessName = BusinessName
			}),
			OnCancel = delegate
			{
				OnMoreHelpOffered().ShowEntry();
			},
			CancelTextOverride = "common_cancel",
			OnConfirm = TerminateContract
		};
	}

	private DialogEntry TerminateContract()
	{
		InstanceBehavior<UIs>.Instance.smartphoneUI.DismissPrivateDriver(force: false, null, instantRemove: true);
		if (!TryReturnVehicles())
		{
			return new DialogEntry
			{
				messageData = "ba:private_driver_no_parking_lot_space".Localize(new
				{
					businessName = BusinessName
				}),
				OnConfirm = OnMoreHelpOffered
			};
		}
		Dictionary<string, string> messageData = new Dictionary<string, string> { { "businessName", BusinessName } };
		CurrentContact.SendMessage(new TextMessage("dialog_private_driver_terminate_reply", messageData, read: true));
		DialogController.current.GetInputTransformSafe<PrivateDriverContractSettings>().Disable();
		PrivateDriverHelpers.SetActiveContract(null);
		return new DialogEntry
		{
			messageData = "dialog_private_driver_terminate_reply".Localize(new
			{
				businessName = BusinessName
			}),
			OnCancel = DialogController.current.CancelDialog,
			OnConfirm = ManageContract,
			ConfirmTextOverride = "dialog_private_driver_hire_driver".Localize()
		};
	}

	private List<VehicleInstance> GetAssignedVehiclesNotFittingContract(PrivateDriverContract contract)
	{
		List<VehicleInstance> list = new List<VehicleInstance>();
		List<VehicleInstance> privateDriverVehicleInstances = SaveGameManager.Current.privateDriverVehicleInstances;
		if (privateDriverVehicleInstances == null || privateDriverVehicleInstances.Count <= 0)
		{
			return list;
		}
		foreach (VehicleInstance privateDriverVehicleInstance in SaveGameManager.Current.privateDriverVehicleInstances)
		{
			if (!contract.usableVehicleTypes.Contains(privateDriverVehicleInstance.vehicleTypeName))
			{
				list.Add(privateDriverVehicleInstance);
			}
		}
		int num = SaveGameManager.Current.privateDriverVehicleInstances.Count - list.Count - contract.maxCars;
		if (num > 0)
		{
			list.AddRange(SaveGameManager.Current.privateDriverVehicleInstances.GetRange(0, num));
		}
		return list;
	}

	private static bool TryReturnVehicles(List<VehicleInstance> specificVehicles = null)
	{
		List<VehicleInstance> privateDriverVehicleInstances = SaveGameManager.Current.privateDriverVehicleInstances;
		if (privateDriverVehicleInstances == null || privateDriverVehicleInstances.Count <= 0)
		{
			return true;
		}
		if (specificVehicles != null && specificVehicles.Count == 0)
		{
			return true;
		}
		int num = specificVehicles?.Count ?? SaveGameManager.Current.privateDriverVehicleInstances.Count;
		if (!InstanceBehavior<CityManager>.Instance.FindCityBuildingController(CurrentContact.Address).GetComponent<PrivateDriverParkingLot>().TryReserveSpots(num, out var spotPositions, out var spotRotations))
		{
			return false;
		}
		List<VehicleInstance> list = specificVehicles ?? SaveGameManager.Current.privateDriverVehicleInstances;
		for (int i = 0; i < num; i++)
		{
			VehicleInstance vehicleInstance = list[i];
			vehicleInstance.parkingState = ParkingState.Legal;
			VehicleHelper.CreateAndSpawnVehicle(vehicleInstance, spotPositions[i], spotRotations[i]);
		}
		if (specificVehicles == null)
		{
			SaveGameManager.Current.privateDriverVehicleInstances.Clear();
		}
		else
		{
			foreach (VehicleInstance specificVehicle in specificVehicles)
			{
				SaveGameManager.Current.privateDriverVehicleInstances.Remove(specificVehicle);
			}
		}
		return true;
	}
}
