using System;
using System.Collections.Generic;
using JimmysUnityUtilities;
using UnityEngine;

namespace BigAmbitions;

public class ModsRow : MonoBehaviour
{
	[SerializeField]
	private SubscribedModUI modUiPrefab;

	[SerializeField]
	private List<Transform> modParentTransforms;

	private readonly List<SubscribedModUI> _modUis = new List<SubscribedModUI>();

	public void Setup(List<ModInfo> modInfos, Action onToggle)
	{
		if (modInfos.Count > modParentTransforms.Count)
		{
			Debug.LogWarning("[ModsRow] More mods than available slots in this row", this);
		}
		_modUis.Clear();
		foreach (Transform modParentTransform in modParentTransforms)
		{
			modParentTransform.DestroyAllChildren();
		}
		for (int i = 0; i < modInfos.Count; i++)
		{
			SubscribedModUI subscribedModUI = UnityEngine.Object.Instantiate(modUiPrefab, modParentTransforms[i]);
			subscribedModUI.Setup(modInfos[i]);
			subscribedModUI.onToggle = onToggle;
			_modUis.Add(subscribedModUI);
		}
	}

	public void UpdateConflicts()
	{
		foreach (SubscribedModUI modUi in _modUis)
		{
			modUi.UpdateConflicts();
		}
	}
}
