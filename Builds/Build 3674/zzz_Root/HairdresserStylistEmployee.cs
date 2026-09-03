using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.Items;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Controllers;
using Entities;
using Helpers;
using UI;
using UI.Notification;
using UnityEngine;

public class HairdresserStylistEmployee : Employee
{
	private const float maxAnimationLengthInSeconds = 30f;

	private bool _isInHeadWasherStation;

	private float _animationLengthInSeconds;

	private WaitForSeconds _waitForHaircut;

	public override void Start()
	{
		base.Start();
		SetAgentProperties();
		_isInHeadWasherStation = ((HairdresserChairController)employeeStationController).isHeadWasher;
		_animationLengthInSeconds = 30f / (float)employeeStationController.Item.addedCustomersPerHour;
		_waitForHaircut = new WaitForSeconds(_animationLengthInSeconds);
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
			if (customer != null)
			{
				customer.tpc.Reset();
				customer = null;
			}
			employeeTpc.SetHandContent(null);
			employeeTpc.Reset();
			ResetAnimation();
			yield return employeeTpc.MoveToPosition(employeeStationController.transform, employeeStationController.GetEmployeePosition());
		}
		else
		{
			yield return new WaitForEndOfFrame();
		}
	}

	protected override IEnumerator ServeCustomer()
	{
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			customer.order.customerServiceSkill = employeeInstance.GetSkillValue("ba:skill_hairstylist") * (employeeInstance.satisfaction / 100f);
		}
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
		IEnumerable<OrderEntry> orderEntries = GetOrderEntries();
		bool firstService = true;
		bool noHairProducts = false;
		Vector3 employeePosition = employeeStationController.GetEmployeePosition();
		BuildingManager buildingManager = InstanceBehavior<BuildingManager>.Instance;
		foreach (OrderEntry entry in orderEntries)
		{
			if (noHairProducts)
			{
				entry.processed = true;
				continue;
			}
			ItemController productShelf = buildingManager.FindOptimalItemController("ba:itemname_haircareproduct", employeeTpc.transform.position);
			if (buildingManager.IsPlayerOwnedBusiness)
			{
				yield return ShowExpressionIfServiceNotPurchasable(entry.itemName, productShelf, entry);
				if (!entry.priceAccceptable)
				{
					continue;
				}
				if (!entry.available)
				{
					noHairProducts = true;
					continue;
				}
			}
			else
			{
				if (ItemHelper.IsAiShelfEmpty(productShelf, InstanceBehavior<BuildingManager>.Instance.buildingRegistration))
				{
					productShelf = null;
				}
				if (productShelf == null)
				{
					noHairProducts = true;
					entry.processed = true;
					continue;
				}
			}
			if (firstService)
			{
				SitCustomer(_isInHeadWasherStation);
			}
			yield return employeeTpc.MoveToPosition(productShelf.transform, productShelf.GetNavMeshTargetPosition(), 0.5f, rotateToLookTarget: true);
			if (productShelf == null)
			{
				yield return employeeTpc.MoveToPosition(employeeStationController.transform, employeePosition, 0.5f, rotateToLookTarget: true);
				continue;
			}
			entry.available = !InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness || productShelf.ItemInstance.SubtractFromStock();
			entry.processed = true;
			if (entry.available)
			{
				if (buildingManager.IsPlayerOwnedBusiness)
				{
					entry.wholesalePrice = productShelf.ItemInstance.GetStockInstance().pricePerUnit;
					entry.price = GetEntryPrice(entry);
					entry.paid = true;
				}
				yield return employeeTpc.animator.RunAnimation(AnimationType.UsingProducer, 1.5f);
			}
			yield return employeeTpc.MoveToPosition(employeeStationController.transform, employeePosition, 0.5f, rotateToLookTarget: true);
			if (entry.available)
			{
				yield return RunHairdresserAnimation(entry.itemName);
				ChangeHairAppearance(entry.itemName);
				yield return RunHairdresserAnimationExit(entry.itemName);
				ExitFromHairdresserAnimationState(entry.itemName);
				BuildingCleanlinessHelper.ApplyDirt(buildingManager.buildingRegistration, employeeStationController.ItemInstance);
			}
			firstService = false;
		}
		FinishServingCustomer();
	}

	private IEnumerator ServePlayer()
	{
		ItemController productShelf = InstanceBehavior<BuildingManager>.Instance.FindOptimalItemController("ba:itemname_haircareproduct", employeeTpc.transform.position);
		if (ItemHelper.IsAiShelfEmpty(productShelf, InstanceBehavior<BuildingManager>.Instance.buildingRegistration))
		{
			productShelf = null;
		}
		OrderEntry entry = customer.order.entries[0];
		Vector3 employeePosition = employeeStationController.GetEmployeePosition();
		if (productShelf == null)
		{
			StopServingPlayer();
			yield break;
		}
		SitCustomer(isHairWasher: false);
		yield return employeeTpc.MoveToPosition(productShelf.transform, productShelf.GetNavMeshTargetPosition(), 0.5f, rotateToLookTarget: true);
		if (productShelf == null)
		{
			yield return employeeTpc.MoveToPosition(employeeStationController.transform, employeePosition, 0.5f, rotateToLookTarget: true);
			StopServingPlayer();
			yield break;
		}
		entry.available = !InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness || productShelf.ItemInstance.SubtractFromStock();
		if (entry.available)
		{
			yield return employeeTpc.animator.RunAnimation(AnimationType.UsingProducer, 1.5f);
			yield return employeeTpc.MoveToPosition(employeeStationController.transform, employeePosition, 0.5f, rotateToLookTarget: true);
			if (entry.available)
			{
				yield return RunHairdresserAnimation(entry.itemName);
				yield return RunHairdresserAnimationExit(entry.itemName);
				ExitFromHairdresserAnimationState(entry.itemName);
				BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, employeeStationController.ItemInstance);
				FinishServingPlayer();
			}
			else
			{
				StopServingPlayer();
			}
		}
		else
		{
			StopServingPlayer();
		}
	}

	private void FinishServingPlayer()
	{
		((HairdresserChairController)employeeStationController).onHairChangeAction();
		customer.order.Pay(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, customer.transform.position, isPlayer: true);
		FinishServingCustomer();
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Close();
	}

	private IEnumerable<OrderEntry> GetOrderEntries()
	{
		if (!_isInHeadWasherStation)
		{
			return customer.order.entries.Where((OrderEntry x) => !x.processed && (ItemsGetter.GetByName(x.itemName).type & ItemType.ServiceProduct) != 0 && x.itemName != "ba:itemname_hairshampooingfee");
		}
		return customer.order.entries.Where((OrderEntry x) => !x.processed && x.itemName == "ba:itemname_hairshampooingfee");
	}

	private float GetEntryPrice(OrderEntry entry)
	{
		if (!customer.isPlayer)
		{
			return ItemHelper.GetPrice(entry.itemName, InstanceBehavior<BuildingManager>.Instance.buildingRegistration);
		}
		return 0f;
	}

	private bool IsServiceFeeAcceptable(string feeItemName)
	{
		float price = ItemHelper.GetPrice(feeItemName, InstanceBehavior<BuildingManager>.Instance.buildingRegistration);
		return customer.citizenData.IsPriceAcceptable(feeItemName, price);
	}

	private IEnumerator ShowExpressionIfServiceNotPurchasable(string serviceName, ItemController shelfWithProduct, OrderEntry entry)
	{
		bool flag = shelfWithProduct != null;
		bool flag2 = IsServiceFeeAcceptable(serviceName);
		entry.available = flag;
		entry.priceAccceptable = flag2;
		if (!flag || !flag2)
		{
			entry.processed = true;
			CharacterEmojiName characterEmojiName = (flag ? CharacterEmojiName.CustomerTooHighPrice : CharacterEmojiName.CustomerNoHairCareProducts);
			var localizationArgs = (flag2 ? null : new
			{
				itemname = LocalizationHelper.GetItemLabel(serviceName)
			});
			yield return customer.tpc.ShowExpression(characterEmojiName, 1f, localizationArgs);
		}
	}

	private void StopServingPlayer()
	{
		Dictionary<string, string> notificationData = new Dictionary<string, string> { { "itemname", "ba:itemname_haircareproduct" } };
		Notifications.Show(NotificationType.Error, "notification_no_items_available", notificationData);
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Close();
		FinishServingCustomer();
	}

	private void SitCustomer(bool isHairWasher)
	{
		PermanentAnimationType animationType = (isHairWasher ? PermanentAnimationType.SittingOnHairdresserWashChair : PermanentAnimationType.SittingOnHairdresserChair);
		customer.tpc.SitOnChair(employeeStationController.SittingPosition, animationType);
	}

	private IEnumerator RunHairdresserAnimation(string serviceName)
	{
		switch (serviceName)
		{
		case "ba:itemname_hairshampooingfee":
			employeeTpc.animator.SetBool(PermanentAnimationType.ShampooHairIdle);
			employeeTpc.animator.SetTrigger(AnimationType.ShampooHair);
			break;
		case "ba:itemname_hairchemicalfee":
			employeeTpc.animator.SetBool(PermanentAnimationType.ChemsHairIdle);
			employeeTpc.animator.SetTrigger(AnimationType.ChemsHair);
			break;
		case "ba:itemname_haircuttingfee":
			employeeTpc.animator.SetBool(PermanentAnimationType.CutHairIdle);
			employeeTpc.animator.SetTrigger(AnimationType.CutHair);
			break;
		case "ba:itemname_hairstylingfee":
			employeeTpc.animator.SetBool(PermanentAnimationType.StyleHairIdle);
			employeeTpc.animator.SetTrigger(AnimationType.StyleHair);
			break;
		default:
			employeeTpc.animator.SetTrigger(AnimationType.UsingProducer);
			break;
		}
		yield return _waitForHaircut;
	}

	private IEnumerator RunHairdresserAnimationExit(string serviceName)
	{
		switch (serviceName)
		{
		case "ba:itemname_hairshampooingfee":
			yield return employeeTpc.animator.RunAnimation(AnimationType.ShampooHairExit);
			break;
		case "ba:itemname_hairchemicalfee":
			yield return employeeTpc.animator.RunAnimation(AnimationType.ChemsHairExit);
			break;
		case "ba:itemname_haircuttingfee":
			yield return employeeTpc.animator.RunAnimation(AnimationType.CutHairExit);
			break;
		case "ba:itemname_hairstylingfee":
			yield return employeeTpc.animator.RunAnimation(AnimationType.StyleHairExit);
			break;
		default:
			employeeTpc.animator.SetTrigger(AnimationType.UsingProducer);
			yield return null;
			break;
		}
	}

	private void ExitFromHairdresserAnimationState(string serviceName)
	{
		switch (serviceName)
		{
		case "ba:itemname_hairshampooingfee":
			employeeTpc.animator.SetBool(PermanentAnimationType.ShampooHairIdle, state: false);
			break;
		case "ba:itemname_hairchemicalfee":
			employeeTpc.animator.SetBool(PermanentAnimationType.ChemsHairIdle, state: false);
			break;
		case "ba:itemname_haircuttingfee":
			employeeTpc.animator.SetBool(PermanentAnimationType.CutHairIdle, state: false);
			break;
		case "ba:itemname_hairstylingfee":
			employeeTpc.animator.SetBool(PermanentAnimationType.StyleHairIdle, state: false);
			break;
		}
	}

	private void ChangeHairAppearance(string serviceName)
	{
		if (serviceName == "ba:itemname_hairchemicalfee" || serviceName == "ba:itemname_haircuttingfee")
		{
			AppearanceSetter appearanceSetter = customer.tpc.appearanceSetter;
			if (serviceName == "ba:itemname_hairchemicalfee")
			{
				appearanceSetter.RandomizeElementColor(AppearanceElementType.Hair, new AppearanceTag[1] { AppearanceTag.All });
			}
			else
			{
				appearanceSetter.RandomizeElement(AppearanceElementType.Hair, new AppearanceTag[1] { AppearanceTag.All }, randomizeColor: false, excludeCurrentVariant: true);
			}
			appearanceSetter.UpdateVisuals();
		}
	}

	private void FinishServingCustomer()
	{
		if ((bool)customer)
		{
			customer.state = CustomerState.Served;
			employeeStationController.GetWaitingLine().customersManagement.RemoveCustomer(customer);
			customer.tpc.Reset();
			customer = null;
			activeCoroutine = null;
		}
	}

	private void ResetAnimation()
	{
		employeeTpc.animator.SetBool(PermanentAnimationType.ShampooHairIdle, state: false);
		employeeTpc.animator.SetBool(PermanentAnimationType.ChemsHairIdle, state: false);
		employeeTpc.animator.SetBool(PermanentAnimationType.CutHairIdle, state: false);
		employeeTpc.animator.SetBool(PermanentAnimationType.StyleHairIdle, state: false);
	}

	public override void SetEmployeeStation(EmployeeStationController stationController)
	{
		base.SetEmployeeStation(stationController);
		employeeTpc.navmeshAgent.Warp(stationController.GetEmployeePosition());
		employeeTpc.LookTarget = stationController.transform.position;
		employeeTpc.LookTarget.y = base.transform.position.y;
	}
}
