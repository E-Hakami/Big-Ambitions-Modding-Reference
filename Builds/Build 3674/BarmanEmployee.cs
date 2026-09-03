using System.Collections;
using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.Items;
using BigAmbitions.SoundSystem;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Entities;
using Helpers;
using UI;
using UnityEngine;
using UnityEngine.AI;

public class BarmanEmployee : Employee
{
	private ObstacleAvoidanceType oldType;

	private float oldSpeed;

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

	private new void Update()
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
			employeeTpc.RemoveHandObject();
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
		if (customer != null)
		{
			yield return null;
		}
		if (customer.isPlayer)
		{
			yield return ServePlayer();
			yield break;
		}
		ThirdPersonCharacter customerTpc = customer.GetComponentInChildren<ThirdPersonCharacter>();
		OrderEntry orderEntry = GetNextOrderEntry(customer);
		ItemController shelfWithProduct = InstanceBehavior<BuildingManager>.Instance.FindOptimalItemController(orderEntry.itemName, employeeTpc.transform.position);
		if (ItemHelper.IsAiShelfEmpty(shelfWithProduct, InstanceBehavior<BuildingManager>.Instance.buildingRegistration))
		{
			shelfWithProduct = null;
		}
		orderEntry.available = shelfWithProduct != null;
		bool flag = IsOrderEntryPurchasable(orderEntry);
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness && !flag)
		{
			bool priceAccceptable = orderEntry.priceAccceptable;
			orderEntry.processed = true;
			CharacterEmojiName characterEmojiName = CharacterEmojiName.CustomerTooHighPrice;
			if (priceAccceptable)
			{
				characterEmojiName = CharacterEmojiName.CustomerCantFindItem;
			}
			yield return customerTpc.ShowExpression(characterEmojiName, 1f, new
			{
				itemname = LocalizationHelper.GetItemLabel(orderEntry.itemName)
			});
			FinishServingCustomer();
			yield break;
		}
		if (employeeStationController == null || shelfWithProduct == null)
		{
			FinishServingCustomer();
			yield break;
		}
		BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, employeeStationController.ItemInstance);
		yield return employeeTpc.MoveToPosition(shelfWithProduct.transform, shelfWithProduct.GetNavMeshTargetPosition(), 0.5f, rotateToLookTarget: true);
		yield return employeeTpc.animator.RunAnimation(AnimationType.UsingProducer, 1.5f);
		string handObjectName = GetHandObjectNameFromItemName(orderEntry.itemName);
		employeeTpc.AddHandObject(handObjectName);
		BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, shelfWithProduct.ItemInstance);
		orderEntry.available = !InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness || shelfWithProduct.ItemInstance.SubtractFromStock();
		if (orderEntry.available && InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			orderEntry.wholesalePrice = shelfWithProduct.ItemInstance.GetStockInstance().pricePerUnit;
		}
		orderEntry.processed = true;
		yield return employeeTpc.MoveToPosition(employeeStationController.transform, employeeStationController.GetEmployeePosition(), 0.5f, rotateToLookTarget: true);
		employeeTpc.RemoveHandObject();
		if (customer == null)
		{
			activeCoroutine = null;
			yield break;
		}
		if (orderEntry.available)
		{
			yield return customerTpc.animator.RunAnimation(AnimationType.UsingProducer, 1.5f);
			customerTpc.AddHandObject(handObjectName);
			customerTpc.HoldAnItem();
			customer.isHoldingADrink = true;
			InstanceBehavior<SfxManager>.Instance.PlayAudio((customerTpc.appearanceSetter.data.gender == Gender.Male) ? SoundType.NpcMaleProducerInteraction : SoundType.NpcFemaleProducerInteraction, customerTpc.transform.position);
			InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.PurchaseSuccess, employeeStationController.transform.position, 1f, isPlayer);
		}
		orderEntry.paid = true;
		customer.order.cleanliness = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.GetCleanliness();
		if (IsLastOrderEntry())
		{
			customer.CompleteOrder();
		}
		FinishServingCustomer();
	}

	private bool IsLastOrderEntry()
	{
		return customer.order.entries.All((OrderEntry x) => x.processed);
	}

	private static OrderEntry GetNextOrderEntry(Customer customer)
	{
		return customer.order.entries.First((OrderEntry x) => !x.processed);
	}

	private IEnumerator ServePlayer()
	{
		if (customer != null)
		{
			yield return null;
		}
		BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, employeeStationController.ItemInstance);
		if (employeeStationController != null)
		{
			yield return employeeTpc.MoveToPosition(employeeStationController.transform, employeeStationController.GetEmployeePosition(), 0.5f, rotateToLookTarget: true);
			GameObject gameObject = PrefabHelper.CreatePrefabItem(ItemsGetter.GetRandomBag()).gameObject;
			employeeTpc.SetHandContent(gameObject.transform);
		}
		yield return GrabOrderEntryItems();
		yield return employeeTpc.MoveToPosition(employeeStationController.transform, employeeStationController.GetEmployeePosition(), 0.5f, rotateToLookTarget: true);
		if (customer == null)
		{
			activeCoroutine = null;
			yield break;
		}
		employeeTpc.SetHandContent(null);
		if (customer.order.Pay(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, base.transform.position, customer.isPlayer))
		{
			customer.order.cleanliness = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.GetCleanliness();
			customer.order.completed = true;
			ItemInstance itemInstance = ItemHelper.InitializeNewInstance(ItemsGetter.GetRandomBag());
			foreach (OrderEntry entry in customer.order.entries)
			{
				itemInstance.AddToCargo(new CargoInstance(entry.itemName, 1, entry.price));
			}
			InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Close();
			PlayerHelper.ItemInstanceInHands = itemInstance;
		}
		FinishServingCustomer();
	}

	private IEnumerator GrabOrderEntryItems()
	{
		ItemController itemController = null;
		foreach (OrderEntry orderEntry in customer.order.entries)
		{
			ItemController itemController2 = InstanceBehavior<BuildingManager>.Instance.FindOptimalItemController(orderEntry.itemName, employeeTpc.transform.position);
			if (ItemHelper.IsAiShelfEmpty(itemController2, InstanceBehavior<BuildingManager>.Instance.buildingRegistration))
			{
				itemController2 = null;
			}
			if (!itemController2)
			{
				orderEntry.available = false;
				continue;
			}
			BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, itemController2.ItemInstance);
			if (itemController != itemController2)
			{
				yield return GrabItem(itemController2);
				itemController = itemController2;
			}
			orderEntry.available = !InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness || itemController2.ItemInstance.SubtractFromStock();
			if (orderEntry.available && InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
			{
				orderEntry.wholesalePrice = itemController2.ItemInstance.GetStockInstance().pricePerUnit;
			}
		}
	}

	private IEnumerator GrabItem(ItemController itemController)
	{
		Vector3 navMeshTargetPosition = itemController.GetNavMeshTargetPosition();
		yield return employeeTpc.MoveToPosition(itemController.transform, navMeshTargetPosition, 0.5f, rotateToLookTarget: true);
		yield return employeeTpc.animator.RunAnimation(AnimationType.UsingProducer, 1.5f);
	}

	private bool IsOrderEntryPurchasable(OrderEntry orderEntry)
	{
		if (!orderEntry.available)
		{
			return false;
		}
		bool result = false;
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
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
		return result;
	}

	private string GetHandObjectNameFromItemName(string itemName)
	{
		return itemName switch
		{
			"ba:itemname_beer" => "GlassOfBeer", 
			"ba:itemname_margarita" => "GlassOfMargarita", 
			"ba:itemname_martini" => "GlassOfMartini", 
			"ba:itemname_whisky" => "GlassOfWhisky", 
			_ => null, 
		};
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
}
