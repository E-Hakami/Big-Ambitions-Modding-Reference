using BigAmbitions.Items;
using Extensions;
using Helpers;
using Localizor.LanguageChangeEvent;
using Player.PlayerMissions;
using Streets;
using UI;
using UI.Tasks;
using UnityEngine;

namespace Player.FoodDeliveryJob;

public class FoodDeliveryJobUI : MissionTasksUI<FoodDeliveryMission>
{
	private const string TitleKey = "food_delivery_job_title";

	private const string CancelConfirmKey = "food_delivery_job_cancel_confirm";

	private const float DecimetersPerMeter = 10f;

	private Transform _destinationEntrance;

	private TextLocalizationComponent _destinationLabel;

	private string _destinationAddressText;

	private int _lastDistanceDecimeters;

	public void Init()
	{
		GlobalEvents.RegisterOnGameLoadedLateCallback(RestoreUI);
	}

	public override void UpdateUI()
	{
		if (FoodDeliveryJobHelper.TryExpireMission() || !TryGetMission(out var mission))
		{
			return;
		}
		if (!tasksGroup)
		{
			CreateUI(mission);
		}
		UpdateTimeLabel(mission);
		if (!_destinationEntrance)
		{
			_destinationLabel.SetValue(_destinationAddressText, clearKey: true);
			return;
		}
		float num = Vector3.Distance(PlayerHelper.GetCityPosition(), _destinationEntrance.position);
		int num2 = (int)(num * 10f);
		if (num2 != _lastDistanceDecimeters)
		{
			_lastDistanceDecimeters = num2;
			_destinationLabel.SetValue(FormatAddressWithDistance(_destinationAddressText, num.ToFormattedDistance()));
		}
	}

	protected override void OnHide()
	{
		_destinationEntrance = null;
		_destinationLabel = null;
		_destinationAddressText = null;
	}

	protected override void OnClickCancelJob()
	{
		if (SaveGameManager.Current.currentPlayerMission is FoodDeliveryMission)
		{
			HudConfirm.Show(null, "food_delivery_job_cancel_confirm", OnConfirmCancelJob);
		}
	}

	private static void OnConfirmCancelJob()
	{
		if (SaveGameManager.Current.currentPlayerMission is FoodDeliveryMission mission)
		{
			FoodDeliveryJobHelper.CancelMission(mission);
		}
	}

	private void RestoreUI()
	{
		if (FoodDeliveryJobHelper.RestoreMission() != null)
		{
			if (InstanceBehavior<UIs>.Instance.tasksUI.IsCollapsed)
			{
				InstanceBehavior<UIs>.Instance.tasksUI.SetCollapsedState(collapsed: false);
			}
			UpdateUI();
			StartUpdateRoutine();
		}
	}

	private void CreateUI(FoodDeliveryMission mission)
	{
		CreateTasksGroup("food_delivery_job_title");
		CreateTimeEntry();
		_destinationAddressText = mission.destinationAddress.ToFormattedString();
		_destinationEntrance = BuildingHelper.GetAddressEntranceTransform(mission.destinationAddress);
		_lastDistanceDecimeters = -1;
		Transform transform = CreateAddressEntry(_destinationAddressText, out _destinationLabel);
		transform.Find("Checkmark").gameObject.SetActive(value: false);
		transform.GetButtonByName("DestinationButton").gameObject.SetActive(value: false);
		Transform itemTemplate = transform.Find("Task/Subtasks/SubLabel");
		for (int i = 0; i < mission.items.Count; i++)
		{
			ItemAmountTarget itemAmountTarget = mission.items[i];
			MissionTasksUI<FoodDeliveryMission>.CreateItemSubLabel(itemTemplate, itemAmountTarget.itemName, itemAmountTarget.targetAmount);
		}
		transform.gameObject.SetActive(value: true);
		InstanceBehavior<UIs>.Instance.tasksUI.ScheduleUpdateObjectivesHeight();
	}
}
