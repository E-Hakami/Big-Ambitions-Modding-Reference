using UnityEngine;

[DisallowMultipleComponent]
public class ViewBlockingEntityExtension : MonoBehaviour
{
	[SerializeField]
	private ViewBlockingEntity viewBlockingEntity;

	private void Awake()
	{
		if (viewBlockingEntity == null)
		{
			Debug.LogWarning("ViewBlockingEntityExtension on " + base.name + " has no ViewBlockingEntity assigned.", this);
		}
	}

	public bool TryGetViewBlockingEntity(out ViewBlockingEntity result)
	{
		result = viewBlockingEntity;
		return result != null;
	}
}
