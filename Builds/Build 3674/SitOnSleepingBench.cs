using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions")]
public class SitOnSleepingBench : Action
{
	public SharedTransform sittingPosition;

	public override void OnStart()
	{
		GetComponent<ThirdPersonCharacter>().SitOnChair(sittingPosition.Value);
		sittingPosition.Value.GetComponentInParent<OutsideBenchController>().currentlySeatedAi = gameObject;
	}
}
