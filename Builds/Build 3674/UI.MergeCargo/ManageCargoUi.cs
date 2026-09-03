using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Extensions;
using Localizor;
using Localizor.LanguageChangeEvent;
using UI.ItemPanel;
using UI.PlayerHUD;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MergeCargo;

public class ManageCargoUi : MonoBehaviour
{
	public bool isPanelOpen;

	[SerializeField]
	private GameObject panel;

	[SerializeField]
	private Transform itemTemplate;

	[SerializeField]
	private Button sellAllButton;

	[SerializeField]
	private TextLocalizationComponent contentsLabel;

	private List<CargoItem> _cargoItems;

	public ICargoHolder currentCargoHolder;

	private void Awake()
	{
		panel.SetActive(value: false);
		sellAllButton.onClick.AddListener(OnSellAllClick);
		InstanceBehavior<GameManager>.Instance.playerController.PlayerChangedNavigation.AddListener(Close);
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate
		{
			if (isPanelOpen)
			{
				Close();
			}
		});
	}

	public void Close()
	{
		currentCargoHolder?.RemoveCallFromOnItemsInCargoUpdated(ReloadCargo);
		panel.SetActive(value: false);
		isPanelOpen = false;
	}

	public void Show(ICargoHolder cargoHolder)
	{
		currentCargoHolder = cargoHolder;
		currentCargoHolder.AddCallToOnItemsInCargoUpdated(ReloadCargo);
		isPanelOpen = true;
		ReloadCargo();
		panel.SetActive(value: true);
	}

	public void ReloadCargo()
	{
		itemTemplate.ResetTemplate();
		List<CargoInstance> cargoInstances = currentCargoHolder.GetCargoInstances();
		_cargoItems = CargoItem.ConvertCargoInstancesToCargoItems(cargoInstances);
		sellAllButton.gameObject.SetActive(_cargoItems.Any((CargoItem x) => x.IsSellable));
		foreach (CargoItem item in _cargoItems.OrderBy((CargoItem x) => x.itemName))
		{
			UnityEngine.Object.Instantiate(itemTemplate, itemTemplate.parent).GetComponent<CargoItemUi>().SetUp(item, InstanceBehavior<UIs>.Instance.playerHUD.manageCargoUI.currentCargoHolder);
		}
		contentsLabel.Arguments = new
		{
			currentBoxes = cargoInstances.Count,
			maxBoxes = currentCargoHolder.GetMaxCargoSize()
		};
	}

	private void OnSellAllClick()
	{
		List<CargoItem> sellableCargoItems = _cargoItems.Where((CargoItem x) => x.IsSellable).ToList();
		if (sellableCargoItems.Count == 0)
		{
			return;
		}
		float totalSellPrice = 0f;
		foreach (CargoItem item in sellableCargoItems)
		{
			CargoInstance cargoInstance = item.cargoInstances[0];
			totalSellPrice += cargoInstance.GetSellingPrice() * (float)item.cargoInstances.Count;
		}
		LanguageChangeEventDataHolder bodyData = "manage_cargo_sell_all_confirm".Localize(new
		{
			cargoName = currentCargoHolder.GetCargoName(),
			totalPrice = totalSellPrice.ToShortCurrencyFormat()
		});
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			bool flag = false;
			foreach (CargoItem item2 in sellableCargoItems)
			{
				if (item2.cargoInstances[0].ItemCached.isSpecialGift)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				ItemPanelUI.ConfirmDiscardingSpecialGift(OnConfirmSellAll);
			}
			else
			{
				OnConfirmSellAll();
			}
		});
		void OnConfirmSellAll()
		{
			foreach (CargoItem item3 in sellableCargoItems)
			{
				foreach (CargoInstance cargoInstance2 in item3.cargoInstances)
				{
					currentCargoHolder.RemoveFromCargo(cargoInstance2);
				}
			}
			Dictionary<string, string> data = new Dictionary<string, string> { 
			{
				"cargoName",
				currentCargoHolder.GetCargoName()
			} };
			TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_cargoholderinventorysold", data);
			GameManager.ChangeMoneySafe(totalSellPrice, transactionInfo);
			GameEvent.Invoke("ba:gameevent_itemcargochanged");
		}
	}
}
