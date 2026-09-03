using UnityEngine;
using UnityEngine.Events;

namespace Character;

public class AnimationTriggerEvents : MonoBehaviour
{
	[HideInInspector]
	public UnityEvent oneActionTrigger;

	public void TriggerOneAction()
	{
		oneActionTrigger.Invoke();
	}

	public void DiscardHeldItemToConsume()
	{
		if (base.transform.IsChildOf(InstanceBehavior<GameManager>.Instance.playerController.transform))
		{
			InstanceBehavior<GameManager>.Instance.playerController.DiscardHeldItemToConsume();
		}
	}
}
