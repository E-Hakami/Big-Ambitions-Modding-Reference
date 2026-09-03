using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.InteriorDesigner.InteriorElements;
using BigAmbitions.InteriorDesigner.Tools;
using Buildings.Indoors.InteriorDesign;
using Extensions;
using JimmysUnityUtilities;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace UI.InteriorDesigner;

public class InteriorElementActionPanelUI : ActionPanelUI
{
	[SerializeField]
	private TextLocalizationComponent titleText;

	[SerializeField]
	private Transform interiorElementUiTemplate;

	[SerializeField]
	private Transform colorPicker;

	[SerializeField]
	private Transform colorTemplate;

	private readonly List<InteriorElementOptionUi> _interiorElementOptionUis = new List<InteriorElementOptionUi>();

	private InteriorElementOptionUi _selectedInteriorElementOptionUi;

	public override ToolName[] ToolNames => new ToolName[2]
	{
		ToolName.Floor,
		ToolName.Wall
	};

	private void Awake()
	{
		InteriorElementRevertibleAction.onInteriorElementsPresetsChanged = (Action)Delegate.Combine(InteriorElementRevertibleAction.onInteriorElementsPresetsChanged, new Action(OnInteriorElementsPresetsChanged));
	}

	public override void OnOpen()
	{
		titleText.Key = ((InteriorDesignerController.CurrentToolName == ToolName.Wall) ? "interiordesigner_wall_tool" : "interiordesigner_floor_tool");
		SetMaterialPresetsList();
		base.gameObject.SetActive(value: true);
	}

	public override void OnClose()
	{
		base.gameObject.SetActive(value: false);
		colorPicker.gameObject.SetActive(value: false);
	}

	public override void OnEnterInteriorDesignerMode()
	{
		InteriorElementTool.StoreOriginalInteriorElementsData();
	}

