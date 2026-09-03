using UnityEngine;

[ExecuteAlways]
public class VisibilityProbe : MonoBehaviour
{
	private int _visibleCount;

	public bool IsVisible => _visibleCount > 0;

	private void OnBecameVisible()
	{
		_visibleCount++;
	}

	private void OnBecameInvisible()
	{
		_visibleCount = Mathf.Max(0, _visibleCount - 1);
	}
}
