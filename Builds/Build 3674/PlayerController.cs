using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.DayNightCycle;
using BigAmbitions.InputSystem;
using BigAmbitions.Items;
using BigAmbitions.PlacementSystem;
using BigAmbitions.SaveSystem;
using BigAmbitions.Tags;
using Buildings;
using Buildings.Indoors.InteriorDesign;
using Character;
using Extensions;
using GleyTrafficSystem;
using Helpers;
using IngameDebugConsole;
using JimmysUnityUtilities;
using Localizor;
using Player.HUD.ItemInfoOverlays;
using PlayerActivity;
using UI;
using UI.Load;
using UI.Notification;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Rendering.HighDefinition;

public class PlayerController : MonoBehaviour
{
	private const int MinutesToGetWet = 5;

	private const float RainCoverCheckDistance = 25f;

	private const float RainCoverCheckOriginHeight = 1.6f;

	public static bool runByDefaultIndoors;

	public static NavMeshQueryFilter navMeshQueryFilter;

	public ThirdPersonCharacter Character;

	[Range(0f, 0.01f)]
	[Tooltip("The minimum distance a move target should be to actually make the character move towards it")]
	[SerializeField]
	private float minimumDistanceToTriggerMove = 0.1f;

	[SerializeField]
	private PlayerActivityBalanceConfig wetBalanceConfig;

	[SerializeField]
	private LayerMask rainCoverLayerMask;

	[SerializeField]
	private float heldItemToConsumeMaxLifetime;

	public UnityEvent PlayerNavigationWhileDisabled = new UnityEvent();

	public UnityEvent PlayerChangedNavigation = new UnityEvent();

	public UnityEvent OnPlayerOutOfEnergy = new UnityEvent();

	private ParticleSystem.EmitParams _emitParams;

	private UnityAction _goalAction;

	private Vector3 _goalPosition;

	private bool _hasGoal;

	private bool _hasPath;

	private Vector3 _inputVelocity;

	private Vector3 _lastSafeTarget = Vector3.positiveInfinity;

	private PointOfInterest _poi;

	private bool _spawnParticle;

	[NonSerialized]
	public bool awaitingRepositioning = true;

	private bool _isHoldingHandAccessory;

	private Timestamp _timeToGetWet = new Timestamp();

	private bool _isCheckingIfPlayerIsWet;

	private readonly HashSet<NavigationBlocker> _activeNavigationBlockers = new HashSet<NavigationBlocker>();

	private readonly HashSet<VehicleNavigationBlocker> _activeVehicleNavigationBlockers = new HashSet<VehicleNavigationBlocker>();

	private GameObject _heldItemToConsume;

	private float _heldItemToConsumeTimer;

	public bool NavigationDisabled
	{
		get
		{
			if (_activeNavigationBlockers.Count <= 0)
			{
				return DebugLogManager.Instance.IsLogWindowVisible;
			}
			return true;
		}
	}

	public bool VehicleNavigationDisabled
	{
		get
		{
			if (_activeVehicleNavigationBlockers.Count <= 0)
			{
				return DebugLogManager.Instance.IsLogWindowVisible;
			}
			return true;
		}
	}

	public bool hasOnGoalReachedAction => _goalAction != null;

	private void Awake()
	{
		InputHelper.SetupPlayerInput();
		Character = GetComponent<ThirdPersonCharacter>();
		navMeshQueryFilter.agentTypeID = Character.navmeshAgent.agentTypeID;
		navMeshQueryFilter.areaMask = -1;
		_emitParams = default(ParticleSystem.EmitParams);
		Character.InitPlayerExpressionsQueue();
		ConvertTemporalBoostsToRegularBoosts();
	}

