using System;
using System.Collections;
using System.Linq;
using BigAmbitions.DayNightCycle;
using Buildings;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Extensions;
using Helpers;
using IngameDebugConsole;
using Localizor;
using Localizor.LanguageChangeEvent;
using Parking.UndergroundParking;
using UI;
using UI.Overlays;
using UI.Purchase;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class CasinoBoatManager : InstanceBehavior<CasinoBoatManager>
{
	public const int ClosingHour = 5;

	public static bool boatSailInSequenceStarted;

	public bool ticketBought;

	private static readonly int InPier = Animator.StringToHash("InPier");

	public Animator boatAnimator;

	public bool boatIsInHarbor;

	public BoatMusic boatMusic;

	public bool kickOutSequenceStarted;

	private CasinoBuildingController _casinoBuildingController;

	private TweenerCore<Vector3, Vector3, VectorOptions> _boatTravelTween;

	private Cloth[] _boatFlags;

	public static Building CasinoBuilding { get; private set; }

	public static bool IsOnCasinoBoat
	{
		get
		{
			if (!InstanceBehavior<CasinoBoatManager>.Instance || !InstanceBehavior<CasinoBoatManager>.Instance.ticketBought)
			{
				if (BuildingManager.IsInsideBuilding)
				{
					return InstanceBehavior<BuildingManager>.Instance.building == CasinoBuilding;
				}
				return false;
			}
			return true;
		}
	}

	public IEnumerator Start()
	{
		CasinoBuilding = BuildingHelper.GetBuilding(new Address("ba:street_pier", 5));
		_boatFlags = GetComponentsInChildren<Cloth>();
		GlobalEvents.onNewHour = (Action)Delegate.Combine(GlobalEvents.onNewHour, new Action(OnNewHour));
		GlobalEvents.onTimeMachineEnded = (Action)Delegate.Combine(GlobalEvents.onTimeMachineEnded, new Action(OnTimeMachineEnded));
		GlobalEvents.RegisterOnGameLoadedCallback(delegate
		{
			_casinoBuildingController = InstanceBehavior<CityManager>.Instance.cityBuildingControllers.FirstOrDefault((CityBuildingController x) => x is CasinoBuildingController) as CasinoBuildingController;
		});
		yield return null;
		if (TimeHelper.GetDayOfWeek() == DayOfWeekOrdered.Friday)
		{
			if (SaveGameManager.Current.Hour.InRange(16, 17))
			{
				boatAnimator.SetBool(InPier, value: true);
				float num = (float)((SaveGameManager.Current.Hour - 16) * 60) + SaveGameManager.Current.Minute;
				boatAnimator.Play("BoatSailIn", -1, num / 120f);
				ResetClothPhysics();
			}
			if (SaveGameManager.Current.Hour.InRange(18, 22))
			{
				boatIsInHarbor = true;
				boatAnimator.SetBool(InPier, value: true);
				boatAnimator.Play("BoatAtPier");
				ResetClothPhysics();
			}
		}
	}

	private void OnTimeMachineEnded()
	{
		if (TimeHelper.GetDayOfWeek() == DayOfWeekOrdered.Friday)
		{
			if (SaveGameManager.Current.Hour.InRange(16, 17))
			{
				float num = (float)((SaveGameManager.Current.Hour - 16) * 60) + SaveGameManager.Current.Minute;
				boatAnimator.Play("BoatSailIn", -1, num / 120f);
				ResetClothPhysics();
			}
			if (SaveGameManager.Current.Hour.InRange(22, 23))
			{
				float num2 = (float)((SaveGameManager.Current.Hour - 22) * 60) + SaveGameManager.Current.Minute;
				boatAnimator.Play("BoatSailOut", -1, num2 / 120f);
				ResetClothPhysics();
			}
		}
	}

	private void OnNewHour()
	{
		if (TimeHelper.GetDayOfWeek() == DayOfWeekOrdered.Friday)
		{
			if (SaveGameManager.Current.Hour == 16)
			{
				DoBoatSailIn();
			}
			if (SaveGameManager.Current.Hour == 18)
			{
				boatAnimator.Play("BoatAtPier");
				boatIsInHarbor = true;
				ResetClothPhysics();
			}
			if (SaveGameManager.Current.Hour == 22 && boatIsInHarbor)
			{
				DoBoatSailOut();
			}
		}
		else if (TimeHelper.GetDayOfWeek() == DayOfWeekOrdered.Saturday)
		{
			if (SaveGameManager.Current.Hour == 0)
			{
				boatAnimator.Play("BoatSailGone");
				ResetClothPhysics();
			}
			if (SaveGameManager.Current.Hour == 5 && IsOnCasinoBoat)
			{
				KickOutPlayer(forcedKickout: true);
			}
			if (SaveGameManager.Current.Hour == 8)
			{
				boatAnimator.Play("BoatSailGone");
				ResetClothPhysics();
				boatMusic.StopMusic();
			}
		}
	}

	public void KickOutPlayer(bool forcedKickout = false)
	{
		InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
		if (!forcedKickout)
		{
			LanguageChangeEventDataHolder bodyData = "casinoboatmanager_leaveboat".Localize();
			Action onConfirmAction = _KickOutPlayer;
			HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, onConfirmAction);
		}
		else
		{
			_KickOutPlayer();
		}
	}

	private void _KickOutPlayer()
	{
		if (!kickOutSequenceStarted)
		{
			kickOutSequenceStarted = true;
			StartCoroutine(StartKickOutPlayerSequenceCoroutine());
		}
	}

	private void ResetClothPhysics()
	{
		Cloth[] boatFlags = _boatFlags;
		for (int i = 0; i < boatFlags.Length; i++)
		{
			boatFlags[i].ClearTransformMotion();
		}
	}

	private IEnumerator StartKickOutPlayerSequenceCoroutine()
	{
		HudConfirm.onClose?.Invoke();
		PlayerController player = InstanceBehavior<GameManager>.Instance.playerController;
		if (InstanceBehavior<UIs>.Instance.playerHUD.dialogUI.isPanelOpen)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.dialogUI.ForceCancel();
			InstanceBehavior<UIs>.Instance.playerHUD.dialogUI.CancelDialog();
		}
		if (PurchaseUI.IsPanelOpen)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Close();
		}
		if (InstanceBehavior<UIs>.Instance.playerActivityUI.GetCurrentActivity != null)
		{
			InstanceBehavior<UIs>.Instance.playerActivityUI.CancelActivity();
		}
		player.ResetNavigation();
		player.SetNavigationBlocker(NavigationBlocker.CasinoExitSequence);
		InstanceBehavior<UIs>.Instance.casinoMessage.ShowMessage(CasinoMessageUI.CasinoMessage.casino_message_trip_over, 4f);
		yield return new WaitForSeconds(3f);
		yield return UiFader.Fade();
		DoBoatSailIn();
		Timestamp timestamp = TimeHelper.Now();
		timestamp.Minute = 40f;
		timestamp.Hour = 6;
		InstanceBehavior<UIs>.Instance.timeMachine.StartTimeMachine(timestamp, disableCancel: true, "casino_timemachine_info_end");
		yield return new WaitUntil(() => !InstanceBehavior<UIs>.Instance.timeMachine.isRunning);
		InstanceBehavior<UIs>.Instance.screenshot.ToggleUIVisibility();
		boatAnimator.Play("BoatSailIn", -1, 11f / 12f);
		yield return CameraHelper.SetCameraRoutine(InstanceBehavior<GameManager>.Instance.boatCinemacticCamera);
		ResetClothPhysics();
		yield return new WaitForSeconds(1.5f);
		InstanceBehavior<GameManager>.Instance.timeOfDayController.OnExitBuilding();
		InstanceBehavior<BuildingManager>.Instance.indoorVolume.enabled = false;
		if ((bool)InstanceBehavior<BuildingManager>.Instance.extraIndoorsDirectionalLight)
		{
			InstanceBehavior<BuildingManager>.Instance.extraIndoorsDirectionalLight.gameObject.SetActive(value: false);
		}
		Transform transform = _casinoBuildingController.ticketHouse.transform;
		GameManager.GetMainCamera().GetComponent<HDAdditionalCameraData>().volumeAnchorOverride = transform;
		Transform rain = InstanceBehavior<GameManager>.Instance.playerController.transform.Find("Rain");
		if (rain != null)
		{
			rain.SetParent(transform);
			rain.localPosition = new Vector3(0f, rain.localPosition.y, 0f);
		}
		yield return UiFader.UnFade();
		yield return new WaitForSeconds(9f);
		yield return InstanceBehavior<BuildingManager>.Instance.ExitFromBuildingCoroutine(0);
		GameManager.GetMainCamera().GetComponent<HDAdditionalCameraData>().volumeAnchorOverride = InstanceBehavior<GameManager>.Instance.playerController.transform;
		if (rain != null)
		{
			rain.SetParent(InstanceBehavior<GameManager>.Instance.playerController.transform);
			rain.localPosition = new Vector3(0f, rain.localPosition.y, 0f);
		}
		boatMusic.SetIndoors(indoors: false);
		GameEvent.Invoke("ba:gameevent_returnedtothecity");
		yield return player.Character.MoveToPosition(player.transform, _casinoBuildingController.ticketHouse.GetNavMeshTargetPosition());
		InstanceBehavior<UIs>.Instance.screenshot.ToggleUIVisibility();
		DoBoatSailOut();
		player.UnsetNavigationBlocker(NavigationBlocker.CasinoExitSequence);
		ticketBought = false;
		kickOutSequenceStarted = false;
		yield return new WaitForSeconds(120f);
		boatMusic.StopMusic();
	}

	public void StartSailOutSequence()
	{
		if (!boatSailInSequenceStarted)
		{
			boatSailInSequenceStarted = true;
			ticketBought = true;
			boatMusic.PlayMusic();
			StartCoroutine(StartSailOutSequenceCoroutine());
		}
	}

	private IEnumerator StartSailOutSequenceCoroutine()
	{
		SaveGameManager.Current.achievementsData.casinoBoatVisits++;
		GameEvent.Invoke(string.Empty);
		PlayerController player = InstanceBehavior<GameManager>.Instance.playerController;
		InstanceBehavior<UIs>.Instance.screenshot.ToggleUIVisibility();
		yield return CameraHelper.SetCameraRoutine(InstanceBehavior<GameManager>.Instance.boatCinemacticCamera);
		InstanceBehavior<UIs>.Instance.gameSpeed.SetPause(newPause: false);
		player.ResetNavigation();
		player.SetNavigationBlocker(NavigationBlocker.CasinoEntrySequence);
		yield return player.Character.MoveToPosition(player.transform, _casinoBuildingController.entranceDoors[0].doorTransform.position);
		player.Character.ToggleVisibility(show: false);
		BuildingEntranceOverlay.Hide();
		DoBoatSailOut();
		yield return new WaitForSecondsRealtime(10f);
		yield return UiFader.Fade();
		Timestamp timestamp = TimeHelper.Now();
		timestamp.Minute = 0f;
		timestamp.Hour = 0;
		timestamp.Day++;
		InstanceBehavior<UIs>.Instance.screenshot.ToggleUIVisibility();
		InstanceBehavior<UIs>.Instance.timeMachine.StartTimeMachine(timestamp, disableCancel: true, "casino_timemachine_info_start");
		yield return new WaitUntil(() => !InstanceBehavior<UIs>.Instance.timeMachine.isRunning);
		InstanceBehavior<BuildingManager>.Instance.EnterBuilding(CasinoBuilding);
		boatMusic.SetIndoors();
		player.UnsetNavigationBlocker(NavigationBlocker.CasinoEntrySequence);
		player.Character.ToggleVisibility(show: true);
		yield return new WaitForSecondsRealtime(1f);
		yield return UiFader.UnFade();
		boatSailInSequenceStarted = false;
		InstanceBehavior<UIs>.Instance.casinoMessage.ShowMessage(CasinoMessageUI.CasinoMessage.casino_message_welcome, 4f);
	}

	private void DoBoatSailIn()
	{
		boatAnimator.SetBool(InPier, value: true);
	}

	private void DoBoatSailOut()
	{
		boatIsInHarbor = false;
		boatAnimator.SetBool(InPier, value: false);
	}

	[ConsoleMethod("GoToCasino", "Instantly teleports to the casino", new string[] { })]
	public static void Command_GoToCasino()
	{
		if (BuildingManager.IsInsideBuilding)
		{
			InstanceBehavior<BuildingManager>.Instance.ExitFromBuilding(0);
		}
		if (UndergroundParkingManager.IsInsideParking)
		{
			UndergroundParkingManager.ExitParking(playFadeAnimation: false);
		}
		InstanceBehavior<BuildingManager>.Instance.EnterBuilding(CasinoBuilding);
		if (InstanceBehavior<CasinoBoatManager>.Instance != null)
		{
			InstanceBehavior<CasinoBoatManager>.Instance.boatMusic.SetIndoors();
		}
		InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
		InstanceBehavior<UIs>.Instance.casinoMessage.ShowMessage(CasinoMessageUI.CasinoMessage.casino_message_welcome, 4f);
	}

	[ConsoleMethod("KickOutPlayerFromCasino", "Instantly kicks the player out from the casino", new string[] { })]
	public static void Command_KickOutPlayerFromCasino(bool forced = false)
	{
		if (IsOnCasinoBoat)
		{
			InstanceBehavior<CasinoBoatManager>.Instance.KickOutPlayer(forced);
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		boatSailInSequenceStarted = false;
		CasinoBuilding = null;
	}
}
