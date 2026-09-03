using System.Collections;
using System.Collections.Generic;
using Helpers;
using UI.Notification;
using UnityEngine;

namespace Buildings.Retail.Businesses.CinemaTheater;

public class TicketEntryBlocker : MonoBehaviour
{
	private static Coroutine UpdateBlockersCoroutine;

	private BoxCollider _boxCollider;

	private static List<TicketEntryBlocker> AllInstances { get; } = new List<TicketEntryBlocker>();

	private void Awake()
	{
		_boxCollider = GetComponent<BoxCollider>();
		AllInstances.Add(this);
	}

	private void OnDestroy()
	{
		AllInstances.Remove(this);
	}

	public static void UpdateBlockers()
	{
		bool flag = false;
		if (BuildingManager.IsInsideBuilding && !InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			string text = InstanceBehavior<BuildingManager>.Instance.buildingRegistration?.businessTypeName;
			if (text == "ba:businesstype_cinema" || text == "ba:businesstype_theater")
			{
				flag = !SaveGameManager.Current.hasCinemaTheaterTicket || !PlayerHelper.HasPaidForAllItems();
			}
		}
		foreach (TicketEntryBlocker allInstance in AllInstances)
		{
			allInstance._boxCollider.enabled = flag;
		}
	}

	public static void UpdateBlockersDelayed()
	{
		if (UpdateBlockersCoroutine == null && (bool)InstanceBehavior<BuildingManager>.Instance)
		{
			UpdateBlockersCoroutine = InstanceBehavior<BuildingManager>.Instance.StartCoroutine(UpdateBlockersDelayedCoroutine());
		}
	}

	private static IEnumerator UpdateBlockersDelayedCoroutine()
	{
		yield return null;
		UpdateBlockers();
		UpdateBlockersCoroutine = null;
	}

	private void OnCollisionEnter(Collision other)
	{
		if (other.gameObject.layer == LayerHelper.PlayerLayerIndex)
		{
			InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
			string obj = (SaveGameManager.Current.hasCinemaTheaterTicket ? "notification_cinema_theater_unpaid_items" : "notification_cinema_theater_need_ticket");
			Notifications.ShowError(obj, obj);
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		AllInstances.Clear();
		UpdateBlockersCoroutine = null;
	}
}
