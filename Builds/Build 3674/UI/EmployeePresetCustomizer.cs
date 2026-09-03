using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

public class EmployeePresetCustomizer : MonoBehaviour
{
	[SerializeField]
	private Transform presetButtonTemplate;

	[SerializeField]
	private Transform newPresetButton;

	[SerializeField]
	private UniformCustomizer uniformCustomizer;

	[SerializeField]
	private Vector3 cameraPosition;

	private List<(Transform presetButton, string presetId)> _presetsEntries;

	private Transform _selectedPresetButton;

	private Action _onClose;

	private bool _previousCharacterZoomEnabled;

	public void Show(string initialPresetId = null, Gender gender = Gender.Male, Action onClose = null)
	{
		CharacterZoom characterZoom = InstanceBehavior<GameManager>.Instance.employeeUniformPreview.characterZoom;
		_previousCharacterZoomEnabled = characterZoom.enabled;
		characterZoom.enabled = false;
		LoadPresetEntries();
		if (string.IsNullOrEmpty(initialPresetId) && _presetsEntries.Count > 0)
		{
			SelectPreset(_presetsEntries[0].presetId);
		}
		else
		{
			SelectPreset(initialPresetId);
		}
		if (gender == Gender.Male)
		{
			uniformCustomizer.SelectMale();
		}
		else
		{
			uniformCustomizer.SelectFemale();
		}
		_onClose = onClose;
		CoroutineUtility.Run(ShowAfterInitialization());
	}

	private IEnumerator ShowAfterInitialization()
	{
		yield return new WaitForEndOfFrame();
		base.gameObject.SetActive(value: true);
		yield return null;
		uniformCustomizer.OpenFirstCategory();
	}

