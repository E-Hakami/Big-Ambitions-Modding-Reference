using System.Collections.Generic;
using System.Linq;
using BigAmbitions.InteriorDesigner.InteriorElements;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings;
using InteriorDesign;

namespace Blueprints;

public static class BlueprintDataElementHelper
{
	public static List<BlueprintDataElement> GetBlueprintDataElements(this BuildingRegistration registration, Building building)
	{
		List<BlueprintDataElement> list = new List<BlueprintDataElement> { registration.GetDataElement(DataElement.InteriorScore) };
		if (registration.businessTypeName != "ba:businesstype_empty")
		{
			list.Add(registration.GetDataElement(DataElement.BusinessTypeName));
			list.Reverse();
		}
		foreach (DataElement blueprintsExtraDatum in BuildingTypeHelper.GetData(building).blueprintsExtraData)
		{
			BlueprintDataElement dataElement = registration.GetDataElement(blueprintsExtraDatum);
			if (dataElement != null)
			{
				list.Add(dataElement);
			}
		}
		return list;
	}

	private static BlueprintDataElement GetDataElement(this BuildingRegistration registration, DataElement dataElement)
	{
		if (dataElement == DataElement.Workstations && registration.businessTypeName == "ba:businesstype_warehouse")
		{
			return null;
		}
		return new BlueprintDataElement(dataElement, dataElement switch
		{
			DataElement.InteriorScore => GetInteriorScoreValue(), 
			DataElement.PointsOfSales => GetPointsOfSalesValue(registration), 
			DataElement.Workstations => GetWorkstationsValue(registration), 
			DataElement.PalletShelves => GetPalletShelvesValue(registration), 
			DataElement.StorageShelves => GetStorageShelvesValue(registration), 
			DataElement.BusinessTypeName => registration.businessTypeName, 
			_ => "0", 
		});
	}

	private static string GetInteriorScoreValue()
	{
		return InteriorScoreCalculator.GetInteriorScorePercentage(InteriorElementsHelper.InteriorElementsCache.Select((KeyValuePair<string, InteriorElement> x) => x.Value.Serialize()).ToList()).ToString();
	}

	private static string GetPointsOfSalesValue(BuildingRegistration registration)
	{
		return registration.itemInstances.Values.Count((ItemInstance x) => (x.ItemCached.type & ItemType.PointOfSale) != 0).ToString();
	}

	private static string GetWorkstationsValue(BuildingRegistration registration)
	{
		if (registration.businessTypeName == "ba:businesstype_factory")
		{
			return registration.itemInstances.Values.Count((ItemInstance x) => x.ItemCached.HasTag(TagRef.Itemtag.isfactoryassemblymachine)).ToString();
		}
		if (registration.GetBuildingType() == "ba:buildingtype_office")
		{
			return registration.itemInstances.Values.Count((ItemInstance x) => (x.ItemCached.type & ItemType.Computer) != 0).ToString();
		}
		return "0";
	}

	private static string GetPalletShelvesValue(BuildingRegistration registration)
	{
		return registration.itemInstances.Values.Count((ItemInstance x) => x.ItemCached.HasTag(TagRef.Itemtag.iswarehousestorage)).ToString();
	}

	private static string GetStorageShelvesValue(BuildingRegistration registration)
	{
		return registration.itemInstances.Values.Count((ItemInstance x) => x.ItemCached.HasTag(TagRef.Itemtag.isbusinessstorage)).ToString();
	}
}
