using System;
using System.Collections.Generic;
using BigAmbitions.Items;
using BigAmbitions.PlacementSystem;
using Buildings;
using Buildings.Indoors.InteriorDesign;
using Helpers;
using UI;
using UI.InteriorDesigner;
using UnityEngine;

public class MultipleHeightsBuildingController : MonoBehaviour, IMultipleHeightsBuildingController
{
	private static readonly int CurrentLevelHeightID = Shader.PropertyToID("_CurrentLevelHeight");

	private const float CeilingItemsOffset = 0.001f;

	public float heightToShowSecondFloor;

	public GameObject secondFloorVisualsParentGo;

	public string buildingSize;

	[SerializeField]
	private GameObject[] floorParents;

	[Header("Second Floor References")]
	[SerializeField]
	private List<Renderer> secondFloorRenderers;

	[SerializeField]
	private List<GameObject> secondFloorAdditionalCollidersObjects;

	[SerializeField]
	private List<int> secondFloorRenderersLayer;

	[SerializeField]
	private List<int> secondFloorAdditionalCollidersObjectsLayer;

	[SerializeField]
	private SizeOrientedBounds[] secondFloorBounds;

	[SerializeField]
	private CeilingPositionOverrideSizeOrientedBounds[] ceilingBounds;

	[NonSerialized]
	public int currentHeightIndex = -1;

	private int _ignoreRaycastLayer;

	private int _maxHeightIndex;

	public int GetCurrentHeightIndex()
	{
		return currentHeightIndex;
	}

	public GameObject[] GetFloorsParents()
	{
		return floorParents;
	}

	private void Awake()
	{
		_ignoreRaycastLayer = LayerHelper.IgnoreRaycastLayerIndex;
		_maxHeightIndex = BuildingSizeHelper.GetData(buildingSize).wallHeights.Length - 1;
	}

	public void OnEnterBuilding()
	{
		InteriorDesignerUI.onCloseInteriorDesigner = (Action)Delegate.Combine(InteriorDesignerUI.onCloseInteriorDesigner, new Action(OnInteriorDesignerClose));
		currentHeightIndex = -1;
	}

	public void OnExitBuilding()
	{
		InteriorDesignerUI.onCloseInteriorDesigner = (Action)Delegate.Remove(InteriorDesignerUI.onCloseInteriorDesigner, new Action(OnInteriorDesignerClose));
	}

	protected virtual void LateUpdate()
	{
		if (ShouldCheckHeight())
		{
			CheckHeight();
			if (currentHeightIndex != _maxHeightIndex)
			{
				UpdateCustomersVisibility();
			}
		}
	}

	protected virtual bool ShouldCheckHeight()
	{
		if (!InteriorDesignerUI.IsOpen && !InteriorDesignerHelper.BlueprintCreatorMode && !GameManager.isCitySceneBeingUnloaded)
		{
			return !BuildingPreview.isPreviewing;
		}
		return false;
	}

	protected void CheckHeight()
	{
		float y = InstanceBehavior<GameManager>.Instance.playerController.transform.position.y;
		int floorIndex = GetFloorIndex(y);
		if (currentHeightIndex != floorIndex)
		{
			OnCurrentHeightChanged(floorIndex, skipPlayerCheck: true);
		}
	}

	private void UpdateCustomersVisibility()
	{
		foreach (Customer customer in IndoorCustomerSpawner.Customers)
		{
			bool positionVisible = GetPositionVisible(customer.transform.position);
			customer.OnCustomerFloorVisibilityChanged(positionVisible);
		}
	}

	public void OnCurrentHeightChanged(int floorIndex, bool skipPlayerCheck = false)
	{
		currentHeightIndex = floorIndex;
		SetGlobalHeightShaderValue(floorIndex);
		if (currentHeightIndex == 0)
		{
			OnFirstFloorEnter();
		}
		else
		{
			OnSecondFloorEnter();
		}
		UpdateCustomersVisibility();
		GlobalEvents.onCurrentHeightChanged?.Invoke();
		if (!skipPlayerCheck && !InteriorDesignerHelper.BlueprintCreatorMode)
		{
			UpdatePlayerVisibility();
		}
	}

