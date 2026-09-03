using System;
using System.Threading.Tasks;
using BAModAPI;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.PlacementSystem;
using Blueprints;
using Buildings;
using Buildings.Indoors;
using Buildings.Indoors.InteriorDesign;
using BusinessLayoutSets;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using UI;
using UI.InteriorDesigner;
using UI.Load;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace BigAmbitions.BlueprintCreator;

public class BlueprintCreatorSystem : MonoBehaviour
{
	[SerializeField]
	private Building blueprintBuilding;

	[SerializeField]
	private InteriorDesignerUI interiorDesignerUI;

	public BlueprintCreatorUI ui;

	public BlueprintCreatorCamera cam;

	private BuildingSizeInfo _currentBuildingSizeInfo;

	private bool _handledIDClosed;

	private bool _editorRequestedOpen;

	public static Blueprint OpenWithBlueprint { get; set; }

	private Transform BuildingTransform { get; set; }

	public static string CurrentBuildingType { get; private set; }

	private void Awake()
	{
		ui.SetUp(StartEditing, OnPreviewVersion);
	}

	private void OnEnable()
	{
		SubscribeEvents();
	}

	private void OnDisable()
	{
		UnsubscribeEvents();
		InteriorDesignerHelper.BlueprintCreatorMode = false;
	}

	private void SubscribeEvents()
	{
		BlueprintCreatorManager.RegisterOnInit(HandleBlueprintInit);
		InteriorDesignerUI.onCloseInteriorDesigner = (Action)Delegate.Combine(InteriorDesignerUI.onCloseInteriorDesigner, new Action(HandleInteriorDesignerClosed));
		SaveBlueprintUI.onClosed = (Action<bool>)Delegate.Combine(SaveBlueprintUI.onClosed, new Action<bool>(HandleCloseSaveBlueprintUI));
	}

	private void UnsubscribeEvents()
	{
		InteriorDesignerUI.onCloseInteriorDesigner = (Action)Delegate.Remove(InteriorDesignerUI.onCloseInteriorDesigner, new Action(HandleInteriorDesignerClosed));
		SaveBlueprintUI.onClosed = (Action<bool>)Delegate.Remove(SaveBlueprintUI.onClosed, new Action<bool>(HandleCloseSaveBlueprintUI));
	}

	private void HandleBlueprintInit()
	{
		InitAsync();
	}

	public async Task InitAsync()
	{
		if (OpenWithBlueprint == null)
		{
			return;
		}
		Debug.Log("Opening blueprint " + OpenWithBlueprint.name);
		try
		{
			ExtractBlueprintMetadata(OpenWithBlueprint.metadata, out var type, out var sizeInfo, out var businessType);
			_currentBuildingSizeInfo = sizeInfo;
			CurrentBuildingType = type;
			await PreviewBlueprintVersionAsync(type, sizeInfo, businessType, OpenWithBlueprint.name);
			StartEditing();
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error in BlueprintCreatorSystem: {arg}");
			OpenWithBlueprint = null;
		}
	}

	private static void ExtractBlueprintMetadata(BlueprintMetadata metadata, out string type, out BuildingSizeInfo sizeInfo, out string businessType)
	{
		type = metadata.buildingType;
		sizeInfo = metadata.buildingSizeInfo;
		businessType = metadata.GetDataElementValue(DataElement.BusinessTypeName);
	}

	private void StartEditing()
	{
		ui.Hide();
		BuildingPreview.isPreviewing = false;
		ConfigureBlueprintBuilding();
		OpenInteriorDesigner();
	}

	private static void ConfigureBlueprintBuilding()
	{
		BuildingManager instance = InstanceBehavior<BuildingManager>.Instance;
		instance.LoadBuilding(isBlueprintCreator: true);
		instance.ForceUpdateAvailableProducers();
		instance.multipleHeightsBuildingController?.OnCurrentHeightChanged(0);
		BusinessSecurityHelper.ForceUpdateSecurityPanelCoverage();
	}

