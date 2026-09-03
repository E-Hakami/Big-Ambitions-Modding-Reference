using UnityEngine;

public abstract class ExitCondition : ScriptableObject
{
	public virtual string BlockedNotificationKey => null;

	public abstract bool CanExit();
}
