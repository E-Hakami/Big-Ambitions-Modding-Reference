using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.InputSystem;
using BigAmbitions.InteriorDesigner.Tools;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings.Indoors.InteriorDesign;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using Tooltip;
using UI.InteriorDesigner;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.PlayerHUD;

public class PackageCargoItemUi : MonoBehaviour, IBeginDragHandler, IEventSystemHandler, IDragHandler, IEndDragHandler
{
	private static Action<int, CargoItem, CargoInstance> OnSellClick;

	private static Action<int, CargoInstance> OnPlaceClick;

	private static Action<int, CargoInstance> OnPackClick;

	private static Action<int, CargoItem> OnDragBegin;

	private static Action<int> RemoveCargoItem;

	private static Action RefreshList;

	private static Action<int, int, bool> ExecuteRevertibleAction;

	[SerializeField]
	private TextLocalizationComponent nameLabel;

	[SerializeField]
	private TMP_Text amountLabel;

	[SerializeField]
	private LocalizedListTooltip bundleItemsTooltip;

	[SerializeField]
	private Image backgroundImage;

	[SerializeField]
	private Button sellButton;

	[SerializeField]
	private Button placeButton;

	[SerializeField]
	private Button packButton;

	[Header("References")]
	[SerializeField]
	private Sprite stackSprite;

	[Header("Drag")]
	[SerializeField]
	private Vector3 offset = new Vector3(10f, 10f, 0f);

	private int _cargoIndex;

	private CargoItem _cargoItem;

	private Camera _mainCamera;

	public string SearchName { get; private set; }

	public bool CanPlaceItem { get; private set; }

	public bool CanPackItem { get; private set; }

	public static void SetActions(Action<int, CargoItem, CargoInstance> onSellClick, Action<int, CargoInstance> onPlaceClick, Action<int, CargoInstance> onPackClick, Action<int, CargoItem> onDragBegin, Action<int> removeCargoItem, Action refreshList, Action<int, int, bool> executeRevertibleAction)
	{
		OnSellClick = onSellClick;
		OnPlaceClick = onPlaceClick;
		OnPackClick = onPackClick;
		OnDragBegin = onDragBegin;
		RemoveCargoItem = removeCargoItem;
		RefreshList = refreshList;
		ExecuteRevertibleAction = executeRevertibleAction;
	}

