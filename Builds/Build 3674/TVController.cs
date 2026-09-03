using PlayerActivity;
using UnityEngine;

public class TVController : ItemController
{
	[Header("TVController")]
	[SerializeField]
	private EntertainDevice entertainDevice;

	public static bool CanUseTV => InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness;

	public void PerformActivity()
	{
		if (CanUseTV)
		{
			PlayerActivityUI.Show(entertainDevice, this);
		}
	}
}
