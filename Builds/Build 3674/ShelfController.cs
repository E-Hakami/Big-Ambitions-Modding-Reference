using System;
using System.Collections.Generic;
using System.Linq;
using BAModAPI.Services;
using BigAmbitions.Items;
using BigAmbitions.SaveSystem;
using Extensions;
using NaughtyAttributes;
using UnityEngine;

public abstract class ShelfController : Producer
{
	[NonSerialized]
	private static readonly Dictionary<string, Dictionary<string, string>> ModShowcaseItems = new Dictionary<string, Dictionary<string, string>>();

	[Header("ShelfController")]
	[SerializeField]
	private Transform itemsVisualsContainer;

	[SerializeField]
	private bool shuffleItems = true;

	[NonSerialized]
	public double fillState;

	public bool canSetColorToItemsHolding;

	[ShowIf("canSetColorToItemsHolding")]
	public CustomColorChannel colorChannelsThatApplyToItemsHolding;

	[ShowIf("canSetColorToItemsHolding")]
	public Renderer[] renderersToRecolor;

	private Transform _selectedItemVisualsContainer;

	private string _selectedItemVisualsItemNameId;

	private GameObject[] _visualItems = Array.Empty<GameObject>();

	public virtual bool IsFull()
	{
		return false;
	}

	public override void Awake()
	{
		base.Awake();
		SpawnModShowcaseItems();
	}

	public override void Start()
	{
		base.Start();
		base.ItemInstance.AddCallToOnItemsInCargoUpdated(UpdateVisuals);
		if (playerItemPurchaserSettings.enabled && !base.BuildingContext.Registration.RentedByPlayer)
		{
			if (playerItemPurchaserSettings.itemName != itemName)
			{
				ShowItemVisuals(playerItemPurchaserSettings.itemName);
			}
		}
		else if (InstanceBehavior<GameManager>.Instance != null)
		{
			UpdateVisuals();
		}
		else if (base.ItemInstance.cargoInstances.Count == 1)
		{
			ShowItemVisuals();
		}
	}

	public virtual void UpdateVisuals()
	{
	}

	public override void Hide()
	{
		base.Hide();
		ToggleItemVisuals(toggle: false);
	}

	public override void Show()
	{
		base.Show();
		ToggleItemVisuals(toggle: true);
	}

	public void ShowItemVisuals(string itemNameToShow = null, bool showDefault = true)
	{
		if (itemsVisualsContainer == null)
		{
			return;
		}
		if (itemNameToShow == itemName)
		{
			ToggleItemVisuals(toggle: false);
			_selectedItemVisualsContainer = null;
			_selectedItemVisualsItemNameId = null;
			return;
		}
		if (string.IsNullOrEmpty(itemNameToShow))
		{
			itemNameToShow = base.ItemInstance.GetStockInstance()?.itemName;
			Item item = ((!string.IsNullOrEmpty(itemNameToShow)) ? ItemsGetter.GetByName(itemNameToShow) : null);
			if (item == null || (item.type & ItemType.RetailProduct) == 0)
			{
				ToggleItemVisuals(toggle: false);
				_selectedItemVisualsContainer = null;
				_selectedItemVisualsItemNameId = null;
				return;
			}
		}
		if (_selectedItemVisualsItemNameId == itemNameToShow)
		{
			return;
		}
		SetItemToShow(itemNameToShow);
		if (!_selectedItemVisualsContainer & showDefault)
		{
			SetItemToShow(base.Item.defaultShelfItemName);
			if (!_selectedItemVisualsContainer)
			{
				SetItemToShow("ba:itemname_closedcardboardbox");
			}
		}
		if (!_selectedItemVisualsContainer)
		{
			if (!string.IsNullOrEmpty(itemNameToShow))
			{
				Debug.LogWarning(base.name + " has no matching item visuals for " + itemNameToShow, this);
			}
		}
		else if ((bool)_selectedItemVisualsContainer)
		{
			CollectVisualItems();
		}
	}

	public void ToggleItemVisuals(bool toggle)
	{
		if (!(itemsVisualsContainer == null) && (bool)_selectedItemVisualsContainer)
		{
			_selectedItemVisualsContainer.gameObject.SetActive(toggle);
		}
	}

