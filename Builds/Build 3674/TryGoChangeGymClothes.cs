using System.Linq;
using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Extensions;
using UnityEngine;
using UnityEngine.AI;

[TaskCategory("Big Ambitions/Gym")]
public class TryGoChangeGymClothes : Action
{
	private const int NavMeshValidationTries = 10;

	private const float RadiusAroundGymLockers = 0.5f;

	[RequiredField]
	public SharedGymCustomer sharedGymCustomer;

	private ItemController _gymLockersController;

	private bool _canMoveToGymLockers;

	private bool _hasStartedRotating;

	private bool _hasStartedWaiting;

	private Timestamp _stopWaitingTimestamp;

	private readonly CharacterMoveToPosition _characterMoveToPosition = new CharacterMoveToPosition();

	private readonly CharacterRotateTowards _characterRotateTowards = new CharacterRotateTowards();

	public override void OnAwake()
	{
		_characterMoveToPosition.Init(sharedGymCustomer.Value.tpc.navmeshAgent);
		_characterRotateTowards.Init(sharedGymCustomer.Value.tpc, base.Owner);
	}

	public override void OnStart()
	{
		if (sharedGymCustomer.Value.arrivedWithSportClothes)
		{
			return;
		}
		_gymLockersController = FindNearestGymLockers();
		if (!(_gymLockersController == null))
		{
			if (sharedGymCustomer.Value.customerTimeState == CustomerTimeState.JustArrived)
			{
				Vector3 randomPositionNearGymLockers = GetRandomPositionNearGymLockers();
				_canMoveToGymLockers = _characterMoveToPosition.TryStartMovingToPosition(randomPositionNearGymLockers);
			}
			else
			{
				UseGymLocker();
			}
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (sharedGymCustomer.Value.arrivedWithSportClothes)
		{
			return TaskStatus.Success;
		}
		if (_gymLockersController == null)
		{
			return TaskStatus.Success;
		}
		if (sharedGymCustomer.Value.customerTimeState != CustomerTimeState.JustArrived)
		{
			return TaskStatus.Success;
		}
		if (!_canMoveToGymLockers)
		{
			return TaskStatus.Success;
		}
		if (!_characterMoveToPosition.HasReachedDestination())
		{
			return TaskStatus.Running;
		}
		if (!_hasStartedRotating)
		{
			OnGymLockersReached();
		}
		if (!_characterRotateTowards.HasFinishedRotating())
		{
			return TaskStatus.Running;
		}
		if (!_hasStartedWaiting)
		{
			OnRotationFinished();
		}
		if (_stopWaitingTimestamp.IsInTheFuture())
		{
			return TaskStatus.Running;
		}
		UseGymLocker();
		return TaskStatus.Success;
	}

	private ItemController FindNearestGymLockers()
	{
		return (from x in InstanceBehavior<BuildingManager>.Instance.allItemControllers
			where x.Item.HasTag(TagRef.Itemtag.isgymlocker)
			orderby MathHelper.DistanceSqr(x.transform.position, transform.position)
			select x).FirstOrDefault();
	}

	private Vector3 GetRandomPositionNearGymLockers()
	{
		Vector3 position = _gymLockersController.transform.position;
		for (int i = 0; i < 10; i++)
		{
			if (NavMesh.SamplePosition(position + Random.insideUnitSphere * 0.5f, out var hit, 0.5f, -1))
			{
				return hit.position;
			}
		}
		return position;
	}

	private void OnGymLockersReached()
	{
		_characterRotateTowards.StartRotatingTowards(_gymLockersController.transform.position);
		_hasStartedRotating = true;
	}

	private void OnRotationFinished()
	{
		_hasStartedWaiting = true;
		_stopWaitingTimestamp = TimeHelper.Now();
		_stopWaitingTimestamp.AddMinutes(Random.Range(2, 3));
	}

	private void UseGymLocker()
	{
		sharedGymCustomer.Value.ChangeGymClothes(backToOriginal: false);
		BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, _gymLockersController.ItemInstance);
	}

	public override void OnEnd()
	{
		Reset();
	}

	public override void OnBehaviorComplete()
	{
		Reset();
	}

	private void Reset()
	{
		_characterMoveToPosition.StopCheckingDestination();
		_hasStartedRotating = false;
		_hasStartedWaiting = false;
	}
}
