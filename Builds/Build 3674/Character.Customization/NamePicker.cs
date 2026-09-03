using System.IO;
using System.Linq;
using BigAmbitions.Characters;
using Localizor;
using Localizor.LanguageChangeEvent;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Character.Customization;

public class NamePicker : MonoBehaviour
{
	[SerializeField]
	private CharacterCustomizer controller;

	[SerializeField]
	private TMP_InputField nameField;

	[SerializeField]
	private Color warningColor;

	[SerializeField]
	private TextLocalizationComponent nameShowcase;

	private Image _fieldBackgroundImage;

	private bool _usingThisTextInput;

	public bool HasANameSet => !string.IsNullOrWhiteSpace(GetName());

	public bool HasInvalidCharacters => GetName().Any((char x) => Path.GetInvalidFileNameChars().Contains(x));

	public string GetName()
	{
		return nameField.text;
	}

	private void Start()
	{
		_fieldBackgroundImage = nameField.GetComponent<Image>();
		nameField.ActivateInputField();
		UpdateName();
		if (!Singleton<SteamAPI>.Instance.steamApiEnabled || !SteamUtils.IsSteamInBigPictureMode)
		{
			return;
		}
		nameField.onSelect.AddListener(delegate
		{
			_usingThisTextInput = true;
			SteamUtils.ShowGamepadTextInput(GamepadTextInputMode.Normal, GamepadTextInputLineMode.SingleLine, "intro_name_placeholder".GetLocalization(), (nameField.characterLimit == 0) ? 100 : nameField.characterLimit, nameField.text);
		});
		SteamUtils.OnGamepadTextInputDismissed += delegate(bool submitted)
		{
			if (_usingThisTextInput & submitted)
			{
				nameField.text = SteamUtils.GetEnteredGamepadText();
			}
			_usingThisTextInput = false;
		};
	}

	public void SetRandomName()
	{
		BigAmbitions.Characters.Gender gender = controller.appearanceSetter.data.gender;
		string randomNameForGender = CharacterNames.GetRandomNameForGender(gender);
		while (randomNameForGender == GetName())
		{
			randomNameForGender = CharacterNames.GetRandomNameForGender(gender);
		}
		nameField.text = randomNameForGender;
	}

	public void UpdateName()
	{
		_fieldBackgroundImage.color = ((nameField.isFocused || HasANameSet) ? Color.white : warningColor);
		if (HasANameSet)
		{
			nameShowcase.Key = "";
			nameShowcase.SetValue(GetName());
		}
		else
		{
			nameShowcase.Key = "intro_character_name";
		}
	}
}
