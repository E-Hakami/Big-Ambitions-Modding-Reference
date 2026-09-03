using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Characters;
using BigAmbitions.DayNightCycle;
using Buildings.BuildingTypes.Shared.Dirtiness;
using UnityEngine;

[TaskCategory("Big Ambitions/Gym")]
public class UseShower : Action
{
	private const int MinShowerTime = 3;

	private const int MaxShowerTime = 15;

	[RequiredField]
	public SharedGymCustomer sharedGymCustomer;

	[RequiredField]
	public SharedItemController sharedShowerItemController;

	private PublicShowerController _publicShowerController;

	private Timestamp _stopShoweringStamp;

	public override void OnStart()
	{
		_publicShowerController = sharedShowerItemController.Value as PublicShowerController;
		if (!(_publicShowerController == null))
		{
			_publicShowerController.Occupied = true;
			_publicShowerController.EnableParticles();
			sharedGymCustomer.Value.tpc.animator.SetBool(PermanentAnimationType.Showering);
			sharedGymCustomer.Value.tpc.appearanceSetter.SetNakedAppearance();
			_stopShoweringStamp = TimeHelper.Now();
			_stopShoweringStamp.AddMinutes(Random.Range(3, 15));
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (_publicShowerController == null)
		{
			return TaskStatus.Failure;
		}
		if (!_stopShoweringStamp.IsInThePast())
		{
			return TaskStatus.Running;
		}
		OnShoweringFinished();
		return TaskStatus.Success;
	}

	private void OnShoweringFinished()
	{
		_publicShowerController.DisableParticles();
		sharedGymCustomer.Value.tpc.animator.SetBool(PermanentAnimationType.Showering, state: false);
		sharedGymCustomer.Value.ChangeGymClothes(backToOriginal: true);
		_publicShowerController.Occupied = false;
		BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, _publicShowerController.ItemInstance);
		_publicShowerController = null;
	}

	public override void OnBehaviorComplete()
	{
		if (BuildingManager.IsInsideBuilding && !(_publicShowerController == null))
		{
			OnShoweringFinished();
		}
	}
}