	public static void SetGlobalHeightShaderValue(int floorIndex)
	{
		Shader.SetGlobalFloat(CurrentLevelHeightID, floorIndex);
	}

	public static int GetGlobalHeightShaderValue()
	{
		return (int)Shader.GetGlobalFloat(CurrentLevelHeightID);
	}

	private void OnInteriorDesignerClose()
	{
		if (!InteriorDesignerHelper.BlueprintCreatorMode)
		{
			CheckHeight();
			UpdatePlayerVisibility();
		}
	}

	private void UpdatePlayerVisibility()
	{
		PlayerController playerController = InstanceBehavior<GameManager>.Instance.playerController;
		if (GetFloorIndex(playerController.transform.position.y) <= currentHeightIndex)
		{
			playerController.Show();
		}
		else
		{
			playerController.Hide();
		}
	}

	private void OnFirstFloorEnter()
	{
		ToggleSecondFloorVisuals(toggle: false);
	}

	private void OnSecondFloorEnter()
	{
		ToggleSecondFloorVisuals(toggle: true);
	}

	private void ToggleSecondFloorVisuals(bool toggle)
	{
		ToggleSecondFloorStructure(toggle);
		if (!BuildingPreview.isPreviewing)
		{
			ToggleSecondFloorItems(toggle);
			InstanceBehavior<BuildingManager>.Instance.UpdateDirtinessOnHeightChange();
		}
	}

	private void ToggleSecondFloorStructure(bool toggle)
	{
		int wallsLayerIndex = LayerHelper.WallsLayerIndex;
		for (int i = 0; i < secondFloorRenderers.Count; i++)
		{
			Renderer renderer = secondFloorRenderers[i];
			renderer.enabled = toggle;
			if (renderer.gameObject.layer != wallsLayerIndex)
			{
				renderer.gameObject.layer = (toggle ? secondFloorRenderersLayer[i] : _ignoreRaycastLayer);
			}
		}
		for (int j = 0; j < secondFloorAdditionalCollidersObjects.Count; j++)
		{
			GameObject gameObject = secondFloorAdditionalCollidersObjects[j];
			if (gameObject.layer != wallsLayerIndex)
			{
				gameObject.layer = (toggle ? secondFloorAdditionalCollidersObjectsLayer[j] : _ignoreRaycastLayer);
			}
		}
	}

	private void ToggleSecondFloorItems(bool toggle)
	{
		float buildingRoofPosition = BuildingSizeHelper.GetBuildingRoofPosition(buildingSize, 0);
		if (InstanceBehavior<BuildingManager>.Instance.allItemControllers == null)
		{
			return;
		}
		foreach (ItemController allItemController in InstanceBehavior<BuildingManager>.Instance.allItemControllers)
		{
			if (!(PlacementSystem.CurrentPlaceableItemBeingPlaced?.GetTransform() == allItemController.transform))
			{
				ToggleSecondFloorItem(toggle, allItemController, buildingRoofPosition);
			}
		}
	}

	protected void ToggleSecondFloorItem(bool toggle, ItemController itemController, float roofY)
	{
		if (itemController.Item.snapToCeiling)
		{
			if (itemController.transform.position.y > roofY)
			{
				ToggleItem(itemController, toggle);
			}
		}
		else if (Mathf.Approximately(itemController.transform.position.y, roofY) || itemController.transform.position.y > roofY || IsInsideAnySecondFloorBounds(itemController.transform.position))
		{
			ToggleItem(itemController, toggle);
		}
	}

	public bool GetPositionVisible(Vector3 position)
	{
		return GetPositionHeightIndex(position) <= currentHeightIndex;
	}

	public int GetPositionHeightIndex(Vector3 position)
	{
		float buildingRoofPosition = BuildingSizeHelper.GetBuildingRoofPosition(buildingSize, 0);
		if (Mathf.Approximately(position.y, buildingRoofPosition) || position.y > buildingRoofPosition || IsInsideAnySecondFloorBounds(position))
		{
			return 1;
		}
		return 0;
	}

