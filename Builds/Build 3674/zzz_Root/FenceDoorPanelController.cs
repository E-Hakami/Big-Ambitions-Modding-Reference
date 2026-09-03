using NaughtyAttributes;

public class FenceDoorPanelController : EntityController
{
	[Required(null)]
	public ItemController parentController;

	[Button(null, EButtonEnableMode.Always)]
	private void AutoSetup()
	{
		parentController = GetComponentInParent<ItemController>();
	}

	public override void Start()
	{
	}

	public override bool ShouldReactToIoEnter()
	{
		return parentController.ShouldReactToIoEnter();
	}

	public override void OnIoEnter()
	{
		parentController.OnIoEnter();
	}

	public override void OnIoExit()
	{
		parentController.OnIoExit();
	}

	public override bool OnIoLeftClick()
	{
		return parentController.OnIoLeftClick();
	}

	public override void OnIoRightClick()
	{
		parentController.OnIoRightClick();
	}
}
