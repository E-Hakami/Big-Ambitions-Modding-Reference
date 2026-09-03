using System.Collections.Generic;
using System.Linq;
using BigAmbitions.GameAnalytics;
using BigAmbitions.Items;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Localizor.LanguageChangeEvent;
using Player.PlayerMissions;
using Streets;
using TMPro;
using UI;
using UI.Guiders;
using UI.Notification;
using UI.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Vehicles.DeliveryDriverJob;

public class DeliveryJobUI : MissionTasksUI<DeliveryDriverMission>
{
	private Toggle _timeLabelToggle;

	private readonly Dictionary<DeliveryJobDestination, Transform> _destinationTransforms = new Dictionary<DeliveryJobDestination, Transform>();

	private readonly Dictionary<DeliveryJobDestination, TextLocalizationComponent> _destinationLabels = new Dictionary<DeliveryJobDestination, TextLocalizationComponent>();

	private readonly Dictionary<DeliveryJobDestination, TextLocalizationComponent[]> _destinationSubLabels = new Dictionary<DeliveryJobDestination, TextLocalizationComponent[]>();

	private readonly Dictionary<DeliveryJobDestination, bool> _destinationCompleted = new Dictionary<DeliveryJobDestination, bool>();

	private readonly Dictionary<DeliveryJobDestination, bool[]> _destinationDeliveries = new Dictionary<DeliveryJobDestination, bool[]>();

	public void Init()
	{
		UpdateUI();
		if (SaveGameManager.Current.currentPlayerMission is DeliveryDriverMission deliveryDriverMission && !deliveryDriverMission.IsOngoing())
		{
			timeLabel.SetData(new LanguageChangeEventDataHolder
			{
				Key = "delivery_job_return_objective"
			});
		}
		StartUpdateRoutine();
	}

	public override void UpdateUI()
	{
		if (!TryGetMission(out var mission))
		{
			return;
		}
		if (!tasksGroup)
		{
			CreateUI();
		}
		if (!mission.IsOngoing() && !mission.AreAllDestinationsCompleted())
		{
			if (mission.shownFinishNotification)
			{
				UpdatePOIs(mission);
				return;
			}
			GameAnalytics.TrackDeliveryJob("completed", mission.GetCompletedDeliveries());
			Notifications.Show(NotificationType.Warning, "notification_delivery_job_end_timeup");
			OnFinishJob();
			return;
		}
		VehicleInstance currentVehicle = VehicleHelper.GetCurrentVehicle();
		if (currentVehicle != null && currentVehicle.id == mission.vehicleId && Mathf.Approximately(currentVehicle.damage, 1f))
		{
			GameAnalytics.TrackDeliveryJob("completed", mission.GetCompletedDeliveries());
			Notifications.Show(NotificationType.Warning, "notification_delivery_job_end_totaled");
			GameAnalytics.TrackDeliveryJob("completed", mission.GetCompletedDeliveries());
			SaveGameManager.Current.currentPlayerMission = null;
			Hide();
			CoroutineUtility.RunAfterSecondsDelay(delegate
			{
				if (VehicleHelper.IsInsideVehicle())
				{
					VehicleHelper.GetCurrentVehicleBase().ExitVehicle();
				}
			}, 1f);
			return;
		}
		if (mission.IsOngoing())
		{
			DeliveryJobHelper.SortDestinations(mission.destinations);
		}
		UpdatePOIs(mission);
		if (!mission.IsOngoing())
		{
			return;
		}
		if (mission.AreAllDestinationsCompleted())
		{
			if (!mission.shownFinishNotification)
			{
				UpdateElements(mission.destinations);
				mission.endTime = TimeHelper.Now();
				GameAnalytics.TrackDeliveryJob("completed", mission.GetCompletedDeliveries());
				Notifications.Show(NotificationType.Info, "notification_delivery_job_end");
				OnFinishJob();
			}
		}
		else
		{
			UpdateTimeLabel(mission);
			UpdateElements(mission.destinations);
		}
	}

	private static void UpdatePOIs(DeliveryDriverMission mission)
	{
		DeliveryJobVehicle deliveryJobVehicle = null;
		foreach (VehicleController allPlayerVehicle in VehicleHelper.AllPlayerVehicles)
		{
			if (!(allPlayerVehicle.vehicleInstance.id != mission.vehicleId))
			{
				deliveryJobVehicle = allPlayerVehicle.GetComponent<DeliveryJobVehicle>();
				break;
			}
		}
		if ((bool)deliveryJobVehicle)
		{
			deliveryJobVehicle.UpdatePOIs();
		}
		else
		{
			GuidersManager.SetGuiderTarget(mission.pinnedAddress ?? mission.destinations[0].address, DirectionGuiderType.JobDestination);
		}
	}

	private void CreateUI()
	{
		if (!(SaveGameManager.Current.currentPlayerMission is DeliveryDriverMission deliveryDriverMission))
		{
			return;
		}
		ClearLists();
		CreateTasksGroup("ba:skill_deliverydriver");
		Transform transform = CreateTimeEntry();
		_timeLabelToggle = transform.Find("Checkmark").GetComponent<Toggle>();
		foreach (DeliveryJobDestination destination in deliveryDriverMission.destinations)
		{
			CreateDestinationEntry(destination);
		}
		InstanceBehavior<UIs>.Instance.tasksUI.ScheduleUpdateObjectivesHeight();
	}