	private void OnPreviewVersion(string type, BuildingSizeInfo sizeInfo)
	{
		PreviewBlueprintVersionAsync(type, sizeInfo);
	}

	private async Task PreviewBlueprintVersionAsync(string type, BuildingSizeInfo sizeInfo, string businessTypeName = null, string blueprintName = null)
	{
		BuildingPreview.isPreviewing = true;
		WallsVisibilityHelper.ToggleWalls(WallsVisibility.AllVisible);
		_currentBuildingSizeInfo = sizeInfo;
		CurrentBuildingType = type;
		blueprintBuilding.BuildingSize = _currentBuildingSizeInfo.buildingSize;
		blueprintBuilding.BuildingVersion = _currentBuildingSizeInfo.buildingVersion;
		blueprintBuilding.BuildingType = CurrentBuildingType;
		BuildingManager instance = InstanceBehavior<BuildingManager>.Instance;
		instance.building = blueprintBuilding;
		instance.buildingRegistration = new BuildingRegistration
		{
			BuildingCached = blueprintBuilding,
			RentedByPlayer = true,
			businessTypeName = (businessTypeName ?? "ba:businesstype_empty"),
			blueprintName = blueprintName
		};
		instance.businessType = BusinessTypeHelper.GetData(instance.buildingRegistration);
		ClearPreviousPreview();
		AsyncOperationHandle<GameObject> asyncOperationHandle = InstanceBehavior<BuildingManager>.Instance.BuildingSizeResolver.LoadBuildingAsync(sizeInfo);
		if (asyncOperationHandle.IsValid())
		{
			try
			{
				await asyncOperationHandle.Task;
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				BuildingPreview.isPreviewing = false;
				return;
			}
		}
		BuildingTransform = InstanceBehavior<BuildingManager>.Instance.ToggleLayout(sizeInfo, state: true);
		if (!BuildingTransform)
		{
			BuildingPreview.isPreviewing = false;
			return;
		}
		BusinessLayoutSet businessLayoutSet = await GetLayoutSetAsync(type, sizeInfo);
		if (businessLayoutSet != null)
		{
			ApplyLayout(businessLayoutSet);
		}
		SetupPlacementSystem(BuildingTransform);
		cam.PreviewVersion(BuildingTransform);
	}

	private static void ClearPreviousPreview()
	{
		BuildingManager instance = InstanceBehavior<BuildingManager>.Instance;
		instance.IndoorItemContainer.ClearChildren();
		if (instance.building != null)
		{
			instance.ToggleBuildingLayout(instance.building, state: false);
		}
		instance.BuildingSizeResolver.DisableAllSizesAndLayouts();
		instance.multipleHeightsBuildingController = null;
	}

	private static async Task<BusinessLayoutSet> GetLayoutSetAsync(string type, BuildingSizeInfo sizeInfo)
	{
		if (OpenWithBlueprint == null)
		{
			if (type == "ba:buildingtype_special")
			{
				return null;
			}
			BusinessLayoutSet orLoadBusinessLayoutSet = BusinessLayoutSetHelper.GetOrLoadBusinessLayoutSet("ba:businesstype_empty", sizeInfo, "default");
			if (orLoadBusinessLayoutSet != null)
			{
				return orLoadBusinessLayoutSet;
			}
		}
		else
		{
			BusinessLayoutSet businessLayoutSet = await OpenWithBlueprint.GetLayout();
			if (businessLayoutSet != null)
			{
				return businessLayoutSet;
			}
		}
		Debug.LogError("Layout not found for " + type + " " + sizeInfo.ToString());
		return null;
	}

