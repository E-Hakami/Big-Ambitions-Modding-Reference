using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AI.Citizens;
using AI.Customers.CustomerEntries;
using BehaviorDesigner.Runtime;
using BigAmbitions.Characters;
using BigAmbitions.Items;
using BigAmbitions.SaveSystem;
using BigAmbitions.SoundSystem;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Controllers;
using EmployeeStations;
using Entities;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using UI;
using UnityEngine;
using UnityEngine.AI;

public class Customer : MonoBehaviour
{
	private const float MinReactionTime = 0.01f;

	private const float MaxReactionTime = 0.05f;

	public CustomerState state;

	public ThirdPersonCharacter tpc;

	public CitizenData citizenData;

	public Order order;

	public CustomerEntry customerEntry;

	public WaitingLine assignedWaitingLine;

	public bool isPlayer;

	public bool hasABasket;

	public bool isHoldingADrink;

	public int currentWaitingLineSpot;

	public BehaviorTree behaviorTree;

	[SerializeField]
	private bool updateHandVisuals = true;

	[NonSerialized]
	public SeatSpot isSittingOn;

	[NonSerialized]
	public CustomerTimeState customerTimeState;

	private bool _floorVisible = true;

	private bool _cameraVisible = true;

	protected List<CharacterEmojiName> otherDemands = new List<CharacterEmojiName>();

	public float GetRandomReactionTime()
	{
		return UnityEngine.Random.Range(0.01f, 0.05f);
	}

	protected virtual void Awake()
	{
		if (tpc != null)
		{
			tpc.navmeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
		}
		behaviorTree = GetComponent<BehaviorTree>();
	}

	private void SubscribeToGlobalEvents()
	{
		GlobalEvents.onTimeMachineStarted = (Action)Delegate.Combine(GlobalEvents.onTimeMachineStarted, new Action(OnTimeMachineStarted));
		GlobalEvents.onNewHour = (Action)Delegate.Combine(GlobalEvents.onNewHour, new Action(OnNewHour));
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
		if (InstanceBehavior<BuildingManager>.Instance?.multipleHeightsBuildingController != null)
		{
			AppearanceSetter appearanceSetter = tpc.appearanceSetter;
			appearanceSetter.visualsUpdated = (Action)Delegate.Combine(appearanceSetter.visualsUpdated, new Action(ResetVisibilityValues));
		}
	}

