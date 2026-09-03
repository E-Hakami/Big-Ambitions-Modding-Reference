using BigAmbitions.PlacementSystem;
using Buildings.Indoors.InteriorDesign;
using CameraControllers;
using UnityEngine;

namespace BigAmbitions.InteriorDesigner.Tools;

public class ChangeFloorRevertibleAction : IRevertibleAction
{
	private readonly int _targetFloorIndex;

	private int _previousFloorIndex = -1;

	public ChangeFloorRevertibleAction(int targetFloorIndex)
	{
		_targetFloorIndex = targetFloorIndex;
	}

	public bool Do()
	{
		MultipleHeightsBuildingController multipleHeightsBuildingController = InstanceBehavior<BuildingManager>.Instance.multipleHeightsBuildingController;
		if ((_previousFloorIndex = multipleHeightsBuildingController.GetCurrentHeightIndex()) == _targetFloorIndex)
		{
			return false;
		}
		multipleHeightsBuildingController.OnCurrentHeightChanged(_targetFloorIndex);
		MoveCamera(multipleHeightsBuildingController.GetYPositionForCamera(_targetFloorIndex));
		ShowGridIfNeeded(_targetFloorIndex);
		return true;
	}

	public bool Undo()
	{
		MultipleHeightsBuildingController multipleHeightsBuildingController = InstanceBehavior<BuildingManager>.Instance.multipleHeightsBuildingController;
		multipleHeightsBuildingController.OnCurrentHeightChanged(_previousFloorIndex);
		MoveCamera(multipleHeightsBuildingController.GetYPositionForCamera(_previousFloorIndex));
		ShowGridIfNeeded(_previousFloorIndex);
		return true;
	}

	private static void ShowGridIfNeeded(int heightIndex)
	{
		IPlaceableItem currentPlaceableItemBeingPlaced = BigAmbitions.PlacementSystem.PlacementSystem.CurrentPlaceableItemBeingPlaced;
		if (currentPlaceableItemBeingPlaced != null)
		{
			GridType type = ((!currentPlaceableItemBeingPlaced.GetItemInstance().ItemCached.wallMounted) ? GridType.Ground : GridType.Wall);
			BigAmbitions.PlacementSystem.PlacementSystem.currentBuildingGrid.ShowGrid(type, heightIndex);
		}
	}

	private static void MoveCamera(float yPos)
	{
		PlacementCam placementCam = InteriorDesignerHelper.PlacementCam;
		Vector3 focusPosition = new Vector3(placementCam.transform.position.x, yPos, placementCam.transform.position.z);
		InteriorDesignerHelper.PlacementCam.FocusOnPosition(focusPosition);
	}
}
