using System.Linq;
using Entities;
using HGAttributes;
using Helpers;
using UI.Notification;
using UnityEngine;

public class CleaningStationController : EmployeeStationController
{
	[SerializeField]
	[AutocompleteDropdown("Items")]
	private string mopItemName = "ba:itemname_mop";

	public override Vector3 GetEmployeePosition()
	{
		if (!IndoorCustomerSpawner.GetRandomPositionInside(out var randomPosition))
		{
			return base.GetEmployeePosition();
		}
		return randomPosition;
	}

	public override void Start()
	{
		employeeType = typeof(CleanerEmployee);
		base.Start();
	}

	public override void AssignEmployee(ThirdPersonCharacter tpc, EmployeeInstance employeeInstance)
	{
		base.AssignEmployee(tpc, employeeInstance);
		tpc.GetComponent<CleanerEmployee>().SetEmployeeStation(this);
	}

	protected override bool IsBuildingAvailableForThisStation()
	{
		if (!base.BuildingContext.Registration.temporarilyClosed)
		{
			return base.BuildingContext.Registration.scheduleDays.First((ScheduleDay x) => x.day == TimeHelper.GetDayOfWeek()).isOpen;
		}
		return false;
	}

	public override EmployeeInstance GetAIEmployeeInstance()
	{
		return EmployeeHelper.CreateAIEmployeeInstance("ba:skill_cleaning");
	}

	public void OnCleaningStationClick()
	{
		if (InstanceBehavior<GameManager>.Instance == null || PlayerHelper.IsHoldingAMop)
		{
			return;
		}
		InstanceBehavior<GameManager>.Instance.playerController.SetGoal(this, delegate
		{
			if (base.BuildingContext.IsPlayerOwnedBusiness)
			{
				if (SaveGameManager.Current.ActiveVehicleId != null)
				{
					Notifications.ShowError("notification_need_empty_hands_to_interact");
				}
				else if (PlayerHelper.IsHoldingItem)
				{
					Notifications.ShowError("notification_need_empty_hands_to_interact");
				}
				else
				{
					PlayerHelper.ItemInstanceInHands = ItemHelper.InitializeNewInstance(mopItemName);
				}
			}
		});
	}

	public static bool ReturnMopToStation()
	{
		if (InstanceBehavior<BuildingManager>.Instance?.allItemControllers == null)
		{
			return false;
		}
		foreach (ItemController allItemController in InstanceBehavior<BuildingManager>.Instance.allItemControllers)
		{
			if (allItemController is CleaningStationController cleaningStationController)
			{
				cleaningStationController.StopCleaning();
				return true;
			}
		}
		return false;
	}

	private void StopCleaning()
	{
		if ((bool)MopController.currentCleaningMop)
		{
			MopController.SetOnStopCleaningAction(delegate
			{
				InstanceBehavior<GameManager>.Instance.playerController.SetGoal(this, PlayerHelper.RemoveItemsFromHands);
			});
		}
		else
		{
			MopController.SetOnStopCleaningAction(null);
			InstanceBehavior<GameManager>.Instance.playerController.SetGoal(this, PlayerHelper.RemoveItemsFromHands);
		}
	}
}
