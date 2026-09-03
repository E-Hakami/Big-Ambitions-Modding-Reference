using JimmysUnityUtilities;
using NaughtyAttributes;
using UnityEngine;

namespace UI.Components.AutoHide;

public abstract class AutoHideBase : MonoBehaviour
{
	[SerializeField]
	private RectTransform stretchableArea;

	[SerializeField]
	protected RectTransform contentToCheck;

	[SerializeField]
	[BoxGroup("Horizontal")]
	private bool horizontal;

	[SerializeField]
	[BoxGroup("Horizontal")]
	[ShowIf("horizontal")]
	private bool useContentWidth = true;

	[SerializeField]
	[BoxGroup("Horizontal")]
	[HideIf("useContentWidth")]
	private float widthThreshold;

	[SerializeField]
	[BoxGroup("Vertical")]
	private bool vertical;

	[SerializeField]
	[BoxGroup("Vertical")]
	[ShowIf("vertical")]
	private bool useContentHeight = true;

	[SerializeField]
	[BoxGroup("Vertical")]
	[HideIf("useContentHeight")]
	private float heightThreshold;

	private AutoHideMonitor _monitor;

	private void Start()
	{
		_monitor = stretchableArea.GetOrAddComponent<AutoHideMonitor>();
		_monitor.Register(this);
		OnMonitorRectChange();
	}

	private void OnDestroy()
	{
		if (_monitor != null)
		{
			_monitor.Unregister(this);
		}
	}

	public void OnMonitorRectChange()
	{
		if (horizontal)
		{
			if (useContentWidth)
			{
				OnHideChange(stretchableArea.rect.width >= contentToCheck.rect.width);
			}
			else
			{
				OnHideChange(stretchableArea.rect.width >= widthThreshold);
			}
		}
		else if (vertical)
		{
			if (useContentHeight)
			{
				OnHideChange(stretchableArea.rect.height >= contentToCheck.rect.height);
			}
			else
			{
				OnHideChange(stretchableArea.rect.height >= heightThreshold);
			}
		}
	}

	protected abstract void OnHideChange(bool hide);
}