	public void UpdateFillState(double fillStateToSet)
	{
		fillState = fillStateToSet;
		for (int i = 0; i < _visualItems.Length; i++)
		{
			_visualItems[i].SetActive(fillState * (double)_visualItems.Length > (double)i);
		}
		fillState = fillStateToSet;
	}

	public static void RegisterItemToShow(string itemNameToShow, string shelfName, string copyItemPlacementOfItemName)
	{
		if (!ModShowcaseItems.TryGetValue(itemNameToShow, out var value))
		{
			value = new Dictionary<string, string>();
			ModShowcaseItems.Add(itemNameToShow, value);
		}
		value.Add(shelfName, copyItemPlacementOfItemName);
	}

	public static void UnregisterItemToShow(string itemNameToShow)
	{
		if (!string.IsNullOrWhiteSpace(itemNameToShow))
		{
			ModShowcaseItems.Remove(itemNameToShow);
		}
	}

	private void SetItemToShow(string itemNameToShow)
	{
		_selectedItemVisualsContainer = null;
		_selectedItemVisualsItemNameId = null;
		string idWithoutType = itemNameToShow.GetIdWithoutType();
		foreach (Transform item in itemsVisualsContainer.transform)
		{
			bool flag = item.name.Equals(idWithoutType, StringComparison.InvariantCultureIgnoreCase);
			item.gameObject.SetActive(flag && visible);
			if (flag)
			{
				_selectedItemVisualsContainer = item;
				_selectedItemVisualsItemNameId = itemNameToShow;
			}
		}
	}

	private void CollectVisualItems()
	{
		_visualItems = new GameObject[_selectedItemVisualsContainer.childCount];
		for (int i = 0; i < _selectedItemVisualsContainer.childCount; i++)
		{
			_visualItems[i] = _selectedItemVisualsContainer.GetChild(i).gameObject;
		}
		if (shuffleItems)
		{
			_visualItems = _visualItems.Shuffle().ToArray();
		}
	}

	public override void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			base.ItemInstance?.RemoveCallFromOnItemsInCargoUpdated(UpdateVisuals);
			base.OnDestroy();
		}
	}

	public override Renderer[] GetRenderersToSetColor(CustomColorChannel customColorChannel)
	{
		if (canSetColorToItemsHolding && colorChannelsThatApplyToItemsHolding.HasFlag(customColorChannel))
		{
			return renderersToRecolor;
		}
		return base.GetRenderersToSetColor(customColorChannel);
	}

	private void SpawnModShowcaseItems()
	{
		foreach (KeyValuePair<string, Dictionary<string, string>> modShowcaseItem in ModShowcaseItems)
		{
			string key = modShowcaseItem.Key;
			if (!modShowcaseItem.Value.TryGetValue(itemName, out var value))
			{
				continue;
			}
			string idWithoutType = key.GetIdWithoutType();
			string idWithoutType2 = value.GetIdWithoutType();
			Transform transform = null;
			foreach (Transform item3 in itemsVisualsContainer)
			{
				if (item3.name.Equals(idWithoutType2, StringComparison.InvariantCultureIgnoreCase))
				{
					transform = UnityEngine.Object.Instantiate(item3, itemsVisualsContainer);
					transform.name = idWithoutType;
					break;
				}
			}
			if (transform == null)
			{
				Debug.LogError("Item placement item '" + value + "' not found on shelf '" + base.name + "'");
				break;
			}
			List<(Vector3, Quaternion)> list = new List<(Vector3, Quaternion)>();
			foreach (Transform item4 in transform)
			{
				list.Add((item4.position, item4.rotation));
			}
			transform.ClearChildren();
			string text = key.GetIdWithoutType() + ".prefab";
			GameObject gameObject = AssetService.FindAssetInAnyBundleByFileName<GameObject>(text);
			if (gameObject == null)
			{
				Debug.LogError(text + " not found");
				UnityEngine.Object.Destroy(transform.gameObject);
				break;
			}
			foreach (var item5 in list)
			{
				Vector3 item = item5.Item1;
				Quaternion item2 = item5.Item2;
				GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, transform);
				if (gameObject2 == null)
				{
					UnityEngine.Object.Destroy(transform.gameObject);
					break;
				}
				gameObject2.transform.position = item;
				gameObject2.transform.rotation = item2;
			}
		}
	}
}
