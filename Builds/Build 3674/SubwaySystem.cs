using System.Collections;
using Buildings;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;
using Helpers;
using NaughtyAttributes;
using UI;
using UI.ItemPanel;
using UI.Overlays;
using UnityEngine;

public class SubwaySystem : MonoBehaviour
{
	private const string IndustryCityNeighborhood = "ba:neighborhood_industrycity";

	private const string TheHamptonsNeighborhood = "ba:neighborhood_thehamptons";

	[ReadOnly]
	public SubwayStation lastSubwayStation;

	[ReadOnly]
	public SubwayStation destinationSubwayStation;

	public Transform subwayCamTarget;

	public float travelSpeed;

	public Sprite poiIcon;

	public Color poiBackgroundColor;

	public AudioSource subwayStart;

	public AudioSource subwayStop;

	public AudioSource subwayLoop;

	public float subwayLowPassCutoffValue = 5000f;

	[SerializeField]
	public Vector3[] manhattanBridgeLmToIc;

	[SerializeField]
	public Vector3[] manhattanBridgeIcToLm;

	private float _subwayDefaultLowPassCutoffValue;

	public static bool IsRiding { get; private set; }

	public static Vector3 CurrentPosition { get; private set; }

	public void Start()
	{
		InstanceBehavior<SfxManager>.Instance.audioMixer.GetFloat("CityVehicleLowPassCutoff", out _subwayDefaultLowPassCutoffValue);
	}

	private void OnDestroy()
	{
		IsRiding = false;
	}

	public void TravelTo(SubwayStation subwayStation)
	{
		StartCoroutine(TravelToCoroutine(subwayStation));
	}

