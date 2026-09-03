using UI.Smartphone;
using UnityEngine.EventSystems;

public class ViewBlockingEntityPart : EntityController
{
	public ViewBlockingEntity cityBuildingController;

	public override void Awake()
	{
	}

	public override void Start()
	{
	}

	public override void OnIoExit()
	{
		if ((bool)cityBuildingController)
		{
			cityBuildingController.OnIoExit();
		}
	}

	public override bool OnIoLeftClick()
	{
		if (!cityBuildingController || EventSystem.current.IsPointerOverGameObject() || FullMenu.IsOpen)
		{
			return false;
		}
		return cityBuildingController.OnIoLeftClick();
	}

	public override void OnIoRightClick()
	{
		if ((bool)cityBuildingController && !FullMenu.IsOpen)
		{
			cityBuildingController.OnIoRightClick();
		}
	}

	public override void OnIoEnter()
	{
		if ((bool)cityBuildingController)
		{
			cityBuildingController.OnIoEnter();
		}
	}

	public override void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			OnIoExit();
			MouseController.currentTargetEntity = null;
		}
	}
}
