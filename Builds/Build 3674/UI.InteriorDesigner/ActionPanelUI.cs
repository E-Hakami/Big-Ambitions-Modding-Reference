using System;
using BigAmbitions.InteriorDesigner;
using UnityEngine;

namespace UI.InteriorDesigner;

public abstract class ActionPanelUI : MonoBehaviour
{
	public static Action<ItemController> focusOnItem;

	public abstract ToolName[] ToolNames { get; }

	public abstract void OnOpen();

	public abstract void OnClose();

	public abstract void OnEnterInteriorDesignerMode();
}
