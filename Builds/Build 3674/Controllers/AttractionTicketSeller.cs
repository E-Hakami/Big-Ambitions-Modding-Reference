using System;
using PlayerActivity.Activities.Paid;
using UI;
using UnityEngine;

namespace Controllers;

public class AttractionTicketSeller : TicketSeller
{
	public Attraction attraction;

	private PaidActivity _paidActivity;

	private PlayerController _playerController;

	private bool _isWaitingForAttractionStart;

	public override void OnPlayerReached(PlayerController playerController, PaidActivity paidActivity)
	{
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.EnableQueueLabel(delegate(bool canceled)
		{
			OnCancelAttractionQueue(canceled, playerController);
		});
		_paidActivity = paidActivity;
		_playerController = playerController;
		_paidActivity.ChangeState(PlayerActivityState.Waiting);
		_isWaitingForAttractionStart = false;
	}

	public override void OnPlayerReachedWaitingPosition()
	{
		attraction.ReserveARandomSeatForPlayer(_playerController.Character);
		Attraction obj = attraction;
		obj.onAttractionStart = (Action)Delegate.Combine(obj.onAttractionStart, new Action(OnAttractionStart));
		_isWaitingForAttractionStart = true;
	}

	public override Transform GetPlayerWaitingTransform()
	{
		return attraction.playerWaitingPosition;
	}

	private void OnAttractionStart()
	{
		_paidActivity.OnPaidActivityStarted();
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Close(false);
		Attraction obj = attraction;
		obj.onAttractionStart = (Action)Delegate.Remove(obj.onAttractionStart, new Action(OnAttractionStart));
	}

	private void OnCancelAttractionQueue(bool canceled, PlayerController playerController)
	{
		if (canceled)
		{
			if (_isWaitingForAttractionStart)
			{
				Attraction obj = attraction;
				obj.onAttractionStart = (Action)Delegate.Remove(obj.onAttractionStart, new Action(OnAttractionStart));
				attraction.UnReservePlayerSeat();
			}
			playerController.UnsetNavigationBlocker(NavigationBlocker.PaidActivity);
			_paidActivity.CancelActivity();
		}
	}

	public override void OnFinish()
	{
		if (!(_playerController?.Character))
		{
			Debug.LogError("Trying to finish a AttractionTicketSeller activity without a valid playerController");
			return;
		}
		_playerController.Character.navmeshAgent.Warp(GetNavMeshTargetPosition());
		_playerController.Character.transform.SetParent(InstanceBehavior<GameManager>.Instance.transform);
		attraction.OnPlayerFinish();
	}
}
