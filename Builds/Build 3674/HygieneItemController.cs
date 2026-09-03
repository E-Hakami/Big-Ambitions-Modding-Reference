using System.Collections.Generic;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.Items;
using Helpers;
using PlayerActivity;
using UI.ItemPanel;
using UI.Notification;
using UnityEngine;

public class HygieneItemController : ItemController
{
	[Header("Hygiene")]
	public HygieneEnvironment hygieneEnvironment;

	protected List<AppearanceElementData> originalAppearance;

	public void PerformActivity()
	{
		PlayerItemPurchaserSettings obj = playerItemPurchaserSettings;
		if ((obj == null || !obj.enabled) && !VehicleHelper.IsInsideVehicle())
		{
			if (PlayerHelper.IsHoldingItem)
			{
				Dictionary<string, string> notificationData = new Dictionary<string, string>
				{
					{ "fromname", itemName },
					{
						"toname",
						PlayerHelper.ItemInstanceInHands.itemName
					}
				};
				Notifications.Show(NotificationType.Warning, "notification_cant_interact_with_item_in_hand", notificationData);
			}
			else if (!ItemPanelUI.IsVisible)
			{
				PlayerActivityUI.Show(hygieneEnvironment, this);
			}
		}
	}

	public bool BeginUse(ThirdPersonCharacter tpc)
	{
		if (Occupied || !tpc)
		{
			return false;
		}
		Occupied = true;
		tpc.animator.SetBool(hygieneEnvironment.AnimationType);
		Transform transform = (hygieneEnvironment.userAttachPoint ? hygieneEnvironment.userAttachPoint : base.transform);
		tpc.ForceToPosition(transform.position);
		tpc.ForceToRotation(transform.rotation);
		tpc.LinkToPointAndClickObject(this);
		OnBeginUse(tpc);
		return true;
	}

	protected virtual void OnBeginUse(ThirdPersonCharacter tpc)
	{
		originalAppearance = tpc.appearanceSetter.GetCopyOfCurrentElements();
	}

	public void UpdateRotation(ThirdPersonCharacter tpc)
	{
		if ((bool)tpc)
		{
			Transform transform = (hygieneEnvironment.userAttachPoint ? hygieneEnvironment.userAttachPoint : base.transform);
			tpc.ForceToRotation(transform.rotation);
		}
	}

	public void EndUse(ThirdPersonCharacter tpc)
	{
		if (!(tpc.CurrentEntityController != this))
		{
			Occupied = false;
			tpc.animator.SetBool(hygieneEnvironment.AnimationType, state: false);
			tpc.Reset();
			OnEndUse(tpc);
		}
	}

	protected virtual void OnEndUse(ThirdPersonCharacter tpc)
	{
		if (originalAppearance != null)
		{
			tpc.appearanceSetter.UpdateElements(originalAppearance);
			originalAppearance = null;
			Employee component = tpc.GetComponent<Employee>();
			if ((bool)component && (bool)component.employeeStationController && component.employeeStationController.employeeInstance == component.employeeInstance)
			{
				component.employeeStationController.SetEmployeeAppearance(tpc);
			}
		}
	}
}
