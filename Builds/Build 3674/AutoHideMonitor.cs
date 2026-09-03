using System.Collections.Generic;
using JimmysUnityUtilities;
using UI.Components.AutoHide;
using UnityEngine;

public class AutoHideMonitor : MonoBehaviour
{
	private readonly List<AutoHideBase> _autoHides = new List<AutoHideBase>();

	private void OnDestroy()
	{
		_autoHides.Clear();
	}

	private void OnRectTransformDimensionsChange()
	{
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			foreach (AutoHideBase autoHide in _autoHides)
			{
				autoHide.OnMonitorRectChange();
			}
		});
	}

	public void Register(AutoHideBase autoHide)
	{
		_autoHides.Add(autoHide);
	}

	public void Unregister(AutoHideBase autoHide)
	{
		_autoHides.Remove(autoHide);
	}
}