	private void Start()
	{
		GlobalEvents.onSaveGame = (Action)Delegate.Combine(GlobalEvents.onSaveGame, new Action(PlayerHelper.SaveCurrentPosition));
		GameInstance current = SaveGameManager.Current;
		if (current.charactersData == null)
		{
			current.charactersData = new List<CharacterData>
			{
				new CharacterData
				{
					ageInDays = TimeHelper.GetDaysByYears(SaveGameManager.Current.gameVariables.startingAge),
					color = SkinColorHelper.GetRandom().color,
					gender = Gender.Male
				}
			};
		}
		Character.appearanceSetter.data = SaveGameManager.Current.charactersData.First();
		if (PlayerHelper.IsHoldingItem)
		{
			PlayerHelper.ItemInstanceInHands.AddCallToOnItemsInCargoUpdated(PlayerHelper.OnItemInHandsCargoUpdated);
		}
		runByDefaultIndoors = PlayerPrefSettings.RunByDefaultIndoors;
		PlacementSystem.onItemPlaced = (Action)Delegate.Combine(PlacementSystem.onItemPlaced, new Action(OnPlacementSystemItemPlaced));
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool open)
		{
			_poi.SetHidden(!open);
		});
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, (Action<Address>)delegate
		{
			_poi.target = base.transform;
		});
		EnergyHelper.AddEnergySpender("living", EnergyConsumption.Minimal);
		PlayerHelper.playerDead = false;
		GlobalEvents.RegisterOnGameLoadedCallback(Init);
	}

	private void InitAccessories()
	{
		if (SaveGameManager.Current.accessoriesData.handAccessoryCargoInstance != null)
		{
			GlobalEvents.onAccessoryEquipped?.Invoke(SaveGameManager.Current.accessoriesData.handAccessoryCargoInstance);
		}
		if (SaveGameManager.Current.accessoriesData.headAccessoryCargoInstance != null)
		{
			GlobalEvents.onAccessoryEquipped?.Invoke(SaveGameManager.Current.accessoriesData.headAccessoryCargoInstance);
		}
		if (SaveGameManager.Current.accessoriesData.phoneAccessoryCargoInstance != null)
		{
			GlobalEvents.onAccessoryEquipped?.Invoke(SaveGameManager.Current.accessoriesData.phoneAccessoryCargoInstance);
		}
		if (SaveGameManager.Current.accessoriesData.headAccessoryCargoInstance != null && SaveGameManager.Current.accessoriesData.headAccessoryVisible)
		{
			ShowAccessoryVisuals(AccessoryType.Head);
		}
		if (SaveGameManager.Current.accessoriesData.phoneAccessoryCargoInstance != null)
		{
			ShowAccessoryVisuals(AccessoryType.Phone);
		}
		ShowHandAccessoryVisualsIfRequired();
	}

	public void Show()
	{
		ToggleSkinnedMeshRenderers(toggle: true);
		Character.ShowHandContent();
		Character.ToggleAllAttachedObjects(toggle: true);
	}

	public void Hide()
	{
		ToggleSkinnedMeshRenderers(toggle: false);
		Character.HideHandContent();
		Character.ToggleAllAttachedObjects(toggle: false);
	}

	private void ToggleSkinnedMeshRenderers(bool toggle)
	{
		foreach (SkinnedMeshRenderer selectedMeshRenderer in Character.appearanceSetter.GetSelectedMeshRenderers())
		{
			selectedMeshRenderer.enabled = toggle;
		}
		Character.appearanceSetter.HeadMesh.enabled = toggle;
		Character.shadowCaster.enabled = toggle;
	}

	private void Update()
	{
		if (LoadScene.isLoading || EnergyHelper.goingToHospital)
		{
			if ((bool)Character)
			{
				Character.velocity = Vector3.zero;
			}
			return;
		}
		if (_hasGoal && !NavigationDisabled && (!_hasPath || Vector3.SqrMagnitude(_goalPosition - PlayerHelper.GetPosition()) < 0.16000001f))
		{
			Character.navmeshAgent.ResetPath();
			_hasPath = false;
			try
			{
				_goalAction?.Invoke();
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
			finally
			{
				RemoveGoal();
			}
		}
		if (InstanceBehavior<GameManager>.Instance.selectedVehicle == null || InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleType.spawnInPlayerObject)
		{
			if (BuildingManager.IsInsideBuilding)
			{
				if (runByDefaultIndoors)
				{
					Character.ToggleRunning(!PlayerAction.ToggleRunning.Pressing());
				}
				else
				{
					Character.ToggleRunning(PlayerAction.ToggleRunning.Pressing());
				}
			}
			else
			{
				Character.ToggleRunning(!PlayerAction.ToggleRunning.Pressing());
			}
			if (!NavigationDisabled && !GameManager.ShouldBlockKeyboardShortcuts())
			{
				HandleWasdNavigation();
			}
			else
			{
				Character.velocity = Vector3.zero;
			}
		}
		else
		{
			Character.velocity = Vector3.zero;
		}
		if (_heldItemToConsumeTimer > 0f)
		{
			_heldItemToConsumeTimer -= Time.deltaTime;
			if (_heldItemToConsumeTimer <= 0f && (bool)_heldItemToConsume)
			{
				UnityEngine.Object.Destroy(_heldItemToConsume);
			}
		}
		CheckIfPlayerIsWet();
	}

	private void OnDestroy()
	{
		PlacementSystem.onItemPlaced = (Action)Delegate.Remove(PlacementSystem.onItemPlaced, new Action(OnPlacementSystemItemPlaced));
	}

	private void CheckIfPlayerIsWet()
	{
		if (!RainHelper.AreRainDropsFalling || BuildingManager.IsInsideBuilding)
		{
			_isCheckingIfPlayerIsWet = false;
		}
		else if (!_isCheckingIfPlayerIsWet)
		{
			ResetTimeToGetWet();
			_isCheckingIfPlayerIsWet = true;
		}
		else if (PlayerHelper.IsCarryingAnUmbrella())
		{
			ResetTimeToGetWet();
		}
		else if (_timeToGetWet.IsInThePast())
		{
			if (IsUnderRainCover())
			{
				ResetTimeToGetWet();
			}
			else if (wetBalanceConfig == null)
			{
				Debug.LogError("No wet balance config assigned.");
				ResetTimeToGetWet();
			}
			else
			{
				HappinessHelper.AddModifier(wetBalanceConfig.FinalType, wetBalanceConfig.GetBoostHours(0));
				ResetTimeToGetWet();
			}
		}
	}

	private bool IsUnderRainCover()
	{
		return Physics.Raycast(base.transform.position + Vector3.up * 1.6f, Vector3.up, 25f, rainCoverLayerMask, QueryTriggerInteraction.Ignore);
	}

	private void ResetTimeToGetWet()
	{
		_timeToGetWet.SetCurrentTime();
		_timeToGetWet.AddMinutes(5f);
	}

	private void LateUpdate()
	{
		if (_spawnParticle)
		{
			_spawnParticle = false;
			InstanceBehavior<GameManager>.Instance.navigationParticleSystem.Emit(_emitParams, 1);
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.magenta;
		Gizmos.DrawCube(_lastSafeTarget, Vector3.one);
	}

	private void ConvertTemporalBoostsToRegularBoosts()
	{
		HappinessHelper.ConvertTemporalBoostsToRegularBoosts();
	}

	private void OnEnterBuilding(Address address)
	{
		RemoveGoal();
		SetEntranceDoorAsPoiTarget(address);
	}

	private void SetEntranceDoorAsPoiTarget(Address address)
	{
		InitPoi();
		if (!(_poi == null))
		{
			CityBuildingController cityBuildingController = InstanceBehavior<CityManager>.Instance.FindCityBuildingController(address);
			if (cityBuildingController == null)
			{
				_poi.target = null;
			}
			else if (cityBuildingController.building != null && cityBuildingController.building.IsHamptonsHouse())
			{
				_poi.target = base.transform;
			}
			else
			{
				_poi.target = cityBuildingController.entranceDoors[0].doorTransform.transform;
			}
		}
	}

	private void Init()
	{
		PlayerDances.Init();
		InitAccessories();
		if (PlayerHelper.IsHoldingItem)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetItemInstance(PlayerHelper.ItemInstanceInHands);
		}
		InitPoi();
		if (PlayerHelper.ShouldKillThePlayer())
		{
			PlayerHelper.KillPlayer();
		}
		GameManager.GetMainCamera().GetComponent<HDAdditionalCameraData>().volumeAnchorOverride = base.transform;
	}

	private void InitPoi()
	{
		if (!(_poi != null) && !(InstanceBehavior<CityManager>.Instance == null))
		{
			_poi = InstanceBehavior<CityManager>.Instance.cityMap.AddPoi(base.transform, InstanceBehavior<UIs>.Instance.mapFilters.playerIcon, InstanceBehavior<UIs>.Instance.mapFilters.playerFilterColor);
			_poi.SetHidden(hide: true);
		}
	}

	public void UpdateCityMapPlayerIconRotation()
	{
		if (!LoadScene.isLoading && !EnergyHelper.goingToHospital && !BuildingManager.IsInsideBuilding && !(SaveGameManager.Current.CurrentStreetName == "ba:street_parking"))
		{
			Transform transform = (VehicleHelper.IsInsideVehicle() ? VehicleHelper.GetCurrentVehicleBase().transform : base.transform);
			Vector3 position = transform.position;
			Vector3 to = GameManager.GetMainCamera().WorldToScreenPointProjected(position) - GameManager.GetMainCamera().WorldToScreenPointProjected(position + transform.forward);
			float num = Vector3.SignedAngle(Vector3.down, to, Vector3.forward);
			_poi.SetIconRotation(num + 45f);
		}
	}

	private void HandleWasdNavigation()
	{
		Vector3 inputVelocity = _inputVelocity;
		_inputVelocity = PlayerAction.Move.Vector().XYtoXZ();
		if (_inputVelocity.sqrMagnitude != 0f)
		{
			InputHelper.autoRunToggled = false;
		}
		if (inputVelocity.sqrMagnitude == 0f && _inputVelocity.sqrMagnitude != 0f)
		{
			RemoveGoal();
			InstanceBehavior<GameManager>.Instance.playerController.PlayerChangedNavigation.Invoke();
		}
		if (_inputVelocity.sqrMagnitude >= 0.05f)
		{
			_lastSafeTarget = Vector3.positiveInfinity;
			if (Character.navmeshAgent.hasPath || Character.navmeshAgent.pathPending)
			{
				Character.navmeshAgent.ResetPath();
			}
			Vector3 vector = Vector3.ProjectOnPlane(GameManager.GetMainCamera().transform.forward, Vector3.up);
			Vector3 vector2 = Vector3.Cross(vector, Vector3.up);
			Vector3 zero = Vector3.zero;
			zero += vector * _inputVelocity.z;
			zero -= vector2 * _inputVelocity.x;
			Quaternion b = Quaternion.LookRotation(zero);
			Character.transform.rotation = Quaternion.Slerp(Character.transform.rotation, b, Time.deltaTime * (Character.navmeshAgent.angularSpeed / 360f) * 4f);
			Vector3 vector3 = zero.normalized * (Character.navmeshAgent.speed * Mathf.Min(_inputVelocity.magnitude, 1f));
			Character.navmeshAgent.Move(vector3 * Time.deltaTime);
			Character.velocity = vector3;
			_hasPath = true;
		}
		else
		{
			Character.velocity = Vector3.zero;
		}
	}

	public void ResetWalkingAnimation()
	{
		if (Character.navmeshAgent.enabled)
		{
			Character.navmeshAgent.ResetPath();
			_hasPath = false;
			_lastSafeTarget = Vector3.positiveInfinity;
		}
		RemoveGoal();
		Character.animator.SetBool(BaseHuman.IsMoving, value: false);
	}

	public void SetNavigationBlocker(NavigationBlocker navigationBlocker)
	{
		if (!_activeNavigationBlockers.Add(navigationBlocker))
		{
			Debug.LogWarning($"Tried to set navigation blocker that is already set: {navigationBlocker}");
			return;
		}
		if (Application.isEditor)
		{
			Debug.Log($"Set navigation blocker: {navigationBlocker}");
		}
		if (!PlacementSystem.IsInPlacementMode)
		{
			HideAccessoryVisuals(AccessoryType.Hand);
		}
		PlayerDances.Disable();
	}

	public void UnsetNavigationBlocker(NavigationBlocker navigationBlocker)
	{
		if (!_activeNavigationBlockers.Remove(navigationBlocker))
		{
			Debug.LogWarning($"Tried to unset navigation blocker that is not set: {navigationBlocker}");
			return;
		}
		if (Application.isEditor)
		{
			Debug.Log($"Unset navigation blocker: {navigationBlocker}");
		}
		if (!NavigationDisabled)
		{
			PlayerDances.Enable();
			ShowHandAccessoryVisualsIfRequired();
		}
	}

	private void UnsetAllNavigationBlockers()
	{
		_activeNavigationBlockers.Clear();
		PlayerDances.Enable();
		ShowHandAccessoryVisualsIfRequired();
	}

	public void SetVehicleNavigationBlocker(VehicleNavigationBlocker navigationBlocker)
	{
		if (!_activeVehicleNavigationBlockers.Add(navigationBlocker))
		{
			Debug.LogWarning($"Tried to set vehicle navigation blocker that is already set: {navigationBlocker}");
			return;
		}
		if (Application.isEditor)
		{
			Debug.Log($"Set vehicle navigation blocker: {navigationBlocker}");
		}
		if (InstanceBehavior<GameManager>.Instance.selectedVehicle is CarController carController)
		{
			carController.vehicleController.input.VC_Disable(calledByParent: false);
		}
	}

	public void UnsetVehicleNavigationBlocker(VehicleNavigationBlocker navigationBlocker)
	{
		if (!_activeVehicleNavigationBlockers.Remove(navigationBlocker))
		{
			Debug.LogWarning($"Tried to unset vehicle navigation blocker that is not set: {navigationBlocker}");
			return;
		}
		if (Application.isEditor)
		{
			Debug.Log($"Unset vehicle navigation blocker: {navigationBlocker}");
		}
		if (_activeVehicleNavigationBlockers.Count == 0 && InstanceBehavior<GameManager>.Instance.selectedVehicle is CarController carController)
		{
			carController.vehicleController.input.VC_Enable(calledByParent: false);
		}
	}

	private void UnsetAllVehicleNavigationBlockers()
	{
		_activeVehicleNavigationBlockers.Clear();
		if (InstanceBehavior<GameManager>.Instance.selectedVehicle is CarController carController)
		{
			carController.vehicleController.input.VC_Enable(calledByParent: false);
		}
	}

	public void SetGoal(Vector3 position, UnityAction onReached)
	{
		if (!(position == Vector3.zero))
		{
			RemoveGoal();
			if (NavigationDisabled)
			{
				PlayerNavigationWhileDisabled.Invoke();
				return;
			}
			_hasGoal = true;
			_goalPosition = SetNewDestination(position, showParticleEffect: true, removeGoal: false);
			_goalAction = onReached;
		}
	}

	public void SetGoal(EntityController entityController, UnityAction onReached, bool checkRoute = false)
	{
		Vector3 closestNavMeshTargetPosition = entityController.GetClosestNavMeshTargetPosition(PlayerHelper.GetPosition());
		if (!checkRoute || ExistsRoute(closestNavMeshTargetPosition, showErrorNotification: true, entityController))
		{
			SetGoal(closestNavMeshTargetPosition, onReached);
		}
	}

	public bool ExistsRoute(Vector3 target, bool showErrorNotification = false, EntityController entityController = null)
	{
		if (target == Vector3.zero)
		{
			if (showErrorNotification)
			{
				ShowCannotReachItemNotification(entityController);
			}
			return false;
		}
		NavMeshPath navMeshPath = new NavMeshPath();
		Character.navmeshAgent.CalculatePath(target, navMeshPath);
		bool num = navMeshPath.status == NavMeshPathStatus.PathComplete;
		if (!num & showErrorNotification)
		{
			ShowCannotReachItemNotification(entityController);
		}
		return num;
	}

	public bool ExistsRoute(EntityController entityController, bool showErrorNotification = false)
	{
		bool flag = ExistsRoute(entityController.GetClosestNavMeshTargetPosition(PlayerHelper.GetPosition()));
		if (flag || !showErrorNotification)
		{
			return flag;
		}
		ShowCannotReachItemNotification(entityController);
		return false;
	}

	private static void ShowCannotReachItemNotification(EntityController entityController = null)
	{
		string value = ((entityController is ItemController itemController) ? itemController.itemName.Localize().ToString() : "");
		Dictionary<string, string> notificationData = new Dictionary<string, string> { { "itemname", value } };
		Notifications.Show(NotificationType.Error, "npc_expression_cannot_reach_item", notificationData);
	}

	public Vector3 SetNewDestination(Vector3 target, bool showParticleEffect, bool removeGoal = true)
	{
		if (InstanceBehavior<OverlayManager>.Instance.blockPlayerNavigation && !_hasGoal)
		{
			return target;
		}
		if (CityMap.IsOpen)
		{
			return target;
		}
		if (DebugLogManager.Instance.IsLogWindowVisible)
		{
			return target;
		}
		if (NavigationDisabled)
		{
			PlayerNavigationWhileDisabled.Invoke();
			return target;
		}
		if (_inputVelocity.sqrMagnitude > 0f)
		{
			return target;
		}
		if (NavMesh.SamplePosition(target, out var hit, 3f, -1))
		{
			if (hit.position.IsPrettyCloseTo(_lastSafeTarget, minimumDistanceToTriggerMove))
			{
				return hit.position;
			}
			_lastSafeTarget = hit.position;
			NavMeshPath path = new NavMeshPath();
			NavMesh.CalculatePath(Character.navmeshAgent.transform.position, hit.position, navMeshQueryFilter, path);
			Character.navmeshAgent.SetPath(path);
			_hasPath = true;
			if (showParticleEffect && ScreenshotController.uiIsVisible && !ScreenshotController.isInFreeLookMode)
			{
				_emitParams.position = hit.position + Vector3.up * 0.2f;
				_spawnParticle = true;
			}
			Character.navmeshAgent.SetDestination(hit.position);
			target = hit.position;
		}
		if (removeGoal)
		{
			RemoveGoal();
		}
		PlayerChangedNavigation.Invoke();
		return target;
	}

	public bool IsOnNavmesh()
	{
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		if (selectedVehicle == null)
		{
			return true;
		}
		return selectedVehicle.vehicleType.spawnInPlayerObject;
	}

	public void RemoveGoal()
	{
		_hasGoal = false;
		_goalAction = null;
		_goalPosition = Vector3.positiveInfinity;
	}

	public void ResetNavigation()
	{
		if (Character.navmeshAgent.enabled)
		{
			Character.navmeshAgent.ResetPath();
			_hasPath = false;
			_lastSafeTarget = Vector3.positiveInfinity;
		}
		RemoveGoal();
	}

	public static void SetNavAgentTypeId(int agentTypeId)
	{
		InstanceBehavior<GameManager>.Instance.playerController.Character.navmeshAgent.agentTypeID = agentTypeId;
		navMeshQueryFilter.agentTypeID = agentTypeId;
	}

	public static void Unstuck()
	{
		HashSet<NavigationBlocker> activeNavigationBlockers = InstanceBehavior<GameManager>.Instance.playerController._activeNavigationBlockers;
		Debug.Log(string.Format("{0} active navigation blocker(s) before Unstuck: {1}", activeNavigationBlockers.Count, string.Join(", ", activeNavigationBlockers)));
		if (VehicleHelper.GetCurrentVehicle() != null)
		{
			try
			{
				InstanceBehavior<GameManager>.Instance.playerController.UnsetAllVehicleNavigationBlockers();
				InstanceBehavior<GameManager>.Instance.selectedVehicle.ExitVehicle();
			}
			catch (Exception arg)
			{
				Debug.Log($"Couldn't exit from vehicle correctly: {arg}");
				SaveGameManager.Current.ActiveVehicleId = null;
				InstanceBehavior<GameManager>.Instance.playerController.Character.ToggleVisibility(show: true);
				InstanceBehavior<GameManager>.Instance.playerController.transform.SetParent(InstanceBehavior<GameManager>.Instance.transform);
				InstanceBehavior<GameManager>.Instance.playerController.UnsetAllNavigationBlockers();
			}
		}
		if (PlayerHelper.IsHoldingItem)
		{
			PlayerHelper.ItemInstanceInHands = null;
			MouseController.Reset();
		}
		if (PlacementSystem.IsInPlacementMode)
		{
			PlacementHelper.CancelPlacementMode();
		}
		if (PlayerActivityUI.IsPanelOpen)
		{
			InstanceBehavior<UIs>.Instance.playerActivityUI.CancelActivity();
		}
		Vector3 position = InstanceBehavior<GameManager>.Instance.playerController.transform.position;
		Vector3 playerPositionBasedOnAddress = GameManager.GetPlayerPositionBasedOnAddress(ClosestBuildingFromPlayer.Get().Address);
		if (playerPositionBasedOnAddress == Vector3.positiveInfinity || Vector3.SqrMagnitude(position - playerPositionBasedOnAddress) < 0.040000003f)
		{
			GameManager.SetPlayerPositionBasedOnAddress(GameManager.hospitalAddress);
		}
		else
		{
			InstanceBehavior<GameManager>.Instance.StartCoroutine(GameManager.SetPlayerPosition(playerPositionBasedOnAddress));
		}
		CameraHelper.SetCamera(InstanceBehavior<GameManager>.Instance.pedestrianCamera);
		if (CasinoBoatManager.IsOnCasinoBoat)
		{
			InstanceBehavior<CasinoBoatManager>.Instance.ticketBought = false;
		}
	}

	public void AttachToObject(Transform obj, NavigationBlocker navigationBlocker)
	{
		Character.isKinematic = true;
		Character.characterRigidbody.isKinematic = true;
		Character.capsuleCollider.enabled = false;
		Manager.TriggerColliderRemovedEvent(Character.capsuleCollider);
		Character.navmeshAgent.enabled = false;
		base.transform.SetParent(obj);
		base.transform.localPosition = Vector3.zero;
		base.transform.localRotation = Quaternion.identity;
		SetNavigationBlocker(navigationBlocker);
	}

	public void DeattachFromObject(NavigationBlocker navigationBlocker)
	{
		Character.isKinematic = false;
		Character.characterRigidbody.isKinematic = false;
		Character.capsuleCollider.enabled = true;
		Character.navmeshAgent.enabled = true;
		base.transform.SetParent(InstanceBehavior<GameManager>.Instance.itemsContainer);
		Character.navmeshAgent.ResetPath();
		_hasPath = false;
		UnsetNavigationBlocker(navigationBlocker);
	}

	private static CargoInstance GetAccessoryOfType(AccessoryType accessoryType)
	{
		return accessoryType switch
		{
			AccessoryType.Head => SaveGameManager.Current.accessoriesData.headAccessoryCargoInstance, 
			AccessoryType.Hand => SaveGameManager.Current.accessoriesData.handAccessoryCargoInstance, 
			AccessoryType.Phone => SaveGameManager.Current.accessoriesData.phoneAccessoryCargoInstance, 
			_ => null, 
		};
	}

	public static bool HasAccessoryEquipped(AccessoryType accessoryType)
	{
		return GetAccessoryOfType(accessoryType) != null;
	}

	private bool CanShowHandAccessory()
	{
		if ((!NavigationDisabled || PlacementSystem.IsInPlacementMode) && !PlayerHelper.IsUsingVehicle)
		{
			if (PlayerHelper.IsHoldingItem)
			{
				return PlayerHelper.ItemInHands?.HasTag(TagRef.Itemtag.isbag) ?? false;
			}
			return true;
		}
		return false;
	}

	public bool CanUnEquipItem()
	{
		if (InstanceBehavior<UIs>.Instance.playerActivityUI.GetCurrentActivity != null || NavigationDisabled)
		{
			return PlayerHelper.IsUsingVehicle;
		}
		return true;
	}

	public void EquipAccessory(CargoInstance cargoInstance)
	{
		switch (cargoInstance.ItemCached.accessoryType)
		{
		case AccessoryType.Head:
			if (SaveGameManager.Current.accessoriesData.headAccessoryVisible)
			{
				ShowHeadAccessory(cargoInstance);
			}
			SaveGameManager.Current.accessoriesData.headAccessoryCargoInstance = cargoInstance;
			InstanceBehavior<UIs>.Instance.topBar.accessoriesUI.UpdateHeadAccessoryPanel(SaveGameManager.Current.accessoriesData);
			break;
		case AccessoryType.Hand:
			if (SaveGameManager.Current.accessoriesData.handAccessoryVisible)
			{
				ShowHandAccessory(cargoInstance);
			}
			SaveGameManager.Current.accessoriesData.handAccessoryCargoInstance = cargoInstance;
			InstanceBehavior<UIs>.Instance.topBar.accessoriesUI.UpdateHandAccessoryPanel(SaveGameManager.Current.accessoriesData);
			break;
		case AccessoryType.Phone:
			SaveGameManager.Current.accessoriesData.phoneAccessoryCargoInstance = cargoInstance;
			InstanceBehavior<UIs>.Instance.topBar.accessoriesUI.UpdatePhoneAccessoryPanel(SaveGameManager.Current.accessoriesData);
			InstanceBehavior<UIs>.Instance.smartphoneUI.UpdateFrame();
			break;
		}
		if (!SaveGameManager.Current.accessoriesData.isPanelOpen)
		{
			SaveGameManager.Current.accessoriesData.isPanelOpen = true;
			InstanceBehavior<UIs>.Instance.topBar.accessoriesUI.UpdatePanelVisibility(isPanelOpen: true);
		}
		GlobalEvents.onAccessoryEquipped?.Invoke(cargoInstance);
	}

	public void UnEquipAccessoryOfType(AccessoryType accessoryType)
	{
		if (CanUnEquipItem())
		{
			CargoInstance accessoryOfType = GetAccessoryOfType(accessoryType);
			if (!TryToAddAccessoryToCargoHolder(accessoryOfType))
			{
				Dictionary<string, string> notificationData = new Dictionary<string, string> { { "itemname", accessoryOfType.itemName } };
				Notifications.Show(NotificationType.Error, "unequip_notification_no_space", notificationData);
			}
			else
			{
				UnEquipAccessory(accessoryOfType);
			}
		}
	}

	public void UnEquipAccessory(CargoInstance accessoryCargoInstance)
	{
		if (accessoryCargoInstance.ItemCached.accessoryType == AccessoryType.Head)
		{
			SaveGameManager.Current.accessoriesData.headAccessoryCargoInstance = null;
			InstanceBehavior<UIs>.Instance.topBar.accessoriesUI.UpdateHeadAccessoryPanel(SaveGameManager.Current.accessoriesData);
			HideAccessoryVisuals(AccessoryType.Head);
		}
		else if (accessoryCargoInstance.ItemCached.accessoryType == AccessoryType.Hand)
		{
			SaveGameManager.Current.accessoriesData.handAccessoryCargoInstance = null;
			InstanceBehavior<UIs>.Instance.topBar.accessoriesUI.UpdateHandAccessoryPanel(SaveGameManager.Current.accessoriesData);
			HideAccessoryVisuals(AccessoryType.Hand);
		}
		else if (accessoryCargoInstance.ItemCached.accessoryType == AccessoryType.Phone)
		{
			SaveGameManager.Current.accessoriesData.phoneAccessoryCargoInstance = null;
			InstanceBehavior<UIs>.Instance.topBar.accessoriesUI.UpdatePhoneAccessoryPanel(SaveGameManager.Current.accessoriesData);
			HideAccessoryVisuals(AccessoryType.Phone);
		}
		if (SaveGameManager.Current.accessoriesData.isPanelOpen && SaveGameManager.Current.accessoriesData.handAccessoryCargoInstance == null && SaveGameManager.Current.accessoriesData.headAccessoryCargoInstance == null && SaveGameManager.Current.accessoriesData.phoneAccessoryCargoInstance == null)
		{
			SaveGameManager.Current.accessoriesData.isPanelOpen = false;
			InstanceBehavior<UIs>.Instance.topBar.accessoriesUI.UpdatePanelVisibility(isPanelOpen: false);
		}
		GlobalEvents.onAccessoryUnEquipped?.Invoke(accessoryCargoInstance);
	}

	private bool TryToAddAccessoryToCargoHolder(CargoInstance accessoryCargoInstance)
	{
		ICargoHolder cargoHolder = null;
		string randomBag = ItemsGetter.GetRandomBag();
		if ((bool)InstanceBehavior<GameManager>.Instance.selectedVehicle)
		{
			cargoHolder = InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleInstance;
			ItemInstance itemInstance = ItemHelper.InitializeNewInstance(randomBag);
			itemInstance.AddToCargo(accessoryCargoInstance);
			return cargoHolder.TryToAddToCargo(itemInstance.ConvertToCargoInstance());
		}
		if (PlayerHelper.ItemInstanceInHands == null)
		{
			PlayerHelper.ItemInstanceInHands = ItemHelper.InitializeNewInstance(randomBag);
			cargoHolder = PlayerHelper.ItemInstanceInHands;
		}
		else
		{
			Item itemInHands = PlayerHelper.ItemInHands;
			if ((object)itemInHands != null && itemInHands.HasTag(TagRef.Itemtag.isbag))
			{
				cargoHolder = PlayerHelper.ItemInstanceInHands;
			}
		}
		return cargoHolder?.TryToAddToCargo(accessoryCargoInstance) ?? false;
	}

	private void ShowHandAccessory(CargoInstance accessoryCargoInstance)
	{
		if (!string.IsNullOrEmpty(accessoryCargoInstance?.itemName))
		{
			string idWithoutType = accessoryCargoInstance.itemName.GetIdWithoutType();
			if (!string.IsNullOrEmpty(idWithoutType))
			{
				Character.HoldAnItem();
				Character.AddHandObject(idWithoutType, isRightHand: true, accessoryCargoInstance.customColors, isPlayer: true);
				_isHoldingHandAccessory = true;
			}
		}
	}

	private void ShowHeadAccessory(CargoInstance accessoryCargoInstance)
	{
		if (!string.IsNullOrEmpty(accessoryCargoInstance?.itemName))
		{
			string idWithoutType = accessoryCargoInstance.itemName.GetIdWithoutType();
			if (!string.IsNullOrEmpty(idWithoutType))
			{
				Vector3 playerMountPosition = accessoryCargoInstance.ItemCached.playerMountPosition;
				Character.AddHeadObject(idWithoutType + "HeadObject", playerMountPosition, accessoryCargoInstance.customColors);
			}
		}
	}

	public void HideAccessoryVisuals(AccessoryType accessoryType)
	{
		switch (accessoryType)
		{
		case AccessoryType.Head:
			Character.RemoveHeadObject();
			return;
		case AccessoryType.Hand:
			if (_isHoldingHandAccessory)
			{
				Character.StopHoldingAnItem();
				Character.RemoveHandObject();
				_isHoldingHandAccessory = false;
				return;
			}
			break;
		}
		if (accessoryType == AccessoryType.Phone)
		{
			InstanceBehavior<UIs>.Instance.smartphoneUI.UpdateFrame();
		}
	}

	public void ShowHandAccessoryVisualsIfRequired()
	{
		if (SaveGameManager.Current.accessoriesData.handAccessoryCargoInstance != null && SaveGameManager.Current.accessoriesData.handAccessoryVisible && CanShowHandAccessory() && !_isHoldingHandAccessory)
		{
			ShowAccessoryVisuals(AccessoryType.Hand);
		}
	}

	public void ShowAccessoryVisuals(AccessoryType accessoryType)
	{
		switch (accessoryType)
		{
		case AccessoryType.Head:
			ShowHeadAccessory(SaveGameManager.Current.accessoriesData.headAccessoryCargoInstance);
			break;
		case AccessoryType.Hand:
			ShowHandAccessory(SaveGameManager.Current.accessoriesData.handAccessoryCargoInstance);
			break;
		case AccessoryType.Phone:
			InstanceBehavior<UIs>.Instance.smartphoneUI.UpdateFrame();
			break;
		}
	}

	public void SetHeldItemToConsume(string prefabName)
	{
		if (string.IsNullOrEmpty(prefabName))
		{
			return;
		}
		GameObject gameObject = PrefabHelper.CreatePrefab(prefabName, Character.rightHand);
		if ((bool)gameObject)
		{
			GameObject gameObject2 = PrefabHelper.LoadPrefabAssetByName(prefabName);
			gameObject.transform.localPosition = gameObject2.transform.localPosition;
			gameObject.transform.localRotation = gameObject2.transform.localRotation;
			if ((bool)_heldItemToConsume)
			{
				UnityEngine.Object.Destroy(_heldItemToConsume);
			}
			_heldItemToConsume = gameObject;
			_heldItemToConsumeTimer = heldItemToConsumeMaxLifetime;
		}
	}

	public void DiscardHeldItemToConsume()
	{
		if ((bool)_heldItemToConsume)
		{
			UnityEngine.Object.Destroy(_heldItemToConsume);
		}
		_heldItemToConsume = null;
		_heldItemToConsumeTimer = 0f;
	}

	private void OnPlacementSystemItemPlaced()
	{
		if (!PlayerHelper.IsUsingVehicle)
		{
			Character?.SetHandContent(null);
		}
	}
}
