using BigAmbitions.Items;
using Extensions;
using Helpers;
using Localizor;
using TMPro;
using UnityEngine;

namespace UI.InteriorDesigner;

public class CustomerCapacityEntry : MonoBehaviour
{
	[SerializeField]
	private TMP_Text itemTitle;

	[SerializeField]
	private TMP_Text totalCapacityText;

	[SerializeField]
	private CustomerCapacityShelf shelfTemplate;

	public void SetUp(Item.ItemCapacity itemCapacity, int buildingLimit)
	{
		itemTitle.text = itemCapacity.itemName.GetLocalization();
		Color32 color = ((itemCapacity.CustomersLimit < buildingLimit) ? InstanceBehavior<GlobalReferences>.Instance.colors.red : InstanceBehavior<GlobalReferences>.Instance.colors.green);
		totalCapacityText.text = $"<color={color.ToHex()}>{itemCapacity.CustomersLimit}</color>/{buildingLimit}";
		shelfTemplate.transform.ResetTemplate();
		foreach (Item.ItemCapacityShelf itemShelf in itemCapacity.itemShelves)
		{
			CustomerCapacityShelf customerCapacityShelf = Object.Instantiate(shelfTemplate, shelfTemplate.transform.parent);
			customerCapacityShelf.SetUp(itemShelf);
			customerCapacityShelf.gameObject.SetActive(value: true);
		}
	}
}
