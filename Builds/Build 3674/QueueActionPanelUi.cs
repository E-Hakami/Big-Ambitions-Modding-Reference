using BigAmbitions.InteriorDesigner;
using Buildings.Indoors.InteriorDesign;
using UI.InteriorDesigner;

public class QueueActionPanelUi : ItemActionPanelUI
{
	public override ToolName[] ToolNames => new ToolName[1] { ToolName.Queue };

	public override void OnOpen()
	{
		allItemControllers.Clear();
		allItemControllers.AddRange(InteriorDesignerController.GetQueueItemControllers);
		base.OnOpen();
	}
}
