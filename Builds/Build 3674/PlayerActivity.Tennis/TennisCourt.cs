using System;
using BigAmbitions.Characters.Appearance;
using Cinemachine;
using DG.Tweening;
using Helpers;
using IngameDebugConsole;
using JimmysUnityUtilities;
using Localizor.LanguageChangeEvent;
using Player.HUD.ItemInfoOverlays;
using UI;
using UI.ItemPanel;
using UI.Notification;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayerActivity.Tennis;

public class TennisCourt : EntityController
{
	public const float PlayFee = 50f;

	private const float BallYDistanceToReset = 1f;

	private const float ServiceAreaAlphaMin = 0.1f;

	private const float ServiceAreaAlphaMax = 0.75f;

	private const float ServiceAreaAlphaPeriod = 1.5f;

	private const int MaxSets = 3;

	private const int SetsToWinMatch = 2;

	private const int BounceParticleEmitCount = 4;

	private const float AudioSpatialBlendDuringGame = 0.7f;

	private static CinemachineVirtualCameraBase InitialCamera;

	private static readonly ActivityWithoutUI ActivityInstance = new ActivityWithoutUI();

	private static readonly TransactionInfo TransactionInfo = new TransactionInfo("ba:transaction_tennisgame");

	private static readonly LanguageChangeEventDataHolder ItemPanelLabel = LanguageChangeEventDataHolder.Create("ba:tennisui_headline");

	public float totalCourtLength;

	[SerializeField]
	private PlayerActivityBalanceConfig balanceConfig;

	[SerializeField]
	private MeshRenderer courtRenderer;

	[SerializeField]
	private TennisCourtSide[] courtSides;

	[SerializeField]
	private Collider courtCollider;

	[SerializeField]
	private Collider extendedCourtCollider;

	[SerializeField]
	private Collider netCollider;

	[SerializeField]
	private TennisBall ballPrefab;

	[SerializeField]
	private TennisUI uiPrefab;

	[SerializeField]
	private ParticleSystem bounceParticlesPrefab;

	[SerializeField]
	private AudioSource hitSound;

	[SerializeField]
	private AudioSource bounceSound;

	[SerializeField]
	private float hitSoundPitchMin = 0.9f;

	[SerializeField]
	private float hitSoundPitchMax = 1.1f;

	[SerializeField]
	private Transform cameraPosition;

	[SerializeField]
	private float nextTurnMinDelay = 2f;

	[SerializeField]
	private bool randomizePlayerAppearance;

	private TennisUI _ui;

	private ParticleSystem _bounceParticles;

	private readonly TennisSideScore[] _sideScores = new TennisSideScore[2];

	private bool _pendingNewTurn;

	private bool _pendingSwitchSides;

	private float _nextTurnTimer;

	public static TennisCourt PlayingInstance { get; private set; }

	public TennisBall Ball { get; private set; }

	public PlayerActivityBalanceConfig BalanceConfig => balanceConfig;

	public TennisCourtSide ServingSide { get; private set; }

	public bool IsServingOnRightSide { get; private set; }

	public bool AwaitingServe { get; private set; }

	public TennisCourtSide LastHitterSide { get; private set; }

	public TennisCourtSide BallBouncedSide { get; private set; }

	public bool HasServeReturned { get; private set; }

	public TennisInteractionNpc LinkedInteractionNpc { get; private set; }

	private bool InTieBreak
	{
		get
		{
			if (_sideScores[0].games == 6)
			{
				return _sideScores[1].games == 6;
			}
			return false;
		}
	}

	public override void Awake()
	{
		base.Awake();
		Ball = UnityEngine.Object.Instantiate(ballPrefab, base.transform);
		Ball.court = this;
		Ball.gameObject.SetActive(value: false);
		_bounceParticles = UnityEngine.Object.Instantiate(bounceParticlesPrefab, base.transform);
		ResetMatch();
		if (IsInTestScene())
		{
			InputHelper.SetupPlayerInput();
			if (IsHumanPlaying())
			{
				_ui = UnityEngine.Object.Instantiate(uiPrefab, InstanceBehavior<UIs>.Instance ? InstanceBehavior<UIs>.Instance.transform : null);
				TennisUI ui = _ui;
				ui.onPlayAgain = (Action)Delegate.Combine(ui.onPlayAgain, new Action(OnClickPlayAgain));
				ResetMatch();
			}
			hitSound.spatialBlend = 0.7f;
			bounceSound.spatialBlend = 0.7f;
		}
	}

