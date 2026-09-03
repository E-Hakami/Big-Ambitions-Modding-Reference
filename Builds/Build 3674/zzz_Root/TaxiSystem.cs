using System.Collections;
using BigAmbitions.DayNightCycle;
using GleyTrafficSystem;
using Helpers;
using UI;
using UnityEngine;
using Vehicles.Taxis;

public class TaxiSystem : InstanceBehavior<TaxiSystem>
{
	private const float PricePerMeter = 0.15f;

	private const float MinutePerMeter = 0.04f;

	private const float FadeInTime = 1f;

	private Coroutine _travelCoroutine;

	public static bool IsTraveling => InstanceBehavior<TaxiSystem>.Instance._travelCoroutine != null;

	public static float GetPrice(float distance)
	{
		if (!(InstanceBehavior<CityManager>.Instance.cityMap.Taxi is PrivateDriverVehicle))
		{
			return distance * 0.15f;
		}
		return 0f;
	}

	public static float GetTravelDurationMinutes(float distance)
	{
		ITaxi taxi = InstanceBehavior<CityManager>.Instance.cityMap.Taxi;
		return distance * 0.04f * PlayerPrefSettings.GameSpeed * taxi.GetTimeMultiplier();
	}

	public void TravelTo(CityBuildingController cbc)
	{
		if (_travelCoroutine != null)
		{
			return;
		}
		float distance = Vector3.Distance(cbc.GetNavMeshTargetPosition(), InstanceBehavior<GameManager>.Instance.playerController.transform.position);
		float price = GetPrice(distance);
		if (price > 0f)
		{
			TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_taxiride");
			transactionInfo.SetTaxDeductibleName("ba:transaction_taxiride");
			if (!GameManager.ChangeMoneySafe(0f - price, transactionInfo, null, null, force: false, showNotification: true))
			{
				return;
			}
		}
		float travelDurationMinutes = GetTravelDurationMinutes(distance);
		_travelCoroutine = StartCoroutine(TravelCoroutine(cbc, travelDurationMinutes));
	}

	private IEnumerator TravelCoroutine(CityBuildingController cbc, float minutes)
	{
		PlayerController playerController = InstanceBehavior<GameManager>.Instance.playerController;
		playerController.Character.ToggleVisibility(show: false);
		playerController.ResetNavigation();
		playerController.SetNavigationBlocker(NavigationBlocker.Taxi);
		CityMap cityMap = InstanceBehavior<CityManager>.Instance.cityMap;
		ITaxi taxi = cityMap.Taxi;
		taxi?.DriveAway();
		cityMap.Close();
		yield return UiFader.Fade(1f);
		Timestamp timestamp = TimeHelper.Now();
		timestamp.AddMinutes(minutes);
		InstanceBehavior<UIs>.Instance.timeMachine.StartTimeMachine(timestamp, disableCancel: true);
		yield return new WaitWhile(() => InstanceBehavior<UIs>.Instance.timeMachine.isRunning);
		if (taxi is TaxiController || taxi is PermanentTaxiController)
		{
			SaveGameManager.Current.achievementsData.taxiRides++;
		}
		if (taxi is PrivateDriverVehicle)
		{
			SaveGameManager.Current.achievementsData.privateDriverRides++;
		}
		GameEvent.Invoke(string.Empty);
		playerController.Character.WarpSafely(cbc.GetNavMeshTargetPosition());
		playerController.Character.ToggleVisibility(show: true);
		playerController.UnsetNavigationBlocker(NavigationBlocker.Taxi);
		playerController.Character.Reset();
		GameEvent.Invoke("ba:gameevent_completedtaxiride");
		if (taxi != null)
		{
			taxi.OnTravelFinished();
			if (!string.IsNullOrEmpty(taxi.GetHappinessModifierName()))
			{
				HappinessHelper.AddModifier(taxi.GetHappinessModifierName());
			}
			Waypoint closestWaypoint = TrafficManager.Instance.GetClosestWaypoint(playerController.transform.position, 50f, IsValidWaypointForLeaving, taxi.GetVehiclePrefab());
			if (closestWaypoint != null)
			{
				taxi.InstantiateVehicle(closestWaypoint);
			}
			else
			{
				InstanceBehavior<UIs>.Instance.smartphoneUI.DismissPrivateDriver(force: true, null, instantRemove: true);
			}
		}
		yield return UiFader.UnFade();
		_travelCoroutine = null;
	}

	private static bool IsValidWaypointForLeaving(Waypoint waypoint)
	{
		if (waypoint.allowedCars.Count == 0)
		{
			return false;
		}
		float y = InstanceBehavior<GameManager>.Instance.playerController.transform.position.y;
		if (TrafficManager.WithinHeight(waypoint, y))
		{
			return TrafficManager.IsNotIntersection(waypoint);
		}
		return false;
	}
}
