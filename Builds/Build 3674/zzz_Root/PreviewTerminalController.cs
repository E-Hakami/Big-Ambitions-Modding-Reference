using Player.HUD.ItemInfoOverlays;
using UI.CustomUI;

public class PreviewTerminalController : ItemController
{
	public void StartPreview()
	{
		InstanceBehavior<GameManager>.Instance.playerController.SetGoal(this, delegate
		{
			PreviewTerminalUI.Show();
			InstanceBehavior<OverlayManager>.Instance.HideSimpleOverlayAndClearCta();
		});
	}
}
