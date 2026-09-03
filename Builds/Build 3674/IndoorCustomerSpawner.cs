using System;
using System.Collections.Generic;
using AI.Citizens;
using AI.Customers.CustomerEntries;
using BigAmbitions.PlacementSystem;
using BigAmbitions.Tags;
using Buildings;
using Buildings.Retail.Businesses.CinemaTheater;
using Entities;
using Extensions;
using Helpers;
using IngameDebugConsole;
using JimmysUnityUtilities;
using UnityEngine;
using UnityEngine.AI;

public class IndoorCustomerSpawner : MonoBehaviour
{
	public static readonly List<Customer> Customers = new List<Customer>();

	public float cullInterval = 0.1f;

	public float cullBorderThreshold = 0.1f;

	private float _cullTimer;

	private float _nextSpawnCheck;

	private bool spawnOnlyOnEnter;

	private static bool SpawningIsDisabled;

	private bool _firstSpawn = true;

	private void Awake()
	{
		ResetCustomerSpawner();
		if (!(InstanceBehavior<GameManager>.Instance == null) && !InstanceBehavior<GameManager>.Instance.IsUIDevScene)
		{
			SubscribeToGlobalEvents();
		}
	}

	private void ResetCustomerSpawner()
	{
		base.enabled = false;
		Customers.Clear();
	}