	private void CreateDestinationEntry(DeliveryJobDestination destination)
	{
		_destinationCompleted.Add(destination, value: false);
		_destinationDeliveries.Add(destination, new bool[destination.itemAmounts.Length]);
		Transform transform = CreateAddressEntry(destination.address.ToFormattedString(), out var addressLabel);
		_destinationTransforms.Add(destination, transform);
		Button buttonByName = transform.GetButtonByName("DestinationButton");
		buttonByName.gameObject.SetActive(!destination.IsCompleted());
		buttonByName.onClick.AddListener(delegate
		{
			if (SaveGameManager.Current.currentPlayerMission is DeliveryDriverMission deliveryDriverMission)
			{
				deliveryDriverMission.SetPinnedAddress(destination.address);
			}
		});
		transform.gameObject.SetActive(value: true);
		_destinationLabels.Add(destination, addressLabel);
		TextLocalizationComponent[] array = new TextLocalizationComponent[destination.itemAmounts.Length];
		Transform itemTemplate = transform.Find("Task/Subtasks/SubLabel");
		for (int num = 0; num < destination.itemAmounts.Length; num++)
		{
			ItemAmountTarget itemAmountTarget = destination.itemAmounts[num];
			array[num] = MissionTasksUI<DeliveryDriverMission>.CreateItemSubLabel(itemTemplate, itemAmountTarget.itemName, itemAmountTarget.targetAmount);
		}
		_destinationSubLabels.Add(destination, array);
	}

	private void UpdateElements(List<DeliveryJobDestination> destinations)
	{
		foreach (DeliveryJobDestination destination in destinations)
		{
			Transform transform = _destinationTransforms[destination];
			transform.SetAsLastSibling();
			TextLocalizationComponent textLocalizationComponent = _destinationLabels[destination];
			bool flag = destination.IsCompleted();
			if (_destinationCompleted[destination] != flag)
			{
				_destinationCompleted[destination] = flag;
				transform.Find("Checkmark").GetComponent<Toggle>().isOn = flag;
				transform.GetButtonByName("DestinationButton").gameObject.SetActive(!flag);
				if (flag)
				{
					textLocalizationComponent.SetValue(destination.address.ToFormattedString(), clearKey: true);
				}
			}
			if (!flag)
			{
				string addressText = destination.address.ToFormattedString();
				string distanceText = destination.playerDistanceCached.ToFormattedDistance();
				textLocalizationComponent.SetValue(FormatAddressWithDistance(addressText, distanceText));
			}
			for (int i = 0; i < destination.itemAmounts.Length; i++)
			{
				bool[] array = _destinationDeliveries[destination];
				if (destination.itemAmountsDelivered[i] && !array[i])
				{
					array[i] = true;
					TMP_Text textContainer = _destinationSubLabels[destination][i].TextContainer;
					textContainer.color = InstanceBehavior<UIs>.Instance.tasksUI.inactiveTaskColor;
					textContainer.fontStyle = FontStyles.Strikethrough;
				}
			}
		}
	}

	private void RemoveDistanceTexts()
	{
		if (!(SaveGameManager.Current.currentPlayerMission is DeliveryDriverMission deliveryDriverMission))
		{
			return;
		}
		foreach (DeliveryJobDestination destination in deliveryDriverMission.destinations)
		{
			_destinationLabels[destination].SetValue(destination.address.ToFormattedString(), clearKey: true);
		}
	}

	private void OnFinishJob()
	{
		if (SaveGameManager.Current.currentPlayerMission is DeliveryDriverMission deliveryDriverMission)
		{
			deliveryDriverMission.shownFinishNotification = true;
		}
		timeLabel.TextContainer.color = timeLabelDefaultColor;
		timeLabel.SetData(new LanguageChangeEventDataHolder
		{
			Key = "delivery_job_return_objective"
		});
		_timeLabelToggle.gameObject.SetActive(value: true);
		DeliveryJobHelper.DiscardSealedBoxes();
		RemoveDistanceTexts();
	}

	protected override void OnClickCancelJob()
	{
		bool flag = !(SaveGameManager.Current.currentPlayerMission is DeliveryDriverMission deliveryDriverMission) || !deliveryDriverMission.IsOngoing();
		HudConfirm.Show(null, flag ? "delivery_job_discard_confirm" : "delivery_job_cancel_confirm", OnConfirmCancelJob);
	}

	private void OnConfirmCancelJob()
	{
		PlayerMission currentPlayerMission = SaveGameManager.Current.currentPlayerMission;
		DeliveryDriverMission mission = currentPlayerMission as DeliveryDriverMission;
		if (mission == null)
		{
			Hide();
			return;
		}
		if (mission.IsOngoing())
		{
			mission.endTime = TimeHelper.Now();
			GameAnalytics.TrackDeliveryJob("canceled", mission.GetCompletedDeliveries());
			Notifications.Show(NotificationType.Info, "notification_delivery_job_end");
			OnFinishJob();
			return;
		}
		VehicleController currentVehicleBase = VehicleHelper.GetCurrentVehicleBase();
		if ((bool)currentVehicleBase && currentVehicleBase.vehicleInstance.id == mission.vehicleId)
		{
			currentVehicleBase.ExitVehicle();
		}
		CoroutineUtility.RunAfterFrameDelay(delegate
		{
			VehicleController vehicleController = VehicleHelper.AllPlayerVehicles.FirstOrDefault((VehicleController x) => x.vehicleInstance.id == mission.vehicleId);
			if ((bool)vehicleController && vehicleController != VehicleHelper.GetCurrentVehicleBase())
			{
				Object.Destroy(vehicleController.gameObject);
			}
		}, 2);
		SaveGameManager.Current.currentPlayerMission = null;
		Hide();
	}

	protected override void OnHide()
	{
		ClearLists();
		GuidersManager.ResetGuider(DirectionGuiderType.JobDestination);
	}

	private void ClearLists()
	{
		_destinationTransforms.Clear();
		_destinationLabels.Clear();
		_destinationSubLabels.Clear();
		_destinationCompleted.Clear();
		_destinationDeliveries.Clear();
	}
}
