using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using Streets;
using TMPro;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Dialog;

public abstract class DeliveryContractSettingsBase : MonoBehaviour
{
	public class ItemToDeliver
	{
		public string itemName;

		public int amount;

		public float price;

		public Transform row;

		public AmountSelector amountSelector;

		public AmountSelector amountSelectorItemsList;

		public ItemToDeliver(string itemName, int amount, float price, Transform row, AmountSelector amountSelector)
		{
			this.itemName = itemName;
			this.amount = amount;
			this.price = price;
			this.row = row;
			this.amountSelector = amountSelector;
		}
	}

	private const float RowPaddingY = 20f;

	private const string ItemsListTitleKey = "bizman_store_furniture_delivery_title";

	private const string DeliverySlotLabelKey = "dialog_furniture_delivery_time_slot";

	public List<ItemToDeliver> itemsToDeliver;

	[HideInInspector]
	public Address selectedAddress;

	public (int, int) selectedDeliverySlot;

	[SerializeField]
	private UI.Elements.Dropdown addressDropdown;

	[SerializeField]
	private UI.Elements.Dropdown deliverySlotDropdown;

	[SerializeField]
	private Button addItemButton;

	[SerializeField]
	private Transform itemTemplate;

	[SerializeField]
	private TMP_Text deliveryFeeText;

	[SerializeField]
	private TMP_Text totalPriceText;

	[SerializeField]
	private DeliveryItemEntry deliveryEntryTemplate;

	private List<(int day, int hour)> _deliverySlots;

	private LayoutElement _layoutElement;

	private RectTransform _rectTransform;

	private List<BuildingRegistration> _buildingRegistrations;

	private float _itemsPriceIndexMultiplier = 1f;

	private readonly Dictionary<string, DeliveryItemEntry> _itemsListEntries = new Dictionary<string, DeliveryItemEntry>();

	public abstract float DeliveryFee { get; }

	public float TotalPrice => itemsToDeliver.Sum((ItemToDeliver x) => (float)x.amount * x.price) + DeliveryFee;

	public int TotalItemsToDeliverAmount => itemsToDeliver.Sum((ItemToDeliver x) => x.amount);

	public bool HasSelectedDeliverySlot
	{
		get
		{
			(int, int) tuple = selectedDeliverySlot;
			if (tuple.Item1 == -1)
			{
				return tuple.Item2 != -1;
			}
			return true;
		}
	}

	protected abstract int MaxContractAmount { get; }

	protected virtual bool ShouldPreselectFirstDeliverySlot => false;

	private bool IsContractFull => TotalItemsToDeliverAmount >= MaxContractAmount;

	private void Start()
	{
		_layoutElement = GetComponent<LayoutElement>();
		_rectTransform = GetComponent<RectTransform>();
		selectedAddress = null;
		selectedDeliverySlot = (-1, -1);
		_buildingRegistrations = GetDeliveryDestinations();
		addressDropdown.SetPlaceholder("dialog_select_address");
		addressDropdown.SetOptions(_buildingRegistrations.Select((BuildingRegistration x) => (!string.IsNullOrEmpty(x.BusinessName)) ? x.BusinessName : x.Address.ToFormattedString()).ToList(), localize: false);
		addressDropdown.onOptionSelected.AddListener(delegate(int addressIndex)
		{
			selectedAddress = _buildingRegistrations[addressIndex].Address;
		});
		_deliverySlots = GenerateDeliverySlots();
		deliverySlotDropdown.SetPlaceholder("dialog_select_delivery_time");
		List<string> list = new List<string>(_deliverySlots.Count);
		foreach (var deliverySlot in _deliverySlots)
		{
			list.Add(GetDeliverySlotLabel(deliverySlot.day, deliverySlot.hour));
		}
		deliverySlotDropdown.SetOptions(list, localize: false);
		deliverySlotDropdown.onOptionSelected.AddListener(delegate(int deliverySlotIndex)
		{
			selectedDeliverySlot = _deliverySlots[deliverySlotIndex];
		});
		if (ShouldPreselectFirstDeliverySlot)
		{
			deliverySlotDropdown.SelectOption(0);
		}
		addItemButton.onClick.AddListener(OpenItemsList);
		itemsToDeliver = new List<ItemToDeliver>();
		itemTemplate.ResetTemplate();
		deliveryFeeText.text = DeliveryFee.ToCurrencyFormat();
		UpdateTotalPrice();
		SetUpInventoryList();
	}

