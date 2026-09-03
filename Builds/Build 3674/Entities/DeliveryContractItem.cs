using System;
using System.Runtime.Serialization;
using BigAmbitions.Items;
using HGAttributes;

namespace Entities;

[Serializable]
public class DeliveryContractItem
{
	[AutocompleteDropdown("Items")]
	public string itemName;

	[Obsolete]
	public int boxes;

	public int amount;

	public int amountOrderedLastWeek;

	public int amountOrderedThisWeek;

	[NonSerialized]
	private Item _itemCached;

	[IgnoreDataMember]
	public Item ItemCached
	{
		get
		{
			if (_itemCached == null)
			{
				_itemCached = ItemsGetter.GetByName(itemName);
			}
			return _itemCached;
		}
	}
}
