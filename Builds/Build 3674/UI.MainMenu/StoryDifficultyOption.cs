using Extensions;
using Localizor.LanguageChangeEvent;
using Player.DifficultySettings;
using UnityEngine;

namespace UI.MainMenu;

public class StoryDifficultyOption : MonoBehaviour
{
	[SerializeField]
	private TextLocalizationComponent headerField;

	[SerializeField]
	private Transform template;

	[SerializeField]
	private GameObject selectedOutline;

	public int Index { get; private set; }

	public void SetUp(DifficultySetting difficultySetting, int index)
	{
		Index = index;
		template.ResetTemplate();
		headerField.Key = difficultySetting.key;
		foreach (DifficultyIndicator indicator in difficultySetting.indicators)
		{
			DifficultyIndicatorTemplate component = Object.Instantiate(template, template.parent).GetComponent<DifficultyIndicatorTemplate>();
			component.SetUp(indicator);
			component.gameObject.SetActive(value: true);
		}
	}

	public void SetSelected(bool selected)
	{
		selectedOutline.SetActive(selected);
	}
}
