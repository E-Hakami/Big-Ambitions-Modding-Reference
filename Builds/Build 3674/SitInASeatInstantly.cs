using System.Linq;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Characters;
using BigAmbitions.DayNightCycle;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Extensions;
using UnityEngine;

[TaskCategory("Big Ambitions")]
public class SitInASeatInstantly : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	public SharedBool runAnimation;

	public AnimationType animationType = AnimationType.Drink;

	public int minSittingTime = 8;

	public int maxSittingTime = 16;

	private SeatSpot _seatSpot;

	private ItemController _seatItemController;

	private bool _hasSitDown;

	private Timestamp _stopWaitingTimeStamp;

	public override void OnStart()
	{
		GetSeatSpot();
	}

	public override TaskStatus OnUpdate()
	{
		if (_seatSpot == null)
		{
			return TaskStatus.Success;
		}
		if (!_hasSitDown)
		{
			OnSeatReached();
		}
		if (!_stopWaitingTimeStamp.IsInThePast())
		{
			return TaskStatus.Running;
		}
		OnFinishedWaiting();
		return TaskStatus.Success;
	}

	public override void OnEnd()
	{
		Reset();
	}

	public override void OnBehaviorComplete()
	{
		Reset();
	}

	private void GetSeatSpot()
	{
		ItemController random = InstanceBehavior<BuildingManager>.Instance.GetAllTablesWithSeatsAvailable().GetRandom();
		if (!(random == null))
		{
			SeatSpot random2 = random.SeatSpots.Where((SeatSpot x) => x.IsAvailable).GetRandom();
			random2.occupied = true;
			_seatSpot = random2;
			sharedCustomer.Value.isSittingOn = random2;
			ItemController getAttachedChair = random2.GetAttachedChair;
			if (getAttachedChair != null)
			{
				_seatItemController = getAttachedChair;
				_seatItemController.Occupied = true;
			}
			else
			{
				_seatItemController = random;
			}
		}
	}

	private void OnSeatReached()
	{
		SitDown();
		RunAnimationIfRequired();
		StartWaitingTimer();
	}

	private void SitDown()
	{
		sharedCustomer.Value.tpc.SitOnChair(_seatSpot.SeatTransform);
		sharedCustomer.Value.ToggleTableFoodForCustomer(show: true);
		_hasSitDown = true;
	}

	private void RunAnimationIfRequired()
	{
		if (runAnimation.Value)
		{
			sharedCustomer.Value.tpc.animator.RunAnimationLength(animationType);
		}
	}

	private void StartWaitingTimer()
	{
		_stopWaitingTimeStamp = TimeHelper.Now();
		_stopWaitingTimeStamp.AddMinutes(Random.Range(minSittingTime, maxSittingTime));
	}

	private void OnFinishedWaiting()
	{
		sharedCustomer.Value.ResetItemsInTable();
		BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, _seatItemController.ItemInstance);
	}

	private void Reset()
	{
		_seatSpot = null;
		_seatItemController = null;
	}
}