	private IEnumerator TravelToCoroutine(SubwayStation subwayStation)
	{
		if (!InstanceBehavior<CityManager>.Instance.cityMap.isSubwayMode || lastSubwayStation == null || subwayStation == lastSubwayStation)
		{
			yield break;
		}
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_subwayride");
		transactionInfo.SetTaxDeductibleName("ba:transaction_subwayride");
		if (!GameManager.ChangeMoneySafe(-3f, transactionInfo, null, null, force: false, showNotification: true))
		{
			yield break;
		}
		InstanceBehavior<GameManager>.Instance.playerController.Character.ToggleVisibility(show: false);
		InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.Subway);
		bool isItemPanelVisible = ItemPanelUI.IsVisible;
		if (isItemPanelVisible)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetVisibility(visible: false);
		}
		BuildingEntranceOverlay.Hide();
		LocationHappinessTrigger.RemoveCurrentLocationHappinessTriggerIfNeeded();
		destinationSubwayStation = subwayStation;
		subwayCamTarget.position = lastSubwayStation.transform.position;
		CurrentPosition = subwayCamTarget.position;
		GameObject itemPanelParkButton = InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.parkButton.gameObject;
		bool enableParkButtonOnEnd = false;
		if (itemPanelParkButton.activeSelf)
		{
			itemPanelParkButton.SetActive(value: false);
			enableParkButtonOnEnd = true;
		}
		IsRiding = true;
		yield return StartCoroutine(InstanceBehavior<CityManager>.Instance.cityMap.Toggle(forceOpen: false, Vector3.zero));
		yield return CameraHelper.SetCameraRoutine(InstanceBehavior<GameManager>.Instance.subwayCamera);
		Vector3[] path = GetPathToSubway(destinationSubwayStation);
		float pathSpeed = GetPathSpeed(path);
		subwayLoop.volume = 0f;
		subwayStart.Play();
		subwayLoop.Play();
		subwayLoop.DOFade(1f, 2f).SetUpdate(isIndependentUpdate: true);
		InstanceBehavior<SfxManager>.Instance.audioMixer.DOSetFloat("CityVehicleLowPassCutoff", subwayLowPassCutoffValue, 1f);
		InstanceBehavior<SfxManager>.Instance.audioMixer.DOSetFloat("CityAmbianceLowPassCutoff", subwayLowPassCutoffValue, 1f);
		TweenerCore<Vector3, Path, PathOptions> tweener = subwayCamTarget.DOPath(path, pathSpeed).OnWaypointChange(delegate(int i)
		{
			subwayCamTarget.LookAt(path[i]);
		}).SetEase(Ease.Linear)
			.SetLink(base.gameObject)
			.OnUpdate(delegate
			{
				CurrentPosition = subwayCamTarget.position;
			})
			.OnComplete(delegate
			{
				InstanceBehavior<SfxManager>.Instance.audioMixer.DOSetFloat("CityVehicleLowPassCutoff", _subwayDefaultLowPassCutoffValue, 1f).OnComplete(delegate
				{
					InstanceBehavior<SfxManager>.Instance.audioMixer.ClearFloat("CityVehicleLowPassCutoff");
				});
				InstanceBehavior<SfxManager>.Instance.audioMixer.DOSetFloat("CityAmbianceLowPassCutoff", _subwayDefaultLowPassCutoffValue, 1f).OnComplete(delegate
				{
					InstanceBehavior<SfxManager>.Instance.audioMixer.ClearFloat("CityAmbianceLowPassCutoff");
				});
				InstanceBehavior<GameManager>.Instance.playerController.Character.ToggleVisibility(show: true);
				InstanceBehavior<GameManager>.Instance.playerController.Character.WarpSafely(destinationSubwayStation.GetNavMeshTargetPosition());
				InstanceBehavior<GameManager>.Instance.playerController.Character.Reset();
				destinationSubwayStation = null;
				lastSubwayStation = null;
				IsRiding = false;
				if (enableParkButtonOnEnd)
				{
					itemPanelParkButton.SetActive(value: true);
				}
				InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.Subway);
				if (isItemPanelVisible)
				{
					InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetVisibility(visible: true);
				}
				CameraHelper.SetCamera(InstanceBehavior<GameManager>.Instance.pedestrianCamera);
				HappinessHelper.AddModifier("ba:happinessmodifier_subway");
			});
		StartCoroutine(OnStop(tweener, pathSpeed - 2f));
	}

	private IEnumerator OnStop(Tweener tweener, float time)
	{
		yield return tweener.WaitForPosition(time);
		subwayLoop.DOFade(0f, 1f).SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject)
			.OnComplete(delegate
			{
				subwayLoop.Stop();
			});
		subwayStop.Play();
	}

	private Vector3[] GetPathToSubway(SubwayStation targetSubwayStation)
	{
		Vector3 position = targetSubwayStation.transform.position;
		string neighbourhood = ClosestBuildingFromPlayer.Get().Neighbourhood;
		if (DoesTripGoThroughManhattanBridge(neighbourhood, targetSubwayStation.neighbourhood))
		{
			if (IsOnIndustryCityBridgeSide(neighbourhood))
			{
				return new Vector3[3]
				{
					manhattanBridgeIcToLm[0],
					manhattanBridgeIcToLm[1],
					position
				};
			}
			return new Vector3[3]
			{
				manhattanBridgeLmToIc[0],
				manhattanBridgeLmToIc[1],
				position
			};
		}
		return new Vector3[1] { position };
	}

	private float GetPathSpeed(Vector3[] path)
	{
		float num = Vector3.Distance(subwayCamTarget.position, path[0]);
		for (int i = 1; i < path.Length; i++)
		{
			num += Vector3.Distance(path[i - 1], path[i]);
		}
		return num / travelSpeed;
	}

	private static bool DoesTripGoThroughManhattanBridge(string currentNeighborhood, string targetNeighborhood)
	{
		return IsOnIndustryCityBridgeSide(currentNeighborhood) != IsOnIndustryCityBridgeSide(targetNeighborhood);
	}

	private static bool IsOnIndustryCityBridgeSide(string neighborhood)
	{
		if (!(neighborhood == "ba:neighborhood_industrycity"))
		{
			return neighborhood == "ba:neighborhood_thehamptons";
		}
		return true;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		IsRiding = false;
		CurrentPosition = Vector3.zero;
	}
}