	public override void Start()
	{
		base.Start();
		if (randomizePlayerAppearance)
		{
			TennisCourtSide[] array = courtSides;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].player.GetComponent<AppearanceSetter>().SetRandomAppearance(new AppearanceTag[1] { AppearanceTag.Sport });
			}
		}
	}

	private void Update()
	{
		if (_nextTurnTimer > 0f)
		{
			_nextTurnTimer -= Time.deltaTime;
		}
		if (AwaitingServe && ServingSide.player.IsPlayer)
		{
			GetOppositeCourtSide(ServingSide).SetServiceAreaAlpha(GetServiceAreaAlpha());
		}
	}

	private void FixedUpdate()
	{
		if (Ball.gameObject.activeSelf && Ball.transform.position.y < base.transform.position.y - 1f)
		{
			OnBallCollisionEnter(null);
		}
		if (_pendingNewTurn && !(_nextTurnTimer > 0f) && (!IsHumanPlaying() || !_ui.IsPopupActive()))
		{
			_pendingNewTurn = false;
			if (IsHumanPlaying() && IsGameOver())
			{
				TennisCourtSide tennisCourtSide = ((_sideScores[0].sets > _sideScores[1].sets) ? courtSides[0] : courtSides[1]);
				_ui.ShowPlayAgainPrompt(tennisCourtSide.player.IsPlayer);
			}
			else
			{
				OnNewTurn();
			}
		}
	}

	private void SetCourtActive(bool active)
	{
		base.enabled = active;
		TennisCourtSide[] array = courtSides;
		foreach (TennisCourtSide obj in array)
		{
			obj.player.gameObject.SetActive(active);
			obj.SetServiceAreaAlpha(0f);
		}
		OnIoExit();
	}

	public override bool ShouldReactToIoEnter()
	{
		if (InstanceBehavior<UIs>.Instance.playerActivityUI.GetCurrentActivity == null && (bool)LinkedInteractionNpc)
		{
			return base.ShouldReactToIoEnter();
		}
		return false;
	}

	public override bool ShouldShowDetailedOverlay()
	{
		return GetClosestNavMeshTargetPosition(PlayerHelper.GetPosition()) != Vector3.zero;
	}

	public override void OnIoEnter()
	{
		if (!CityMap.IsOpen && !GameManager.IsAnyMiniGameActive() && (bool)LinkedInteractionNpc)
		{
			base.OnIoEnter();
		}
	}

	public void SetLinkedInteractionNpc(TennisInteractionNpc npc)
	{
		LinkedInteractionNpc = npc;
		primaryInteractionEnabled = true;
		SetCourtActive(active: false);
	}

	public void PerformActivity()
	{
		if (PlayerHelper.ItemInHands != null || !string.IsNullOrEmpty(SaveGameManager.Current.ActiveVehicleId))
		{
			Notifications.ShowError("notification_need_empty_hands_to_interact");
		}
		else if (!ItemPanelUI.IsVisible)
		{
			PlayerActivityUI.Show(new TennisActivity(LinkedInteractionNpc), LinkedInteractionNpc);
		}
	}

	public void StartGame(TennisInteractionNpc interactionNpc, bool automated)
	{
		if (!PlayingInstance && (automated || GameManager.ChangeMoneySafe(-50f, TransactionInfo, null, null, force: false, showNotification: true)))
		{
			SetCourtActive(active: true);
			if (!automated)
			{
				PlayingInstance = this;
			}
			PlayerController playerController = InstanceBehavior<GameManager>.Instance.playerController;
			playerController.ResetNavigation();
			EnergyHelper.RemoveEnergySpender("move");
			playerController.SetNavigationBlocker(NavigationBlocker.PlayingTennis);
			ThirdPersonCharacter character = playerController.Character;
			character.ToggleVisibility(show: false);
			if (!automated)
			{
				balanceConfig.EnableTemporalBoost(character);
			}
			InstanceBehavior<OverlayManager>.Instance.HideOverlays();
			if (automated)
			{
				InitialCamera = null;
			}
			else
			{
				InitialCamera = CameraHelper.GetCurrentCamera();
				CinemachineVirtualCameraBase dummyCamera = InstanceBehavior<GameManager>.Instance.dummyCamera;
				dummyCamera.transform.DOKill();
				dummyCamera.transform.SetPositionAndRotation(cameraPosition.position, cameraPosition.rotation);
				CameraHelper.SetCamera(dummyCamera);
			}
			TennisPlayer player = courtSides[0].player;
			TennisPlayer player2 = courtSides[1].player;
			if ((bool)player.controller)
			{
				UnityEngine.Object.Destroy(player.controller);
			}
			player.controller = (automated ? ((TennisController)player.AddComponent<TennisNpcController>()) : ((TennisController)player.AddComponent<TennisHumanController>()));
			player.GetComponent<AppearanceSetter>().SetAppearance(character.appearanceSetter.data);
			player2.GetComponent<AppearanceSetter>().SetAppearance(interactionNpc.appearanceSetter.data);
			interactionNpc.gameObject.SetActive(value: false);
			LinkedInteractionNpc = interactionNpc;
			hitSound.spatialBlend = 0.7f;
			bounceSound.spatialBlend = 0.7f;
			if (!automated)
			{
				_ui = UnityEngine.Object.Instantiate(uiPrefab, InstanceBehavior<UIs>.Instance ? InstanceBehavior<UIs>.Instance.transform : null);
				TennisUI ui = _ui;
				ui.onPlayAgain = (Action)Delegate.Combine(ui.onPlayAgain, new Action(OnClickPlayAgain));
				TennisUI ui2 = _ui;
				ui2.onQuit = (Action)Delegate.Combine(ui2.onQuit, new Action(RequestFinish));
				PlayerActivityUI.SetActivityWithoutUI(ActivityInstance);
				InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetMiniGameMode(ItemPanelLabel);
				InstanceBehavior<UIs>.Instance.playerHUD.currentBuildingUI.Toggle(on: false);
				InstanceBehavior<UIs>.Instance.tasksUI.ChangeVisibility(show: false);
				InstanceBehavior<UIs>.Instance.smartphoneUI.gameObject.SetActive(value: false);
				InstanceBehavior<CityManager>.Instance.cityMap.SetCanvasEnabled(isEnabled: false);
			}
			ResetMatch();
			Ball.SetTrailEnabled(!automated);
		}
	}

	public static void RequestFinish()
	{
		if ((bool)PlayingInstance)
		{
			PlayingInstance.Finish();
		}
	}

	public void Finish()
	{
		bool num = IsHumanPlaying();
		PlayingInstance = null;
		PlayerController playerController = InstanceBehavior<GameManager>.Instance.playerController;
		playerController.UnsetNavigationBlocker(NavigationBlocker.PlayingTennis);
		ThirdPersonCharacter character = playerController.Character;
		character.ToggleVisibility(show: true);
		character.Reset();
		if (num)
		{
			balanceConfig.DisableTemporalBoost(character);
		}
		character.WarpSafely(playerController.transform.position);
		if ((bool)InitialCamera)
		{
			CameraHelper.SetCamera(InitialCamera);
		}
		InitialCamera = null;
		TennisController controller = courtSides[0].player.controller;
		if ((bool)controller)
		{
			UnityEngine.Object.Destroy(controller);
		}
		if ((bool)LinkedInteractionNpc)
		{
			LinkedInteractionNpc.gameObject.SetActive(value: true);
		}
		if ((bool)_ui)
		{
			UnityEngine.Object.Destroy(_ui.gameObject);
		}
		_ui = null;
		hitSound.spatialBlend = 1f;
		bounceSound.spatialBlend = 1f;
		if (num)
		{
			PlayerActivityUI.SetActivityWithoutUI(null);
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.Toggle(isEnabled: false);
			InstanceBehavior<UIs>.Instance.tasksUI.ChangeVisibility(show: true);
			InstanceBehavior<UIs>.Instance.smartphoneUI.gameObject.SetActive(value: true);
			InstanceBehavior<CityManager>.Instance.cityMap.SetCanvasEnabled(isEnabled: true);
		}
		Ball.SetTrailEnabled(newEnabled: false);
		SetCourtActive(active: false);
	}

	public bool IsHumanPlaying()
	{
		TennisController controller = courtSides[0].player.controller;
		if ((bool)controller)
		{
			return controller is TennisHumanController;
		}
		return false;
	}

	private void ResetMatch()
	{
		_pendingNewTurn = true;
		_nextTurnTimer = 0f;
		_pendingSwitchSides = false;
		Ball.gameObject.SetActive(value: false);
		ServingSide = courtSides[0];
		IsServingOnRightSide = true;
		AwaitingServe = false;
		HasServeReturned = false;
		for (int i = 0; i < _sideScores.Length; i++)
		{
			_sideScores[i] = default(TennisSideScore);
		}
		UpdateScoreUI();
		if ((bool)_ui)
		{
			_ui.OnResetMatch();
		}
	}

	public void SetBallActive(bool active)
	{
		if (Ball.gameObject.activeSelf != active)
		{
			Ball.gameObject.SetActive(active);
		}
	}

	public void OnBallHit(TennisCourtSide hitterSide, float pitchFactor, bool isServe)
	{
		AwaitingServe = false;
		HasServeReturned = !isServe;
		BallBouncedSide = null;
		LastHitterSide = hitterSide;
		TennisCourtSide[] array = courtSides;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetServiceAreaAlpha(0f);
		}
		hitSound.Stop();
		hitSound.pitch = Mathf.Lerp(hitSoundPitchMin, hitSoundPitchMax, pitchFactor);
		hitSound.transform.position = Ball.transform.position;
		hitSound.Play();
	}

	private void OnNewTurn()
	{
		if (_pendingSwitchSides)
		{
			_pendingSwitchSides = false;
			ServingSide = GetOppositeCourtSide(ServingSide);
			IsServingOnRightSide = true;
			UpdateScoreUI();
		}
		if (IsHumanPlaying())
		{
			if (InTieBreak && _sideScores[0].points == 0 && _sideScores[1].points == 0)
			{
				_ui.ShowNotification("ba:tennisui_tie_breaker");
			}
			if (IsMatchPoint())
			{
				_ui.ShowNotification("ba:tennisui_match_point");
			}
		}
		Vector3 center = ServingSide.localServeLine.center;
		center.x = ServingSide.localServeLine.extents.x / 2f * (float)(IsServingOnRightSide ? 1 : (-1));
		Vector3 vector = ServingSide.transform.TransformPoint(center);
		if (IsHumanPlaying())
		{
			ServingSide.player.transform.position = vector;
			ServingSide.player.goToPosition = Vector3.zero;
		}
		else
		{
			ServingSide.player.goToPosition = vector;
		}
		TennisCourtSide oppositeCourtSide = GetOppositeCourtSide(ServingSide);
		Vector3 localServeReceivePosition = oppositeCourtSide.localServeReceivePosition;
		if (!IsServingOnRightSide)
		{
			localServeReceivePosition.x = 0f - localServeReceivePosition.x;
		}
		vector = oppositeCourtSide.transform.TransformPoint(localServeReceivePosition);
		if (IsHumanPlaying())
		{
			oppositeCourtSide.player.transform.position = vector;
			oppositeCourtSide.player.goToPosition = Vector3.zero;
		}
		else
		{
			oppositeCourtSide.player.goToPosition = vector;
		}
		ServingSide.player.ResetState();
		oppositeCourtSide.player.ResetState();
		BallBouncedSide = null;
		LastHitterSide = null;
		AwaitingServe = true;
		HasServeReturned = false;
		TennisCourtSide[] array = courtSides;
		foreach (TennisCourtSide obj in array)
		{
			obj.SetServiceAreaAlpha(0f);
			obj.UpdateServiceAreaSide();
		}
	}

	public bool RayCastNet(Ray ray, out RaycastHit hitInfo, float maxDistance)
	{
		netCollider.enabled = true;
		bool result = netCollider.Raycast(ray, out hitInfo, maxDistance);
		netCollider.enabled = false;
		return result;
	}

	public Vector3 GetCursorAimedPosition()
	{
		Ray ray = GameManager.GetMainCamera().ScreenPointToRay(Input.mousePosition);
		extendedCourtCollider.enabled = true;
		Vector3 result = (extendedCourtCollider.Raycast(ray, out var hitInfo, 1000f) ? hitInfo.point : base.transform.position);
		extendedCourtCollider.enabled = false;
		return result;
	}

	public TennisCourtSide GetOppositeCourtSide(TennisCourtSide courtSide)
	{
		if (!courtSide)
		{
			return null;
		}
		TennisCourtSide[] array = courtSides;
		foreach (TennisCourtSide tennisCourtSide in array)
		{
			if (tennisCourtSide != courtSide)
			{
				return tennisCourtSide;
			}
		}
		return null;
	}

	private static float GetServiceAreaAlpha()
	{
		float t = Mathf.PingPong(Time.time / 1.5f, 1f);
		return Mathf.Lerp(0.1f, 0.75f, t);
	}

	public bool AreBothPlayersReadyForServe()
	{
		TennisCourtSide[] array = courtSides;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].player.goToPosition != Vector3.zero)
			{
				return false;
			}
		}
		return true;
	}

	public void OnBallCollisionEnter(Collision other)
	{
		if (!base.enabled)
		{
			return;
		}
		Vector3 position = Ball.transform.position;
		bool flag = true;
		TennisCourtSide ballBouncedSide = BallBouncedSide;
		if (other != null)
		{
			TennisCourtSide[] array = courtSides;
			foreach (TennisCourtSide tennisCourtSide in array)
			{
				if (tennisCourtSide.IsInBallBounds(position))
				{
					flag = false;
					BallBouncedSide = tennisCourtSide;
					break;
				}
			}
			if (IsHumanPlaying())
			{
				ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
				{
					position = position
				};
				_bounceParticles.Emit(emitParams, 4);
			}
			bounceSound.Stop();
			bounceSound.transform.position = Ball.transform.position;
			bounceSound.Play();
		}
		if (flag || !(ballBouncedSide != BallBouncedSide))
		{
			TennisCourtSide courtSide = (ballBouncedSide ? ballBouncedSide : LastHitterSide);
			TennisCourtSide oppositeCourtSide = GetOppositeCourtSide(courtSide);
			AwardPoint(oppositeCourtSide);
		}
	}

	public void AwardPoint(TennisCourtSide courtSide)
	{
		if (!courtSide)
		{
			throw new ArgumentNullException("courtSide");
		}
		if (!IsHumanPlaying())
		{
			AfterPointAwarded(courtSide);
			return;
		}
		int num = Array.IndexOf(courtSides, courtSide);
		if (num < 0)
		{
			throw new ArgumentException("Invalid court side");
		}
		_sideScores[num].points++;
		if (courtSide.player.IsPlayer)
		{
			_ui.PlayApplause();
			_ui.ShowNotification(isPositive: true, "ba:tennisui_you_scored");
		}
		else
		{
			_ui.ShowNotification(isPositive: false, "ba:tennisui_opponent_scored");
		}
		int points = _sideScores[num].points;
		int points2 = _sideScores[1 - num].points;
		if (points >= (InTieBreak ? 7 : 4) && points - points2 >= 2)
		{
			_sideScores[num].games++;
			for (int i = 0; i < _sideScores.Length; i++)
			{
				_sideScores[i].points = 0;
			}
			_pendingSwitchSides = true;
			bool flag = Mathf.Abs(_sideScores[0].games - _sideScores[1].games) >= 2;
			if (InTieBreak || ((_sideScores[num].games >= 6) & flag))
			{
				_sideScores[num].sets++;
				for (int j = 0; j < _sideScores.Length; j++)
				{
					_sideScores[j].games = 0;
				}
				if (IsGameOver())
				{
					_ui.ShowNotification(courtSide.player.IsPlayer, "ba:tennisui_game_set_match");
					if (courtSide.player.IsPlayer)
					{
						SaveGameManager.Current.achievementsData.tennisMatchesWon++;
						GameEvent.Invoke("ba:gameevent_tennismatchwon");
					}
				}
				else
				{
					_ui.ShowNotification(courtSide.player.IsPlayer, courtSide.player.IsPlayer ? "ba:tennisui_set_won" : "ba:tennisui_set_lost");
				}
			}
			else
			{
				_ui.ShowNotification(courtSide.player.IsPlayer, courtSide.player.IsPlayer ? "ba:tennisui_game_won" : "ba:tennisui_game_lost");
			}
		}
		AfterPointAwarded(courtSide);
	}

	private void AfterPointAwarded(TennisCourtSide courtSide)
	{
		UpdateScoreUI();
		SetBallActive(active: false);
		IsServingOnRightSide = !IsServingOnRightSide;
		_pendingNewTurn = true;
		_nextTurnTimer = nextTurnMinDelay;
		courtSide.player.OnPointWon();
		GetOppositeCourtSide(courtSide).player.OnPointLost();
	}

	private bool IsMatchPoint()
	{
		if (!IsHumanPlaying())
		{
			return false;
		}
		for (int i = 0; i < _sideScores.Length; i++)
		{
			if (_sideScores[i].sets != 1)
			{
				continue;
			}
			int num = 1 - i;
			int points = _sideScores[i].points;
			int points2 = _sideScores[num].points;
			int num2 = (InTieBreak ? 7 : 4);
			if (points + 1 >= num2 && points + 1 - points2 >= 2)
			{
				int num3 = _sideScores[i].games + 1;
				int games = _sideScores[num].games;
				if (InTieBreak || (num3 >= 6 && Mathf.Abs(num3 - games) >= 2))
				{
					return true;
				}
			}
		}
		return false;
	}

	private void UpdateScoreUI()
	{
		if ((bool)_ui)
		{
			for (int i = 0; i < courtSides.Length; i++)
			{
				int points = _sideScores[i].points;
				int points2 = _sideScores[1 - i].points;
				bool advantage = points > points2 && points >= 3 && points2 >= 3;
				_ui.scoreLines[i].UpdateScore(_sideScores[i], advantage, courtSides[i] == ServingSide);
			}
		}
	}

	public bool IsPopupActive()
	{
		if ((bool)_ui)
		{
			return _ui.IsPopupActive();
		}
		return false;
	}

	private bool IsGameOver()
	{
		if (!IsHumanPlaying())
		{
			return false;
		}
		TennisSideScore[] sideScores = _sideScores;
		for (int i = 0; i < sideScores.Length; i++)
		{
			if (sideScores[i].sets >= 2)
			{
				return true;
			}
		}
		return false;
	}

	private void OnClickPlayAgain()
	{
		if (GameManager.ChangeMoneySafe(-50f, TransactionInfo, null, null, force: false, showNotification: true))
		{
			ResetMatch();
			OnNewTurn();
		}
	}

	public TennisCourtSide GetSide(int index)
	{
		return courtSides[index];
	}

	public static bool IsInTestScene()
	{
		return SceneManager.GetActiveScene().name == "TennisTestScene";
	}

	[ConsoleMethod("Tennis.ForceGameWin", "Forces the player to win the current tennis game", new string[] { })]
	public static void ForceGameWin()
	{
		TennisCourt playingInstance = PlayingInstance;
		if (playingInstance == null || !playingInstance.IsHumanPlaying())
		{
			Debug.LogWarning("This command can only be used while playing tennis");
			return;
		}
		TennisCourtSide side = playingInstance.GetSide(0);
		if (side == null)
		{
			Debug.LogWarning("Unable to read the current tennis player side");
			return;
		}
		int games = playingInstance._sideScores[0].games;
		int sets = playingInstance._sideScores[0].sets;
		int sets2 = playingInstance._sideScores[1].sets;
		for (int i = 0; i < 32; i++)
		{
			playingInstance.AwardPoint(side);
			if (playingInstance._sideScores[0].games != games || playingInstance._sideScores[0].sets != sets || playingInstance._sideScores[1].sets != sets2)
			{
				return;
			}
		}
		Debug.LogWarning("Unable to force tennis game win");
	}
}
