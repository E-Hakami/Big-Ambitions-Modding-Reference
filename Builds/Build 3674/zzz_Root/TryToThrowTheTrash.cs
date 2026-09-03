using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Characters;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Shared.Dirtiness;
using UnityEngine;

[TaskCategory("Big Ambitions")]
public class TryToThrowTheTrash : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	private ItemController _trashBin;

	private bool _canMoveToTrashBin;

	private bool _hasStartedRotating;

	private bool _hasStartedRunningAnimation;

	private readonly CharacterMoveToPosition _characterMoveToPosition = new CharacterMoveToPosition();

	private readonly CharacterRotateTowards _characterRotateTowards = new CharacterRotateTowards();

	private readonly CharacterRunAnimation _characterRunAnimation = new CharacterRunAnimation();

	public override void OnAwake()
	{
		_characterMoveToPosition.Init(sharedCustomer.Value.tpc.navmeshAgent);
		_characterRotateTowards.Init(sharedCustomer.Value.tpc, base.Owner);
		_characterRunAnimation.Init(sharedCustomer.Value.tpc);
	}

	public override void OnStart()
	{
		_trashBin = FindNearestTrashBin();
		if (!(_trashBin == null))
		{
			_canMoveToTrashBin = _characterMoveToPosition.TryStartMovingToPosition(_trashBin.GetNavMeshTargetPosition());
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (_trashBin == null || !_canMoveToTrashBin)
		{
			return TaskStatus.Failure;
		}
		if (!_characterMoveToPosition.HasReachedDestination())
		{
			return TaskStatus.Running;
		}
		if (!_hasStartedRotating)
		{
			OnTrashBinReached();
		}
		if (!_characterRotateTowards.HasFinishedRotating())
		{
			return TaskStatus.Running;
		}
		if (!_hasStartedRunningAnimation)
		{
			OnRotationFinished();
		}
		if (!_characterRunAnimation.IsAnimationFinished())
		{
			return TaskStatus.Running;
		}
		OnAnimationFinished();
		return TaskStatus.Success;
	}

	private void OnTrashBinReached()
	{
		_characterMoveToPosition.StopCheckingDestination();
		_characterRotateTowards.StartRotatingTowards(_trashBin.transform.position);
		_hasStartedRotating = true;
	}

	private void OnRotationFinished()
	{
		sharedCustomer.Value.tpc.SetHandContent(null);
		_characterRunAnimation.StartRunningAnimation(AnimationType.ThrowingTrash, 2f);
		_hasStartedRunningAnimation = true;
	}

	private void OnAnimationFinished()
	{
		BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, _trashBin.ItemInstance);
	}

	public override void OnEnd()
	{
		Reset();
	}

	public override void OnBehaviorComplete()
	{
		Reset();
	}

	private ItemController FindNearestTrashBin()
	{
		ItemController result = null;
		float num = float.MaxValue;
		Vector3 position = sharedCustomer.Value.tpc.transform.position;
		foreach (ItemController allItemController in InstanceBehavior<BuildingManager>.Instance.allItemControllers)
		{
			if ((bool)allItemController && allItemController.Item.HasTag(TagRef.Itemtag.istrashbin))
			{
				float sqrMagnitude = (allItemController.transform.position - position).sqrMagnitude;
				if (!(sqrMagnitude > num))
				{
					num = sqrMagnitude;
					result = allItemController;
				}
			}
		}
		return result;
	}

	private void Reset()
	{
		_characterMoveToPosition.StopCheckingDestination();
		_characterRotateTowards.StopRotating();
		_hasStartedRotating = false;
		_hasStartedRunningAnimation = false;
	}
}
