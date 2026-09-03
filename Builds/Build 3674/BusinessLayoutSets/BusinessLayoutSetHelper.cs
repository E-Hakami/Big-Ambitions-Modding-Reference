using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BigAmbitions.InteriorDesigner.InteriorElements;
using BigAmbitions.Items;
using BigAmbitions.SaveSystem;
using BigAmbitions.Tags;
using Blueprints;
using Buildings;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Buildings.Indoors.InteriorDesign;
using Controllers;
using Entities;
using Extensions;
using Helpers;
using IngameDebugConsole;
using Newtonsoft.Json;
using Streets;
using UI.Tasks;
using UnityEngine;

namespace BusinessLayoutSets;

public static class BusinessLayoutSetHelper
{
	public const string BusinessLayoutsFolderName = "BusinessLayouts";

	public static bool loadingLayouts;

	private static readonly Dictionary<string, BusinessLayoutSet> BusinessLayoutSets = new Dictionary<string, BusinessLayoutSet>();

	private static bool DoneInitSynchronous;

	public static IEnumerable<ItemInstance> GetItemInstancesFromLayoutItems(List<Item> items)
	{
		List<ItemInstance> list = new List<ItemInstance>();
		foreach (Item item in items)
		{
			ItemInstance itemInstance = ItemHelper.InitializeNewInstance(item.itemName);
			itemInstance.position = item.position;
			itemInstance.yRotation = ((Quaternion)item.rotation).eulerAngles.y;
			itemInstance.cargoInstances.Add(new CargoInstance(item.playerItemPurchaserSettings.itemName, 0, 0f));
			itemInstance.dirtSpotsThatAffects = item.dirtSpotsThatAffects;
			itemInstance.customPositions = item.customPositions;
			itemInstance.customColors = item.customColors;
			itemInstance.worldSpaceTextValue = item.worldSpaceTextValue;
			list.Add(itemInstance);
		}
		return list;
	}

	private static string GetLayoutPath(string layout, string businessTypeName, BuildingSizeInfo sizeInfo)
	{
		return Path.Combine(GetBusinessLayoutSetsFolderPath(businessTypeName, sizeInfo), layout.ToLowerInvariant() + ".json");
	}

	public static Dictionary<string, BusinessLayoutSet> GetAllBusinessLayoutSets()
	{
		if (!loadingLayouts && (DoneInitSynchronous || BusinessLayoutSets.Count != 0))
		{
			return BusinessLayoutSets;
		}
		Debug.Log("BusinessLayoutSets haven't been loaded yet, loading them now");
		InitSynchronous();
		DoneInitSynchronous = true;
		return BusinessLayoutSets;
	}

	public static BusinessLayoutSet GetOrLoadBusinessLayoutSet(string businessTypeName, BuildingSizeInfo sizeInfo, string layout, bool warnIfNotFound = true)
	{
		string key = CreateLayoutSetKey(businessTypeName, sizeInfo, layout);
		if (BusinessLayoutSets.TryGetValue(key, out var value))
		{
			return value;
		}
		string layoutPath = GetLayoutPath(layout, businessTypeName, sizeInfo);
		if (!File.Exists(layoutPath))
		{
			if (warnIfNotFound)
			{
				Debug.LogError("Couldn't find layout set with path " + layoutPath);
			}
			return null;
		}
		BusinessLayoutSet businessLayoutSet = Deserialize(layoutPath);
		if (!IsValidLayoutSet(businessLayoutSet))
		{
			if (warnIfNotFound)
			{
				Debug.LogError("Skipping unsupported business layout set at '" + layoutPath + "' (businessType='" + businessLayoutSet?.BusinessType + "')");
			}
			return null;
		}
		businessLayoutSet.LayoutName = layout;
		BusinessLayoutSets.Add(key, businessLayoutSet);
		return businessLayoutSet;
	}

	public static Task Init()
	{
		return Init(force: false);
	}

	private static async Task Init(bool force)
	{
		if (!force && DoneInitSynchronous && BusinessLayoutSets.Count != 0)
		{
			return;
		}
		loadingLayouts = true;
		BusinessLayoutSets.Clear();
		foreach (string businessTypeName in BusinessTypeHelper.BusinessTypeNames)
		{
			string path = Path.Combine(Application.streamingAssetsPath, "BusinessLayouts", businessTypeName.GetIdWithoutType());
			if (!Directory.Exists(path))
			{
				continue;
			}
			string[] directories = Directory.GetDirectories(path);
			foreach (string path2 in directories)
			{
				string[] files = Directory.GetFiles(path2, "*.json", SearchOption.TopDirectoryOnly);
				for (int j = 0; j < files.Length; j++)
				{
					await SetBusinessLayout(files[j]);
				}
			}
		}
		loadingLayouts = false;
	}

