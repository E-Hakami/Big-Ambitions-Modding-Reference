using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.Characters.Appearance;
using Character.Customization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UniformCustomizer : CharacterCustomizer
{
	private static Color32 PreviewSkinColor = Color.white;

	private const float PreviewStrength = 0f;

	private const float PreviewFatness = 0.5f;

	public UnityEvent onUniformSave = new UnityEvent();

	[SerializeField]
	private Button saveChangesToSelectedPresetButton;

	[SerializeField]
	private Transform maleButton;

	[SerializeField]
	private Transform femaleButton;

	private EmployeePreset _employeePreset;

	private List<AppearanceElementData> _modifiedMaleElements;

	private List<AppearanceElementData> _modifiedFemaleElements;

	private Transform _currentSelectedElementButton;

	private void Awake()
	{
		onAppearanceChange = (Action)Delegate.Combine(onAppearanceChange, (Action)delegate
		{
			saveChangesToSelectedPresetButton.interactable = true;
		});
	}

	public void Show(EmployeePreset employeePreset)
	{
		_employeePreset = employeePreset;
		saveChangesToSelectedPresetButton.interactable = false;
		_modifiedMaleElements = employeePreset.maleElements.Copy();
		_modifiedFemaleElements = employeePreset.femaleElements.Copy();
		if (!base.gameObject.activeSelf || appearanceSetter == null)
		{
			if ((object)appearanceSetter == null)
			{
				appearanceSetter = InstanceBehavior<GameManager>.Instance.employeeUniformPreview.appearanceSetter;
			}
			appearanceSetter.data.elements.RemoveAll(delegate(AppearanceElementData x)
			{
				AppearanceElementType type = x.type;
				return type == AppearanceElementType.Hair || type == AppearanceElementType.Head;
			});
			appearanceSetter.data.color = PreviewSkinColor;
			maleButton.Find("Selected").gameObject.SetActive(appearanceSetter.data.gender == BigAmbitions.Characters.Gender.Male);
			femaleButton.Find("Selected").gameObject.SetActive(appearanceSetter.data.gender == BigAmbitions.Characters.Gender.Female);
			UpdatePreviewAppearance();
			Show(AppearanceElementType.Torso);
			base.gameObject.SetActive(value: true);
		}
		else
		{
			UpdatePreviewAppearance();
		}
	}

	public void SelectMale()
	{
		SelectGender(BigAmbitions.Characters.Gender.Male);
		maleButton.Find("Selected").gameObject.SetActive(value: true);
		femaleButton.Find("Selected").gameObject.SetActive(value: false);
	}

	public void SelectFemale()
	{
		SelectGender(BigAmbitions.Characters.Gender.Female);
		maleButton.Find("Selected").gameObject.SetActive(value: false);
		femaleButton.Find("Selected").gameObject.SetActive(value: true);
	}

	private void SelectGender(BigAmbitions.Characters.Gender gender)
	{
		if (appearanceSetter.data.gender == BigAmbitions.Characters.Gender.Female)
		{
			_modifiedFemaleElements = GetCurrentUniformElements();
		}
		else
		{
			_modifiedMaleElements = GetCurrentUniformElements();
		}
		appearanceSetter.data.gender = gender;
		appearanceSetter.data.color = PreviewSkinColor;
		appearanceSetter.data.strength = 0f;
		appearanceSetter.data.fatness = 0.5f;
		UpdatePreviewAppearance();
	}

	private void UpdatePreviewAppearance()
	{
		List<AppearanceElementData> list = ((appearanceSetter.data.gender == BigAmbitions.Characters.Gender.Male) ? _modifiedMaleElements : _modifiedFemaleElements);
		if (list == null || list.Count == 0)
		{
			CharacterData appearance = new CharacterData
			{
				gender = appearanceSetter.data.gender,
				color = PreviewSkinColor,
				strength = 0f,
				fatness = 0.5f
			};
			appearanceSetter.SetAppearance(appearance);
			appearanceSetter.RandomizeElement(AppearanceElementType.HeadAccessory, tags);
			appearanceSetter.RandomizeElement(AppearanceElementType.Torso, tags);
			appearanceSetter.RandomizeElement(AppearanceElementType.TorsoAccessory, tags);
			appearanceSetter.RandomizeElement(AppearanceElementType.Legs, tags);
			appearanceSetter.RandomizeElement(AppearanceElementType.Feet, tags);
			appearanceSetter.UpdateVisuals();
			appearanceSetter.UpdateVisualAge();
		}
		else
		{
			appearanceSetter.UpdateElements(list);
		}
		Show(currentElementType);
	}

	public void SaveChangesToSelectedPreset()
	{
		if (_employeePreset != null)
		{
			if (appearanceSetter.data.gender == BigAmbitions.Characters.Gender.Female)
			{
				_modifiedFemaleElements = GetCurrentUniformElements();
			}
			else
			{
				_modifiedMaleElements = GetCurrentUniformElements();
			}
			_employeePreset.maleElements = _modifiedMaleElements.Copy();
			_employeePreset.femaleElements = _modifiedFemaleElements.Copy();
			saveChangesToSelectedPresetButton.interactable = false;
			onUniformSave.Invoke();
			InstanceBehavior<BuildingManager>.Instance?.onUniformChanged.Invoke(string.Empty, _employeePreset.id);
		}
	}

	private List<AppearanceElementData> GetCurrentUniformElements()
	{
		return appearanceSetter.data.elements.Where((AppearanceElementData x) => x.type != AppearanceElementType.Hair && x.type != AppearanceElementType.Head).ToList();
	}

	public bool HasUnsavedChanges()
	{
		return saveChangesToSelectedPresetButton.interactable;
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
	}

	public void OpenFirstCategory()
	{
		menu.OnCategoryButtonClick(menu.categories[0]);
	}
}