	protected virtual List<BuildingRegistration> GetDeliveryDestinations()
	{
		return BuildingHelper.GetPlayerBuildingRegistrations();
	}

	protected abstract List<(int day, int hour)> GenerateDeliverySlots();

	public static string GetDeliverySlotLabel(int day, int hour)
	{
		return "dialog_furniture_delivery_time_slot".Localize(new
		{
			day = TimeHelper.GetDayOfWeek(day).GetLocalizeKey(),
			number = day,
			hour = hour.GetFormattedTime()
		}).ToString();
	}

	protected abstract (List<string> itemsForSale, float priceMultiplier) GetItemsForSale();

	protected abstract void UpdateItemsListTitle();

	protected void SetItemsListTitle(string businessName)
	{
		InstanceBehavior<UIs>.Instance.itemsList.SetTitle(LanguageChangeEventDataHolder.Create("bizman_store_furniture_delivery_title", new
		{
			businessName = businessName,
			currentAmount = TotalItemsToDeliverAmount,
			maxAmount = MaxContractAmount
		}).ToString());
	}

	private void OpenItemsList()
	{
		SetUpInventoryList();
		InstanceBehavior<UIs>.Instance.itemsList.Toggle(newState: true);
	}

	private void SetUpInventoryList()
	{
		InstanceBehavior<UIs>.Instance.itemsList.Clear();
		_itemsListEntries.Clear();
		(List<string> itemsForSale, float priceMultiplier) itemsForSale = GetItemsForSale();
		List<string> item = itemsForSale.itemsForSale;
		float item2 = itemsForSale.priceMultiplier;
		_itemsPriceIndexMultiplier = item2;
		foreach (string item3 in item.OrderBy((string x) => x))
		{
			string itemForSale = item3;
			DeliveryItemEntry entry = Object.Instantiate(deliveryEntryTemplate);
			_itemsListEntries[itemForSale] = entry;
			InstanceBehavior<UIs>.Instance.itemsList.AddEntry(entry.ItemsListEntry);
			Item byName = ItemsGetter.GetByName(itemForSale);
			entry.Init(itemForSale.Localize(), ItemHelper.GetIconWithFallback(itemForSale), byName.DefaultMarketPrice * _itemsPriceIndexMultiplier);
			ItemToDeliver itemToDeliver = itemsToDeliver.FirstOrDefault((ItemToDeliver x) => x.itemName == itemForSale);
			UpdateItemsListEntryState(itemForSale, entry);
			if (itemToDeliver != null)
			{
				SetUpAmountSelector(entry.transform, entry.AmountSelector, itemForSale, itemToDeliver.amount);
				itemToDeliver.amountSelectorItemsList = entry.AmountSelector;
			}
			entry.AddClickListener(AddItem);
			entry.gameObject.SetActive(value: true);
			void AddItem()
			{
				ItemToDeliver itemToDeliver2 = itemsToDeliver.FirstOrDefault((ItemToDeliver x) => x.itemName == itemForSale);
				if (itemToDeliver2 == null)
				{
					if (!IsContractFull)
					{
						entry.SetAddButtonState(isSelected: true, canAddItem: true);
						AddItemToList(itemForSale);
						SetUpAmountSelector(entry.transform, entry.AmountSelector, itemForSale);
						itemToDeliver2 = itemsToDeliver.FirstOrDefault((ItemToDeliver x) => x.itemName == itemForSale);
						if (itemToDeliver2 != null)
						{
							itemToDeliver2.amountSelectorItemsList = entry.AmountSelector;
							UpdateListInfo();
						}
					}
				}
				else
				{
					itemToDeliver2.amountSelectorItemsList.Increase();
					itemToDeliver2.amountSelector.UpdateAmountText(itemToDeliver2.amount);
					UpdateListInfo();
				}
			}
		}
		UpdateListInfo();
	}

