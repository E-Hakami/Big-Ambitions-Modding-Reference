using System.Collections.Generic;
using Extensions;
using Items.SpecialItems;
using Localizor.LanguageChangeEvent;
using PlayerActivity;
using PlayerActivity.Tennis;
using UI;
using UI.Notification;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Player.HUD.ItemInfoOverlays;

public class CustomizableButtonsOverlay : IOverlay
{
	[Header("Buttons Overlay")]
	[SerializeField]
	private Button buttonTemplate;

	private readonly List<Button> _buttons = new List<Button>();

	public override bool IsValid(EntityController entityController)
	{
		if (!(entityController is ComputerController) && !(entityController is GolfPlatformController) && !(entityController is TennisInteractionNpc) && !(entityController is TennisCourt))
		{
			return entityController is UniformLockerController;
		}
		return true;
	}

	public override bool ShouldShow(EntityController entityController)
	{
		if (!PlayerActivityUI.CanStartActivity(showNotification: false))
		{
			return false;
		}
		if ((entityController is GolfPlatformController || entityController is TennisInteractionNpc || entityController is TennisCourt { LinkedInteractionNpc: not null }) ? true : false)
		{
			return InstanceBehavior<UIs>.Instance.playerActivityUI.GetCurrentActivity == null;
		}
		if (entityController is ComputerController computerController)
		{
			if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness && InstanceBehavior<BuildingManager>.Instance.buildingRegistration?.BuildingCached?.BuildingType == "ba:buildingtype_residential")
			{
				return !ItemHelper.HasAnyMissingRequirements(computerController.ItemInstance);
			}
			return false;
		}
		if (entityController is UniformLockerController uniformLockerController)
		{
			return uniformLockerController.CanAssignUniforms();
		}
		return false;
	}

	public override void UpdateOverlay(EntityController entityController)
	{
		ClearButtons();
		if (entityController is ComputerController)
		{
			AddButton(LanguageChangeEventDataHolder.Create("playpanel_headline"), PlayGameClicked);
			AddButton(LanguageChangeEventDataHolder.Create("pass_the_time"), PassTimeClicked);
		}
		else if (entityController is GolfPlatformController golfPlatformController)
		{
			object arguments = new
			{
				fee = golfPlatformController.GetCourse().playFee.ToShortCurrencyFormat()
			};
			AddButton(LanguageChangeEventDataHolder.Create("play_golf", arguments), PlayGameClicked);
			AddButton(LanguageChangeEventDataHolder.Create("pass_the_time"), PassTimeClicked);
		}
		else if (entityController is TennisInteractionNpc || entityController is TennisCourt)
		{
			object arguments2 = new
			{
				fee = 50f.ToShortCurrencyFormat()
			};
			AddButton(LanguageChangeEventDataHolder.Create("ba:play_tennis", arguments2), PlayGameClicked);
			AddButton(LanguageChangeEventDataHolder.Create("pass_the_time"), PassTimeClicked);
		}
		else if (entityController is UniformLockerController)
		{
			AddButton(LanguageChangeEventDataHolder.Create("change_character_clothes_title"), ChangeClothesClicked);
			AddButton(LanguageChangeEventDataHolder.Create("uniform_locker_assign_uniforms"), AssignUniformsClicked);
		}
	}

	private void ClearButtons()
	{
		foreach (Button button in _buttons)
		{
			Object.Destroy(button.gameObject);
		}
		_buttons.Clear();
	}

	private void AddButton(LanguageChangeEventDataHolder data, UnityAction onClick)
	{
		Button button = Object.Instantiate(buttonTemplate, buttonTemplate.transform.parent);
		button.GetComponentInChildren<TextLocalizationComponent>().SetData(data);
		button.onClick.AddListener(onClick);
		button.gameObject.SetActive(value: true);
		_buttons.Add(button);
	}

	private void PlayGameClicked()
	{
		if (!PlayerActivityUI.CanStartActivity())
		{
			return;
		}
		if (linkedController is ComputerController computerController)
		{
			computerController.MoveToStartVideoGame();
		}
		else
		{
			EntityController entityController = linkedController;
			GolfPlatformController golfPlatformController = entityController as GolfPlatformController;
			if ((object)golfPlatformController != null)
			{
				if (golfPlatformController.HasNpc)
				{
					Notifications.ShowError("ba:notification_golf_station_occupied");
				}
				else
				{
					golfPlatformController.MoveTowardsEntity(delegate
					{
						golfPlatformController.StartGolfGame();
					});
				}
			}
			else if (linkedController is TennisInteractionNpc tennisInteractionNpc)
			{
				tennisInteractionNpc.court.StartGame(tennisInteractionNpc, automated: false);
			}
			else if (linkedController is TennisCourt tennisCourt)
			{
				tennisCourt.StartGame(tennisCourt.LinkedInteractionNpc, automated: false);
			}
		}
		InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
	}

	private void PassTimeClicked()
	{
		if (linkedController is ComputerController computerController)
		{
			computerController.PerformActivity();
		}
		else if (linkedController is TennisInteractionNpc tennisInteractionNpc)
		{
			tennisInteractionNpc.PerformActivity();
		}
		else if (linkedController is TennisCourt tennisCourt)
		{
			tennisCourt.PerformActivity();
		}
		else if (linkedController is GolfPlatformController golfPlatformController)
		{
			if (golfPlatformController.HasNpc)
			{
				Notifications.ShowError("ba:notification_golf_station_occupied");
			}
			else
			{
				golfPlatformController.PerformActivity();
			}
		}
		InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
	}

	private void ChangeClothesClicked()
	{
		if (linkedController is UniformLockerController uniformLockerController)
		{
			uniformLockerController.PerformAction();
		}
		InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
	}

	private void AssignUniformsClicked()
	{
		if (linkedController is UniformLockerController uniformLockerController)
		{
			uniformLockerController.AssignUniforms();
		}
		InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
	}
}
