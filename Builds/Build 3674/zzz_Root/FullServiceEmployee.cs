using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.Items;
using BigAmbitions.SoundSystem;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Entities;
using Extensions;
using Helpers;
using UI;
using UI.Notification;
using UnityEngine;

public class FullServiceEmployee : Employee
{
	public override void Start()
	{
		base.Start();
		SetAgentProperties();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		RevertAgentProperties();
	}

	protected override void Update()
	{
		if (IsEmployeeAvailable())
		{
			CallForNextCustomer();
			TryStartToiletCoroutine();
		}
	}

	public override IEnumerator CancelCurrentOrder()
	{
		if (activeCoroutine != null)
		{
			StopCoroutine(activeCoroutine);
			activeCoroutine = null;
			customer = null;
			employeeTpc.SetHandContent(null);
			yield return employeeTpc.MoveToPosition(employeeStationController.transform, employeeStationController.GetEmployeePosition());
		}
		else
		{
			yield return new WaitForEndOfFrame();
		}
	}

	protected override IEnumerator ServeCustomer()
	{
		customer.order.customerServiceSkill = employeeInstance.GetSkillValue("ba:skill_customerservice") * (employeeInstance.satisfaction / 100f);
		customer.state = CustomerState.BeingServed;
		ThirdPersonCharacter customerTpc = customer.GetComponentInChildren<ThirdPersonCharacter>();
		List<OrderEntry> orderEntries = customer.order.entries.FindAll((OrderEntry x) => (ItemsGetter.GetByName(x.itemName).type & ItemType.RetailProduct) != 0);
		bool flag = orderEntries.Count > 0;
		bool flag2 = true;
		if (flag)
		{
			flag2 = CheckConditions(orderEntries);
		}
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness && !flag2)
		{
			List<OrderEntry> list = orderEntries.Where((OrderEntry x) => !x.priceAccceptable).ToList();
			if (!customer.isPlayer)
			{
				if (list.Count > 0)
				{
					yield return customerTpc.ShowExpression(CharacterEmojiName.CustomerTooHighPrice, 1f, new
					{
						itemname = LocalizationHelper.GetItemLabel(list.GetRandom().itemName)
					});
				}
				else
				{
					yield return customerTpc.ShowExpression(CharacterEmojiName.CustomerCantFindItem, 1f, new
					{
						itemname = LocalizationHelper.GetItemLabel(orderEntries.GetRandom().itemName)
					});
				}
				customer.Leave();
			}
			FinishServingCustomer();
			yield break;
		}
		BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, employeeStationController.ItemInstance);
		GameObject container = null;
		if (flag && employeeStationController != null)
		{
			yield return employeeTpc.MoveToPosition(employeeStationController.transform, employeeStationController.GetEmployeePosition(), 0.5f, rotateToLookTarget: true);
			if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness && !employeeStationController.ItemInstance.SubtractFromStock())
			{
				if (customer.isPlayer)
				{
					InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Close();
					Notifications.ShowError("cashregister_no_paperbags");
				}
				else
				{
					yield return customerTpc.ShowExpression(CharacterEmojiName.CustomerNoPaperbags);
					customer.Leave();
				}
				FinishServingCustomer();
				yield break;
			}
			container = SetPaperbag(employeeTpc);
			if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness && !customer.isPlayer)
			{
				float pricePerUnit = employeeStationController.ItemInstance.GetStockInstance().pricePerUnit;
				customer.order.AddPaperBagEntry(pricePerUnit);
			}
		}
		yield return GrabOrderEntryItems(orderEntries, employeeTpc);
		yield return employeeTpc.MoveToPosition(employeeStationController.transform, employeeStationController.GetEmployeePosition(), 0.5f, rotateToLookTarget: true);
		if (customer == null)
		{
			activeCoroutine = null;
			yield break;
		}
		if (!customer.isPlayer && container != null)
		{
			yield return customerTpc.animator.RunAnimation(AnimationType.UsingProducer, 2.5f);
			employeeTpc.SetHandContent(null);
			SetPaperbag(customerTpc);
			InstanceBehavior<SfxManager>.Instance.PlayAudio((customerTpc.appearanceSetter.data.gender == Gender.Male) ? SoundType.NpcMaleProducerInteraction : SoundType.NpcFemaleProducerInteraction, customerTpc.transform.position);
		}
		else
		{
			employeeTpc.SetHandContent(null);
		}
		bool flag3 = InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness && !customer.isPlayer;
		if (customer.order.Pay(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, base.transform.position, customer.isPlayer, flag3))
		{
			customer.order.cleanliness = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.GetCleanliness();
			customer.order.completed = true;
			Order order = customer.order;
			if (order.timestamp == null)
			{
				order.timestamp = TimeHelper.Now();
			}
			if (flag3)
			{
				yield return ReturnUnacceptablePriceItems();
			}
			if (customer.isPlayer)
			{
				UpdatePlayerPurchase(customer.order.entries);
				InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Close();
			}
			else if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
			{
				InstanceBehavior<BuildingManager>.Instance.buildingRegistration.unprocessedCompletedOrders.Add(customer.order);
			}
		}
		if ((bool)customer)
		{
			customer.state = CustomerState.Served;
		}
		FinishServingCustomer();
	}

	private bool CheckConditions(IEnumerable<OrderEntry> orderEntries)
	{
		bool result = false;
		foreach (OrderEntry orderEntry in orderEntries)
		{
			ItemController itemController = InstanceBehavior<BuildingManager>.Instance.FindOptimalItemController(orderEntry.itemName, employeeTpc.transform.position);
			if (ItemHelper.IsAiShelfEmpty(itemController, InstanceBehavior<BuildingManager>.Instance.buildingRegistration))
			{
				itemController = null;
			}
			orderEntry.available = itemController != null;
			if (orderEntry.available)
			{
				if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness && !customer.isPlayer)
				{
					orderEntry.price = ItemHelper.GetPrice(orderEntry.itemName, InstanceBehavior<BuildingManager>.Instance.buildingRegistration);
					orderEntry.priceAccceptable = customer.citizenData.IsPriceAcceptable(orderEntry.itemName, orderEntry.price);
					if (orderEntry.priceAccceptable)
					{
						result = true;
					}
				}
				else
				{
					result = true;
				}
			}
			if (!orderEntry.available || !orderEntry.priceAccceptable)
			{
				orderEntry.processed = true;
			}
		}
		return result;
	}

	public static bool CheckStockAvailableForEntries(List<OrderEntry> orderEntries, Vector3 position)
	{
		foreach (OrderEntry orderEntry in orderEntries)
		{
			ItemController itemController = InstanceBehavior<BuildingManager>.Instance.FindOptimalItemController(orderEntry.itemName, position);
			if (itemController != null)
			{
				return itemController.ItemInstance.GetStockInstance().amount > 0;
			}
		}
		return false;
	}

	public static GameObject SetPaperbag(ThirdPersonCharacter employeeTpc)
	{
		GameObject gameObject = PrefabHelper.CreatePrefabItem(ItemsGetter.GetRandomBag()).gameObject;
		employeeTpc.SetHandContent(gameObject.transform);
		return gameObject;
	}

	public static IEnumerator GrabOrderEntryItems(IEnumerable<OrderEntry> orderEntries, ThirdPersonCharacter employeeTpc)
	{
		ItemController itemController = null;
		foreach (OrderEntry orderEntry in orderEntries)
		{
			ItemController itemController2 = InstanceBehavior<BuildingManager>.Instance.FindOptimalItemController(orderEntry.itemName, employeeTpc.transform.position);
			if (ItemHelper.IsAiShelfEmpty(itemController2, InstanceBehavior<BuildingManager>.Instance.buildingRegistration))
			{
				itemController2 = null;
			}
			if (!itemController2)
			{
				orderEntry.available = false;
				orderEntry.processed = true;
				continue;
			}
			if (itemController != itemController2)
			{
				yield return GrabItem(itemController2, employeeTpc);
				itemController = itemController2;
			}
			orderEntry.processed = true;
			if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
			{
				orderEntry.available = itemController2.ItemInstance.SubtractFromStock();
				if (orderEntry.available)
				{
					orderEntry.wholesalePrice = itemController2.ItemInstance.GetStockInstance().pricePerUnit;
				}
			}
			else
			{
				orderEntry.available = true;
			}
		}
	}

	private static IEnumerator GrabItem(ItemController itemController, ThirdPersonCharacter employeeTpc)
	{
		Vector3 vector = itemController.GetClosestNavMeshTargetPosition(employeeTpc.transform.position);
		if (vector == Vector3.zero)
		{
			vector = itemController.GetNavMeshTargetPosition();
		}
		yield return employeeTpc.MoveToPosition(itemController.transform, vector, 0.5f, rotateToLookTarget: true, null, 0f, null, abortIfStuck: true);
		if (employeeTpc.WasLastTargetReached)
		{
			employeeTpc.StartCoroutine(employeeTpc.animator.RunAnimation(AnimationType.UsingProducer, 2.5f));
		}
		BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, itemController.ItemInstance);
	}

	public static void UpdatePlayerPurchase(List<OrderEntry> orderEntries)
	{
		ItemInstance itemInstance = new ItemInstance(ItemsGetter.GetRandomBag());
		foreach (OrderEntry orderEntry in orderEntries)
		{
			if (orderEntry.available)
			{
				CargoInstance cargoInstance = new CargoInstance(orderEntry.itemName, 1, orderEntry.price);
				itemInstance.AddToCargo(cargoInstance);
			}
		}
		PlayerHelper.ItemInstanceInHands = itemInstance;
	}

	private void FinishServingCustomer()
	{
		if ((bool)customer)
		{
			customer.state = CustomerState.Served;
			employeeStationController.GetWaitingLine().customersManagement.RemoveCustomer(customer);
			customer = null;
			activeCoroutine = null;
		}
	}

	public override void SetEmployeeStation(EmployeeStationController stationController)
	{
		base.SetEmployeeStation(stationController);
		employeeTpc.navmeshAgent.Warp(stationController.GetEmployeePosition());
		employeeTpc.LookTarget = stationController.transform.position;
		employeeTpc.LookTarget.y = base.transform.position.y;
	}

	private IEnumerator ReturnUnacceptablePriceItems()
	{
		OrderEntry orderEntry = null;
		foreach (OrderEntry entry in customer.order.entries)
		{
			if (!entry.priceAccceptable && entry.available && entry.processed && (ItemsGetter.GetByName(entry.itemName).type & ItemType.ServiceProduct) == 0)
			{
				if (!new CargoInstance(entry.itemName, 1, entry.wholesalePrice).ReturnToAShelf(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Address))
				{
					Debug.LogWarning("Couldn't return customer " + entry.itemName + " to a shelf. It will disappear");
				}
				orderEntry = entry;
			}
		}
		if (orderEntry != null)
		{
			ThirdPersonCharacter componentInChildren = customer.GetComponentInChildren<ThirdPersonCharacter>();
			yield return componentInChildren.ShowExpression(CharacterEmojiName.CustomerTooHighPrice, 1f, new
			{
				itemname = LocalizationHelper.GetItemLabel(orderEntry.itemName)
			});
		}
	}
}
