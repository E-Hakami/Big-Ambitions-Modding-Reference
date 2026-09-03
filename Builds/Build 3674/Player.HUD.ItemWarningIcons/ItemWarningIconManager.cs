using System;
using System.Collections.Generic;
using JimmysUnityUtilities;
using NaughtyAttributes;
using UI.InteriorDesigner;
using UI.Load;
using UnityEngine;
using UnityEngine.Pool;

namespace Player.HUD.ItemWarningIcons;

public class ItemWarningIconManager : InstanceBehavior<ItemWarningIconManager>
{
	[SerializeField]
	private bool isBlueprintMode;

	public Transform warningIconsParent;

	public Transform emojiParent;

	[SerializeField]
	private CanvasGroup canvasGroup;

	[SerializeField]
	private ItemWarningIcon iconPrefab;

	[BoxGroup("Icon foreground")]
	[SerializeField]
	private Sprite lowStockIcon;

	[BoxGroup("Icon foreground")]
	[SerializeField]
	private Sprite exclamationIcon;

	[BoxGroup("Icon foreground")]
	[SerializeField]
	private Sprite infoIcon;

	[BoxGroup("Icon foreground")]
	[SerializeField]
	private Sprite checkmarkIcon;

	[BoxGroup("Backgrounds")]
	[SerializeField]
	private Sprite neutralBackground;

	[BoxGroup("Backgrounds")]
	[SerializeField]
	private Sprite orangeBackground;

	[BoxGroup("Backgrounds")]
	[SerializeField]
	private Sprite redBackground;

	[BoxGroup("Backgrounds")]
	[SerializeField]
	private Sprite greenBackground;

	private readonly Dictionary<int, ItemWarningIcon> _activeIcons = new Dictionary<int, ItemWarningIcon>();

	private ObjectPool<ItemWarningIcon> _iconPool;

	private bool _isVisible;

	public void Start()
	{
		GlobalEvents.onEnterBuildingDelayed = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuildingDelayed, (Action<Address>)delegate
		{
			RefreshCurrentBuildingWarningIcons();
		});
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, (Action<Address>)delegate
		{
			ReleaseAllIcons();
		});
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, new Action<bool>(HandleCanvasVisibility));
		InteriorDesignerUI.OnInteriorDesignerToggle.AddListener(HandleCanvasVisibility);
		HandleCanvasVisibility(shouldHide: false);
		_iconPool = new ObjectPool<ItemWarningIcon>(() => UnityEngine.Object.Instantiate(iconPrefab, warningIconsParent), delegate(ItemWarningIcon icon)
		{
			icon.gameObject.SetActive(value: true);
		}, delegate(ItemWarningIcon icon)
		{
			icon.gameObject.SetActive(value: false);
		}, UnityEngine.Object.Destroy);
	}

	private void LateUpdate()
	{
		if (_isVisible && !LoadScene.isLoading && (bool)GameManager.GetMainCamera())
		{
			UpdatePositions();
		}
	}

	private void UpdatePositions()
	{
		Camera mainCamera = GameManager.GetMainCamera();
		foreach (ItemWarningIcon value in _activeIcons.Values)
		{
			value.rectTransform.position = mainCamera.WorldToScreenPoint(value.linkedItemController.transform.position + value.linkedItemController.itemWarningIconOffset);
		}
	}

	public void HandleCanvasVisibility(bool shouldHide)
	{
		if (shouldHide)
		{
			canvasGroup.alpha = 0f;
			_isVisible = false;
			return;
		}
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			canvasGroup.alpha = 1f;
			_isVisible = true;
		});
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		_iconPool?.Dispose();
	}

	public void UpdateWarningIconByIds(HashSet<string> itemIds)
	{
		foreach (string itemId in itemIds)
		{
			if (!string.IsNullOrEmpty(itemId))
			{
				ItemController itemControllerByID = ItemHelper.GetItemControllerByID(itemId);
				if (itemControllerByID != null)
				{
					UpdateWarningIcon(itemControllerByID);
				}
			}
		}
	}

	public void UpdateWarningIcon(ItemController itemController)
	{
		BuildingManager instance = InstanceBehavior<BuildingManager>.Instance;
		if (!itemController || !instance || _iconPool == null)
		{
			return;
		}
		int hashCode = itemController.GetHashCode();
		bool flag = itemController.alwaysShowWarningIcon || instance.IsPlayerOwnedBusiness || itemController.CanInteractInAnyBusiness;
		if (!itemController.HasAnyRendererVisible() || !flag)
		{
			HideWarningIcon(hashCode);
			return;
		}
		WarningIconType warningIconType = itemController.GetWarningIconType();
		if (warningIconType == WarningIconType.None || (isBlueprintMode && (warningIconType == WarningIconType.LowStock || warningIconType == WarningIconType.VeryLowStock)))
		{
			HideWarningIcon(hashCode);
			return;
		}
		if (_activeIcons.TryGetValue(hashCode, out var value))
		{
			if (value.currentIconType == warningIconType)
			{
				return;
			}
			HideWarningIcon(hashCode);
		}
		ItemWarningIcon itemWarningIcon = _iconPool.Get();
		itemWarningIcon.linkedItemController = itemController;
		var (sprite, sprite2, active) = GetIconVisuals(warningIconType);
		itemWarningIcon.icon.sprite = sprite;
		itemWarningIcon.background.sprite = sprite2;
		itemWarningIcon.Pointer.SetActive(active);
		itemWarningIcon.currentIconType = warningIconType;
		_activeIcons.Add(hashCode, itemWarningIcon);
	}

	public void HideWarningIcon(ItemController itemController)
	{
		HideWarningIcon(itemController.GetHashCode());
	}

	private void HideWarningIcon(int itemControllerHash)
	{
		if (_activeIcons.TryGetValue(itemControllerHash, out var value))
		{
			_iconPool.Release(value);
			_activeIcons.Remove(itemControllerHash);
		}
	}

	private void ReleaseAllIcons()
	{
		foreach (ItemWarningIcon value in _activeIcons.Values)
		{
			_iconPool.Release(value);
		}
		_activeIcons.Clear();
	}

	public void RefreshCurrentBuildingWarningIcons()
	{
		BuildingManager instance = InstanceBehavior<BuildingManager>.Instance;
		if (!instance)
		{
			return;
		}
		List<ItemController> allItemControllers = instance.allItemControllers;
		if (allItemControllers == null)
		{
			return;
		}
		foreach (ItemController item in allItemControllers)
		{
			UpdateWarningIcon(item);
		}
	}

	private (Sprite icon, Sprite background, bool showPointer) GetIconVisuals(WarningIconType iconType)
	{
		return iconType switch
		{
			WarningIconType.Info => (icon: infoIcon, background: neutralBackground, showPointer: false), 
			WarningIconType.Warning => (icon: exclamationIcon, background: orangeBackground, showPointer: false), 
			WarningIconType.Danger => (icon: exclamationIcon, background: redBackground, showPointer: false), 
			WarningIconType.LowStock => (icon: lowStockIcon, background: orangeBackground, showPointer: false), 
			WarningIconType.VeryLowStock => (icon: lowStockIcon, background: redBackground, showPointer: false), 
			WarningIconType.Action => (icon: infoIcon, background: greenBackground, showPointer: false), 
			WarningIconType.Completed => (icon: checkmarkIcon, background: greenBackground, showPointer: false), 
			WarningIconType.FoodDelivery => (icon: InstanceBehavior<GlobalReferences>.Instance.foodDeliveryJobConfig.MapIcon, background: neutralBackground, showPointer: true), 
			_ => (icon: null, background: null, showPointer: false), 
		};
	}
}
