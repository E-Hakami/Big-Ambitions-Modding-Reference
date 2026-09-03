using Localizor.LanguageChangeEvent;
using Player.DifficultySettings;
using TMPro;
using UnityEngine;

namespace UI.MainMenu;

public class DifficultyIndicatorTemplate : MonoBehaviour
{
	[SerializeField]
	private TMP_Text difficultyField;

	[SerializeField]
	private TextLocalizationComponent headerField;

	public void SetUp(DifficultyIndicator indicator)
	{
		headerField.Key = indicator.key;
		difficultyField.color = indicator.color;
		difficultyField.text = GetDifficultyText(indicator);
	}

	private static string GetDifficultyText(DifficultyIndicator indicator)
	{
		return indicator.difficulty switch
		{
			1 => "+", 
			2 => "++", 
			-1 => "-", 
			_ => "", 
		};
	}
}
