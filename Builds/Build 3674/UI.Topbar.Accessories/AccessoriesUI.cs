using BigAmbitions.Characters;
using JimmysUnityUtilities;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Topbar.Accessories;

public class AccessoriesUI : MonoBehaviour
{
	[SerializeField]
	private Image accessoriesButtonBackgroundImage;

	[SerializeField]
	private Sprite openBackgroundSprite;

	[SerializeField]
	private Sprite closedBackgroundSprite;

	[SerializeField]
	private Image accessoriesButtonImage;

	[SerializeField]
	private GameObject accessoriesPanel;

	[SerializeField]
	private Image handAccessoryImage;

	[SerializeField]
	private Sprite handAccessoryDefaultSprite;

	[SerializeField]
	private Image headAccessoryImage;

	[SerializeField]
	private Sprite headAccessoryDefaultSprite;

	[SerializeField]
	private Image phoneAccessoryImage;

	[SerializeField]
	private Sprite phoneAccessoryDefaultSprite;

	[SerializeField]
	private Image handAccessoryVisibleImage;

	[SerializeField]
	private Image headAccessoryVisibleImage;

	[SerializeField]
	private Sprite accessoryVisibleSprite;

	[SerializeField]
	private Sprite accessoryNotVisibleSprite;

	public void OnAccessoriesButtonClick()
	{
		AccessoriesData accessoriesData = SaveGameManager.Current.accessoriesData;
		accessoriesData.isPanelOpen = !accessoriesData.isPanelOpen;
		UpdatePanelVisibility(accessoriesData.isPanelOpen);
	}

	public void OnHandAccessoryButtonClick()
	{
		if (SaveGameManager.Current.accessoriesData.handAccessoryCargoInstance != null)
		{
			if (InstanceBehavior<UIs>.Instance.playerActivityUI.GetCurrentActivity != null)
			{
				Notifications.ShowError("ba:notification_cannot_do_action_during_activity", null, trackOnSaveGame: false);
			}
			else
			{
				InstanceBehavior<GameManager>.Instance.playerController.UnEquipAccessoryOfType(AccessoryType.Hand);
			}
		}
	}

	public void OnHeadAccessoryButtonClick()
	{
		if (SaveGameManager.Current.accessoriesData.headAccessoryCargoInstance != null)
		{
			InstanceBehavior<GameManager>.Instance.playerController.UnEquipAccessoryOfType(AccessoryType.Head);
		}
	}

	public void OnPhoneAccessoryButtonClick()
	{
		if (SaveGameManager.Current.accessoriesData.phoneAccessoryCargoInstance != null)
		{
			InstanceBehavior<GameManager>.Instance.playerController.UnEquipAccessoryOfType(AccessoryType.Phone);
		}
	}

	public void OnHandAccessoryVisibleButtonClick()
	{
		AccessoriesData accessoriesData = SaveGameManager.Current.accessoriesData;
		accessoriesData.handAccessoryVisible = !accessoriesData.handAccessoryVisible;
		UpdateHandAccessoryVisibilityImage(accessoriesData.handAccessoryVisible);
		if (accessoriesData.handAccessoryCargoInstance != null)
		{
			if (accessoriesData.handAccessoryVisible)
			{
				InstanceBehavior<GameManager>.Instance.playerController.ShowHandAccessoryVisualsIfRequired();
			}
			else
			{
				InstanceBehavior<GameManager>.Instance.playerController.HideAccessoryVisuals(AccessoryType.Hand);
			}
		}
	}

	public void OnHeadAccessoryVisibleButtonClick()
	{
		AccessoriesData accessoriesData = SaveGameManager.Current.accessoriesData;
		accessoriesData.headAccessoryVisible = !accessoriesData.headAccessoryVisible;
		UpdateHeadAccessoryVisibilityImage(accessoriesData.headAccessoryVisible);
		if (accessoriesData.headAccessoryCargoInstance != null)
		{
			if (accessoriesData.headAccessoryVisible)
			{
				InstanceBehavior<GameManager>.Instance.playerController.ShowAccessoryVisuals(AccessoryType.Head);
			}
			else
			{
				InstanceBehavior<GameManager>.Instance.playerController.HideAccessoryVisuals(AccessoryType.Head);
			}
		}
	}

	public void UpdatePanelVisibility(bool isPanelOpen)
	{
		accessoriesButtonBackgroundImage.sprite = (isPanelOpen ? openBackgroundSprite : closedBackgroundSprite);
		float alpha = (isPanelOpen ? 0.2f : 1f);
		accessoriesButtonImage.SetAlpha(alpha);
		accessoriesPanel.SetActive(isPanelOpen);
	}

	public void UpdateHandAccessoryPanel(AccessoriesData data)
	{
		handAccessoryImage.sprite = ((data.handAccessoryCargoInstance == null) ? handAccessoryDefaultSprite : data.handAccessoryCargoInstance.ItemCached.accessoryIcon);
		handAccessoryImage.SetAlpha((data.handAccessoryCargoInstance == null) ? 0.195f : 1f);
		UpdateHandAccessoryVisibilityImage(data.handAccessoryVisible);
	}

	public void UpdateHandAccessoryVisibilityImage(bool handAccessoryVisible)
	{
		handAccessoryVisibleImage.sprite = (handAccessoryVisible ? accessoryVisibleSprite : accessoryNotVisibleSprite);
		handAccessoryVisibleImage.SetAlpha(handAccessoryVisible ? 1f : 0.275f);
	}

	public void UpdateHeadAccessoryPanel(AccessoriesData data)
	{
		headAccessoryImage.sprite = ((data.headAccessoryCargoInstance == null) ? headAccessoryDefaultSprite : data.headAccessoryCargoInstance.ItemCached.accessoryIcon);
		headAccessoryImage.SetAlpha((data.headAccessoryCargoInstance == null) ? 0.195f : 1f);
		UpdateHeadAccessoryVisibilityImage(data.headAccessoryVisible);
	}

	private void UpdateHeadAccessoryVisibilityImage(bool headAccessoryVisible)
	{
		headAccessoryVisibleImage.sprite = (headAccessoryVisible ? accessoryVisibleSprite : accessoryNotVisibleSprite);
		headAccessoryVisibleImage.SetAlpha(headAccessoryVisible ? 1f : 0.275f);
	}

	public void UpdatePhoneAccessoryPanel(AccessoriesData data)
	{
		phoneAccessoryImage.sprite = ((data.phoneAccessoryCargoInstance == null) ? phoneAccessoryDefaultSprite : data.phoneAccessoryCargoInstance.ItemCached.accessoryIcon);
		phoneAccessoryImage.SetAlpha((data.phoneAccessoryCargoInstance == null) ? 0.195f : 1f);
	}

	public void UpdateUI(AccessoriesData data)
	{
		UpdatePanelVisibility(data.isPanelOpen);
		UpdateHandAccessoryPanel(data);
		UpdateHeadAccessoryPanel(data);
		UpdatePhoneAccessoryPanel(data);
	}
}