	private void AddItemToList(string itemName)
	{
		float num = ItemHelper.GetDefaultMarketPrice(itemName) * _itemsPriceIndexMultiplier;
		Transform transform = Object.Instantiate(itemTemplate, itemTemplate.parent);
		transform.GetLanguageChangeEventByName("ItemName").Arguments = new
		{
			itemName = itemName,
			itemPrice = num.ToCurrencyFormat()
		};
		transform.gameObject.SetActive(value: true);
		AmountSelector componentInChildren = transform.GetComponentInChildren<AmountSelector>();
		SetUpAmountSelector(transform, componentInChildren, itemName);
		itemsToDeliver.Add(new ItemToDeliver(itemName, componentInChildren.Amount, num, transform, componentInChildren));
		UpdateListInfo();
		addItemButton.transform.SetAsLastSibling();
		float num2 = transform.GetComponent<RectTransform>().sizeDelta.y + 20f;
		_rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, _rectTransform.sizeDelta.y + num2);
		_layoutElement.preferredHeight += num2;
		DialogController.current.ScrollConversationToBottom();
	}

	private void UpdateItemAmount(string itemName, int amount)
	{
		foreach (ItemToDeliver item in itemsToDeliver)
		{
			if (!(item.itemName != itemName))
			{
				if (amount <= 0)
				{
					DeleteRow(item.row);
					break;
				}
				item.amount = amount;
				UpdateListInfo();
				break;
			}
		}
	}

	private void UpdateListInfo()
	{
		UpdateTotalPrice();
		UpdateItemsListTitle();
		UpdateItemsToDeliverMaxAmounts();
		UpdateItemsListEntryStates();
	}

	private void UpdateItemsToDeliverMaxAmounts()
	{
		foreach (ItemToDeliver item in itemsToDeliver)
		{
			item.amountSelector.SetMaxAmount(MaxContractAmount - TotalItemsToDeliverAmount + item.amount);
			item.amountSelector.UpdateAmountText(item.amount);
			if (item.amountSelectorItemsList != null)
			{
				item.amountSelectorItemsList.SetMaxAmount(MaxContractAmount - TotalItemsToDeliverAmount + item.amount);
				item.amountSelectorItemsList.UpdateAmountText(item.amount);
			}
		}
	}

	private void UpdateItemsListEntryStates()
	{
		foreach (KeyValuePair<string, DeliveryItemEntry> itemsListEntry in _itemsListEntries)
		{
			UpdateItemsListEntryState(itemsListEntry.Key, itemsListEntry.Value);
		}
	}

	private void UpdateItemsListEntryState(string itemName, DeliveryItemEntry entry)
	{
		bool isSelected = itemsToDeliver.FirstOrDefault((ItemToDeliver x) => x.itemName == itemName) != null;
		bool canAddItem = !IsContractFull;
		entry.SetAddButtonState(isSelected, canAddItem);
	}

	private void UpdateTotalPrice()
	{
		totalPriceText.text = TotalPrice.ToCurrencyFormat();
	}

	private void SetUpAmountSelector(Transform row, AmountSelector amountSelector, string itemName, int initialAmount = -1)
	{
		int num = MaxContractAmount - TotalItemsToDeliverAmount;
		initialAmount = ((initialAmount != -1) ? initialAmount : ((num != 0) ? 1 : 0));
		amountSelector.SetMaxAmount(num);
		amountSelector.SetAmount(initialAmount);
		amountSelector.onAmountUpdate.RemoveAllListeners();
		amountSelector.onAmountUpdate.AddListener(delegate(int amount)
		{
			UpdateItemAmount(itemName, amount);
		});
		amountSelector.onDelete.RemoveAllListeners();
		amountSelector.onDelete.AddListener(delegate
		{
			DeleteRow(row);
		});
	}

	private void DeleteRow(Transform row)
	{
		AmountSelector amountSelector = row.GetComponentInChildren<AmountSelector>();
		amountSelector.onAmountUpdate.RemoveAllListeners();
		amountSelector.onDelete.RemoveAllListeners();
		ItemToDeliver itemToDeliver = itemsToDeliver.First((ItemToDeliver x) => x.amountSelector == amountSelector);
		itemsToDeliver.Remove(itemToDeliver);
		UpdateListInfo();
		_itemsListEntries[itemToDeliver.itemName].SetAddButtonState(isSelected: false, !IsContractFull);
		int amount = ((itemToDeliver.amountSelectorItemsList.maxAmount != 0) ? 1 : 0);
		amountSelector.SetAmount(amount);
		float num = row.GetComponent<RectTransform>().sizeDelta.y + 20f;
		_rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, _rectTransform.sizeDelta.y - num);
		_layoutElement.preferredHeight -= num;
		Object.Destroy(row.gameObject);
		DialogController.current.ScrollConversationToBottom();
	}
}
