using System;
using System.Collections.Generic;
using BigAmbitions.Items;
using HGAttributes;

namespace BusinessLayoutSets;

[Serializable]
public class Item
{
	public string id;

	public SerializableQuaternion rotation;

	public SerializableVector3 position;

	[AutocompleteDropdown("Items")]
	public string itemName;

	public PlayerItemPurchaserSettings playerItemPurchaserSettings;

	public List<AttachableChild> stackedItems = new List<AttachableChild>();

	public string parentId;

	public List<int> dirtSpotsThatAffects = new List<int>();

	public List<SerializableVector3> customPositions;

	public List<CustomColor> customColors;

	public string customValue;

	public string worldSpaceTextValue;

	public string linkedItemName;

	public List<CellPosition> dirtAffectedCells;
}