	private void ApplyLayout(BusinessLayoutSet layoutSet)
	{
		InteriorElement[] componentsInChildren = BuildingTransform.GetComponentsInChildren<InteriorElement>();
		BuildingManager.ApplyInteriorDesign(layoutSet.interiorDesigns, componentsInChildren);
		BuildingRegistration buildingRegistration = InstanceBehavior<BuildingManager>.Instance.buildingRegistration;
		InstanceBehavior<BuildingManager>.Instance.multipleHeightsBuildingController = BuildingTransform.GetComponent<MultipleHeightsBuildingController>();
		BusinessLayoutSetHelper.InsertLayoutSet(buildingRegistration, layoutSet, shouldRandomlyFillShelves: false, isBlueprintCreator: true);
		InstanceBehavior<BuildingManager>.Instance.LoadItems();
		BusinessSecurityHelper.UpdateCamerasCoverage();
	}

	private static void SetupPlacementSystem(Transform buildingTransform)
	{
		IBuildingGrid buildingGrid = (BigAmbitions.PlacementSystem.PlacementSystem.currentBuildingGrid = buildingTransform.GetComponent<BuildingGridBase>());
		BigAmbitions.PlacementSystem.PlacementSystem.multipleHeightsBuildingController = buildingTransform.GetComponent<IMultipleHeightsBuildingController>();
		((BuildingGridBase)buildingGrid).HideGrid(GridType.Both, 0);
	}

	private void OpenInteriorDesigner()
	{
		_handledIDClosed = false;
		if (InteriorDesignerUI.IsOpen)
		{
			InteriorDesignerUI.onCloseInteriorDesigner = (Action)Delegate.Remove(InteriorDesignerUI.onCloseInteriorDesigner, new Action(HandleInteriorDesignerClosed));
			interiorDesignerUI.Hide();
			InteriorDesignerUI.onCloseInteriorDesigner = (Action)Delegate.Combine(InteriorDesignerUI.onCloseInteriorDesigner, new Action(HandleInteriorDesignerClosed));
		}
		interiorDesignerUI.undoAllAfterBlueprintSave = true;
		interiorDesignerUI.saveBlueprintOnConfirm = true;
		interiorDesignerUI.DisableTool(ToolName.SaveBlueprint);
		interiorDesignerUI.DisableTool(ToolName.Package);
		interiorDesignerUI.SetAlternativeCameraToggle(cam.AlternativeCameraToggle);
		interiorDesignerUI.Show();
	}

	private void HandleCloseSaveBlueprintUI(bool hasSaved)
	{
		if (hasSaved && !_editorRequestedOpen)
		{
			OpenWithBlueprint = null;
			_handledIDClosed = true;
			MainMenuController.showBlueprintLibraryOnStart = true;
			LoadScene.LoadMainMenu(ModActivationScope.BlueprintCreator);
		}
	}

	private void HandleInteriorDesignerClosed()
	{
		if (!_handledIDClosed && !_editorRequestedOpen)
		{
			if (OpenWithBlueprint != null)
			{
				OpenWithBlueprint = null;
				MainMenuController.showBlueprintLibraryOnStart = true;
				LoadScene.LoadMainMenu(ModActivationScope.BlueprintCreator);
			}
			else
			{
				ui.Show(CurrentBuildingType, _currentBuildingSizeInfo);
				OnPreviewVersion(CurrentBuildingType, _currentBuildingSizeInfo);
			}
		}
	}

	public async Task SaveAndOpenBlueprint(Blueprint blueprint)
	{
		if (!_editorRequestedOpen)
		{
			OpenWithBlueprint = blueprint;
			if (!interiorDesignerUI.HasChanges)
			{
				await InitAsync();
				return;
			}
			_editorRequestedOpen = true;
			SaveBlueprintUI.onClosed = (Action<bool>)Delegate.Combine(SaveBlueprintUI.onClosed, new Action<bool>(HandleEditorOpenBlueprint));
			interiorDesignerUI.HideWithConfirm();
		}
	}

	private void HandleEditorOpenBlueprint(bool hasSaved)
	{
		if (_editorRequestedOpen)
		{
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				_editorRequestedOpen = false;
				SaveBlueprintUI.onClosed = (Action<bool>)Delegate.Remove(SaveBlueprintUI.onClosed, new Action<bool>(HandleEditorOpenBlueprint));
				InitAsync();
			});
		}
	}
}
