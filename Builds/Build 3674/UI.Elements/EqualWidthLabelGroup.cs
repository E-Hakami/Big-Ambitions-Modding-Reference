using System;
using System.Collections.Generic;
using JimmysUnityUtilities;
using Localizor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Elements;

[ExecuteAlways]
public class EqualWidthLabelGroup : MonoBehaviour
{
	[SerializeField]
	private List<TMP_Text> labels;

	private void OnEnable()
	{
		LocalizorManager.OnLanguageChanged = (Action)Delegate.Combine(LocalizorManager.OnLanguageChanged, new Action(ScheduleMatch));
		ScheduleMatch();
	}

	private void OnDisable()
	{
		LocalizorManager.OnLanguageChanged = (Action)Delegate.Remove(LocalizorManager.OnLanguageChanged, new Action(ScheduleMatch));
	}

	private void ScheduleMatch()
	{
		CoroutineUtility.RunAfterOneFrame(Match);
	}

	private void Match()
	{
		float num = 0f;
		foreach (TMP_Text label in labels)
		{
			if ((bool)label)
			{
				float x = label.GetPreferredValues().x;
				if (x > num)
				{
					num = x;
				}
			}
		}
		foreach (TMP_Text label2 in labels)
		{
			if ((bool)label2)
			{
				if (!label2.TryGetComponent<LayoutElement>(out var component))
				{
					component = label2.gameObject.AddComponent<LayoutElement>();
				}
				component.preferredWidth = num;
			}
		}
	}
}
