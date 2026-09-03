using UnityEngine;
using UnityEngine.UI;

namespace UI.Components;

public class ScrollBarHandleSizeLimiter : MonoBehaviour
{
	[Range(0f, 1f)]
	[Tooltip("The minimum size of the scrollbar handle in percentage, where 1 means it fills the entire scrollbar.")]
	public float minSize;

	[SerializeField]
	private Scrollbar scrollbar;

	private void LateUpdate()
	{
		scrollbar.size = Mathf.Max(scrollbar.size, minSize);
	}
}
