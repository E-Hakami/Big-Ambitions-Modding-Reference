using System.Collections.Generic;
using System.Linq;
using Entities;
using Extensions;
using Helpers;
using Streets;
using UI.Notification;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;
using UnityEngine.Serialization;

namespace Buildings.Office.Headquarters;

public class LogisticsManagerPlan
{
	public const string DeliveryAlertContactId = "logistics_alerts";

	public const string DeliveryAlertContactDescription = "contact_description_logistics_alerts";

	public readonly string id = UuidHelper.GenerateBase64Uuid();

	public static readonly List<(float amount, TransactionInfo transactionInfo)> PendingTransactions = new List<(float, TransactionInfo)>();

	private static readonly Dictionary<LogisticsManagerPlan, List<BusinessDeliveryInfo>> AllUndeliveredNoStock = new Dictionary<LogisticsManagerPlan, List<BusinessDeliveryInfo>>();

	private static readonly Dictionary<LogisticsManagerPlan, List<BusinessDeliveryInfo>> AllUndeliveredNoShelves = new Dictionary<LogisticsManagerPlan, List<BusinessDeliveryInfo>>();

	private static int DeliveryAlerts;

	private static string DeliveryAlertLastBusinessName;

	public bool isFactory;

	public string assignedEmployeeId;

	public Address headquartersAddress;

	[FormerlySerializedAs("warehouseAddress")]
	public Address targetAddress;

	public List<LogisticsManagerPlanDestination> destinations = new List<LogisticsManagerPlanDestination>();

	public int MaxDestinations => CalculateMaxDestinations(targetAddress, assignedEmployeeId);

	public EmployeeInstance LogisticsManagerInstance => EmployeeHelper.GetEmployeeById(assignedEmployeeId);

	private static Contact GetDeliveryAlertContact()
	{
		return Contact.EnsurePermanentContact("logistics_alerts", ContactCategoryName.ImportsAndGoods, "contact_description_logistics_alerts");
	}

	public IEnumerable<LogisticsManagerPlanDestination> GetPlannedDeliveries()
	{
		if (targetAddress.IsUndefined() || assignedEmployeeId == null || MaxDestinations == 0 || !LogisticsManagerInstance.IsEmployeeAvailable())
		{
			yield break;
		}
		if (!((Warehouse)BuildingHelper.GetBuildingRegistration(targetAddress)).RentedByPlayer)
		{
			targetAddress = null;
			yield break;
		}
		int maxDestinations = MaxDestinations;
		int destinationsDone = 0;
		AllUndeliveredNoStock.Clear();
		AllUndeliveredNoShelves.Clear();
		PendingTransactions.Clear();
		for (; destinationsDone < maxDestinations && destinationsDone < destinations.Count; destinationsDone++)
		{
			yield return destinations[destinationsDone];
		}
	}

	public void DeliverDestination(LogisticsManagerPlanDestination destination)
	{
		if (targetAddress.IsUndefined())
		{
			return;
		}
		Warehouse warehouse = (Warehouse)BuildingHelper.GetBuildingRegistration(targetAddress);
		if (!warehouse.RentedByPlayer)
		{
			return;
		}
		bool isImportExport = isFactory && BuildingHelper.GetBuildingRegistration(destination.deliveryTargetAddress)?.businessTypeName == "ba:businesstype_importexport";
		destination.Deliver(warehouse, isImportExport, out var undeliveredNoStock, out var undeliveredNoShelves);
		InstanceBehavior<GameManager>.Instance.shouldUpdateAfterDeliveries = true;
		bool flag = !isFactory && undeliveredNoStock != null && undeliveredNoStock.Count > 0;
		bool flag2 = undeliveredNoShelves != null && undeliveredNoShelves.Count > 0;
		if (!flag && !flag2)
		{
			return;
		}
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(destination.deliveryTargetAddress);
		if (buildingRegistration == null)
		{
			return;
		}
		if (flag)
		{
			if (!AllUndeliveredNoStock.ContainsKey(this))
			{
				AllUndeliveredNoStock.Add(this, new List<BusinessDeliveryInfo>());
			}
			AllUndeliveredNoStock[this].Add(new BusinessDeliveryInfo(buildingRegistration.BusinessName, undeliveredNoStock));
		}
		if (flag2)
		{
			if (!AllUndeliveredNoShelves.ContainsKey(this))
			{
				AllUndeliveredNoShelves.Add(this, new List<BusinessDeliveryInfo>());
			}
			AllUndeliveredNoShelves[this].Add(new BusinessDeliveryInfo(buildingRegistration.BusinessName, undeliveredNoShelves));
		}
	}

