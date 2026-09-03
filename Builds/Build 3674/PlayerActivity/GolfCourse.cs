using System;
using System.Collections.Generic;
using AI;
using Character;
using Cinemachine;
using Culling;
using DG.Tweening;
using Helpers;
using Items.SpecialItems;
using JimmysUnityUtilities;
using Localizor.LanguageChangeEvent;
using Player.HUD.ItemInfoOverlays;
using UI;
using UI.Notification;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PlayerActivity;

public class GolfCourse : MonoBehaviour, ICullable
{
	private const int TrajectoryLinePointCount = 20;

	private const float InhibitControlsZoomThreshold = 0.1f;

	private static CinemachineVirtualCameraBase InitialCamera;

	private static RuntimeAnimatorController InitialAnimatorController;

	private static readonly ActivityWithoutUI ActivityInstance = new ActivityWithoutUI();

	private static readonly Vector3[] TrajectoryPoints = new Vector3[20];

	private static readonly TransactionInfo TransactionInfo = new TransactionInfo("ba:transaction_golfgame");

	private static readonly LanguageChangeEventDataHolder ItemPanelLabel = LanguageChangeEventDataHolder.Create("ba:golfui_headline");

	private static readonly int GolfSwing = Animator.StringToHash("GolfSwing");

	[SerializeField]
	private Transform cameraPosition;

	[SerializeField]
	private GameObject aimArrowPrefab;

	[SerializeField]
	private Vector3 aimArrowOffset;

	[SerializeField]
	private float aimArrowScaleMultiplier = 0.1f;

	[SerializeField]
	private Collider aimAreaCollider;

	[SerializeField]
	private List<Collider> sandTrapColliders;

	[SerializeField]
	private List<Collider> pondColliders;

	[SerializeField]
	private LineRenderer trajectoryLinePrefab;

	[SerializeField]
	private float trajectoryLineTimeScale = 10f;

	[SerializeField]
	private Vector3 maxShotVelocity;

	public float playFee;

	[SerializeField]
	private int startingBalls;

	[SerializeField]
	private AnimationCurve shotPowerCurve;

	[SerializeField]
	private float shotPowerRiseSpeed = 0.75f;

	[SerializeField]
	private GolfCourseBall ballPrefab;

	[SerializeField]
	private Bounds bounds;

	[SerializeField]
	private float zoomedFov = 30f;

	[SerializeField]
	private float shotZoomNormalized = 0.25f;

	[SerializeField]
	private float zoomTransitionSpeed = 1.5f;

	[SerializeField]
	private float shotZoomTransitionSpeed = 1f;

	[SerializeField]
	private float minClickDurationForSwing = 0.2f;

	[SerializeField]
	private float scoreFreezeDuration = 1.5f;

	[SerializeField]
	private float trapFreezeDuration = 1f;

	[SerializeField]
	private int windEveryXShots = 3;

	[SerializeField]
	private Transform windDirection;

	[SerializeField]
	private float windStrength = 1f;

	[SerializeField]
	private AudioSource windAudioSource;

	[SerializeField]
	private GolfUI uiPrefab;

	[SerializeField]
	private AudioSource shotAudioSource;

	[SerializeField]
	private ParticleSystem ballInHoleEffect;

	[SerializeField]
	private ParticleSystem sandTrapEffect;

	[SerializeField]
	private ParticleSystem pondEffect;

	[SerializeField]
	private GolfCourseHole[] holes;

	[SerializeField]
	private GolfCart golfCart;

	[SerializeField]
	private float golfCartCullRadius = 90f;

	private GolferNpc _playerGolfer;

	private GameObject _aimArrow;

	private Transform _aimArrowLine;

	private LineRenderer _trajectoryLine;

	private GolfCourseBall _ball;

	private PlayerActivityBalanceConfig _balanceConfig;

	private float _aimArrowInitialScaleX;

	private float _shotPower;

	private Vector3 _pendingShotVelocity;

	private Vector3 _readyShotVelocity;

	private Quaternion _aimRotation;

	private float _initialFov;

	private float _zoom;

	private Vector3 _zoomPos;

	private float _clickTimer;

	private float _freezeTimer;

	private GolfUI _ui;

	private int _score;

	private int _ballsLeft;

	private bool _pendingNewShot;

	private bool _inhibitShot;

	private float _wind;

	private void Start()
	{
		InstanceBehavior<CullingManager>.Instance.generalCullingGroupController.RegisterCullable(this);
		GlobalEvents.onHospitalRespawnStarts = (Action)Delegate.Combine(GlobalEvents.onHospitalRespawnStarts, new Action(OnHospitalRespawnStarts));
		base.enabled = false;
	}

