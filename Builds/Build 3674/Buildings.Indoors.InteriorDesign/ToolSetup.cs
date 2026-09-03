using BigAmbitions.InteriorDesigner;
using BigAmbitions.InteriorDesigner.Tools;
using UI.InteriorDesigner;
using UnityEngine;

namespace Buildings.Indoors.InteriorDesign;

public abstract class ToolSetup
{
	public abstract IInteriorDesignerTool Tool { get; protected set; }

	public abstract ToolName ToolName { get; }

	public abstract void Setup(ActionPanelUI actionPanel, MonoBehaviour overlay);

	protected ItemController GetItemControllerAtIndex(int itemControllerIndex)
	{
		return InteriorDesignerController.ItemControllersCache[itemControllerIndex];
	}

	protected int GetIndexOfItemController(ItemController itemController)
	{
		return InteriorDesignerController.ItemControllersCache.IndexOf(itemController);
	}

	protected VehicleController GetVehicleControllerAtIndex(int vehicleControllerIndex)
	{
		return InteriorDesignerController.VehicleControllersCache[vehicleControllerIndex];
	}
}
