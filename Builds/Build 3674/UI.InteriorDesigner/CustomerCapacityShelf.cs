using BigAmbitions.Items;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;

namespace UI.InteriorDesigner;

public class CustomerCapacityShelf : MonoBehaviour
{
	[SerializeField]
	private TextLocalizationComponent itemTitle;

	[SerializeField]
	private TMP_Text totalCapacityText;

	public void SetUp(Item.ItemCapacityShelf itemCapacityShelf)
	{
		itemTitle.SetData("bizman_insight_shelf_type_capacity".Localize(new
		{
			shelfAmount = itemCapacityShelf.amount,
			shelfLabel = itemCapacityShelf.itemName,
			customersPerHour = itemCapacityShelf.customersPerHour
		}));
		totalCapacityText.text = $"+{itemCapacityShelf.TotalCustomersPerHour}";
	}
}
