using System.Collections.Generic;
using BigAmbitions.Characters;
using BigAmbitions.Characters.Appearance;
using Buildings.BuildingTypes.Special;
using Dialogs;
using Helpers;
using PlayerActivity;
using RoboRyanTron.SearchableEnum;
using UI;
using UI.Notification;
using UnityEngine;

public class CasinoGameController : ItemController
{
	[Header("Casino Game Properties")]
	public PlaySpotsManager playSpotsManager;

	public CasinoGameEmployeeController casinoGameEmployeeController;

	[SerializeField]
	private Transform employeePosition;

	[SerializeField]
	private string employeeSkill;

	[SerializeField]
	[SearchableEnum]
	private CallDialogType callDialogType;

	[SerializeField]
	[SearchableEnum]
	private PermanentAnimationType playingGameAnimationType;

	[SerializeField]
	private string notificationKeyWhenGameIsFull;

	[SerializeField]
	private PlayerActivityBalanceConfig gambledBalanceConfig;

	[HideInInspector]
	public ThirdPersonCharacter employee;

	private bool _subscribedToPlayerNavigationChanges;

	private bool _isPlayerPlaying;

	private PlayerController _playerController;

	public PlayerActivityBalanceConfig GambledBalanceConfig => gambledBalanceConfig;

	public override bool ShouldReactToIoEnter()
	{
		return true;
	}

	public override void Start()
	{
		base.Start();
		if ((object)employee == null)
		{
			employee = PrefabHelper.CreatePrefab<ThirdPersonCharacter>("Characters/HumanDefinitionLow", base.transform);
		}
		employee.gameObject.SetActive(value: true);
		employee.appearanceSetter.SetRandomAppearance();
		List<AppearanceElementData> uniformElements = EmployeeHelper.GetUniformElements(new List<string> { employeeSkill }, employee.appearanceSetter.data.gender);
		if (uniformElements != null)
		{
			employee.appearanceSetter.UpdateElements(uniformElements);
		}
		employee.ForceToTransform(employeePosition);
		casinoGameEmployeeController.SetEmployeeTpc(employee);
		playSpotsManager.Init();
		_playerController = InstanceBehavior<GameManager>.Instance?.playerController;
	}

	public override bool Interact()
	{
		if (base.Interact())
		{
			playSpotsManager.ReleasePlayerSpot();
			return true;
		}
		if (PlayerHelper.ItemInstanceInHands != null)
		{
			Notifications.ShowError("notification_need_empty_hands_to_interact");
			playSpotsManager.ReleasePlayerSpot();
			return false;
		}
		if (_isPlayerPlaying)
		{
			return true;
		}
		if (GetClosestNavMeshTargetPosition(_playerController.transform.position) == Vector3.zero)
		{
			return true;
		}
		Transform playerSpot = playSpotsManager.GetPlayerSpot();
		Vector3 lookTarget = playerSpot.position + playerSpot.forward;
		StartCoroutine(_playerController.Character.MoveToPosition(lookTarget, playerSpot.position, 0.25f, rotateToLookTarget: true));
		_playerController.Character.LinkToPointAndClickObject(this);
		InstanceBehavior<UIs>.Instance.playerHUD.dialogUI.ShowDialog(callDialogType, NavigationBlocker.CasinoGame);
		_isPlayerPlaying = true;
		return true;
	}

	public override Vector3 GetClosestNavMeshTargetPosition(Vector3 entityPosition)
	{
		Transform playerSpot = playSpotsManager.GetPlayerSpot();
		if (playerSpot != null)
		{
			return playerSpot.position;
		}
		if (!playSpotsManager.IsAnySpotAvailableForPlayer())
		{
			Notifications.ShowError(notificationKeyWhenGameIsFull);
			return Vector3.zero;
		}
		return playSpotsManager.GetClosestPlaySpot(entityPosition);
	}

	public override bool OnIoLeftClick()
	{
		if (base.OnIoLeftClick())
		{
			if (!_subscribedToPlayerNavigationChanges)
			{
				_playerController.PlayerChangedNavigation.AddListener(OnPlayerChangedNavigation);
				_subscribedToPlayerNavigationChanges = true;
			}
			return true;
		}
		return false;
	}

	public void ShowPlayingAnimation()
	{
		_playerController.Character.animator.SetBool(playingGameAnimationType);
	}

	public void ShowVictoryAnimation()
	{
		_playerController.Character.animator.RunAnimationLength(AnimationType.VictoryStanding);
	}

	public void ShowDefeatAnimation()
	{
		_playerController.Character.animator.RunAnimationLength(AnimationType.DefeatStanding);
	}

	private void OnPlayerChangedNavigation()
	{
		playSpotsManager.ReleasePlayerSpot();
		UnsubscribePlayerChangedNavigationListener();
	}

	public void OnExitGame()
	{
		playSpotsManager.ReleasePlayerSpot();
		_playerController.Character.animator.SetBool(playingGameAnimationType, state: false);
		_isPlayerPlaying = false;
		UnsubscribePlayerChangedNavigationListener();
	}

	private void OnDisable()
	{
		UnsubscribePlayerChangedNavigationListener();
		_isPlayerPlaying = false;
	}

	private void UnsubscribePlayerChangedNavigationListener()
	{
		InstanceBehavior<GameManager>.Instance?.playerController.PlayerChangedNavigation.RemoveListener(OnPlayerChangedNavigation);
		_subscribedToPlayerNavigationChanges = false;
	}
}