	private void SubscribeToGlobalEvents()
	{
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, (Action<Address>)delegate
		{
			OnEnterBuilding();
		});
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		GlobalEvents.onGameUnloaded = (Action)Delegate.Combine(GlobalEvents.onGameUnloaded, new Action(ResetCustomerSpawner));
		PlacementSystem.onPlacementModeEnd = (Action)Delegate.Combine(PlacementSystem.onPlacementModeEnd, new Action(CinemaTheaterHelper.PopulateLists));
		GlobalEvents.onTimeMachineStarted = (Action)Delegate.Combine(GlobalEvents.onTimeMachineStarted, (Action)delegate
		{
			if (BuildingManager.IsInsideBuilding)
			{
				ResetAiCustomerEntries(InstanceBehavior<BuildingManager>.Instance.buildingRegistration);
			}
		});
		GlobalEvents.onTimeMachineEnded = (Action)Delegate.Combine(GlobalEvents.onTimeMachineEnded, (Action)delegate
		{
			if (BuildingManager.IsInsideBuilding)
			{
				_firstSpawn = true;
				_nextSpawnCheck = 0f;
			}
		});
	}

	private void OnEnterBuilding()
	{
		CinemaTheaterHelper.PopulateLists();
		TicketEntryBlocker.UpdateBlockers();
		BusinessType businessType = InstanceBehavior<BuildingManager>.Instance.businessType;
		if ((object)businessType != null && businessType.spawnCustomers)
		{
			_firstSpawn = true;
			spawnOnlyOnEnter = InstanceBehavior<BuildingManager>.Instance.businessType.spawnCustomersOnlyOnEnter;
			CoroutineUtility.RunAfterFrameDelay(delegate
			{
				base.enabled = true;
			}, 2);
			if (spawnOnlyOnEnter)
			{
				CoroutineUtility.RunAfterFrameDelay(TrySpawnCustomer, 2);
			}
		}
	}

	private void OnExitBuilding(Address address)
	{
		if (base.enabled)
		{
			CleanCustomers();
			CinemaTheaterHelper.ClearLists();
			SaveGameManager.Current.hasCinemaTheaterTicket = false;
			TicketEntryBlocker.UpdateBlockers();
			base.enabled = false;
			_nextSpawnCheck = 0f;
			ResetAiCustomerEntries(BuildingHelper.GetBuildingRegistration(address));
		}
	}

	private static void ResetAiCustomerEntries(BuildingRegistration buildingRegistration)
	{
		if (buildingRegistration == null || buildingRegistration.RentedByPlayer || !BusinessTypeHelper.GetData(buildingRegistration).spawnCustomers)
		{
			return;
		}
		foreach (CustomerEntry item in CustomerEntriesHelper.GetEntriesByAddress(buildingRegistration.Address))
		{
			item.completed = false;
			item.order.ResetEntries();
		}
	}

	private void CleanCustomers()
	{
		for (int num = Customers.Count - 1; num >= 0; num--)
		{
			Customer customer = Customers[num];
			if (customer != null && customer.gameObject.activeSelf)
			{
				customer.ReleaseCustomer();
			}
		}
	}

	private void Update()
	{
		_cullTimer += Time.unscaledDeltaTime;
		if (_cullTimer >= cullInterval)
		{
			_cullTimer = 0f;
			for (int i = 0; i < Customers.Count; i++)
			{
				Customer customer = Customers[i];
				bool visible = IsVisibleOnCamera(customer);
				customer.OnCustomerCameraVisibilityChanged(visible);
			}
		}
		if (!spawnOnlyOnEnter && !SpawningIsDisabled)
		{
			_nextSpawnCheck -= Time.deltaTime;
			if (!(_nextSpawnCheck > 0f))
			{
				TrySpawnCustomer();
				_nextSpawnCheck = 1f;
			}
		}
	}

	private bool IsVisibleOnCamera(Customer customer)
	{
		Vector3 position = customer.transform.position + Vector3.up;
		Vector3 vector = GameManager.GetMainCamera().WorldToViewportPoint(position);
		if (vector.z < 0f)
		{
			return false;
		}
		if (vector.x < 0f - cullBorderThreshold || vector.x > 1f + cullBorderThreshold)
		{
			return false;
		}
		if (vector.y < 0f - cullBorderThreshold || vector.y > 1f + cullBorderThreshold)
		{
			return false;
		}
		return true;
	}

	private void TrySpawnCustomer()
	{
		if (!InstanceBehavior<BuildingManager>.Instance.isOpen)
		{
			return;
		}
		float num = TimeHelper.NowInMinutes();
		bool rentedByPlayer = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.RentedByPlayer;
		float num2 = num - InstanceBehavior<BuildingManager>.Instance.businessType.minutesToCheckBeforeTheSpawnTimeInAiBusinesses;
		foreach (CustomerEntry item in CustomerEntriesHelper.GetEntriesByAddress(InstanceBehavior<BuildingManager>.Instance.building.Address))
		{
			float totalMinutes = item.spawnTime.GetTotalMinutes();
			if (!item.completed && !(totalMinutes >= num) && (rentedByPlayer || !(totalMinutes <= num2)))
			{
				if (Customers.Count >= InstanceBehavior<BuildingManager>.Instance.buildingRegistration.customerCapacity)
				{
					return;
				}
				if (!_firstSpawn)
				{
					item.spawnTime = TimeHelper.Now();
					item.order.timestamp = TimeHelper.Now();
				}
				if (CanSpawnCustomer())
				{
					SpawnCustomer(item);
				}
				item.completed = true;
			}
		}
		_firstSpawn = false;
	}

	public static bool GetRandomPositionInside(out Vector3 randomPosition)
	{
		randomPosition = Vector3.zero;
		for (int i = 0; i < 30; i++)
		{
			Bounds interiorBounds = InstanceBehavior<BuildingManager>.Instance.interiorBounds;
			randomPosition = interiorBounds.center + new Vector3((UnityEngine.Random.value - 0.5f) * interiorBounds.size.x, (UnityEngine.Random.value - 0.5f) * interiorBounds.size.y, (UnityEngine.Random.value - 0.5f) * interiorBounds.size.z);
			randomPosition = interiorBounds.ClosestPoint(randomPosition);
			if (NavMesh.SamplePosition(randomPosition, out var hit, 1f, -1) && CanReachAnyExit(hit.position))
			{
				randomPosition = hit.position;
				return true;
			}
		}
		return false;
	}

	private static bool CanReachAnyExit(Vector3 position)
	{
		foreach (ExitZone exitZone in InstanceBehavior<BuildingManager>.Instance.exitZones)
		{
			if (GenericExtensions.PathCanBeCompleted(position, exitZone.transform.position))
			{
				return true;
			}
		}
		return false;
	}

	private bool CanSpawnCustomer()
	{
		if (InstanceBehavior<BuildingManager>.Instance.businessType.customerType == CustomerType.Nightclub)
		{
			return NightclubBusinessHelper.CanSpawnCustomers();
		}
		return true;
	}

	private static void SpawnCustomer(CustomerEntry customerEntry)
	{
		CitizenData citizenData = CitizenHelper.CreateRandomCitizenDataFromNeighborhood(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Neighborhood);
		if (TryToProcessEntranceFeeEntryToOrder(citizenData, customerEntry.order))
		{
			Customer customer = SpawnCustomer(InstanceBehavior<BuildingManager>.Instance.businessType.customerType);
			NavMeshAgent componentInChildren = customer.GetComponentInChildren<NavMeshAgent>();
			ExitZone random = InstanceBehavior<BuildingManager>.Instance.exitZones.GetRandom();
			Vector3 position = random.transform.position;
			Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.5f;
			position.x += vector.x;
			position.z += vector.y;
			componentInChildren.Warp(position);
			customer.transform.rotation = random.transform.rotation;
			customer.citizenData = citizenData;
			customer.tpc.appearanceSetter.data.ageInDays = customer.citizenData.Age;
			customer.order = customerEntry.order;
			customer.customerEntry = customerEntry;
			customer.gameObject.SetActive(value: true);
			customer.Init();
		}
	}

	private static Customer SpawnCustomer(CustomerType customerType)
	{
		Customer customer = InstanceBehavior<BuildingManager>.Instance.customerSpawner.GetCustomer(customerType);
		Customers.Add(customer);
		return customer;
	}

	private static bool TryToProcessEntranceFeeEntryToOrder(CitizenData citizenData, Order order)
	{
		if (!InstanceBehavior<BuildingManager>.Instance.businessType.hasEntranceFee || !InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			return true;
		}
		string entranceFeeNameForBusinessType = BusinessTypeHelper.GetEntranceFeeNameForBusinessType(InstanceBehavior<BuildingManager>.Instance.businessType);
		if (string.IsNullOrEmpty(entranceFeeNameForBusinessType))
		{
			return true;
		}
		float price = ItemHelper.GetPrice(entranceFeeNameForBusinessType, InstanceBehavior<BuildingManager>.Instance.buildingRegistration);
		bool flag = citizenData.IsPriceAcceptable(entranceFeeNameForBusinessType, price);
		OrderEntry item = new OrderEntry
		{
			itemName = entranceFeeNameForBusinessType,
			available = true,
			price = price,
			paid = flag,
			priceAccceptable = flag
		};
		order.entries.Add(item);
		return flag;
	}

	[ConsoleMethod("DisableCustomersSpawn", "Disables customers spawning", new string[] { })]
	public static void DisableCustomersSpawn()
	{
		SpawningIsDisabled = true;
		UnityEngine.Object.FindObjectOfType<IndoorCustomerSpawner>().CleanCustomers();
	}

	[ConsoleMethod("EnableCustomersSpawn", "Enables customers spawning if previously disabled", new string[] { })]
	public static void EnableCustomersSpawn()
	{
		SpawningIsDisabled = false;
	}

	[ConsoleMethod("SpawnCustomers", "Instantly spawn customers in the current business", new string[] { })]
	public static void SpawnCustomers(int amount)
	{
		if (BuildingManager.IsInsideBuilding && (BuildingTypeHelper.GetData(InstanceBehavior<BuildingManager>.Instance.building).HasTag(TagRef.Buildingtypetag.canspawncustomersindoors) || !(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.businessTypeName != "ba:businesstype_casino")))
		{
			for (int i = 0; i < amount; i++)
			{
				CustomerEntry obj = new CustomerEntry
				{
					spawnTime = TimeHelper.Now()
				};
				OrderEntry item = new OrderEntry
				{
					itemName = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.cachedAvailableProducts.GetRandom()
				};
				obj.order.entries.Add(item);
				SpawnCustomer(obj);
			}
		}
	}
}
