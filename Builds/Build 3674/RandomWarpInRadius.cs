using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Big Ambitions")]
public class RandomWarpInRadius : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	public SharedFloat randomRadius = 5f;

	public SharedFloat offset = 0f;

	public override void OnStart()
	{
		Vector3 randomPosition = sharedCustomer.Value.tpc.GetRandomPosition(randomRadius.Value, offset.Value);
		sharedCustomer.Value.tpc.navmeshAgent.Warp(randomPosition);
	}
}
