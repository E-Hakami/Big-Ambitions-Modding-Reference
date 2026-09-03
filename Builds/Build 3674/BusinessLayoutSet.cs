using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessLayoutSets;

[Serializable]
public class BusinessLayoutSet
{
	public List<Item> Items = new List<Item>();

	public List<SerializedInteriorDesign> interiorDesigns = new List<SerializedInteriorDesign>();

	public string BusinessType;

	public string BuildingSize;

	public int BuildingVersion;

	public string LayoutName;

	public int buildNumber;

	public List<string> requiredModIds = new List<string>();

	public float GetValuation()
	{
		return Items.Sum((Item item) => ItemHelper.GetDefaultMarketPrice(item.itemName));
	}

	public async Task Serialize(string path)
	{
		await BusinessLayoutSetHelper.Serialize(path, this);
	}
}