	public static void OnFinishedDeliveries()
	{
		LogisticsManagerPlan key;
		List<BusinessDeliveryInfo> value;
		foreach (KeyValuePair<LogisticsManagerPlan, List<BusinessDeliveryInfo>> item in AllUndeliveredNoStock)
		{
			item.Deconstruct(out key, out value);
			LogisticsManagerPlan logisticsManagerPlan = key;
			List<BusinessDeliveryInfo> deliveryInfoList = value;
			logisticsManagerPlan.SendDeliveryReport(deliveryInfoList, "ba:messagetype_phone_logistics_manager_delivery_no_stock");
		}
		foreach (KeyValuePair<LogisticsManagerPlan, List<BusinessDeliveryInfo>> allUndeliveredNoShelf in AllUndeliveredNoShelves)
		{
			allUndeliveredNoShelf.Deconstruct(out key, out value);
			LogisticsManagerPlan logisticsManagerPlan2 = key;
			List<BusinessDeliveryInfo> deliveryInfoList2 = value;
			logisticsManagerPlan2.SendDeliveryReport(deliveryInfoList2, "ba:messagetype_phone_logistics_manager_delivery_no_available_space");
		}
		foreach (var pendingTransaction in PendingTransactions)
		{
			GameManager.ChangeMoneySafe(pendingTransaction.amount, pendingTransaction.transactionInfo);
		}
		AllUndeliveredNoStock.Clear();
		AllUndeliveredNoShelves.Clear();
		PendingTransactions.Clear();
		NotifyDeliveryAlerts();
	}

	public static int CalculateMaxDestinations(Address address, string employeeId)
	{
		if (address == null || address.IsUndefined() || employeeId == null)
		{
			return 0;
		}
		int num = ((Warehouse)BuildingHelper.GetBuildingRegistration(address)).vehicleSlots.Sum((VehicleSlot vehicleSlot) => vehicleSlot.DestinationsThatCanDeliver);
		if (num == 0)
		{
			return 0;
		}
		float skillOfEmployee = EmployeeHelper.GetSkillOfEmployee(employeeId, "ba:skill_logisticsmanager");
		return num + Mathf.FloorToInt((float)num * skillOfEmployee / 100f);
	}

	public static int GetMaxPossibleDestinations(Address address)
	{
		if (address.IsUndefined())
		{
			return 0;
		}
		return ((Warehouse)BuildingHelper.GetBuildingRegistration(address)).vehicleSlots.Sum((VehicleSlot vehicleSlot) => vehicleSlot.DestinationsThatCanDeliver) * 2;
	}

	public void CancelDeliveriesForAddress(Address address)
	{
		foreach (LogisticsManagerPlanDestination destination in destinations)
		{
			if (destination.deliveryTargetAddress == address)
			{
				destination.Reset();
			}
		}
	}

	public int GetRunsOutIn(string product, int currentStock)
	{
		if (currentStock == 0)
		{
			return -1;
		}
		int num = (from business in SaveGameManager.Current.BuildingRegistrations
			select business.orderHistory.Where((OrderHistoryEntry x) => x.dayNumber.InRange(SaveGameManager.Current.Day - 7, SaveGameManager.Current.Day)).ToList() into histories
			select histories.Sum((OrderHistoryEntry x) => x.itemSales.FirstOrDefault((OrderHistoryEntry.ItemReport item) => item.itemName == product)?.amountSold).GetValueOrDefault()).Sum();
		if (num == 0)
		{
			return -2;
		}
		float num2 = (float)num / 7f;
		return (int)Mathf.Ceil((float)currentStock / num2);
	}

	public void UnAssignEmployee()
	{
		assignedEmployeeId = null;
	}

	public void UnAssignAddress()
	{
		targetAddress = null;
	}

	private void SendDeliveryReport(List<BusinessDeliveryInfo> deliveryInfoList, string messageType)
	{
		if (deliveryInfoList.Count != 0 && !targetAddress.IsUndefined())
		{
			Warehouse warehouse = (Warehouse)BuildingHelper.GetBuildingRegistration(targetAddress);
			if (warehouse.RentedByPlayer)
			{
				Dictionary<string, string> messageData = new Dictionary<string, string>
				{
					{ "businessName", warehouse.BusinessName },
					{
						"deliveryInfoList",
						BusinessDeliveryInfo.GetLocalizedList(deliveryInfoList)
					}
				};
				GetDeliveryAlertContact().SendMessage(new TextMessage(messageType, messageData));
				DeliveryAlerts++;
				DeliveryAlertLastBusinessName = warehouse.BusinessName;
			}
		}
	}

	private static void NotifyDeliveryAlerts()
	{
		if (DeliveryAlerts != 0)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			string headerKey;
			if (DeliveryAlerts > 1)
			{
				headerKey = "delivery_alert_notification_multiple";
				dictionary.Add("amount", DeliveryAlerts.ToString());
			}
			else
			{
				headerKey = "delivery_alert_notification";
				dictionary.Add("name", DeliveryAlertLastBusinessName);
			}
			Notifications.Show(NotificationType.Warning, headerKey, dictionary, 4f, null, delegate
			{
				GetDeliveryAlertContact().OnClickNotification();
			});
			DeliveryAlerts = 0;
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		DeliveryAlerts = 0;
		DeliveryAlertLastBusinessName = null;
		AllUndeliveredNoStock.Clear();
		AllUndeliveredNoShelves.Clear();
	}
}