	private static void InitSynchronous()
	{
		foreach (string businessTypeName in BusinessTypeHelper.BusinessTypeNames)
		{
			string path = Path.Combine(Application.streamingAssetsPath, "BusinessLayouts", businessTypeName.GetIdWithoutType());
			if (!Directory.Exists(path))
			{
				continue;
			}
			string[] directories = Directory.GetDirectories(path);
			for (int i = 0; i < directories.Length; i++)
			{
				string[] files = Directory.GetFiles(directories[i], "*.json", SearchOption.TopDirectoryOnly);
				for (int j = 0; j < files.Length; j++)
				{
					SetBusinessLayoutSynchronous(files[j]);
				}
			}
		}
	}

	[ConsoleMethod("ReloadBusinessLayoutSets", "Reloads BusinessLayoutSets", new string[] { })]
	public static void ReloadBusinessLayoutSets()
	{
		Task.Run((Func<Task>)ReloadLayouts);
	}

	private static async Task ReloadLayouts()
	{
		Debug.Log("Reloading business layouts, please wait...");
		await Init(force: true);
		Debug.Log("Business layouts reloaded!");
	}

	private static async Task SetBusinessLayout(string businessLayoutSetPath)
	{
		TryStoreBusinessLayout(businessLayoutSetPath, await DeserializeAsync(businessLayoutSetPath));
	}

	private static void SetBusinessLayoutSynchronous(string businessLayoutSetPath)
	{
		BusinessLayoutSet businessLayoutSet = Deserialize(businessLayoutSetPath);
		TryStoreBusinessLayout(businessLayoutSetPath, businessLayoutSet);
	}

	private static void TryStoreBusinessLayout(string businessLayoutSetPath, BusinessLayoutSet businessLayoutSet)
	{
		if (!IsValidLayoutSet(businessLayoutSet))
		{
			Debug.LogError("Skipping unsupported business layout set at '" + businessLayoutSetPath + "' (businessType='" + businessLayoutSet?.BusinessType + "')");
		}
		else
		{
			businessLayoutSet.LayoutName = Path.GetFileNameWithoutExtension(businessLayoutSetPath);
			string key = CreateLayoutSetKey(businessLayoutSet.BusinessType, new BuildingSizeInfo(businessLayoutSet), businessLayoutSet.LayoutName);
			BusinessLayoutSets.TryAdd(key, businessLayoutSet);
		}
	}

	private static bool IsValidLayoutSet(BusinessLayoutSet businessLayoutSet)
	{
		if (businessLayoutSet == null || string.IsNullOrEmpty(businessLayoutSet.BusinessType))
		{
			return false;
		}
		return BusinessTypeHelper.GetData(businessLayoutSet.BusinessType) != null;
	}

	public static bool ContainsBusinessLayoutSet(string businessTypeName, BuildingSizeInfo sizeInfo, string layout)
	{
		string key = CreateLayoutSetKey(businessTypeName, sizeInfo, layout);
		return BusinessLayoutSets.ContainsKey(key);
	}

