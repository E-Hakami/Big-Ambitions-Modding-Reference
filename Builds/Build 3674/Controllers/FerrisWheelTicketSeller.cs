using System;
using PlayerActivity.Activities.Paid;
using UI;
using UnityEngine;

namespace Controllers;

public class FerrisWheelTicketSeller : TicketSeller
{
	public FerrisWheel ferrisWheel;

	private PaidActivity _paidActivity;

	private PlayerController _playerController;

	public override void OnPlayerReached(PlayerController playerController, PaidActivity paidActivity)
	{
		paidActivity.ChangeState(PlayerActivityState.Waiting);
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.EnableQueueLabel(delegate(bool canceled)
		{
			OnCancelFerrisWheelQueue(canceled, playerController);
		});
		_paidActivity = paidActivity;
		_playerController = playerController;
	}

	public override void OnPlayerReachedWaitingPosition()
	{
		if (ferrisWheel.PlayerTryRide(_playerController.Character))
		{
			_paidActivity.OnPaidActivityStarted();
			InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Close(false);
		}
		else
		{
			ferrisWheel.EnqueuePlayer(_playerController.Character);
			FerrisWheel obj = ferrisWheel;
			obj.onCurrentCabinChanged = (Action)Delegate.Combine(obj.onCurrentCabinChanged, new Action(OnFerrisWheelCurrentCabinChanged));
		}
	}

	public override Transform GetPlayerWaitingTransform()
	{
		return ferrisWheel.playerWaitingPosition;
	}

	private void OnCancelFerrisWheelQueue(bool canceled, PlayerController playerController)
	{
		if (canceled)
		{
			ferrisWheel.UnEnqueuePlayer();
			FerrisWheel obj = ferrisWheel;
			obj.onCurrentCabinChanged = (Action)Delegate.Remove(obj.onCurrentCabinChanged, new Action(OnFerrisWheelCurrentCabinChanged));
			playerController.UnsetNavigationBlocker(NavigationBlocker.PaidActivity);
			_paidActivity.CancelActivity();
		}
	}

	private void OnFerrisWheelCurrentCabinChanged()
	{
		FerrisWheel obj = ferrisWheel;
		obj.onCurrentCabinChanged = (Action)Delegate.Remove(obj.onCurrentCabinChanged, new Action(OnFerrisWheelCurrentCabinChanged));
		_paidActivity.OnPaidActivityStarted();
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Close(false);
	}

	public override void OnFinish()
	{
		if (!(_playerController?.Character))
		{
			Debug.LogError("Trying to finish a FerrisWheelTicketSeller activity without a valid playerController");
			return;
		}
		_playerController.Character.navmeshAgent.Warp(GetNavMeshTargetPosition());
		_playerController.Character.transform.SetParent(InstanceBehavior<GameManager>.Instance.transform);
		ferrisWheel.onPlayerLeft?.Invoke();
	}
}
