using BigAmbitions.Tags;
using Buildings;
using Entities;
using Helpers;
using PlayerActivity;
using UI.Notification;
using UnityEngine;

namespace Controllers;

public class DJBoothController : EmployeeStationController
{
	[SerializeField]
	private EntertainDevice entertainDevice;

	public override void Start()
	{
		employeeType = typeof(DJEmployee);
		base.Start();
	}

	public override bool Interact()
	{
		if (base.Interact())
		{
			return true;
		}
		if (!base.BuildingContext.Registration.RentedByPlayer)
		{
			return false;
		}
		if (BuildingTypeHelper.GetData(base.BuildingContext.Registration).HasTag(TagRef.Buildingtypetag.containsnobusiness))
		{
			if (PlayerHelper.ItemInstanceInHands != null)
			{
				Notifications.ShowError("djbooth_notification_need_free_hands");
			}
			else
			{
				PlayerActivityUI.Show(entertainDevice, this);
			}
		}
		else
		{
			Notifications.Show(NotificationType.Warning, "djbooth_notification_cant_use");
		}
		return true;
	}

	public override void AssignEmployee(ThirdPersonCharacter tpc, EmployeeInstance employeeInstance)
	{
		base.AssignEmployee(tpc, employeeInstance);
		tpc.GetComponent<DJEmployee>().SetEmployeeStation(this);
	}

	public override EmployeeInstance GetAIEmployeeInstance()
	{
		return EmployeeHelper.CreateAIEmployeeInstance("ba:skill_dj");
	}
}