	public void SelectButtonByPresetId(string presetId, int colorIndex)
	{
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			foreach (InteriorElementOptionUi interiorElementOptionUi in _interiorElementOptionUis)
			{
				if (!(interiorElementOptionUi.Preset.uuid != presetId))
				{
					if (colorIndex < interiorElementOptionUi.Preset.variants.Count)
					{
						SelectInteriorElementColor(interiorElementOptionUi, new Material(interiorElementOptionUi.previewImage.material.shader), Math.Max(0, colorIndex));
					}
					else
					{
						SetSelectedInteriorElementOption(interiorElementOptionUi);
					}
					break;
				}
			}
		});
	}

	private void SetMaterialPresetsList()
	{
		interiorElementUiTemplate.ResetTemplate();
		ToolName currentToolName = InteriorDesignerController.CurrentToolName;
		InteriorMaterialPreset.MaterialType materialType = ((currentToolName == ToolName.Floor) ? InteriorMaterialPreset.MaterialType.Floor : InteriorMaterialPreset.MaterialType.Wall);
		IOrderedEnumerable<KeyValuePair<string, InteriorMaterialPreset>> orderedEnumerable = from x in InteriorElementsHelper.PresetsDictionary
			where x.Value.canBePurchased && x.Value.type.HasFlag(materialType)
			orderby x.Value.price
			select x;
		_interiorElementOptionUis.Clear();
		foreach (KeyValuePair<string, InteriorMaterialPreset> item in orderedEnumerable)
		{
			CreateInteriorElementOptionUi(item.Value);
		}
	}

	private void CreateInteriorElementOptionUi(InteriorMaterialPreset preset)
	{
		InteriorElementOptionUi interiorElementOptionUi = UnityEngine.Object.Instantiate(interiorElementUiTemplate, interiorElementUiTemplate.parent).GetComponent<InteriorElementOptionUi>();
		interiorElementOptionUi.SetUp(preset, delegate
		{
			OpenColorPicker(interiorElementOptionUi);
		});
		interiorElementOptionUi.button.onClick.AddListener(delegate
		{
			if (_selectedInteriorElementOptionUi == interiorElementOptionUi)
			{
				ResetSelectedInteriorElementOption();
			}
			else
			{
				SetSelectedInteriorElementOption(interiorElementOptionUi);
			}
		});
		_interiorElementOptionUis.Add(interiorElementOptionUi);
	}

	private void SetSelectedInteriorElementOption(InteriorElementOptionUi interiorElementOptionUi)
	{
		colorPicker.gameObject.SetActive(value: false);
		InteriorElementTool.selectedPresetId = interiorElementOptionUi.Preset.uuid;
		InteriorElementTool.selectedPresetColorIndex = interiorElementOptionUi.ColorIndex;
		if (_selectedInteriorElementOptionUi != null)
		{
			_selectedInteriorElementOptionUi.SetAllBackgroundsColor(InstanceBehavior<GlobalReferences>.Instance.colors.white);
		}
		_selectedInteriorElementOptionUi = interiorElementOptionUi;
		_selectedInteriorElementOptionUi.SetAllBackgroundsColor(InstanceBehavior<GlobalReferences>.Instance.colors.blue);
		MouseController.SetRestrictedObjectTypes(InteractiveObjectType.Ground);
	}

	private void ResetSelectedInteriorElementOption()
	{
		InteriorElementTool.selectedPresetId = null;
		InteriorElementTool.selectedPresetColorIndex = 0;
		if (_selectedInteriorElementOptionUi != null)
		{
			_selectedInteriorElementOptionUi.SetAllBackgroundsColor(InstanceBehavior<GlobalReferences>.Instance.colors.white);
			_selectedInteriorElementOptionUi = null;
		}
		MouseController.Reset();
	}

	private void OpenColorPicker(InteriorElementOptionUi interiorElementOptionUi)
	{
		if (_selectedInteriorElementOptionUi == interiorElementOptionUi && colorPicker.gameObject.activeSelf)
		{
			colorPicker.gameObject.SetActive(value: false);
			return;
		}
		colorTemplate.ResetTemplate();
		colorPicker.gameObject.SetActive(value: true);
		for (int i = 0; i < interiorElementOptionUi.Preset.variants.Count; i++)
		{
			int index = i;
			Transform obj = UnityEngine.Object.Instantiate(colorTemplate, colorTemplate.parent);
			Image component = obj.GetComponent<Image>();
			Material mat = new Material(interiorElementOptionUi.previewImage.material.shader);
			component.material.CopyPropertiesFromMaterial(mat);
			InteriorElementOptionUi.SetMaterialPresetToUIMaterial(mat, interiorElementOptionUi.Preset, index, applyPreviewData: true);
			component.material = mat;
			component.sprite = interiorElementOptionUi.Preset.preview;
			obj.GetComponent<Button>().onClick.AddListener(delegate
			{
				SelectInteriorElementColor(interiorElementOptionUi, mat, index);
			});
			obj.gameObject.SetActive(value: true);
		}
		RectTransform component2 = interiorElementOptionUi.GetComponent<RectTransform>();
		colorPicker.GetComponent<RectTransform>().position = component2.position + Vector3.up * (component2.rect.height / 2f - 10f);
	}

	private void SelectInteriorElementColor(InteriorElementOptionUi interiorElementOptionUi, Material mat, int index)
	{
		InteriorElementOptionUi.SetMaterialPresetToUIMaterial(mat, interiorElementOptionUi.Preset, index, applyPreviewData: true);
		interiorElementOptionUi.previewImage.material = mat;
		interiorElementOptionUi.ColorIndex = index;
		SetSelectedInteriorElementOption(interiorElementOptionUi);
		colorPicker.gameObject.SetActive(value: false);
	}

	private void OnInteriorElementsPresetsChanged()
	{
		InstanceBehavior<BuildingManager>.Instance.SerializeInteriorDesign();
		InteriorDesignerController.UpdateInteriorScore();
	}
}