	public static async Task Serialize(string path, BusinessLayoutSet layoutSet)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
		layoutSet.buildNumber = GameVersion.GetCurrent().buildNumber;
		JsonSerializerSettings settings = new JsonSerializerSettings
		{
			Formatting = Formatting.Indented,
			Converters = new List<JsonConverter>
			{
				new TypeJsonConverter<Item>()
			}
		};
		string contents = JsonConvert.SerializeObject(layoutSet, settings);
		await File.WriteAllTextAsync(path, contents);
	}

	public static BusinessLayoutSet Deserialize(string path)
	{
		if (!File.Exists(path))
		{
			Debug.LogError("Couldn't find layout set with path " + path);
			return null;
		}
		JsonSerializerSettings settings = new JsonSerializerSettings
		{
			Converters = new List<JsonConverter>
			{
				new TypeJsonConverter<Item>()
			}
		};
		try
		{
			return JsonConvert.DeserializeObject<BusinessLayoutSet>(File.ReadAllText(path), settings);
		}
		catch (Exception ex)
		{
			Debug.LogError("Failed to deserialize business layout set at '" + path + "': " + ex.Message);
			return null;
		}
	}

	public static async Task<BusinessLayoutSet> DeserializeAsync(string path)
	{
		if (!File.Exists(path))
		{
			return null;
		}
		JsonSerializerSettings settings = new JsonSerializerSettings
		{
			Converters = new List<JsonConverter>
			{
				new TypeJsonConverter<Item>()
			}
		};
		try
		{
			return JsonConvert.DeserializeObject<BusinessLayoutSet>(await File.ReadAllTextAsync(path), settings);
		}
		catch (Exception ex)
		{
			Debug.LogError("Failed to deserialize business layout set at '" + path + "': " + ex.Message);
			return null;
		}
	}

	public static BusinessLayoutSet Collect(bool collectDirtSpots = true)
	{
		BusinessLayoutSet businessLayoutSet = new BusinessLayoutSet
		{
			BusinessType = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.businessTypeName,
			BuildingSize = InstanceBehavior<BuildingManager>.Instance.building.BuildingSize,
			BuildingVersion = InstanceBehavior<BuildingManager>.Instance.building.BuildingVersion
		};
		IOrderedEnumerable<ItemController> orderedEnumerable = from x in InstanceBehavior<BuildingManager>.Instance.IndoorItemContainer.GetComponentsInChildren<ItemController>().Where(delegate(ItemController x)
			{
				BigAmbitions.Items.Item byName = ItemsGetter.GetByName(x.itemName);
				return (object)byName != null && !byName.HasTag(TagRef.Itemtag.ignoredbylayoutset);
			})
			where x.parentItemController == null
			orderby x.ItemInstance?.id
			select x;
		bool flag = false;
		if (collectDirtSpots && (InstanceBehavior<BuildingManager>.Instance.buildingRegistration.dirtSpots == null || InstanceBehavior<BuildingManager>.Instance.buildingRegistration.dirtSpots.Count == 0))
		{
			InstanceBehavior<BuildingManager>.Instance.buildingRegistration.dirtSpots = BuildingCleanlinessHelper.GetDirtSpotsForBuilding(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.BuildingCached);
			flag = true;
		}
		foreach (ItemController item in orderedEnumerable)
		{
			if (item.Item.HasTag(TagRef.Itemtag.ignoredbylayoutset) || !item.gameObject.activeInHierarchy)
			{
				continue;
			}
			List<Item> list = CollectItem(item);
			if (list.Count == 0)
			{
				continue;
			}
			foreach (Item item2 in list)
			{
				bool flag2 = false;
				foreach (Item item3 in businessLayoutSet.Items)
				{
					if (!(item3.id != item2.id))
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					businessLayoutSet.Items.Add(item2);
				}
			}
		}
		if (businessLayoutSet.BuildingSize == "ba:buildingsize_t" && !InteriorDesignerHelper.BlueprintCreatorMode)
		{
			AdjustPositionsFromHamptonsCbcToHamptonsBuilding(businessLayoutSet);
		}
		if (flag)
		{
			InstanceBehavior<BuildingManager>.Instance.buildingRegistration.dirtSpots = new List<DirtSpot>();
		}
		foreach (KeyValuePair<string, InteriorElement> item4 in new SortedDictionary<string, InteriorElement>(InteriorElementsHelper.InteriorElementsCache))
		{
			businessLayoutSet.interiorDesigns.Add(item4.Value.Serialize());
		}
		return businessLayoutSet;
	}

	private static void AdjustPositionsFromHamptonsCbcToHamptonsBuilding(BusinessLayoutSet layoutSet)
	{
		Transform buildingTransform = InstanceBehavior<BuildingManager>.Instance.GetBuildingTransform(new BuildingSizeInfo(layoutSet));
		Transform transform = InstanceBehavior<CityManager>.Instance.FindCityBuildingController(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Address).transform;
		foreach (Item item in layoutSet.Items)
		{
			CopyRelativePositionAndRotation(transform, buildingTransform, ref item.position, ref item.rotation);
		}
	}

	public static List<Item> CollectItem(ItemController itemController)
	{
		List<Item> list = new List<Item>();
		if (itemController.ItemInstance == null)
		{
			return list;
		}
		Item layoutSetItem = GetLayoutSetItem(itemController);
		for (int i = 0; i < itemController.AttachmentPoints.Length; i++)
		{
			foreach (ItemController item in from x in itemController.AttachmentPoints[i].GetComponentsInChildren<ItemController>()
				orderby x.ItemInstance?.id
				select x)
			{
				foreach (Item item2 in CollectItem(item))
				{
					list.Add(item2);
					layoutSetItem.stackedItems.Add(new AttachableChild
					{
						childId = item2.id,
						childItemName = item2.itemName,
						attachmentIndex = i
					});
				}
			}
		}
		list.Add(layoutSetItem);
		return list;
	}

	public static BusinessLayoutSet CollectFromRegistration(BuildingRegistration registration, BuildingSizeInfo buildingSizeInfo, HashSet<string> ignoreItems, bool includeDirtSpots = true)
	{
		if (registration == null)
		{
			return null;
		}
		bool num = BuildingSizeHelper.GetBuildingTypeBySizeInfo(buildingSizeInfo) == "ba:buildingtype_residential";
		string text = registration.Address.ToFormattedString();
		string businessType;
		if (num)
		{
			businessType = "ba:businesstype_empty";
		}
		else
		{
			businessType = ((registration.businessTypeName == "ba:businesstype_empty") ? "ba:businesstype_bookstore" : registration.businessTypeName);
			if (registration.businessTypeName != "ba:businesstype_empty")
			{
				text = text + " " + registration.BusinessName;
			}
		}
		BusinessLayoutSet layoutSet = new BusinessLayoutSet
		{
			BusinessType = businessType,
			BuildingSize = buildingSizeInfo.buildingSize,
			BuildingVersion = buildingSizeInfo.buildingVersion,
			LayoutName = text
		};
		if (includeDirtSpots && (registration.dirtSpots == null || registration.dirtSpots.Count == 0))
		{
			registration.dirtSpots = BuildingCleanlinessHelper.GetDirtSpotsForBuilding(registration.BuildingCached);
		}
		IEnumerable<ItemInstance> enumerable = registration.itemInstances?.Values;
		List<ItemInstance> list = (enumerable ?? Enumerable.Empty<ItemInstance>()).Where((ItemInstance x) => x != null && !ignoreItems.Contains(x.itemName)).ToList();
		Dictionary<string, ItemInstance> byId = list.ToDictionary((ItemInstance x) => x.id, (ItemInstance x) => x);
		HashSet<string> added = new HashSet<string>();
		foreach (ItemInstance item2 in list.Where((ItemInstance x) => string.IsNullOrEmpty(x.parentId)))
		{
			AddWithChildren(item2);
		}
		foreach (ItemInstance item3 in list)
		{
			AddWithChildren(item3);
		}
		List<SerializedInteriorDesign> interiorDesigns = registration.interiorDesigns;
		if (interiorDesigns != null && interiorDesigns.Count > 0)
		{
			layoutSet.interiorDesigns.AddRange(registration.interiorDesigns);
		}
		else
		{
			foreach (KeyValuePair<string, InteriorElement> item4 in new SortedDictionary<string, InteriorElement>(InteriorElementsHelper.InteriorElementsCache))
			{
				layoutSet.interiorDesigns.Add(item4.Value.Serialize());
			}
		}
		return layoutSet;
		void AddWithChildren(ItemInstance ii)
		{
			if (ii != null && !added.Contains(ii.id) && !ShouldSkip(ii))
			{
				if (ii.stackedItems != null)
				{
					foreach (AttachableChild stackedItem in ii.stackedItems)
					{
						if (byId.TryGetValue(stackedItem.childId, out var value))
						{
							AddWithChildren(value);
						}
					}
				}
				Item item = Convert(ii);
				if (item != null)
				{
					item.parentId = ii.parentId;
					if (layoutSet.Items.All((Item x) => x.id != item.id))
					{
						layoutSet.Items.Add(item);
					}
					added.Add(ii.id);
				}
			}
		}
		static Item Convert(ItemInstance itemInstance)
		{
			if (itemInstance == null)
			{
				return null;
			}
			Item item = new Item
			{
				id = itemInstance.id,
				itemName = itemInstance.itemName,
				rotation = itemInstance.Rotation,
				position = itemInstance.position,
				dirtSpotsThatAffects = (itemInstance.dirtSpotsThatAffects ?? new List<int>()),
				customPositions = itemInstance.customPositions,
				customColors = itemInstance.customColors,
				worldSpaceTextValue = itemInstance.worldSpaceTextValue
			};
			ItemType type = itemInstance.ItemCached.type;
			bool flag = (type & ItemType.ShowcaseShelf) != 0;
			bool flag2 = (type & ItemType.PointOfSale) != 0;
			if (flag | flag2)
			{
				CargoInstance stockInstance = itemInstance.GetStockInstance();
				item.playerItemPurchaserSettings = new PlayerItemPurchaserSettings
				{
					itemName = stockInstance.itemName,
					itemQuantity = 1,
					enabled = (flag && !string.IsNullOrEmpty(stockInstance.itemName))
				};
			}
			if (itemInstance.stackedItems == null)
			{
				return item;
			}
			foreach (AttachableChild stackedItem2 in itemInstance.stackedItems)
			{
				item.stackedItems.Add(new AttachableChild
				{
					childId = stackedItem2.childId,
					childItemName = stackedItem2.childItemName,
					attachmentIndex = stackedItem2.attachmentIndex
				});
			}
			return item;
		}
		static bool ShouldSkip(ItemInstance ii)
		{
			if (ii != null && !ii.ItemCached.isSpecialGift)
			{
				return ii.ItemCached.HasTag(TagRef.Itemtag.ignoredbylayoutset);
			}
			return true;
		}
	}

	[ConsoleMethod("SaveBusinessLayoutSet", "Saves the current business layout using the current layout name", new string[] { })]
	public static void SaveBusinessLayoutSet()
	{
		SaveBusinessLayoutSet(null);
	}

	[ConsoleMethod("SaveBusinessLayoutSet", "Saves the current business layout in the given layout name", new string[] { })]
	public static void SaveBusinessLayoutSet(string name)
	{
		SaveBusinessLayoutSet(name, InstanceBehavior<BuildingManager>.Instance.buildingRegistration.businessTypeName);
	}

	[ConsoleMethod("SaveBusinessLayoutSet", "Saves the current business layout in the given layout name and business type", new string[] { })]
	public static async void SaveBusinessLayoutSet(string name, string businessTypeName)
	{
		if (BuildingManager.IsInsideBuilding)
		{
			if (string.IsNullOrEmpty(name))
			{
				name = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Layout;
			}
			if (string.IsNullOrEmpty(name))
			{
				Debug.LogWarning("Can't save layout due to no layout name found");
				return;
			}
			BusinessLayoutSet businessLayoutSet = Collect();
			businessLayoutSet.BusinessType = businessTypeName;
			businessLayoutSet.LayoutName = name;
			string businessLayoutSetsFolderPath = GetBusinessLayoutSetsFolderPath(businessTypeName, new BuildingSizeInfo(InstanceBehavior<BuildingManager>.Instance.building));
			Directory.CreateDirectory(businessLayoutSetsFolderPath);
			await businessLayoutSet.Serialize(Path.Combine(businessLayoutSetsFolderPath, name + ".json"));
			Debug.Log("Layout " + name + " saved to business type " + businessTypeName);
		}
	}

	public static string GetBusinessLayoutSetsFolderPath(string businessTypeName, BuildingSizeInfo sizeInfo)
	{
		return Path.Combine(Application.streamingAssetsPath, "BusinessLayouts", businessTypeName.GetIdWithoutType(), sizeInfo.ToString());
	}

	[ConsoleMethod("LoadBusinessLayoutSet", "Loads the layout into the building. Needs a business type selected", new string[] { })]
	public static void LoadBusinessLayoutSet(string layoutName)
	{
		InstanceBehavior<BuildingManager>.Instance.IndoorItemContainer.ClearChildren();
		foreach (ItemInstance item in InstanceBehavior<BuildingManager>.Instance.buildingRegistration.itemInstances.Values.ToList())
		{
			InstanceBehavior<BuildingManager>.Instance.buildingRegistration.RemoveItemInstanceFromBuilding(item);
		}
		if (!InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			BuildingSizeInfo buildingSizeInfo = new BuildingSizeInfo(InstanceBehavior<BuildingManager>.Instance.building);
			BusinessLayoutSet orLoadBusinessLayoutSet = GetOrLoadBusinessLayoutSet(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.businessTypeName, buildingSizeInfo, layoutName, warnIfNotFound: false);
			if (orLoadBusinessLayoutSet == null)
			{
				Debug.LogError("Layout " + layoutName + " not found for " + InstanceBehavior<BuildingManager>.Instance.buildingRegistration.businessTypeName + " (" + buildingSizeInfo.ToString() + ")");
			}
			InstanceBehavior<BuildingManager>.Instance.LoadBusinessLayoutSet(orLoadBusinessLayoutSet);
		}
		else
		{
			InsertBusinessLayoutSet(InstanceBehavior<BuildingManager>.Instance.building.Address, InstanceBehavior<BuildingManager>.Instance.buildingRegistration.businessTypeName, new BuildingSizeInfo(InstanceBehavior<BuildingManager>.Instance.building), layoutName);
			InstanceBehavior<BuildingManager>.Instance.buildingRegistration.GenerateInteriorDesignerLookup();
			if (InstanceBehavior<BuildingManager>.Instance.LoadBuilding())
			{
				InstanceBehavior<BuildingManager>.Instance.LoadItems();
			}
		}
	}

	public static void InsertBusinessLayoutSet(Address address, string businessTypeName, BuildingSizeInfo sizeInfo, string layout, bool shouldRandomlyFillShelves = false)
	{
		string key = CreateLayoutSetKey(businessTypeName, sizeInfo, layout);
		if (!BusinessLayoutSets.TryGetValue(key, out var value))
		{
			Debug.LogError("Can't find a layout set for this business type and building with name " + layout);
		}
		else if (value == null)
		{
			Debug.LogError("Can't find a layout set for this business type and building with name " + layout);
		}
		else if (value.BusinessType != businessTypeName || value.BuildingSize != sizeInfo.buildingSize || value.BuildingVersion != sizeInfo.buildingVersion)
		{
			Debug.LogError("BusinessLayoutSet does not match building registration.");
		}
		else
		{
			InsertLayoutSet(address, value, shouldRandomlyFillShelves);
		}
	}

	public static void InsertLayoutSet(Address address, BusinessLayoutSet layoutSet, bool shouldRandomlyFillShelves = false)
	{
		InsertLayoutSet(BuildingHelper.GetBuildingRegistration(address), layoutSet, shouldRandomlyFillShelves);
	}

	public static void InsertLayoutSet(BuildingRegistration buildingRegistration, BusinessLayoutSet layoutSet, bool shouldRandomlyFillShelves = false, bool isBlueprintCreator = false)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		List<ItemInstance> list = new List<ItemInstance>();
		List<DirtSpot> buildingDirtSpots = ((!isBlueprintCreator) ? BuildingCleanlinessHelper.GetDirtSpotsForBuilding(buildingRegistration.BuildingCached) : null);
		bool flag = buildingRegistration.RentedByPlayer | isBlueprintCreator;
		Transform transform = null;
		Transform transform2 = null;
		if (!isBlueprintCreator && buildingRegistration.BuildingCached.IsHamptonsHouse())
		{
			transform2 = InstanceBehavior<CityManager>.Instance.FindCityBuildingController(buildingRegistration.Address).transform;
			transform = InstanceBehavior<BuildingManager>.Instance.GetBuildingTransform(new BuildingSizeInfo(buildingRegistration.BuildingCached));
		}
		foreach (Item item in layoutSet.Items)
		{
			if (string.IsNullOrEmpty(item.itemName))
			{
				continue;
			}
			BigAmbitions.Items.Item byName = ItemsGetter.GetByName(item.itemName);
			if (byName.HasTag(TagRef.Itemtag.ignoredbylayoutset))
			{
				continue;
			}
			if (flag && byName.isSeasonalItemFromAIBusiness)
			{
				byName = ItemsGetter.GetByName(byName.itemToReplaceWithWhenOvertakingBusiness);
			}
			ItemInstance itemInstance = ItemHelper.InitializeNewInstance(byName.itemName);
			if (isBlueprintCreator)
			{
				itemInstance.id = item.id;
			}
			itemInstance.position = item.position;
			if (transform2 != null)
			{
				SerializableQuaternion rotation = item.rotation.Copy();
				CopyRelativePositionAndRotation(transform, transform2, ref itemInstance.position, ref rotation);
				itemInstance.yRotation = ((Quaternion)rotation).eulerAngles.y;
			}
			else
			{
				itemInstance.yRotation = ((Quaternion)item.rotation).eulerAngles.y;
			}
			buildingRegistration.AddItemInstanceToBuilding(itemInstance);
			itemInstance.priceOnPurchase = byName.DefaultMarketPrice;
			PlayerItemPurchaserSettings playerItemPurchaserSettings = item.playerItemPurchaserSettings ?? new PlayerItemPurchaserSettings();
			itemInstance.playerItemPurchaserSettings = new PlayerItemPurchaserSettings
			{
				enabled = playerItemPurchaserSettings.enabled,
				itemName = playerItemPurchaserSettings.itemName,
				itemQuantity = playerItemPurchaserSettings.itemQuantity
			};
			if (buildingRegistration.RentedByPlayer && !isBlueprintCreator)
			{
				itemInstance.playerItemPurchaserSettings.enabled = false;
			}
			itemInstance.customValue = item.customValue;
			if (item is FactoryItem factoryItem)
			{
				FactoryWorkstationInstance obj = (FactoryWorkstationInstance)itemInstance;
				obj.selectedRecipeId = factoryItem.selectedRecipeId;
				obj.workstationType = factoryItem.workstationType;
				obj.priority = factoryItem.priority;
				obj.produceUpTo = factoryItem.produceUpTo;
				obj.produceUpToValue = factoryItem.produceUpToValue;
			}
			string text = null;
			if ((itemInstance.ItemCached.type & ItemType.PointOfSale) != 0)
			{
				text = ItemsGetter.GetRandomBag();
			}
			else if ((itemInstance.ItemCached.type & ItemType.ShowcaseShelf) != 0)
			{
				text = playerItemPurchaserSettings.itemName;
			}
			if (!string.IsNullOrEmpty(text))
			{
				CargoInstance stockInstance = itemInstance.GetStockInstance();
				stockInstance.itemName = text;
				if (shouldRandomlyFillShelves)
				{
					int maxStockCapacity = stockInstance.GetMaxStockCapacity(itemInstance);
					stockInstance.amount = Mathf.RoundToInt(UnityEngine.Random.Range((float)maxStockCapacity * 0.1f, maxStockCapacity));
				}
			}
			if (itemInstance.dirtAffectedCells != null)
			{
				itemInstance.dirtSpotsThatAffects = (from x in buildingDirtSpots
					where itemInstance.dirtAffectedCells.Any((CellPosition y) => x.x == y.x && x.z == y.z)
					select buildingDirtSpots.IndexOf(x)).ToList();
			}
			else
			{
				itemInstance.dirtSpotsThatAffects = item.dirtSpotsThatAffects.CopyList();
			}
			itemInstance.customPositions = item.customPositions.CopyList();
			itemInstance.customColors = item.customColors.CopyList();
			itemInstance.worldSpaceTextValue = item.worldSpaceTextValue;
			itemInstance.linkedItemName = item.linkedItemName;
			list.Add(itemInstance);
			dictionary.Add(item.id, itemInstance.id);
			if (itemInstance.ItemCached.HasTag(TagRef.Itemtag.issecuritypanel))
			{
				itemInstance.UpdateSecurityPanelCoverage();
			}
		}
		foreach (Item item2 in layoutSet.Items)
		{
			if (string.IsNullOrEmpty(item2.itemName) || ItemsGetter.GetByName(item2.itemName).HasTag(TagRef.Itemtag.ignoredbylayoutset) || !dictionary.TryGetValue(item2.id, out var instancedItemId))
			{
				continue;
			}
			ItemInstance itemInstance2 = list.First((ItemInstance x) => x.id == instancedItemId);
			if (item2.stackedItems == null || item2.stackedItems.Count == 0)
			{
				continue;
			}
			foreach (AttachableChild stackedItem in item2.stackedItems)
			{
				if (dictionary.TryGetValue(stackedItem.childId, out var value) && buildingRegistration.itemInstances.ContainsKey(value))
				{
					itemInstance2.stackedItems.Add(new AttachableChild
					{
						attachmentIndex = stackedItem.attachmentIndex,
						childId = value,
						childItemName = (string.IsNullOrEmpty(stackedItem.childItemName) ? buildingRegistration.itemInstances[value].itemName : stackedItem.childItemName)
					});
				}
			}
		}
		foreach (ItemInstance item3 in list)
		{
			foreach (AttachableChild childItems in item3.stackedItems)
			{
				list.Single((ItemInstance x) => x.id == childItems.childId).parentId = item3.id;
			}
		}
		List<SerializedInteriorDesign> interiorDesigns = layoutSet.interiorDesigns;
		if ((interiorDesigns == null || interiorDesigns.Count != 0) && (buildingRegistration.RentedByPlayer | isBlueprintCreator))
		{
			buildingRegistration.interiorDesigns = layoutSet.interiorDesigns.CopyList();
			buildingRegistration.GenerateInteriorDesignerLookup();
		}
		if (!isBlueprintCreator)
		{
			VehicleHelper.AddHandTruckSpawnersToBuildingIfNeeded(buildingRegistration);
			TasksUI.UpdateTasksFromBusiness(buildingRegistration);
			BusinessSecurityHelper.UpdateCamerasCoverage(buildingRegistration.Address);
		}
	}

	private static Item GetLayoutSetItem(ItemController itemController)
	{
		Item item = ((itemController.ItemInstance is FactoryWorkstationInstance) ? new FactoryItem() : new Item());
		item.id = itemController.ItemInstance.id;
		item.itemName = itemController.ItemInstance.itemName;
		item.rotation = itemController.transform.rotation;
		item.position = itemController.transform.position;
		item.dirtSpotsThatAffects = InstanceBehavior<BuildingManager>.Instance.GetDirtAffectedCells(itemController);
		item.customPositions = itemController.ItemInstance?.customPositions ?? itemController.customPositions;
		item.customColors = itemController.ItemInstance?.customColors ?? itemController.customColors;
		item.customValue = itemController.customValue;
		item.worldSpaceTextValue = itemController.ItemInstance?.worldSpaceTextValue;
		if (itemController.ItemInstance is FactoryWorkstationInstance factoryWorkstationInstance)
		{
			FactoryItem obj = (FactoryItem)item;
			obj.selectedRecipeId = factoryWorkstationInstance.selectedRecipeId;
			obj.workstationType = factoryWorkstationInstance.workstationType;
			obj.priority = factoryWorkstationInstance.priority;
			obj.produceUpTo = factoryWorkstationInstance.produceUpTo;
			obj.produceUpToValue = factoryWorkstationInstance.produceUpToValue;
		}
		if (itemController is SignController && itemController.ItemInstance != null && !string.IsNullOrEmpty(itemController.ItemInstance.linkedItemName))
		{
			item.linkedItemName = itemController.ItemInstance.linkedItemName;
		}
		bool flag;
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness && itemController.ItemInstance != null)
		{
			flag = (itemController.ItemInstance.ItemCached.type & ItemType.ShowcaseShelf) != 0;
			bool flag2 = (itemController.ItemInstance.ItemCached.type & ItemType.PointOfSale) != 0;
			if (flag)
			{
				goto IL_01cb;
			}
			if (flag2)
			{
				PlayerItemPurchaserSettings playerItemPurchaserSettings = itemController.ItemInstance.playerItemPurchaserSettings;
				if (playerItemPurchaserSettings == null || !playerItemPurchaserSettings.enabled)
				{
					goto IL_01cb;
				}
			}
			item.playerItemPurchaserSettings = itemController.playerItemPurchaserSettings;
		}
		else
		{
			item.playerItemPurchaserSettings = itemController.playerItemPurchaserSettings;
		}
		goto IL_022e;
		IL_022e:
		return item;
		IL_01cb:
		CargoInstance stockInstance = itemController.ItemInstance.GetStockInstance();
		item.playerItemPurchaserSettings = new PlayerItemPurchaserSettings
		{
			itemName = stockInstance.itemName,
			itemQuantity = 1,
			enabled = (flag && !string.IsNullOrEmpty(stockInstance.itemName))
		};
		goto IL_022e;
	}

	private static string CreateLayoutSetKey(string businessTypeName, BuildingSizeInfo sizeInfo, string layoutName)
	{
		string text = (layoutName ?? string.Empty).ToLowerInvariant();
		return $"{businessTypeName}|{sizeInfo.buildingSize}|{sizeInfo.buildingVersion}|{text}";
	}

	public static void CopyRelativePositionAndRotation(Transform from, Transform to, ref SerializableVector3 position, ref SerializableQuaternion rotation)
	{
		Vector3 position2 = from.InverseTransformPoint(position);
		Quaternion quaternion = Quaternion.Inverse(from.rotation) * rotation;
		position = to.TransformPoint(position2);
		rotation = to.rotation * quaternion;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		BusinessLayoutSets.Clear();
		DoneInitSynchronous = false;
		loadingLayouts = false;
	}
}
