using System;
using System.Collections.Generic;
using System.Linq;
using BAModAPI;
using Blueprints;
using Buildings;
using DG.Tweening;
using HGAttributes;
using JimmysUnityUtilities;
using UI;
using UI.Elements;
using UI.Load;
using UnityEngine;

namespace BigAmbitions.BlueprintCreator;

public class BlueprintCreatorUI : MonoBehaviour
{
	[SerializeField]
	private CanvasGroup canvasGroup;

	[SerializeField]
	private Dropdown buildingTypeDropdown;

	[SerializeField]
	private Dropdown buildingVersionDropdown;

	[AutocompleteDropdown("BuildingTypes")]
	[SerializeField]
	private List<string> excludedBuildingTypes;

	private readonly List<string> _buildingTypeOptions = new List<string>();

	private readonly List<BuildingSizeInfo> _buildingVersionOptions = new List<BuildingSizeInfo>();

	private Action<string, BuildingSizeInfo> _onPreviewClick;

	private Action _onStartEditingClick;

	private void Start()
	{
		BlueprintCreatorManager.RegisterOnInit(OnBlueprintCreatorInit);
		SelectDefaultOptions();
	}

	private void OnEnable()
	{
		BlueprintCreatorManager.OnReturnToMainMenu += OnReturnToMainMenuClick;
	}

	private void OnDisable()
	{
		BlueprintCreatorManager.OnReturnToMainMenu -= OnReturnToMainMenuClick;
	}

	private void OnBlueprintCreatorInit()
	{
		InitializeBuildingTypeDropdown();
		buildingVersionDropdown.onOptionSelected.AddListener(OnBuildingVersionChanged);
	}

	public void SetUp(Action onStartEditingClick, Action<string, BuildingSizeInfo> onPreviewClick)
	{
		_onStartEditingClick = onStartEditingClick;
		_onPreviewClick = onPreviewClick;
	}

	public void Show(string buildingType, BuildingSizeInfo buildingSizeInfo)
	{
		base.gameObject.SetActive(value: true);
		canvasGroup.DOFade(1f, 0.5f).SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject);
		SetSelectedOptions(buildingType, buildingSizeInfo);
	}

	public void Hide()
	{
		canvasGroup.DOFade(0f, 0.5f).SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject)
			.OnComplete(delegate
			{
				base.gameObject.SetActive(value: false);
			});
	}

	public void OnStartEditingClick()
	{
		_onStartEditingClick?.Invoke();
	}

	public void OnReturnToMainMenuClick()
	{
		BuildingPreview.isPreviewing = false;
		LoadScene.LoadMainMenu(ModActivationScope.BlueprintCreator);
	}

	private void InitializeBuildingTypeDropdown()
	{
		Dictionary<string, BuildingTypeData>.KeyCollection keys = BuildingTypeHelper.BuildingTypes.Keys;
		List<string> buildingTypeOptions = _buildingTypeOptions;
		IEnumerable<string> collection;
		if (!GameManager.IsDevMode)
		{
			collection = keys.Except(excludedBuildingTypes);
		}
		else
		{
			IEnumerable<string> enumerable = keys;
			collection = enumerable;
		}
		buildingTypeOptions.AddRange(collection);
		buildingTypeDropdown.SetOptions(_buildingTypeOptions.ToList());
		buildingTypeDropdown.onOptionSelected.AddListener(OnBuildingTypeChanged);
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			buildingTypeDropdown.SelectOption(0);
		});
	}

	private void OnBuildingTypeChanged(int index)
	{
		bool update = BlueprintCreatorSystem.OpenWithBlueprint == null;
		string type = _buildingTypeOptions[index];
		LoadBuildingVersions(type, update);
	}

	private void LoadBuildingVersions(string type, bool update)
	{
		_buildingVersionOptions.Clear();
		_buildingVersionOptions.AddRange(BuildingSizeHelper.GetBuildingVersionsByBuildingType(type));
		buildingVersionDropdown.SetOptions(_buildingVersionOptions.Select((BuildingSizeInfo info) => info.ToString()).ToList(), localize: false);
		if (update)
		{
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				buildingVersionDropdown.SelectOption(0);
			});
		}
		else
		{
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				buildingVersionDropdown.SetVisualSelectedOption(0);
			});
		}
	}

	private void OnBuildingVersionChanged(int index)
	{
		if (BlueprintCreatorSystem.OpenWithBlueprint == null)
		{
			string arg = _buildingTypeOptions[buildingTypeDropdown.SelectedOptionIndex];
			_onPreviewClick?.Invoke(arg, _buildingVersionOptions[index]);
		}
	}

	private void SelectDefaultOptions()
	{
		if (_buildingTypeOptions.Count > 0)
		{
			buildingTypeDropdown.SelectOption(0);
		}
		if (_buildingVersionOptions.Count > 0)
		{
			buildingVersionDropdown.SelectOption(0);
		}
	}

	private void SetSelectedOptions(string type, BuildingSizeInfo sizeInfo)
	{
		buildingTypeDropdown.SetVisualSelectedOption(_buildingTypeOptions.IndexOf(type));
		buildingVersionDropdown.SetVisualSelectedOption(_buildingVersionOptions.IndexOf(sizeInfo));
	}
}