	private void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			InstanceBehavior<CullingManager>.Instance?.generalCullingGroupController.UnregisterCullable(this);
			GlobalEvents.onHospitalRespawnStarts = (Action)Delegate.Remove(GlobalEvents.onHospitalRespawnStarts, new Action(OnHospitalRespawnStarts));
		}
	}

	private void Update()
	{
		if (_freezeTimer > 0f)
		{
			_freezeTimer -= Time.deltaTime;
			if (_freezeTimer <= 0f)
			{
				DecreaseBallsLeft();
			}
		}
		if (_ball.gameObject.activeSelf || _freezeTimer > 0f)
		{
			_zoom = (_ball.IsKinematic ? 1f : Mathf.MoveTowards(_zoom, shotZoomNormalized, shotZoomTransitionSpeed * Time.deltaTime));
			if (_ball.gameObject.activeSelf)
			{
				_zoomPos = _ball.transform.position;
				CheckBall();
			}
		}
		else
		{
			_zoom = Mathf.MoveTowards(_zoom, 0f, zoomTransitionSpeed * Time.deltaTime);
		}
		float fieldOfView = Mathf.Lerp(_initialFov, zoomedFov, _zoom);
		CinemachineVirtualCamera obj = (CinemachineVirtualCamera)InstanceBehavior<GameManager>.Instance.dummyCamera;
		obj.m_Lens.FieldOfView = fieldOfView;
		Vector3 forward = Vector3.Lerp(b: (_zoomPos - cameraPosition.position).normalized, a: cameraPosition.forward, t: _zoom);
		obj.transform.rotation = Quaternion.LookRotation(forward);
		_trajectoryLine.enabled = false;
		if (_zoom > 0.1f || _ballsLeft <= 0)
		{
			if (_aimArrow.activeSelf)
			{
				_aimArrow.SetActive(value: false);
			}
			_ui.SetPowerBarActive(active: false);
			return;
		}
		if (_pendingNewShot)
		{
			_pendingNewShot = false;
			OnStartingNewShot();
		}
		bool flag = _pendingShotVelocity != Vector3.zero;
		bool flag2 = EventSystem.current.IsPointerOverGameObject();
		aimAreaCollider.enabled = true;
		Ray ray = GameManager.GetMainCamera().ScreenPointToRay(Input.mousePosition);
		bool num = aimAreaCollider.Raycast(ray, out var hitInfo, 1000f);
		aimAreaCollider.enabled = false;
		bool flag3 = num && !flag2 && !Input.GetMouseButton(0);
		if (_aimArrow.activeSelf != flag3)
		{
			_aimArrow.SetActive(flag3);
		}
		if (num && (!flag2 | flag))
		{
			PlayerController playerController = InstanceBehavior<GameManager>.Instance.playerController;
			Vector3 forward2 = hitInfo.point - playerController.transform.position;
			forward2.y = 0f;
			_aimRotation = Quaternion.LookRotation(forward2);
			Vector3 localScale = _aimArrowLine.localScale;
			localScale.x = _aimArrowInitialScaleX * forward2.magnitude * aimArrowScaleMultiplier;
			_aimArrowLine.localScale = localScale;
		}
		_aimArrow.transform.rotation = _aimRotation;
		if (!Input.GetMouseButton(0) || _inhibitShot)
		{
			if (!Input.GetMouseButton(0))
			{
				_inhibitShot = false;
			}
			_shotPower = 0f;
			_ui.SetPowerBarActive(active: false);
			if (flag)
			{
				if (_clickTimer < minClickDurationForSwing)
				{
					_pendingShotVelocity = Vector3.zero;
					return;
				}
				_readyShotVelocity = _aimRotation * _pendingShotVelocity;
				_pendingShotVelocity = Vector3.zero;
				_playerGolfer.animator.Rebind();
				_playerGolfer.animator.Update(0f);
				_playerGolfer.animator.SetTrigger(GolfSwing);
				_ball.gameObject.SetActive(value: false);
				_ball.transform.position = _aimArrow.transform.position;
				_ball.SetKinematic(isKinematic: true);
				_ball.gameObject.SetActive(value: true);
				_aimArrow.SetActive(value: false);
				golfCart.enabled = false;
				_ui.OnShotSubmit();
			}
		}
		else if (flag2 && !flag)
		{
			_ui.SetPowerBarActive(active: false);
		}
		else
		{
			if (!flag)
			{
				_clickTimer = 0f;
			}
			_clickTimer += Time.deltaTime;
			_shotPower += shotPowerRiseSpeed * Time.deltaTime;
			_shotPower %= 1f;
			float num2 = shotPowerCurve.Evaluate(_shotPower);
			_ui.SetPowerBarActive(active: true);
			_ui.UpdatePowerBar(num2);
			_pendingShotVelocity = maxShotVelocity * num2;
			_trajectoryLine.transform.rotation = _aimRotation;
			UpdateTrajectoryLine(_pendingShotVelocity);
			if (Input.GetMouseButton(1))
			{
				_pendingShotVelocity = Vector3.zero;
				_inhibitShot = true;
			}
		}
	}

	public bool StartGame(GolfPlatformController golfPlatformController)
	{
		if (!GameManager.ChangeMoneySafe(0f - playFee, TransactionInfo, null, null, force: false, showNotification: true))
		{
			return false;
		}
		PlayerController playerController = InstanceBehavior<GameManager>.Instance.playerController;
		playerController.ResetNavigation();
		EnergyHelper.RemoveEnergySpender("move");
		ThirdPersonCharacter character = playerController.Character;
		character.navmeshAgent.Warp(golfPlatformController.standingPoint.position);
		character.transform.forward = golfPlatformController.standingPoint.forward;
		_balanceConfig = golfPlatformController.balanceConfig;
		_balanceConfig.EnableTemporalBoost(character);
		InstanceBehavior<OverlayManager>.Instance.HideOverlays();
		InitialCamera = CameraHelper.GetCurrentCamera();
		CinemachineVirtualCameraBase dummyCamera = InstanceBehavior<GameManager>.Instance.dummyCamera;
		_initialFov = ((CinemachineVirtualCamera)dummyCamera).m_Lens.FieldOfView;
		dummyCamera.transform.DOKill();
		dummyCamera.transform.SetPositionAndRotation(cameraPosition.position, cameraPosition.rotation);
		CameraHelper.SetCamera(dummyCamera);
		InitialAnimatorController = playerController.Character.animator.runtimeAnimatorController;
		playerController.Character.animator.runtimeAnimatorController = golfPlatformController.GolferAnimatorController;
		playerController.SetNavigationBlocker(NavigationBlocker.PlayingGolf);
		_playerGolfer = playerController.AddComponent<GolferNpc>();
		_playerGolfer.pool = golfPlatformController.npcPool;
		_playerGolfer.animator = playerController.Character.animator;
		_playerGolfer.manualControl = true;
		_playerGolfer.animator.GetComponent<AnimationTriggerEvents>().oneActionTrigger.AddListener(OnAnimationTrigger);
		_aimArrow = UnityEngine.Object.Instantiate(aimArrowPrefab, golfPlatformController.transform);
		_aimArrow.transform.localPosition = aimArrowOffset;
		_aimArrowLine = _aimArrow.transform.GetChild(0);
		_aimArrowInitialScaleX = _aimArrowLine.localScale.x;
		_aimArrow.SetActive(value: false);
		_trajectoryLine = UnityEngine.Object.Instantiate(trajectoryLinePrefab, golfPlatformController.transform);
		_trajectoryLine.transform.localPosition = aimArrowOffset;
		_trajectoryLine.enabled = false;
		_ball = UnityEngine.Object.Instantiate(ballPrefab, _aimArrow.transform.position, Quaternion.identity);
		_ball.ownerCourse = this;
		_ball.gameObject.SetActive(value: false);
		_ballsLeft = startingBalls;
		_ui = UnityEngine.Object.Instantiate(uiPrefab, InstanceBehavior<UIs>.Instance ? InstanceBehavior<UIs>.Instance.transform : null);
		_ui.playFee = playFee;
		GolfUI ui = _ui;
		ui.onForfeitBall = (Action)Delegate.Combine(ui.onForfeitBall, new Action(ForfeitBall));
		GolfUI ui2 = _ui;
		ui2.onPlayAgain = (Action)Delegate.Combine(ui2.onPlayAgain, new Action(RestartGame));
		GolfUI ui3 = _ui;
		ui3.onQuit = (Action)Delegate.Combine(ui3.onQuit, new Action(GolfPlatformController.RequestFinish));
		_ui.UpdateBallsLeft(_ballsLeft);
		foreach (Collider sandTrapCollider in sandTrapColliders)
		{
			sandTrapCollider.gameObject.SetActive(value: true);
		}
		foreach (Collider pondCollider in pondColliders)
		{
			pondCollider.gameObject.SetActive(value: true);
		}
		PlayerActivityUI.SetActivityWithoutUI(ActivityInstance);
		InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetMiniGameMode(ItemPanelLabel);
		InstanceBehavior<UIs>.Instance.playerHUD.currentBuildingUI.Toggle(on: false);
		InstanceBehavior<UIs>.Instance.tasksUI.ChangeVisibility(show: false);
		InstanceBehavior<UIs>.Instance.smartphoneUI.gameObject.SetActive(value: false);
		InstanceBehavior<CityManager>.Instance.cityMap.SetCanvasEnabled(isEnabled: false);
		base.enabled = true;
		_pendingNewShot = true;
		return true;
	}

	public void StopGame()
	{
		PlayerController playerController = InstanceBehavior<GameManager>.Instance.playerController;
		playerController.UnsetNavigationBlocker(NavigationBlocker.PlayingGolf);
		ThirdPersonCharacter character = playerController.Character;
		character.Reset();
		character.animator.runtimeAnimatorController = InitialAnimatorController;
		_balanceConfig?.DisableTemporalBoost(character);
		_balanceConfig = null;
		character.animator.GetComponent<AnimationTriggerEvents>().oneActionTrigger.RemoveListener(OnAnimationTrigger);
		if ((bool)_playerGolfer)
		{
			UnityEngine.Object.Destroy(_playerGolfer);
		}
		_playerGolfer = null;
		if ((bool)InitialCamera)
		{
			CameraHelper.SetCamera(InitialCamera);
		}
		InitialCamera = null;
		CinemachineVirtualCameraBase dummyCamera = InstanceBehavior<GameManager>.Instance.dummyCamera;
		if ((bool)dummyCamera)
		{
			((CinemachineVirtualCamera)dummyCamera).m_Lens.FieldOfView = _initialFov;
		}
		if ((bool)_aimArrow)
		{
			UnityEngine.Object.Destroy(_aimArrow.gameObject);
		}
		_aimArrow = null;
		if ((bool)_trajectoryLine)
		{
			UnityEngine.Object.Destroy(_trajectoryLine.gameObject);
		}
		_trajectoryLine = null;
		if ((bool)_ball)
		{
			UnityEngine.Object.Destroy(_ball.gameObject);
		}
		_ball = null;
		if ((bool)_ui)
		{
			UnityEngine.Object.Destroy(_ui.gameObject);
		}
		_ui = null;
		foreach (Collider sandTrapCollider in sandTrapColliders)
		{
			sandTrapCollider.gameObject.SetActive(value: false);
		}
		foreach (Collider pondCollider in pondColliders)
		{
			pondCollider.gameObject.SetActive(value: false);
		}
		windAudioSource.Stop();
		aimAreaCollider.enabled = false;
		PlayerActivityUI.SetActivityWithoutUI(null);
		InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.Toggle(isEnabled: false);
		InstanceBehavior<UIs>.Instance.tasksUI.ChangeVisibility(show: true);
		InstanceBehavior<UIs>.Instance.smartphoneUI.gameObject.SetActive(value: true);
		InstanceBehavior<CityManager>.Instance.cityMap.SetCanvasEnabled(isEnabled: true);
		base.enabled = false;
		SaveGameManager.Current.achievementsData.golfHighScore = Mathf.Max(SaveGameManager.Current.achievementsData.golfHighScore, _score);
		GameEvent.Invoke("ba:gameevent_golfactivityfinished");
	}

	private void UpdateTrajectoryLine(Vector3 shotVelocity)
	{
		int num = 0;
		for (int i = 0; i < TrajectoryPoints.Length; i++)
		{
			float num2 = (float)i / (float)(TrajectoryPoints.Length - 1) * trajectoryLineTimeScale;
			Vector3 vector = shotVelocity * num2 + Physics.gravity * (num2 * num2 / 2f);
			TrajectoryPoints[i] = vector;
			num++;
			if (vector.y < 0f)
			{
				break;
			}
		}
		_trajectoryLine.positionCount = num;
		_trajectoryLine.SetPositions(TrajectoryPoints);
		_trajectoryLine.enabled = true;
	}

	private void CheckBall()
	{
		Vector3 point = base.transform.InverseTransformPoint(_ball.transform.position);
		if (!bounds.Contains(point))
		{
			Notifications.Show(NotificationType.Warning, "ba:notification_ball_out_of_bounds", null, 4f, null, null, notificationSound: true, trackOnSaveGame: false);
			_ball.gameObject.SetActive(value: false);
		}
	}

	private void OnAnimationTrigger()
	{
		if (_ball.gameObject.activeSelf && !(_readyShotVelocity == Vector3.zero))
		{
			_ball.SetKinematic(isKinematic: false);
			_ball.Launch(_readyShotVelocity);
			_readyShotVelocity = Vector3.zero;
			shotAudioSource.Play();
			_ui.SetForfeitBallButtonActive(active: true);
			golfCart.enabled = true;
		}
	}

	public void OnBallCollisionCheck(Collision collision)
	{
		if (!_ball.gameObject.activeSelf)
		{
			return;
		}
		if (sandTrapColliders.Contains(collision.collider))
		{
			_freezeTimer = trapFreezeDuration;
			_ball.gameObject.SetActive(value: false);
			UnityEngine.Object.Instantiate(sandTrapEffect, collision.GetContact(0).point, Quaternion.identity);
			return;
		}
		if (pondColliders.Contains(collision.collider))
		{
			_freezeTimer = trapFreezeDuration;
			_ball.gameObject.SetActive(value: false);
			UnityEngine.Object.Instantiate(pondEffect, collision.GetContact(0).point, Quaternion.identity);
			return;
		}
		if (!golfCart.IsHit && collision.collider.transform.IsChildOf(golfCart.transform))
		{
			golfCart.OnHit();
		}
		Vector3 position = _ball.transform.position;
		GolfCourseHole[] array = holes;
		foreach (GolfCourseHole golfCourseHole in array)
		{
			Vector3 vector = golfCourseHole.transform.position - position;
			float num = golfCourseHole.radius * golfCourseHole.transform.localScale.x;
			if (!(vector.sqrMagnitude > num * num))
			{
				OnBallInHole(golfCourseHole);
				break;
			}
		}
	}

	private void OnBallInHole(GolfCourseHole hole)
	{
		_freezeTimer = scoreFreezeDuration;
		_ball.gameObject.SetActive(value: false);
		AddScore(hole.score);
		Dictionary<string, string> notificationData = new Dictionary<string, string> { 
		{
			"points",
			hole.score.ToString()
		} };
		Notifications.Show(NotificationType.Success, "ba:notification_ball_in_hole", notificationData, 4f, null, null, notificationSound: true, trackOnSaveGame: false);
		UnityEngine.Object.Instantiate(ballInHoleEffect, hole.transform.position, Quaternion.identity).transform.localScale *= hole.transform.localScale.x;
	}

	private void AddScore(int score)
	{
		_score += score;
		_ui.UpdateScore(_score);
	}

	public void OnBallDeactivated()
	{
		_pendingNewShot = true;
		if (_freezeTimer <= 0f)
		{
			DecreaseBallsLeft();
		}
	}

	private void DecreaseBallsLeft()
	{
		if (_ballsLeft > 0)
		{
			_ballsLeft--;
		}
		if ((bool)_ui)
		{
			_ui.SetForfeitBallButtonActive(active: false);
			_ui.UpdateBallsLeft(_ballsLeft);
		}
	}

	private void OnStartingNewShot()
	{
		int num = startingBalls - _ballsLeft;
		_wind = ((num % windEveryXShots != windEveryXShots - 1) ? 0f : ((UnityEngine.Random.value < 0.5f) ? (-1f) : 1f));
		_ui.UpdateWind(_wind);
		_ball.wind = windDirection.forward * (_wind * windStrength);
		windAudioSource.Stop();
		if (_wind != 0f)
		{
			windAudioSource.Play();
		}
		golfCart.OnNewTurn();
	}

	private void ForfeitBall()
	{
		_ball.gameObject.SetActive(value: false);
		_ui.SetForfeitBallButtonActive(active: false);
	}

	private void RestartGame()
	{
		if (GameManager.ChangeMoneySafe(0f - playFee, TransactionInfo, null, null, force: false, showNotification: true))
		{
			_ballsLeft = startingBalls;
			_score = 0;
			_ui.UpdateBallsLeft(_ballsLeft);
			_ui.UpdateScore(_score);
			_ui.SetForfeitBallButtonActive(active: false);
			_ball.gameObject.SetActive(value: false);
		}
	}

	private static void OnHospitalRespawnStarts()
	{
		GolfPlatformController.RequestFinish();
	}

	public void OnLod0()
	{
		golfCart.Spawn();
	}

	public void OnLod1()
	{
		golfCart.Despawn();
	}

	public void OnLod2()
	{
		OnLod1();
	}

	public BoundingSphere GetCullingBoundingSphere()
	{
		return new BoundingSphere(base.transform.position, golfCartCullRadius);
	}
}
