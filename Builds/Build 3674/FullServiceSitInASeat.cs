using System.Linq;
using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Characters;
using BigAmbitions.DayNightCycle;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Entities;
using Extensions;
using UnityEngine;

[TaskCategory("Big Ambitions/FullServiceCustomer")]
public class FullServiceSitInASeat : Action
{
	private const float MinSittingTime = 10f;

	private const float MaxSittingTime = 18f;

	private const float MinPreparationTime = 1f;

	private const float MaxPreparationTime = 2f;

	[SharedRequired]
	public SharedCustomer sharedCustomer;

	public bool isInstant;

	private Timestamp _stopActionTimeStamp;

	private Timestamp _nextIceCreamAnimationTimeStamp;

	private ItemController _seatItemController;

	private SeatSpot _seatSpot;

	private bool _canMoveToSeat;

	private bool _hasStartedPreparation;

	private bool _isEatingFood;

	private bool _isEatingIceCream;

	private bool _hasFood;

	private bool _hasIceCream;

	private float _sittingTime;

	private float _eatingFoodTime;

	private readonly CharacterMoveToPosition _characterMoveToPosition = new CharacterMoveToPosition();

	public override void OnAwake()
	{
		_characterMoveToPosition.Init(sharedCustomer.Value.tpc.navmeshAgent);
	}

	public override void OnStart()
	{
		GetSeatSpot();
		if (_seatSpot != null)
		{
			if (!isInstant)
			{
				_canMoveToSeat = _characterMoveToPosition.TryStartMovingToPosition(_seatItemController.GetNavMeshTargetPositionClosestToTheItem());
			}
			else
			{
				OnSeatReached();
			}
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (_seatSpot == null || (!isInstant && !_canMoveToSeat))
		{
			return TaskStatus.Failure;
		}
		if (_characterMoveToPosition.HasReachedDestination() && !_hasStartedPreparation)
		{
			OnSeatReached();
		}
		if (!_hasStartedPreparation)
		{
			return TaskStatus.Running;
		}
		if (!_isEatingFood && !_isEatingIceCream)
		{
			if (_stopActionTimeStamp.IsInThePast())
			{
				return OnPreparationEnded();
			}
			return TaskStatus.Running;
		}
		if (_isEatingFood)
		{
			if (!_stopActionTimeStamp.IsInThePast())
			{
				return TaskStatus.Running;
			}
			_isEatingFood = false;
			sharedCustomer.Value.tpc.animator.SetBool(PermanentAnimationType.ConsumingFoodSitting, state: false);
			if (_hasIceCream)
			{
				StartEatingIceCream(_sittingTime - _eatingFoodTime);
				return TaskStatus.Running;
			}
		}
		else if (!_stopActionTimeStamp.IsInThePast())
		{
			if (!_nextIceCreamAnimationTimeStamp.IsInThePast())
			{
				return TaskStatus.Running;
			}
			RunEatIceCreamAnimation();
			return TaskStatus.Running;
		}
		OnEatingFinished();
		return TaskStatus.Success;
	}

	private TaskStatus OnPreparationEnded()
	{
		if (_hasFood)
		{
			StartEatingFood();
			return TaskStatus.Running;
		}
		StartEatingIceCream(_sittingTime);
		return TaskStatus.Running;
	}

	private void OnEatingFinished()
	{
		EndAnimations();
		sharedCustomer.Value.ResetItemsInTable();
		BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, _seatItemController.ItemInstance);
	}

	private void OnSeatReached()
	{
		_characterMoveToPosition.StopCheckingDestination();
		sharedCustomer.Value.tpc.SitOnChair(_seatSpot.SeatTransform);
		sharedCustomer.Value.ToggleTableFoodForCustomer(show: true);
		_hasFood = AnyFoodEntryPaid();
		_hasIceCream = AnyIceCreamPaid();
		_sittingTime = Random.Range(10f, 18f);
		_eatingFoodTime = (_hasIceCream ? (_sittingTime * 0.5f) : _sittingTime);
		_stopActionTimeStamp = TimeHelper.Now();
		_stopActionTimeStamp.AddMinutes(Random.Range(1f, 2f));
		_hasStartedPreparation = true;
	}

	private void GetSeatSpot()
	{
		ItemController random = InstanceBehavior<BuildingManager>.Instance.GetAllTablesWithSeatsAvailable().GetRandom();
		if (random == null)
		{
			_seatSpot = null;
			return;
		}
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

	private void StartEatingFood()
	{
		_isEatingFood = true;
		sharedCustomer.Value.tpc.animator.SetBool(PermanentAnimationType.ConsumingFoodSitting);
		_stopActionTimeStamp.AddMinutes(_eatingFoodTime);
	}

	private bool AnyFoodEntryPaid()
	{
		return sharedCustomer.Value.order.entries.Exists((OrderEntry x) => x.paid && x.priceAccceptable && x.itemName != "ba:itemname_icecream" && x.itemName != "ba:itemname_sodacan" && x.itemName != "ba:itemname_cupofcoffee");
	}

	private bool AnyIceCreamPaid()
	{
		return sharedCustomer.Value.order.entries.Exists((OrderEntry x) => x.itemName == "ba:itemname_icecream" && x.paid && x.priceAccceptable);
	}

	private void EndAnimations()
	{
		sharedCustomer.Value.tpc.animator.SetBool(PermanentAnimationType.ConsumingFoodSitting, state: false);
		sharedCustomer.Value.tpc.animator.SetBool(PermanentAnimationType.HoldingIceCream, state: false);
		sharedCustomer.Value.tpc.RemoveHandObject();
	}

	private void StartEatingIceCream(float time)
	{
		_isEatingIceCream = true;
		string handObjectNameFromPermanentAnimationType = BaseHuman.GetHandObjectNameFromPermanentAnimationType(PermanentAnimationType.HoldingIceCream);
		sharedCustomer.Value.tpc.AddHandObject(handObjectNameFromPermanentAnimationType);
		sharedCustomer.Value.tpc.animator.SetBool(PermanentAnimationType.HoldingIceCream);
		RunEatIceCreamAnimation();
		_stopActionTimeStamp.AddMinutes(time);
	}

	private void RunEatIceCreamAnimation()
	{
		float minutes = sharedCustomer.Value.tpc.animator.RunAnimationLength(AnimationType.EatIceCream);
		_nextIceCreamAnimationTimeStamp = TimeHelper.Now();
		_nextIceCreamAnimationTimeStamp.AddMinutes(minutes);
	}

	public override void OnEnd()
	{
		Reset();
	}

	public override void OnBehaviorComplete()
	{
		if (_stopActionTimeStamp != null && !_stopActionTimeStamp.IsInThePast())
		{
			EndAnimations();
		}
		Reset();
	}

	private void Reset()
	{
		_characterMoveToPosition.StopCheckingDestination();
		_hasStartedPreparation = false;
		_isEatingFood = false;
		_isEatingIceCream = false;
	}
}
