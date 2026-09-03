using BigAmbitions.InteriorDesigner;
using Buildings.Indoors.InteriorDesign;
using Controllers;
using NaughtyAttributes;
using UnityEngine;

namespace UI.InteriorDesigner;

public class ProducerActionPanelUi : ItemActionPanelUI
{
	[BoxGroup("Sprites")]
	[SerializeField]
	private Sprite textIcon;

	public override ToolName[] ToolNames => new ToolName[1] { ToolName.Producer };

	protected override bool UseOverlay => true;

	public override void OnOpen()
	{
		allItemControllers.Clear();
		allItemControllers.AddRange(InteriorDesignerController.GetProducerItemControllers);
		getOverlayIcon = (ItemController x) => (!(x is ItemWithTextController)) ? null : textIcon;
		getOverlayBackground = (ItemController _) => (Sprite)null;
		base.OnOpen();
	}
}
