using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Entities;
using Extensions;
using JimmysUnityUtilities;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.Warehouse;

public class Inventory : MonoBehaviour
{
	public WarehouseProductsScrollerController productsScrollerController;

	[SerializeField]
	private TextLocalizationComponent boxesLabel;

	[SerializeField]
	private Button sellAllButton;

	private BizManBusiness _bizManBusiness;

	private Entities.Warehouse _warehouse;

	private List<ItemInstance> _palletShelves;

	private void Awake()
	{
		_bizManBusiness = GetComponentInParent<BizManBusiness>();
	}

	private void OnEnable()
	{
		RefreshData();
	}

	private void RefreshData()
	{
		_warehouse = (Entities.Warehouse)_bizManBusiness.buildingRegistration;
		if (_palletShelves == null)
		{
			_palletShelves = new List<ItemInstance>();
		}
		_palletShelves.Clear();
		int num = 0;
		int num2 = 0;
		foreach (ItemInstance value in _warehouse.itemInstances.Values)
		{
			Item byName = ItemsGetter.GetByName(value.itemName);
			if (byName.HasTag(TagRef.Itemtag.iswarehousestorage))
			{
				_palletShelves.Add(value);
				num += byName.cargoCapacity;
				num2 += value.cargoInstances.Count;
			}
		}
		boxesLabel.Arguments = new
		{
			currentBoxes = num2.ToFormattedNumber(),
			maxBoxes = num.ToFormattedNumber()
		};
		sellAllButton.interactable = num2 > 0;
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			productsScrollerController.LoadProducts(_warehouse);
		});
	}

	public void SellAllInventory()
	{
		IEnumerable<CargoInstance> enumerable = _palletShelves.SelectMany((ItemInstance x) => x.cargoInstances);
		float totalPrice = 0f;
		foreach (CargoInstance item in enumerable)
		{
			totalPrice += (float)item.amount * item.pricePerUnit;
		}
		totalPrice *= ItemHelper.sellingMultiplier;
		HudConfirm.Show(default(LanguageChangeEventDataHolder), new LanguageChangeEventDataHolder
		{
			Key = "bizman_inventory_sell_all_confirm",
			Arguments = new
			{
				totalPrice = totalPrice.ToShortCurrencyFormat()
			}
		}, delegate
		{
			OnConfirmSellAllInventory(totalPrice);
		});
	}

	private void OnConfirmSellAllInventory(float price)
	{
		foreach (ItemInstance palletShelf in _palletShelves)
		{
			palletShelf.cargoInstances.Clear();
			if (InstanceBehavior<BuildingManager>.Instance?.building?.Address == _warehouse.Address)
			{
				palletShelf.OnItemsInCargoUpdated()();
			}
		}
		Dictionary<string, string> data = new Dictionary<string, string> { { "businessName", _warehouse.BusinessName } };
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_businessinventorysold", data);
		GameManager.ChangeMoneySafe(price, transactionInfo);
		RefreshData();
	}
}
