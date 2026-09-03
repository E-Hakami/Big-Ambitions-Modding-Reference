using BigAmbitions.InteriorDesigner;
using BigAmbitions.InteriorDesigner.Tools;
using Helpers;
using UI.InteriorDesigner;
using UnityEngine;

namespace Buildings.Indoors.InteriorDesign;

public class WallToolSetup : ToolSetup
{
	public override IInteriorDesignerTool Tool { get; protected set; }

	public override ToolName ToolName => ToolName.Wall;

	public override void Setup(ActionPanelUI actionPanel, MonoBehaviour overlay)
	{
		Tool = new InteriorElementTool
		{
			getInteriorElementClicked = () => ((string interiorElementId, int materialIndex))IInteriorDesignerTool.getInteriorElementClicked(LayerHelper.wallsLayerMask),
			onInteriorElementMaterialChanged = OnInteriorElementMaterialChanged,
			paintMode = PaintMode.Default,
			isBlueprintCreatorMode = () => InteriorDesignerHelper.BlueprintCreatorMode
		};
	}

	private void OnInteriorElementMaterialChanged()
	{
		UiSoundHelper.Play(UiSound.PaintWall, randomPitch: true);
	}
}
