using BigAmbitions.InputSystem;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.InteriorDesigner.Tools;
using UnityEngine.EventSystems;

namespace Buildings.Indoors.InteriorDesign;

public static class InteriorDesignerInput
{
	public static ToolInputAction HandleInput(InteriorDesignerController controller)
	{
		IInteriorDesignerTool currentTool = InteriorDesignerController.CurrentTool;
		if (currentTool == null)
		{
			return ToolInputAction.None;
		}
		if (PlayerAction.Click.Released())
		{
			return currentTool.OnLeftClickFinish();
		}
		if (InteriorDesignerAction.SpecialBehavior.Released())
		{
			return currentTool.OnSpecialBehaviorFinish();
		}
		if (InteriorDesignerAction.SpecialBehavior.Pressed())
		{
			return currentTool.OnSpecialBehaviorStart();
		}
		if (PlayerAction.SnapFreePlacement.Pressed())
		{
			return currentTool.OnAltBehaviorStart();
		}
		if (EventSystem.current.IsPointerOverGameObject())
		{
			return ToolInputAction.None;
		}
		if (PlayerAction.Click.Pressed())
		{
			return currentTool.OnLeftClickStart();
		}
		if (PlayerAction.RightClick.Pressed())
		{
			return currentTool.OnRightClickStart();
		}
		if (PlayerAction.RightClick.Released())
		{
			return currentTool.OnRightClickFinish();
		}
		if (PlayerAction.Interact.Pressed())
		{
			return currentTool.OnInteract();
		}
		if (PlayerAction.Sell.Pressed())
		{
			return currentTool.OnDelete();
		}
		return currentTool.OnHover();
	}
}
