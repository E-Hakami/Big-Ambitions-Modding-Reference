using Helpers;
using PlayerActivity;
using UnityEngine;

public class LocationHappinessTrigger : MonoBehaviour
{
	[SerializeField]
	private PlayerActivityBalanceConfig balanceConfig;

	private static LocationHappinessTrigger currentLocationHappinessTrigger;

	private void OnTriggerEnter(Collider other)
	{
		if (other.transform.parent.CompareTag("Player"))
		{
			Enable();
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.transform.parent.CompareTag("Player"))
		{
			Disable();
		}
	}

	private void Enable()
	{
		HappinessHelper.EnableTemporalHappinessBoost(balanceConfig.TemporalType, balanceConfig.FinalType);
		SaveGameManager.Current.timeEnteredTemporalBoost = TimeHelper.Now();
		SaveGameManager.Current.currentActivityHappinessPerHour = balanceConfig.BoostHoursPerHour;
		currentLocationHappinessTrigger = this;
	}

	private void Disable()
	{
		HappinessHelper.DisableTemporalHappinessBoost(balanceConfig.TemporalType, balanceConfig.FinalType);
		currentLocationHappinessTrigger = null;
	}

	public static void RemoveCurrentLocationHappinessTriggerIfNeeded()
	{
		if (!(currentLocationHappinessTrigger == null))
		{
			currentLocationHappinessTrigger.Disable();
		}
	}
}
