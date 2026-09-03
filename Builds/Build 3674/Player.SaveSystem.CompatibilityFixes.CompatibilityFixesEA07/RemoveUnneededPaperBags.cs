using System.Linq;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;

public class RemoveUnneededPaperBags : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration item in gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => x.RentedByPlayer))
		{
			BusinessType data = BusinessTypeHelper.GetData(item);
			if (!data || data.HasTag(TagRef.Businesstag.customersneedpaperbags))
			{
				continue;
			}
			foreach (ItemInstance item2 in from x in item.itemsInBuilding
				select gameInstance.WorldItemsHashSet.FirstOrDefault((ItemInstance y) => y.id == x) into x
				where x != null && (x.ItemCached.type & ItemType.PointOfSale) != 0
				select x)
			{
				item2.shelfItem.stock = 0;
			}
		}
	}
}