	public void Close(bool force = false)
	{
		if (!force && uniformCustomizer.HasUnsavedChanges())
		{
			LanguageChangeEventDataHolder bodyData = "myemployees_preset_customizer_unsaved_changes_warning".Localize();
			Action onConfirmAction = CloseEmployeePresetCustomizer;
			HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, onConfirmAction);
		}
		else
		{
			CloseEmployeePresetCustomizer();
		}
	}

	private void CloseEmployeePresetCustomizer()
	{
		uniformCustomizer.Hide();
		InstanceBehavior<GameManager>.Instance.employeeUniformPreview.Hide();
		InstanceBehavior<GameManager>.Instance.employeeUniformPreview.characterZoom.enabled = _previousCharacterZoomEnabled;
		base.gameObject.SetActive(value: false);
		_onClose?.Invoke();
	}

	public void CreateNewPreset()
	{
		EmployeePreset fromPreset = BusinessTypeHelper.GetData("ba:businesstype_giftshop").uniforms[0].Copy();
		CreateFromPreset(fromPreset);
	}

	private void CreateFromPreset(EmployeePreset fromPreset, int siblingIndex = -1, bool defaultName = true)
	{
		if (uniformCustomizer.HasUnsavedChanges())
		{
			LanguageChangeEventDataHolder bodyData = "myemployees_preset_customizer_unsaved_changes_warning".Localize();
			Action onConfirmAction = CreatePreset;
			HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, onConfirmAction);
		}
		else
		{
			CreatePreset();
		}
		void CreatePreset()
		{
			EmployeePreset employeePreset = fromPreset.Copy();
			employeePreset.name = (defaultName ? "myemployees_preset_customizer_generic_name".Localize(new
			{
				number = SaveGameManager.Current.employeePresets.Count + 1
			}).ToString() : GetDuplicateName(employeePreset));
			employeePreset.id = UuidHelper.GenerateBase64Uuid();
			if (siblingIndex < 0)
			{
				SaveGameManager.Current.employeePresets.Add(employeePreset);
			}
			else
			{
				SaveGameManager.Current.employeePresets.Insert(siblingIndex, employeePreset);
			}
			AddPresetEntry(employeePreset, select: true, siblingIndex);
		}
	}

	private void LoadPresetEntries()
	{
		_selectedPresetButton = null;
		presetButtonTemplate.ResetTemplate();
		_presetsEntries = new List<(Transform, string)>();
		foreach (EmployeePreset employeePreset in SaveGameManager.Current.employeePresets)
		{
			AddPresetEntry(employeePreset, select: false, _presetsEntries.Count);
		}
	}

	private void AddPresetEntry(EmployeePreset preset, bool select, int siblingIndex = -1)
	{
		Transform presetButton = UnityEngine.Object.Instantiate(presetButtonTemplate, presetButtonTemplate.parent);
		TMP_InputField tmpInputByName = presetButton.GetTmpInputByName("NameInputField");
		tmpInputByName.text = preset.name;
		tmpInputByName.onEndEdit.AddListener(delegate(string updatedName)
		{
			preset.name = updatedName;
		});
		presetButton.GetButtonByName("Delete").onClick.AddListener(delegate
		{
			LanguageChangeEventDataHolder bodyData = "myemployees_preset_customizer_remove_preset".Localize(new
			{
				presetName = preset.name
			});
			HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
			{
				DeletePreset(presetButton, preset.id);
			});
		});
		presetButton.GetButtonByName("Duplicate").onClick.AddListener(delegate
		{
			DuplicatePreset(preset.id);
		});
		presetButton.GetComponent<Button>().onClick.AddListener(delegate
		{
			if (uniformCustomizer.HasUnsavedChanges())
			{
				LanguageChangeEventDataHolder bodyData = "myemployees_preset_customizer_unsaved_changes_warning".Localize();
				HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
				{
					SelectPreset(preset.id);
				});
			}
			else
			{
				SelectPreset(preset.id);
			}
		});
		presetButton.gameObject.SetActive(value: true);
		_presetsEntries.Add((presetButton, preset.id));
		if (siblingIndex >= 0)
		{
			newPresetButton.SetAsFirstSibling();
			presetButton.SetSiblingIndex(1 + siblingIndex);
		}
		else
		{
			presetButton.transform.SetAsFirstSibling();
			newPresetButton.SetAsFirstSibling();
		}
		if (select)
		{
			SelectPreset(preset.id);
		}
	}

	private void SelectPreset(string presetId)
	{
		if (_selectedPresetButton != null)
		{
			_selectedPresetButton.Find("Selected").gameObject.SetActive(value: false);
		}
		EmployeePreset employeePreset = SaveGameManager.Current.employeePresets.FirstOrDefault((EmployeePreset x) => x.id == presetId);
		(Transform, string) tuple = _presetsEntries.FirstOrDefault(((Transform presetButton, string presetId) x) => x.presetId == presetId);
		if (employeePreset != null)
		{
			var (transform, text) = tuple;
			if (!(transform == null) || !(text == null))
			{
				(_selectedPresetButton, _) = tuple;
				_selectedPresetButton.Find("Selected").gameObject.SetActive(value: true);
				InstanceBehavior<GameManager>.Instance.employeeUniformPreview.SetCameraPosition(cameraPosition);
				InstanceBehavior<GameManager>.Instance.employeeUniformPreview.Show();
				uniformCustomizer.Show(employeePreset);
				return;
			}
		}
		uniformCustomizer.Hide();
		InstanceBehavior<GameManager>.Instance.employeeUniformPreview.Hide();
	}

	private void DeletePreset(Transform presetButton, string presetId)
	{
		_presetsEntries.RemoveAll(((Transform presetButton, string presetId) x) => x.presetId == presetId);
		if (_selectedPresetButton == presetButton)
		{
			if (_presetsEntries.Count > 0)
			{
				SelectPreset(_presetsEntries[0].presetId);
			}
			else
			{
				uniformCustomizer.Hide();
				InstanceBehavior<GameManager>.Instance.employeeUniformPreview.Hide();
			}
		}
		SaveGameManager.Current.employeePresets.RemoveAll((EmployeePreset x) => x.id == presetId);
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer)
			{
				buildingRegistration.uniformsBySkill.RemoveAllEntriesWithValue(presetId);
			}
		}
		UnityEngine.Object.Destroy(presetButton.gameObject);
	}

	private void DuplicatePreset(string presetId)
	{
		int num = SaveGameManager.Current.employeePresets.FindIndex((EmployeePreset x) => x.id == presetId);
		if (num >= 0)
		{
			EmployeePreset fromPreset = SaveGameManager.Current.employeePresets[num];
			CreateFromPreset(fromPreset, num + 1, defaultName: false);
		}
	}

	private static string GetDuplicateName(EmployeePreset preset)
	{
		string arg = preset.name;
		string duplicateName = preset.name;
		int num = 1;
		int num2 = duplicateName.LastIndexOf(" #", StringComparison.Ordinal);
		if (num2 > 0)
		{
			if (int.TryParse(duplicateName.Substring(num2 + 1), out var result))
			{
				num = result + 1;
			}
			arg = duplicateName.Substring(0, num2).TrimEnd();
			duplicateName = $"{arg} #{num}";
		}
		while (SaveGameManager.Current.employeePresets.Exists((EmployeePreset x) => x.name == duplicateName))
		{
			duplicateName = $"{arg} #{num}";
			num++;
		}
		return duplicateName;
	}
}
