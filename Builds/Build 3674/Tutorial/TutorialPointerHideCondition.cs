using UnityEngine;

namespace Tutorial;

public abstract class TutorialPointerHideCondition : ScriptableObject
{
	[SerializeField]
	protected bool invertCondition;

	public bool ConditionMet()
	{
		return ConditionMetInternal() ^ invertCondition;
	}

	protected virtual bool ConditionMetInternal()
	{
		return false;
	}
}
