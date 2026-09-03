using BigAmbitions.InteriorDesigner;
using BigAmbitions.InteriorDesigner.Tools;
using Helpers;
using UI.InteriorDesigner;
using UnityEngine;

namespace Buildings.Indoors.InteriorDesign;

public class FloorToolSetup : ToolSetup
{
	public override IInteriorDesignerTool Tool { get; protected set; }

	public override ToolName ToolName => ToolName.Floor;

	public override void Setup(ActionPanelUI actionPanel, MonoBehaviour overlay)
	{
		Tool = new InteriorElementTool
		{
			getInteriorElementClicked = () => ((string interiorElementId, int materialIndex))IInteriorDesignerTool.getInteriorElementClicked(LayerHelper.groundLayerMask),
			onInteriorElementMaterialChanged = OnInteriorElementMaterialChanged,
			paintMode = PaintMode.Neighbors,
			isBlueprintCreatorMode = () => InteriorDesignerHelper.BlueprintCreatorMode
		};
	}

	private void OnInteriorElementMaterialChanged()
	{
		UiSoundHelper.Play(UiSound.PaintFloor, randomPitch: true);
	}
}
