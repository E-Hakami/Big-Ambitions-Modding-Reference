using System.Collections;
using BigAmbitions.Characters;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Extensions;
using Helpers;
using UI;
using UnityEngine;

public class SelfServiceEmployee : Employee
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
		}
		yield return new WaitForEndOfFrame();
	}

	protected override IEnumerator ServeCustomer()
	{
		if (customer == null)
		{
			customer = null;
			activeCoroutine = null;
			yield break;
		}
		customer.order.customerServiceSkill = employeeInstance.GetSkillValue("ba:skill_customerservice") * (employeeInstance.satisfaction / 100f);
		bool isCustomerPlayer = customer.CompareTag("Player");
		if (isCustomerPlayer)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.cancelButton.interactable = false;
		}
		customer.state = CustomerState.BeingServed;
		yield return employeeTpc.animator.RunAnimation(AnimationType.UsingCashRegister, employeeStationController.Item.HasTag(TagRef.Itemtag.isfullservicecashregister) ? 2f : 3f);
		ItemController component = null;
		customer.tpc.GetHandContent()?.TryGetComponent<ItemController>(out component);
		bool flag = (bool)component && component.Item.HasTag(TagRef.Itemtag.isshoppingcontainer);
		int num;
		if (flag && InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			string businessTypeName = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.businessTypeName;
			if (!(businessTypeName == "ba:businesstype_cinema") && !(businessTypeName == "ba:businesstype_theater"))
			{
				num = (employeeStationController.ItemInstance.SubtractFromStock() ? 1 : 0);
				goto IL_0193;
			}
		}
		num = 1;
		goto IL_0193;
		IL_0193:
		if (num != 0)
		{
			if (customer.order.Pay(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, base.transform.position, isCustomerPlayer))
			{
				if (flag)
				{
					if (isCustomerPlayer)
					{
						ConvertPlayerShoppingContainerToPaperBag();
					}
					else
					{
						float pricePerUnit = employeeStationController.ItemInstance.GetStockInstance().pricePerUnit;
						ConvertAiShoppingContainerToPaperBag(customer, pricePerUnit);
					}
				}
				if (isCustomerPlayer)
				{
					UpdatePlayerPurchase();
				}
			}
			else if (isCustomerPlayer)
			{
				InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Close();
			}
		}
		else
		{
			customer.order.AddPaperBagEntry(0f, priceAccceptable: true, available: false);
			customer.ReturnItemsToShelf();
			customer.tpc.SetHandContent(null);
			customer.order.completed = true;
			yield return customer.tpc.ShowExpression(CharacterEmojiName.CustomerNoPaperbags);
		}
		employeeStationController.GetWaitingLine().customersManagement.RemoveCustomer(customer);
		TellCurrentCustomerToLeave();
	}

	public static void ConvertPlayerShoppingContainerToPaperBag()
	{
		ItemInstance itemInstance = ItemHelper.InitializeNewInstance(ItemsGetter.GetRandomBag());
		for (int num = PlayerHelper.ItemInstanceInHands.cargoInstances.Count - 1; num >= 0; num--)
		{
			PlayerHelper.ItemInstanceInHands.cargoInstances[num].TryToMoveCargoBetweenHolders(PlayerHelper.ItemInstanceInHands, itemInstance);
		}
		PlayerHelper.ItemInstanceInHands = itemInstance;
	}

	public static void ConvertAiShoppingContainerToPaperBag(Customer customer, float paperBagCost)
	{
		ItemController itemController = PrefabHelper.CreatePrefabItem(ItemsGetter.GetRandomBag());
		customer.tpc.SetHandContent(itemController.transform);
		customer.order.AddPaperBagEntry(paperBagCost);
	}

	public static void UpdatePlayerPurchase()
	{
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.SetCargoInstancesToPaid();
		InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.onPaidForItems.Invoke();
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Close();
		GameEvent.Invoke("ba:gameevent_purchasecompleted");
	}

	protected void TellCurrentCustomerToLeave()
	{
		if ((bool)customer)
		{
			ShowCustomerServiceExpression();
			customer.order.completed = true;
			Order order = customer.order;
			if (order.timestamp == null)
			{
				order.timestamp = TimeHelper.Now();
			}
			InstanceBehavior<BuildingManager>.Instance.buildingRegistration.unprocessedCompletedOrders.Add(customer.order);
			customer.state = CustomerState.Served;
			customer.assignedWaitingLine = null;
			customer.customerEntry.completed = true;
			customer = null;
			activeCoroutine = null;
		}
	}

	private void ShowCustomerServiceExpression()
	{
		if (customer.order.customerServiceSkill <= 30f && RngHelper.Chance(70))
		{
			StartCoroutine(customer.tpc.ShowExpression(CharacterEmojiName.CustomerBadService, 3f));
		}
		if (customer.order.customerServiceSkill >= 70f && !customer.order.completed && RngHelper.Chance(70))
		{
			StartCoroutine(customer.tpc.ShowExpression(CharacterEmojiName.CustomerGoodService, 3f));
		}
	}
}
