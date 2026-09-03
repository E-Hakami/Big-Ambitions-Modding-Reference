using Extensions;
using Localizor;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace Player.HUD.ItemInfoOverlays;

public class SellerStandOverlay : IOverlay
{
	[SerializeField]
	private Transform buyButtonTemplate;

	public override bool IsValid(EntityController entityController)
	{
		return entityController is SellerStandController;
	}

	public override bool ShouldShow(EntityController entityController)
	{
		if (entityController is SellerStandController sellerStandController)
		{
			return sellerStandController.itemsToSell.Length != 0;
		}
		return false;
	}

	public override void UpdateOverlay(EntityController entityController)
	{
		SellerStandController sellerStandController = entityController as SellerStandController;
		if ((object)sellerStandController == null)
		{
			return;
		}
		buyButtonTemplate.ResetTemplate();
		interactablePriority.Clear();
		string[] itemsToSell = sellerStandController.itemsToSell;
		foreach (string itemName in itemsToSell)
		{
			string localization = itemName.GetLocalization();
			LanguageChangeEventDataHolder data = "seller_buy_item_price".Localize(new
			{
				itemName = localization,
				price = ItemHelper.GetDefaultMarketPrice(itemName).ToShortCurrencyFormat()
			});
			Transform transform = Object.Instantiate(buyButtonTemplate, buyButtonTemplate.parent);
			transform.name = itemName;
			transform.GetComponentInChildren<TextLocalizationComponent>().SetData(data);
			Button component = transform.GetComponent<Button>();
			component.onClick.RemoveAllListeners();
			component.onClick.AddListener(delegate
			{
				sellerStandController.WalkToBuyItem(itemName);
			});
			transform.gameObject.SetActive(value: true);
			interactablePriority.Add(transform);
		}
	}
}
