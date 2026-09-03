using Localizor.LanguageChangeEvent;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Components;

public class TabMenuButton : MonoBehaviour
{
	public TextLocalizationComponent localization;

	public TMP_Text text;

	public Button button;

	[ReadOnly]
	public int index;

	public void SetUp(string textLocalizationKey, int i, string objectName)
	{
		base.name = objectName;
		localization.Key = textLocalizationKey;
		index = i;
		text.color = ((i == 0) ? InstanceBehavior<GlobalReferences>.Instance.colors.white : InstanceBehavior<GlobalReferences>.Instance.colors.lightGrey);
	}
}