	public int GetItemHeightIndex(ItemController itemController)
	{
		float y = itemController.transform.position.y;
		float buildingRoofPosition = BuildingSizeHelper.GetBuildingRoofPosition(buildingSize, 0);
		if (itemController.Item.snapToCeiling)
		{
			if (!(y > buildingRoofPosition))
			{
				return 0;
			}
			return 1;
		}
		if (Mathf.Approximately(y, buildingRoofPosition) || y > buildingRoofPosition || IsInsideAnySecondFloorBounds(itemController.transform.position))
		{
			return 1;
		}
		return 0;
	}

	private void ToggleItem(ItemController itemController, bool toggle)
	{
		if (toggle)
		{
			itemController.Show();
		}
		else
		{
			itemController.Hide();
		}
	}

	private bool IsInsideAnySecondFloorBounds(Vector3 position)
	{
		SizeOrientedBounds[] array = secondFloorBounds;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].Contains(position))
			{
				return true;
			}
		}
		return false;
	}

	private int GetFloorIndex(float yPos)
	{
		if (!(yPos >= heightToShowSecondFloor))
		{
			return 0;
		}
		return 1;
	}

	public float GetYPositionForCamera(int heightIndex = -1)
	{
		if (heightIndex == -1)
		{
			heightIndex = currentHeightIndex;
		}
		if (heightIndex == 0)
		{
			return 0f;
		}
		return BuildingSizeHelper.GetBuildingWallHeight(buildingSize, heightIndex - 1);
	}

	public float GetCeilingYPositionForRoofObject(Vector3 position, int heightIndex = -1)
	{
		if (heightIndex == -1)
		{
			heightIndex = currentHeightIndex;
		}
		float num = (position.y = BuildingSizeHelper.GetBuildingRoofPosition(buildingSize, heightIndex));
		CeilingPositionOverrideSizeOrientedBounds[] array = ceilingBounds;
		foreach (CeilingPositionOverrideSizeOrientedBounds ceilingPositionOverrideSizeOrientedBounds in array)
		{
			if (ceilingPositionOverrideSizeOrientedBounds.Contains(position))
			{
				num = ceilingPositionOverrideSizeOrientedBounds.heightPosition;
				break;
			}
		}
		return num - 0.001f;
	}

	public bool FitsInPosition(Vector3 position, Item item)
	{
		float buildingHeightRequirement = item.buildingHeightRequirement;
		CeilingPositionOverrideSizeOrientedBounds[] array = ceilingBounds;
		foreach (CeilingPositionOverrideSizeOrientedBounds ceilingPositionOverrideSizeOrientedBounds in array)
		{
			if (ceilingPositionOverrideSizeOrientedBounds.Contains(position) && buildingHeightRequirement <= ceilingPositionOverrideSizeOrientedBounds.heightPosition)
			{
				return true;
			}
		}
		float num = BuildingSizeHelper.GetData(buildingSize).wallHeights[currentHeightIndex];
		if (currentHeightIndex == 0)
		{
			return buildingHeightRequirement <= num;
		}
		return buildingHeightRequirement <= num - BuildingSizeHelper.GetData(buildingSize).wallHeights[currentHeightIndex - 1];
	}

	public bool FitsInBuilding(Item item)
	{
		float buildingHeightRequirement = item.buildingHeightRequirement;
		CeilingPositionOverrideSizeOrientedBounds[] array = ceilingBounds;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].heightPosition >= buildingHeightRequirement)
			{
				return true;
			}
		}
		float num = 0f;
		float[] wallHeights = BuildingSizeHelper.GetData(buildingSize).wallHeights;
		for (int j = 0; j < wallHeights.Length; j++)
		{
			num = wallHeights[j] - num;
			if (num >= buildingHeightRequirement)
			{
				return true;
			}
		}
		return false;
	}

	public float GetFloorHeightForPosition(Vector3 position)
	{
		int positionHeightIndex = GetPositionHeightIndex(position);
		if (positionHeightIndex != 0)
		{
			return BuildingSizeHelper.GetBuildingWallHeight(buildingSize, positionHeightIndex - 1);
		}
		return 0f;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		Shader.SetGlobalFloat(CurrentLevelHeightID, 0f);
	}
}
