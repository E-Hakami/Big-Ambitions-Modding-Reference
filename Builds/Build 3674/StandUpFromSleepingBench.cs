using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Big Ambitions")]
public class StandUpFromSleepingBench : Action
{
	public SharedTransform benchAiSeatingPosition;

	public override void OnStart()
	{
		OutsideBenchController componentInParent = benchAiSeatingPosition.Value.GetComponentInParent<OutsideBenchController>();
		if (componentInParent.currentlySeatedAi != gameObject)
		{
			Debug.LogError("Pedestrian: Tried to release pedestrian from incorrect bench.");
			return;
		}
		GetComponent<ThirdPersonCharacter>().Reset();
		componentInParent.currentlySeatedAi = null;
	}
}
