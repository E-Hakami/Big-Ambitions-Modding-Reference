using System.Collections.Generic;
using BigAmbitions.Characters;
using UnityEngine;

namespace UI.Smartphone.Apps.Contacts;

[CreateAssetMenu(menuName = "BigAmbitions/Apps/Contacts/EmployeeContactIconSettings", fileName = "EmployeeContactIconSettings")]
public class EmployeeContactIconSettings : ScriptableObject
{
	[SerializeField]
	private LetterEntry[] letters;

	[SerializeField]
	private Sprite fallbackSprite;

	[SerializeField]
	private Color maleTint = new Color(0.4f, 0.6f, 1f);

	[SerializeField]
	private Color femaleTint = new Color(1f, 0.4f, 0.4f);

	[SerializeField]
	private Color unknownGenderTint = Color.white;

	private Dictionary<char, Sprite> _letterMap;

	private void OnEnable()
	{
		_letterMap = new Dictionary<char, Sprite>(letters.Length);
		LetterEntry[] array = letters;
		for (int i = 0; i < array.Length; i++)
		{
			LetterEntry letterEntry = array[i];
			if (!(letterEntry.sprite == null))
			{
				_letterMap[char.ToUpperInvariant(letterEntry.letter)] = letterEntry.sprite;
			}
		}
	}

	public ContactIconData Resolve(char firstLetter, Gender? gender)
	{
		char upper = char.ToUpperInvariant(firstLetter);
		Sprite sprite = FindSprite(upper);
		Color tint = ((gender == Gender.Male) ? maleTint : ((gender != Gender.Female) ? unknownGenderTint : femaleTint));
		return new ContactIconData(tint, sprite);
	}

	private Sprite FindSprite(char upper)
	{
		if (_letterMap.TryGetValue(upper, out var value))
		{
			return value;
		}
		return fallbackSprite;
	}
}