	public void SetUp(int cargoIndex, CargoItem cargoItem, bool canPlaceItem, bool canPackItem)
	{
		_cargoIndex = cargoIndex;
		_cargoItem = cargoItem;
		CanPlaceItem = canPlaceItem;
		CanPackItem = canPackItem;
		CargoInstance firstCargoInstance = cargoItem.cargoInstances[0];
		bool isSealed = firstCargoInstance.IsSealed;
		nameLabel.Key = firstCargoInstance.itemName;
		SearchName = firstCargoInstance.itemName.GetLocalization().Replace(" ", "");
		if (firstCargoInstance.nestedCargoInstances.Count > 0)
		{
			amountLabel.gameObject.SetActive(value: false);
			if (isSealed)
			{
				NestedCargoInstance nestedCargoInstance = firstCargoInstance.nestedCargoInstances[0];
				nameLabel.SetData(LocalizationHelper.GetItemLabel(nestedCargoInstance.itemName, nestedCargoInstance.amount));
				bundleItemsTooltip.gameObject.SetActive(value: false);
			}
			else
			{
				Dictionary<string, int> dictionary = new Dictionary<string, int>(firstCargoInstance.nestedCargoInstances.Count);
				foreach (NestedCargoInstance nestedCargoInstance3 in firstCargoInstance.nestedCargoInstances)
				{
					if (dictionary.TryGetValue(nestedCargoInstance3.itemName, out var value))
					{
						dictionary[nestedCargoInstance3.itemName] = value + nestedCargoInstance3.amount;
					}
					else
					{
						dictionary[nestedCargoInstance3.itemName] = nestedCargoInstance3.amount;
					}
				}
				if (dictionary.Count == 1)
				{
					NestedCargoInstance nestedCargoInstance2 = firstCargoInstance.nestedCargoInstances[0];
					nameLabel.SetData(LocalizationHelper.GetItemLabel(nestedCargoInstance2.itemName));
					amountLabel.text = LocalizationHelper.GetAmountLabel(1, dictionary[nestedCargoInstance2.itemName]);
					amountLabel.gameObject.SetActive(value: true);
					bundleItemsTooltip.gameObject.SetActive(value: false);
				}
				else
				{
					bundleItemsTooltip.list = dictionary.Select((KeyValuePair<string, int> kv) => LocalizationHelper.GetItemLabel(kv.Key, kv.Value).ToString()).ToList();
					bundleItemsTooltip.gameObject.SetActive(value: true);
				}
			}
		}
		else
		{
			amountLabel.text = LocalizationHelper.GetAmountLabel(cargoItem.cargoInstances.Count, firstCargoInstance.amount);
			bundleItemsTooltip.gameObject.SetActive(value: false);
			amountLabel.gameObject.SetActive(value: true);
		}
		if (cargoItem.cargoInstances.Count > 1)
		{
			backgroundImage.sprite = stackSprite;
		}
		sellButton.onClick.RemoveAllListeners();
		placeButton.onClick.RemoveAllListeners();
		packButton.onClick.RemoveAllListeners();
		if (ItemsGetter.GetByName(cargoItem.itemName).HasTag(TagRef.Itemtag.issealedcontainer))
		{
			sellButton.gameObject.SetActive(value: false);
			placeButton.gameObject.SetActive(value: false);
			packButton.gameObject.SetActive(value: true);
		}
		else
		{
			sellButton.gameObject.SetActive(value: true);
			sellButton.onClick.AddListener(delegate
			{
				OnSellClick?.Invoke(cargoIndex, cargoItem, firstCargoInstance);
			});
			placeButton.gameObject.SetActive(canPlaceItem);
			placeButton.onClick.AddListener(delegate
			{
				OnPlaceClick?.Invoke(cargoIndex, firstCargoInstance);
			});
			packButton.gameObject.SetActive(canPackItem);
			packButton.onClick.AddListener(delegate
			{
				OnPackClick?.Invoke(cargoIndex, firstCargoInstance);
			});
		}
		base.gameObject.SetActive(value: true);
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		OnDragBegin?.Invoke(_cargoIndex, _cargoItem);
		if (amountLabel.gameObject.activeSelf)
		{
			CargoInstance cargoInstance = _cargoItem.cargoInstances[0];
			if (cargoInstance.nestedCargoInstances.Count == 0)
			{
				amountLabel.text = LocalizationHelper.GetAmountLabel(1, cargoInstance.amount);
			}
		}
		MouseController.SetCursor(new PackageCursorChangeEvent
		{
			ChangedCursor = true
		});
		if (base.transform is RectTransform rectTransform)
		{
			rectTransform.pivot = new Vector2(0f, 1f);
		}
		OnDrag(eventData);
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left && base.transform is RectTransform rectTransform && RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out var worldPoint))
		{
			rectTransform.position = worldPoint + offset;
		}
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (!EventSystem.current.IsPointerOverGameObject())
		{
			ICargoHolder cargoHolder = null;
			bool arg = false;
			int num = IInteriorDesignerTool.getSelectedItemControllerIndex();
			if (num == -1)
			{
				num = IInteriorDesignerTool.getSelectedVehicleControllerIndex();
				if (num == -1)
				{
					if ((object)_mainCamera == null)
					{
						_mainCamera = Camera.main;
					}
					if (Physics.Raycast(_mainCamera.ScreenPointToRay(InputActionHelper.GetCursorPosition()), out var _, 600f, LayerHelper.groundLayerMask))
					{
						if (CanPlaceItem)
						{
							OnPlaceClick?.Invoke(_cargoIndex, _cargoItem.cargoInstances[0]);
						}
						else if (CanPackItem)
						{
							OnPackClick?.Invoke(_cargoIndex, _cargoItem.cargoInstances[0]);
						}
					}
					MouseController.SetCursor(null);
					UnityEngine.Object.Destroy(base.gameObject);
					RefreshList?.Invoke();
					return;
				}
				cargoHolder = InteriorDesignerController.VehicleControllersCache[num]?.vehicleInstance;
				arg = true;
			}
			else if (CanItemHoldCargo(InteriorDesignerController.ItemControllersCache[num]))
			{
				cargoHolder = InteriorDesignerController.ItemControllersCache[num]?.ItemInstance;
			}
			if (cargoHolder != null)
			{
				RemoveCargoItem?.Invoke(_cargoIndex);
				ExecuteRevertibleAction?.Invoke(_cargoIndex, num, arg);
			}
		}
		MouseController.SetCursor(null);
		UnityEngine.Object.Destroy(base.gameObject);
		RefreshList?.Invoke();
	}

	private static bool CanItemHoldCargo(ItemController itemController)
	{
		if (!itemController.Item.IsStockCarrier())
		{
			return itemController.ItemInstance.ItemCached.cargoCapacity > 0;
		}
		return true;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		OnSellClick = null;
		OnPlaceClick = null;
		OnPackClick = null;
		OnDragBegin = null;
		RemoveCargoItem = null;
		RefreshList = null;
		ExecuteRevertibleAction = null;
	}
}
