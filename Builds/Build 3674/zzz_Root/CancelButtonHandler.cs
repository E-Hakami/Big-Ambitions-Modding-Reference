using BlueprintsUI;
using Buildings.Indoors.InteriorDesign;
using Character.Customization;
using IngameDebugConsole;
using Player.HUD.ItemInfoOverlays;
using PlayerActivity;
using Scenes.MainMenu;
using TMPro;
using UI;
using UI.CustomUI;
using UI.Dialog;
using UI.Elements;
using UI.InteriorDesigner;
using UI.MergeCargo;
using UI.MiniMenu;
using UI.Notification;
using UI.PlayerHUD;
using UI.Purchase;
using UI.PurchaseVehicle;
using UI.Smartphone;
using UI.Smartphone.Apps.Feedback;
using UI.Topbar;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CancelButtonHandler
{
	private static PlayerHUD PlayerHud => InstanceBehavior<UIs>.Instance.playerHUD;

	public static void HandleEscapeClick()
	{
		if (HasInputSelectedConsumedClick())
		{
			return;
		}
		if (PurchaseVehicleUI.runningShowcaseAnimation)
		{
			PlayerHud.purchaseVehicleUI.CancelShowcaseAnimation();
			return;
		}
		if (HudConfirm.isOpen)
		{
			HudConfirm.onClose?.Invoke();
			return;
		}
		if ((bool)InstanceBehavior<UIs>.Instance.miniMenuUI && MiniMenu.IsOpen)
		{
			InstanceBehavior<UIs>.Instance.miniMenuUI.Toggle(show: false);
			return;
		}
		FullMenu fullMenu = InstanceBehavior<UIs>.Instance.fullMenu;
		if ((object)fullMenu != null && fullMenu.schedule?.scheduleConfirm?.gameObject.activeInHierarchy == true)
		{
			InstanceBehavior<UIs>.Instance.fullMenu.schedule.scheduleConfirm.ClickCancel();
			return;
		}
		CustomColorPicker customColorPicker = InstanceBehavior<UIs>.Instance.customColorPicker;
		if ((bool)customColorPicker && customColorPicker.gameObject.activeInHierarchy)
		{
			customColorPicker.Close();
			return;
		}
		SignAppearance signAppearance = InstanceBehavior<UIs>.Instance.draggableWindows.signAppearance;
		if ((bool)signAppearance && signAppearance.IsOpen)
		{
			signAppearance.Close();
			return;
		}
		if (DebugLogManager.Instance.IsLogWindowVisible)
		{
			DebugLogManager.Instance.HideLogWindow();
			return;
		}
		if ((bool)InstanceBehavior<HelpSystem>.Instance && InstanceBehavior<HelpSystem>.Instance.container.activeInHierarchy)
		{
			InstanceBehavior<HelpSystem>.Instance.Toggle(show: false);
			return;
		}
		if ((bool)InstanceBehavior<Feedback>.Instance && Feedback.IsOpen)
		{
			InstanceBehavior<Feedback>.Instance.Toggle(show: false);
			return;
		}
		if ((bool)InstanceBehavior<UIs>.Instance.notificationsListUI && InstanceBehavior<UIs>.Instance.notificationsListUI.isVisible)
		{
			InstanceBehavior<UIs>.Instance.notificationsListUI.Toggle(show: false);
			return;
		}
		if ((bool)InstanceBehavior<UIs>.Instance.previewTerminalUI && PreviewTerminalUI.IsVisible)
		{
			InstanceBehavior<UIs>.Instance.previewTerminalUI.HideTerminal();
			return;
		}
		Topbar topBar = InstanceBehavior<UIs>.Instance.topBar;
		if ((object)topBar != null && topBar.playerDancesUI?.gameObject?.activeSelf == true)
		{
			InstanceBehavior<UIs>.Instance.topBar.playerDancesUI.gameObject.SetActive(value: false);
		}
		else if ((bool)InstanceBehavior<UIs>.Instance.itemsList && InstanceBehavior<UIs>.Instance.itemsList.gameObject.activeInHierarchy)
		{
			InstanceBehavior<UIs>.Instance.itemsList.Toggle(newState: false);
		}
		else if ((bool)InstanceBehavior<UIs>.Instance.rivalEmployeesUi && InstanceBehavior<UIs>.Instance.rivalEmployeesUi.gameObject.activeInHierarchy)
		{
			InstanceBehavior<UIs>.Instance.rivalEmployeesUi.Toggle(newState: false);
		}
		else if ((bool)InstanceBehavior<UIs>.Instance.timeMachine && InstanceBehavior<UIs>.Instance.timeMachine.isRunning)
		{
			InstanceBehavior<UIs>.Instance.timeMachine.CancelTimeMachine();
		}
		else
		{
			if (HasClosedFullMenu())
			{
				return;
			}
			if (BuildingPreview.isPreviewing && (bool)InstanceBehavior<UIs>.Instance.buildingPreview)
			{
				InstanceBehavior<UIs>.Instance.buildingPreview.CancelPreview();
			}
			else if (Options.IsVisible)
			{
				if (!LoadingSpinner.isLoading)
				{
					InstanceBehavior<UIs>.Instance.miniMenuUI?.CloseOptions();
				}
			}
			else
			{
				if (HasClosedBlueprintPanel())
				{
					return;
				}
				if ((bool)InstanceBehavior<UIs>.Instance.buildingResume?.CityBuildingController)
				{
					InstanceBehavior<UIs>.Instance.buildingResume.Close();
				}
				else if (CityMap.IsOpen)
				{
					InstanceBehavior<CityManager>.Instance.cityMap.Toggle();
				}
				else if (InstanceBehavior<OverlayManager>.Instance.IsDetailedOverlayActive)
				{
					InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
				}
				else
				{
					if (HasClosedPlayerHudElement())
					{
						return;
					}
					if (InteriorDesignerUI.IsOpen)
					{
						InteriorDesignerUI.OnEscapeClick?.Invoke();
					}
					else
					{
						if (PlacementHelper.CancelPlacementModeIfIsActive() || HasConsumedPlayerActivity())
						{
							return;
						}
						if (PurchaseUI.IsPanelOpen)
						{
							PurchaseUI purchaseUI = PlayerHud.purchaseUI;
							if ((object)purchaseUI != null && purchaseUI.cancelButton?.interactable == true)
							{
								PlayerHud.purchaseUI.Close();
								return;
							}
						}
						if (PurchaseVehicleUI.IsPanelOpen && (bool)PlayerHud.purchaseVehicleUI)
						{
							PlayerHud.purchaseVehicleUI.Close();
							return;
						}
						ChangeCharacterClothesUI changeCharacterClothesUI = InstanceBehavior<UIs>.Instance.changeCharacterClothesUI;
						if ((object)changeCharacterClothesUI != null && changeCharacterClothesUI.gameObject?.activeInHierarchy == true)
						{
							InstanceBehavior<UIs>.Instance.changeCharacterClothesUI.Hide();
							return;
						}
						ChangeCharacterHairUI changeCharacterHairUI = InstanceBehavior<UIs>.Instance.changeCharacterHairUI;
						if ((object)changeCharacterHairUI != null && changeCharacterHairUI.gameObject?.activeInHierarchy == true)
						{
							InstanceBehavior<UIs>.Instance.changeCharacterHairUI.Hide();
							return;
						}
						PlasticSurgeryUI plasticSurgeryUI = InstanceBehavior<UIs>.Instance.plasticSurgeryUI;
						if ((object)plasticSurgeryUI != null && plasticSurgeryUI.gameObject?.activeInHierarchy == true)
						{
							InstanceBehavior<UIs>.Instance.plasticSurgeryUI.Hide();
						}
						else if (!MopController.HandleEscape())
						{
							InstanceBehavior<UIs>.Instance.miniMenuUI?.Toggle();
						}
					}
				}
			}
		}
	}

	private static bool HasConsumedPlayerActivity()
	{
		if (!InstanceBehavior<UIs>.Instance.playerActivityUI)
		{
			return false;
		}
		if (!PlayerActivityUI.IsPanelOpen || PlayerActivityUI.IsWaiting)
		{
			return false;
		}
		if (PlayerActivityUI.IsMovingTowardsActivity)
		{
			InstanceBehavior<UIs>.Instance.playerActivityUI.CancelActivityMovement();
			return true;
		}
		InstanceBehavior<UIs>.Instance.playerActivityUI.OnPlayerActionPressed(PlayerAction.Cancel);
		return true;
	}

	private static bool HasClosedPlayerHudElement()
	{
		ManageCargoUi manageCargoUI = PlayerHud.manageCargoUI;
		if ((object)manageCargoUI != null && manageCargoUI.isPanelOpen)
		{
			PlayerHud.manageCargoUI.Close();
		}
		else
		{
			DialogUI dialogUI = PlayerHud.dialogUI;
			if ((object)dialogUI != null && dialogUI.isPanelOpen)
			{
				PlayerHud.dialogUI.OnCancel();
			}
			else
			{
				JobOffer jobOfferPanel = PlayerHud.jobOfferPanel;
				if ((object)jobOfferPanel == null || !jobOfferPanel.isPanelOpen)
				{
					return false;
				}
				PlayerHud.jobOfferPanel.Cancel();
			}
		}
		return true;
	}

	private static bool HasClosedBlueprintPanel()
	{
		if (!BlueprintsPanel.IsOpen || !(InstanceBehavior<UIs>.Instance.miniMenuUI?.blueprintsPanel))
		{
			return false;
		}
		if (InstanceBehavior<UIs>.Instance.miniMenuUI.blueprintsPanel.IsWorkshopConfirmOpen)
		{
			InstanceBehavior<UIs>.Instance.miniMenuUI.blueprintsPanel.CloseWorkshopConfirm();
		}
		else if (InstanceBehavior<UIs>.Instance.miniMenuUI.blueprintsPanel.IsBlueprintInfoOpen)
		{
			InstanceBehavior<UIs>.Instance.miniMenuUI.blueprintsPanel.CloseBlueprintInfo();
		}
		else
		{
			InstanceBehavior<UIs>.Instance.miniMenuUI.CloseBlueprints();
		}
		return true;
	}

	private static bool HasClosedFullMenu()
	{
		if (!FullMenu.IsOpen || !InstanceBehavior<UIs>.Instance.fullMenu)
		{
			return false;
		}
		EmployeePresetCustomizer employeePresetCustomizer = InstanceBehavior<UIs>.Instance.employeePresetCustomizer;
		if ((object)employeePresetCustomizer != null && employeePresetCustomizer.gameObject.activeSelf)
		{
			InstanceBehavior<UIs>.Instance.employeePresetCustomizer.Close();
		}
		else
		{
			BizManBusiness business = InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business;
			if ((object)business != null && business.hrHrManagerPlanUI.IsAssignEmployeesListOpen)
			{
				InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.hrHrManagerPlanUI.CloseAssignEmployeesList();
			}
			else
			{
				BizManBusiness business2 = InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business;
				if ((object)business2 != null && business2.bizManSettings.HasUnsavedChanges)
				{
					if (string.IsNullOrEmpty(InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.buildingRegistration.BusinessName))
					{
						Notifications.ShowError("bizman_settings_notification_name_empty");
						return true;
					}
					HudConfirm.Show(null, "change_character_clothes_unsaved_changes_warning", delegate
					{
						InstanceBehavior<UIs>.Instance.fullMenu.Toggle(show: false);
					});
				}
				else if (InstanceBehavior<UIs>.Instance.scheduleEmployeeSelection.IsOpen)
				{
					InstanceBehavior<UIs>.Instance.scheduleEmployeeSelection.Hide();
				}
				else if (!InstanceBehavior<UIs>.Instance.fullMenu.schedule.HandleEscapeClick() && !InstanceBehavior<UIs>.Instance.fullMenu.econoView.HandleEscapeClick())
				{
					InstanceBehavior<UIs>.Instance.fullMenu.Toggle(show: false);
				}
			}
		}
		return true;
	}

	private static bool HasInputSelectedConsumedClick()
	{
		if (!GameManager.HasInputSelected())
		{
			return false;
		}
		GameObject currentSelectedGameObject = EventSystem.current.currentSelectedGameObject;
		if (!currentSelectedGameObject)
		{
			return false;
		}
		if (!currentSelectedGameObject.activeInHierarchy || (bool)currentSelectedGameObject.GetComponent<Button>() || (bool)currentSelectedGameObject.GetComponent<Toggle>())
		{
			ClearSelection();
			return false;
		}
		UI.Elements.Dropdown componentInParent = currentSelectedGameObject.GetComponentInParent<UI.Elements.Dropdown>();
		if ((bool)componentInParent)
		{
			componentInParent.HideOptions();
			ClearSelection();
		}
		else if ((bool)currentSelectedGameObject.GetComponent<TMP_InputField>())
		{
			ClearSelection();
		}
		return true;
	}

	private static void ClearSelection()
	{
		EventSystem.current.SetSelectedGameObject(null);
	}
}
