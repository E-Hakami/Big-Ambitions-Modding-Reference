using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Extensions;

[TaskCategory("Big Ambitions/Pedestrian")]
public class GetRandomPedestrianSpawnerWithinDistance : Action
{
	public SharedFloat minimumDistance;

	public SharedFloat maximumDistance;

	public SharedVector3 origin;

	public SharedVector3 targetPosition;

	private List<StationaryAiSpawner> _spawners;

	public override TaskStatus OnUpdate()
	{
		StationaryAiSpawner randomPedestrianSpawner = GetRandomPedestrianSpawner();
		if (randomPedestrianSpawner == null)
		{
			return TaskStatus.Failure;
		}
		if (randomPedestrianSpawner.GetPosition(out var pos))
		{
			targetPosition.Value = pos;
			return TaskStatus.Success;
		}
		return TaskStatus.Failure;
	}

	private StationaryAiSpawner GetRandomPedestrianSpawner()
	{
		_spawners.Clear();
		foreach (StationaryAiSpawner allPedestrianSpawner in StationaryAiSpawner.AllPedestrianSpawners)
		{
			if (!allPedestrianSpawner.disabled)
			{
				float num = MathHelper.DistanceSqr(allPedestrianSpawner.transform.position, origin.Value);
				if (num > minimumDistance.Value * minimumDistance.Value && num < maximumDistance.Value * maximumDistance.Value)
				{
					_spawners.Add(allPedestrianSpawner);
				}
			}
		}
		return _spawners.GetRandom();
	}
}
