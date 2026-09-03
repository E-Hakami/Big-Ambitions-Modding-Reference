using Helpers;
using Player.HUD.ItemInfoOverlays;
using UI;

public class UniformLockerController : WardrobeController
{
	public override void Awake()
	{
		base.Awake();
		detailedOverlayType |= DetailedOverlayType.CustomizableButtons;
	}

	public override bool CanChangeClothes()
	{
		BuildingRegistration registration = base.BuildingContext.Registration;
		if (!registration.RentedByPlayer)
		{
			return registration.Address == TutorialHelper.ElGatoAddress;
		}
		return true;
	}

	public bool CanAssignUniforms()
	{
		if (base.BuildingContext.IsPlayerOwnedBusiness)
		{
			return base.BuildingContext.Registration.businessTypeName != "ba:businesstype_empty";
		}
		return false;
	}

	public void AssignUniforms()
	{
		InstanceBehavior<GameManager>.Instance.playerController.SetGoal(this, OpenUniformSettings);
	}

	private void OpenUniformSettings()
	{
		InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.BizMan);
		InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(base.BuildingContext.Registration.Address, "Settings");
	}
}