	public void UnsubscribeToGlobalEvents()
	{
		GlobalEvents.onTimeMachineStarted = (Action)Delegate.Remove(GlobalEvents.onTimeMachineStarted, new Action(OnTimeMachineStarted));
		GlobalEvents.onNewHour = (Action)Delegate.Remove(GlobalEvents.onNewHour, new Action(OnNewHour));
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Remove(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		if (InstanceBehavior<BuildingManager>.Instance?.multipleHeightsBuildingController != null)
		{
			AppearanceSetter appearanceSetter = tpc.appearanceSetter;
			appearanceSetter.visualsUpdated = (Action)Delegate.Remove(appearanceSetter.visualsUpdated, new Action(ResetVisibilityValues));
		}
	}

	private void OnBuildingRegistrationChange(Address address)
	{
		if (!this)
		{
			Debug.LogError("null customer still subscribed to event");
		}
		else if (!(InstanceBehavior<BuildingManager>.Instance.building.Address != address) && InstanceBehavior<BuildingManager>.Instance.buildingRegistration.temporarilyClosed)
		{
			InstantlyLeave();
		}
	}

	private void OnNewHour()
	{
		if (!this)
		{
			Debug.LogError("null customer still subscribed to event");
		}
		else if (base.isActiveAndEnabled && !InstanceBehavior<BuildingManager>.Instance.isOpen && (!isPlayer || InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness))
		{
			InstantlyLeave();
		}
	}

	private void OnTimeMachineStarted()
	{
		if (!this)
		{
			Debug.LogError("null customer still subscribed to event");
		}
		else if (base.isActiveAndEnabled && !isPlayer && !InstanceBehavior<BuildingManager>.Instance.businessType.spawnCustomersOnlyOnEnter)
		{
			ResetItemsInTable();
			ReleaseCustomer();
			if ((bool)assignedWaitingLine)
			{
				assignedWaitingLine.data.customers.Remove(this);
			}
		}
	}

	private void InstantlyLeave()
	{
		tpc.SetHandContent(null);
		if ((bool)assignedWaitingLine)
		{
			assignedWaitingLine.customersManagement.RemoveCustomer(this);
		}
		Leave();
	}

	private IEnumerator CustomerDemandExpressionCoroutine(CharacterEmojiName characterEmojiName, float delay)
	{
		yield return new WaitForSeconds(delay);
		InstanceBehavior<SfxManager>.Instance.PlayAudio((citizenData.Gender == Gender.Male) ? SoundType.NpcMaleDisappointed : SoundType.NpcFemaleDisappointed, base.transform.position);
		yield return tpc.ShowExpression(characterEmojiName, 3f);
	}

	public ItemController FindShoppingBasket()
	{
		return InstanceBehavior<BuildingManager>.Instance.FindOptimalItemControllerWithTag(TagRef.Itemtag.isshoppingcontainer, tpc.transform.position);
	}

	public void SetBasket(ItemController basket = null)
	{
		if (!InstanceBehavior<BuildingManager>.Instance.businessType.CustomersNeedShoppingContainer)
		{
			return;
		}
		if ((object)basket == null)
		{
			basket = InstanceBehavior<BuildingManager>.Instance.allItemControllers.Where((ItemController x) => x.itemName == "ba:itemname_stackofshoppingbaskets").GetRandom();
		}
		if ((bool)basket)
		{
			hasABasket = true;
			BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, basket.ItemInstance);
			ItemController container = PrefabHelper.CreatePrefabItem(basket.Item.producerSettings.itemsToProduce.GetRandom());
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				container.SetCustomColors(basket.customColors);
			});
			tpc.SetHandContent(container.transform);
		}
	}

	public virtual void Init()
	{
		ResetVisibilityValues();
		tpc.animator.enabled = true;
		SubscribeToGlobalEvents();
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			order.customerDemandScore = OrderHelper.CalculateOrderDemandScore(order, InstanceBehavior<BuildingManager>.Instance.buildingRegistration);
		}
		if (!isPlayer)
		{
			SetAppearance();
			SetCurrentTimeState();
			if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness && customerTimeState == CustomerTimeState.JustArrived)
			{
				ComplainAboutUnfulfilledDemands();
			}
		}
	}

	public virtual void ResetVisibilityValues()
	{
		tpc.visible = true;
		_cameraVisible = true;
		_floorVisible = true;
	}

	protected virtual void SetAppearance()
	{
		tpc.appearanceSetter.SetRandomAppearance(citizenData.Gender, citizenData.AppearanceTags);
	}

	public void PayEntranceFee()
	{
		string entranceFeeItemName = BusinessTypeHelper.GetEntranceFeeNameForBusinessType(InstanceBehavior<BuildingManager>.Instance.businessType);
		OrderEntry orderEntry = order.entries.FirstOrDefault((OrderEntry x) => x.itemName == entranceFeeItemName);
		if (orderEntry != null)
		{
			orderEntry.available = true;
			orderEntry.paid = true;
			orderEntry.processed = true;
		}
		if (order.entries.Count <= 1)
		{
			CompleteOrder();
		}
	}

	private void ComplainAboutUnfulfilledDemands()
	{
		BuildingRegistration buildingRegistration = InstanceBehavior<BuildingManager>.Instance.buildingRegistration;
		List<CustomerDemand> list = new List<CustomerDemand>();
		List<string> cachedFulfilledCustomerDemands = buildingRegistration.cachedFulfilledCustomerDemands;
		HashSet<string> hashSet = ((cachedFulfilledCustomerDemands != null) ? new HashSet<string>(cachedFulfilledCustomerDemands) : null);
		bool flag = false;
		for (int i = 0; i < order.customerDemandTypes.Count; i++)
		{
			string text = order.customerDemandTypes[i];
			if (text == "ba:customerdemand_employeeuniforms")
			{
				flag = true;
			}
			else if (hashSet == null || !hashSet.Contains(text))
			{
				list.Add(CustomerDemandHelper.GetByType(text));
			}
		}
		if (flag)
		{
			CustomerDemand byType = CustomerDemandHelper.GetByType("ba:customerdemand_employeeuniforms");
			if (!byType.Fulfilled(buildingRegistration, null))
			{
				list.Add(byType);
			}
		}
		RemoveSinkDemandIfThereIsNoToilet(buildingRegistration, list);
		int num = 0;
		for (int j = 0; j < list.Count; j++)
		{
			CustomerDemand customerDemand = list[j];
			StartCoroutine(CustomerDemandExpressionCoroutine(customerDemand.characterEmojiName, (float)num * 4f));
			num++;
		}
		for (int k = 0; k < otherDemands.Count; k++)
		{
			StartCoroutine(CustomerDemandExpressionCoroutine(otherDemands[k], (float)num * 4f));
			num++;
		}
	}

	private void RemoveSinkDemandIfThereIsNoToilet(BuildingRegistration registration, List<CustomerDemand> unfulfilledCustomerDemands)
	{
		if (order.customerDemandTypes.Contains("ba:customerdemand_employeeuniforms") && !registration.cachedFulfilledCustomerDemands.Contains("ba:customerdemand_toilet"))
		{
			unfulfilledCustomerDemands.Remove(CustomerDemandHelper.GetByType("ba:customerdemand_sink"));
		}
	}

	public virtual void Leave()
	{
		ResetItemsInTable();
		if (isPlayer)
		{
			if (!InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
			{
				return;
			}
			InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Close(false);
			tpc.Reset();
			tpc.Bump();
			UnsubscribeToGlobalEvents();
			UnityEngine.Object.Destroy(this);
		}
		else
		{
			CustomerLeaveBuilding();
		}
		if (!order.completed && InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			ReturnItemsToShelf();
			order.completed = true;
		}
	}

	public void ReturnItemsToShelf()
	{
		if (!InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			return;
		}
		foreach (OrderEntry entry in order.entries)
		{
			if (entry.processed && (ItemsGetter.GetByName(entry.itemName).type & ItemType.ServiceProduct) == 0 && !new CargoInstance(entry.itemName, 1, entry.wholesalePrice).ReturnToAShelf(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Address))
			{
				Debug.LogWarning("Couldn't return customer " + entry.itemName + " to a shelf. It will disappear");
			}
		}
	}

	public void ResetItemsInTable()
	{
		if (isSittingOn != null)
		{
			Transform transform = tpc.isSittingOn;
			tpc.Reset();
			if ((bool)transform && transform.TryGetComponentInParent<SeatController>(out var component))
			{
				component.OnSittingChanged(transform, isSitting: false);
			}
			isSittingOn.occupied = false;
			if (isSittingOn.seatAttachmentPoint != null && isSittingOn.GetAttachedChair != null)
			{
				isSittingOn.GetAttachedChair.Occupied = false;
			}
			if (!isSittingOn.requiresPhysicalChair && isSittingOn.SeatTransform != null)
			{
				Vector3 navMeshTargetPosition = isSittingOn.SeatTransform.parent.GetComponent<ItemController>().GetNavMeshTargetPosition();
				tpc.navmeshAgent.Warp(navMeshTargetPosition);
			}
			ToggleTableFoodForCustomer(show: false);
			isSittingOn = null;
		}
	}

	public void RemoveDrink()
	{
		tpc.RemoveHandObject();
		tpc.StopHoldingAnItem();
		isHoldingADrink = false;
	}

	protected void ForceFinishOrder()
	{
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		if (!order.completed)
		{
			order.entries.RemoveAll((OrderEntry x) => !x.processed);
			CompleteOrder();
		}
	}

	protected void OnExitBuilding(Address _)
	{
		if (!this)
		{
			Debug.LogError("null customer still subscribed to event");
		}
		else
		{
			ForceFinishOrder();
		}
	}

	public void CompleteOrder()
	{
		this.order.completed = true;
		Order order = this.order;
		if (order.timestamp == null)
		{
			order.timestamp = TimeHelper.Now();
		}
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			InstanceBehavior<BuildingManager>.Instance.buildingRegistration.unprocessedCompletedOrders.Add(this.order);
		}
	}

	public void CustomerLeaveBuilding()
	{
		behaviorTree.DisableBehavior();
		tpc.StopDancing();
		tpc.StopAllCoroutines();
		StartCoroutine(LeaveBuilding());
	}

	private IEnumerator LeaveBuilding()
	{
		ExitZone exitZone = (from x in InstanceBehavior<BuildingManager>.Instance.exitZones
			where GenericExtensions.PathCanBeCompleted(base.transform.position, x.transform.position)
			orderby MathHelper.DistanceSqr(x.transform.position, base.transform.position)
			select x).FirstOrDefault();
		if ((object)exitZone == null)
		{
			exitZone = InstanceBehavior<BuildingManager>.Instance.exitZones[0];
		}
		yield return tpc.MoveToPosition(exitZone.transform, exitZone.despawner.transform.position, 0.5f, rotateToLookTarget: false, null, 0f, null, abortIfStuck: true);
		ReleaseCustomer();
	}

	public void ToggleTableFoodForCustomer(bool show)
	{
		if (isSittingOn == null || isSittingOn.seatAttachmentPoint == null)
		{
			return;
		}
		if (isSittingOn.seatAttachmentPoint.foodDisplay != null)
		{
			bool flag = false;
			Transform transform = null;
			bool flag2 = false;
			Transform transform2 = null;
			foreach (OrderEntry entry in order.entries)
			{
				if (!entry.paid || !entry.priceAccceptable)
				{
					continue;
				}
				string itemName = entry.itemName;
				if (itemName == "ba:itemname_sodacan" || itemName == "ba:itemname_cupofcoffee")
				{
					if (!flag2)
					{
						transform2 = FindTableFoodDisplay(entry.itemName);
						if (transform2 != null)
						{
							flag2 = true;
						}
					}
				}
				else if (!flag)
				{
					transform = FindTableFoodDisplay(entry.itemName);
					if (transform != null)
					{
						flag = true;
					}
				}
				if (flag2 & flag)
				{
					break;
				}
			}
			if (flag2 | flag)
			{
				isSittingOn.seatAttachmentPoint.foodDisplay.gameObject.SetActive(show);
				if (flag)
				{
					transform.gameObject.SetActive(show);
				}
				if (flag2)
				{
					transform2.gameObject.SetActive(show);
				}
			}
		}
		if (updateHandVisuals)
		{
			if (show)
			{
				tpc.SetHandContent(null);
				return;
			}
			GameObject gameObject = PrefabHelper.CreatePrefabItem("ba:itemname_paperbag").gameObject;
			tpc.SetHandContent(gameObject.transform);
		}
	}

	private Transform FindTableFoodDisplay(string itemName)
	{
		string idWithoutType = itemName.GetIdWithoutType();
		Transform foodDisplay = isSittingOn.seatAttachmentPoint.foodDisplay;
		for (int i = 0; i < foodDisplay.childCount; i++)
		{
			Transform child = foodDisplay.GetChild(i);
			if (child.name.Equals(idWithoutType, StringComparison.OrdinalIgnoreCase))
			{
				return child;
			}
		}
		return foodDisplay.Find(itemName);
	}

	public static bool GrabItem(ItemController producerController, Customer customer, OrderEntry orderEntry)
	{
		if (producerController.GetProducedItemName() == "ba:itemname_undefined")
		{
			return false;
		}
		bool flag = true;
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			if (producerController.ItemInstance.SubtractFromStock())
			{
				if (orderEntry.priceAccceptable && orderEntry.available)
				{
					if (InstanceBehavior<BuildingManager>.Instance.businessType.CustomersNeedShoppingContainer)
					{
						UpdateContainer(customer);
					}
				}
				else
				{
					flag = false;
				}
			}
			else
			{
				orderEntry.available = false;
				flag = false;
			}
		}
		else if (InstanceBehavior<BuildingManager>.Instance.businessType.CustomersNeedShoppingContainer)
		{
			UpdateContainer(customer);
		}
		if (flag && !InstanceBehavior<BuildingManager>.Instance.businessType.CustomersNeedShoppingContainer && customer.tpc.handContent.childCount <= 0)
		{
			ItemController itemController = PrefabHelper.CreatePrefabItem(customer.GetContainerItemName());
			customer.tpc.SetHandContent(itemController.transform);
		}
		return flag;
	}

	private static void UpdateContainer(Customer customer)
	{
		customer.tpc.UpdateHandContentVisuals(customer.order.entries.Count((OrderEntry x) => x.available && x.priceAccceptable));
	}

	public void ReleaseCustomer()
	{
		ReleaseGameObject();
		IndoorCustomerSpawner.Customers.Remove(this);
	}

	protected virtual string GetContainerItemName()
	{
		return "ba:itemname_closedcardboardbox";
	}

	protected virtual void ReleaseGameObject()
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public void OnCustomerFloorVisibilityChanged(bool visible)
	{
		_floorVisible = visible;
		bool visible2 = _floorVisible;
		if (_floorVisible && !_cameraVisible)
		{
			visible2 = false;
		}
		OnCustomerVisibleChanged(visible2);
	}

	public void OnCustomerCameraVisibilityChanged(bool visible)
	{
		_cameraVisible = visible;
		bool visible2 = _cameraVisible;
		if (!_floorVisible)
		{
			visible2 = false;
		}
		OnCustomerVisibleChanged(visible2);
	}

	public virtual void OnCustomerVisibleChanged(bool visible)
	{
		tpc.OnVisibleChanged(visible);
	}

	public virtual void SetCurrentTimeState()
	{
		if (customerEntry.spawnTime.GetTotalMinutes() + 45f <= TimeHelper.NowInMinutes())
		{
			customerTimeState = CustomerTimeState.AlmostLeaving;
		}
		else if (customerEntry.spawnTime.GetTotalMinutes() + 15f <= TimeHelper.NowInMinutes())
		{
			customerTimeState = CustomerTimeState.AlreadyInAction;
		}
		else if (customerEntry.spawnTime.GetTotalMinutes() + 5f <= TimeHelper.NowInMinutes())
		{
			customerTimeState = CustomerTimeState.RecentlyArrived;
		}
		else
		{
			customerTimeState = CustomerTimeState.JustArrived;
		}
	}
}
