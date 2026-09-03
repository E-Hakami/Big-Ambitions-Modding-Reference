using BigAmbitions.InteriorDesigner;
using BigAmbitions.InteriorDesigner.Tools;
using UI.InteriorDesigner;
using UnityEngine;

namespace Buildings.Indoors.InteriorDesign;

public class SaveBlueprintToolSetup : ToolSetup
{
	public override IInteriorDesignerTool Tool { get; protected set; }

	public override ToolName ToolName => ToolName.SaveBlueprint;

	public override void Setup(ActionPanelUI actionPanel, MonoBehaviour overlay)
	{
		Tool = new SaveBlueprintTool();
	}
}
