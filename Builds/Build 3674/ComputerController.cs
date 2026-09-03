using BigAmbitions.Tags;
using Buildings;
using Controllers;
using Helpers;
using JimmysUnityUtilities;
using NaughtyAttributes;
using PlayerActivity;
using UI.Notification;
using UnityEngine;

public class ComputerController : WorkstationController
{
	[SerializeField]
	private EntertainDevice entertainDevice;

	[SerializeField]
	[Required(null)]
	private VideoGameSetup videoGameSetup;

	public PlayerActivityBalanceConfig VideoGameBalanceConfig => entertainDevice.balanceConfig;

	public override void Start()
	{
		base.Start();
		if (FindChair() is SeatController seatController)
		{
			seatController.UpdateRestingAvailability();
		}
	}

	public override bool Interact()
	{
		if (base.Interact())
		{
			return true;
		}
		if (PlayerHelper.ItemInstanceInHands != null || SaveGameManager.Current.ActiveVehicleId != null || !base.BuildingContext.Registration.RentedByPlayer || ItemHelper.HasAnyMissingRequirements(base.ItemInstance) || BuildingTypeHelper.GetData(base.BuildingContext.Registration).HasTag(TagRef.Buildingtypetag.containsnobusiness))
		{
			return false;
		}
		Notifications.ShowError("notification_cannot_use_outside_of_home");
		return true;
	}

	public void PerformActivity()
	{
		if (PlayerHelper.ItemInstanceInHands != null || SaveGameManager.Current.ActiveVehicleId != null)
		{
			Notifications.ShowError("notification_need_empty_hands_to_interact");
		}
		else if (base.BuildingContext.Registration.RentedByPlayer && BuildingTypeHelper.GetData(base.BuildingContext.Registration).HasTag(TagRef.Buildingtypetag.containsnobusiness) && !ItemHelper.HasAnyMissingRequirements(base.ItemInstance))
		{
			PlayerActivityUI.Show(entertainDevice, this);
		}
	}

	public override Vector3 GetNavMeshTargetPosition(int index = 0)
	{
		if (employeeChair != null)
		{
			return employeeChair.position;
		}
		ItemController itemController = FindChair();
		if (!itemController || !(itemController != this))
		{
			return base.GetNavMeshTargetPosition(index);
		}
		return itemController.GetNavMeshTargetPosition(index);
	}

	public void StartVideoGame()
	{
		videoGameSetup.StartPlaying();
	}

	public void MoveToStartVideoGame()
	{
		EntityController interactedEntity = this;
		ItemController itemController = FindChair();
		Vector3 target;
		if ((bool)itemController)
		{
			interactedEntity = itemController;
			target = itemController.GetClosestNavMeshTargetPosition(PlayerHelper.GetPosition());
		}
		else
		{
			target = GetClosestNavMeshTargetPosition(PlayerHelper.GetPosition());
		}
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if ((bool)this && (bool)interactedEntity && InstanceBehavior<GameManager>.Instance.playerController.ExistsRoute(interactedEntity, showErrorNotification: true))
			{
				if (target == Vector3.zero)
				{
					InstanceBehavior<GameManager>.Instance.playerController.ResetWalkingAnimation();
					StartVideoGame();
				}
				else
				{
					InstanceBehavior<GameManager>.Instance.playerController.SetGoal(target, StartVideoGame);
				}
			}
		});
	}
}
