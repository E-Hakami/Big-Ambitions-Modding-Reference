using System;
using System.Collections;
using Cinemachine;
using DG.Tweening;
using Helpers;
using Items.SpecialItems.VideoGames;
using Localizor.LanguageChangeEvent;
using Player.HUD.ItemInfoOverlays;
using PlayerActivity;
using UI;
using UI.InteriorDesigner;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Controllers;

public class VideoGameSetup : MonoBehaviour
{
	private const float CameraRotationTransitionDuration = 0.7f;

	private static GameObject InstanceContainer;

	private static VideoGameSetup PlayingInstance;

	private static readonly ActivityWithoutUI ActivityInstance = new ActivityWithoutUI();

	private static CinemachineVirtualCameraBase InitialCamera;

	private static RenderTexture ScreenTexture;

	private static GameObject VideoGameVolumeInstance;

	private static readonly LanguageChangeEventDataHolder ItemPanelLabel = LanguageChangeEventDataHolder.Create("playpanel_headline");

	[SerializeField]
	private Transform cameraTransform;

	[SerializeField]
	private BoxCollider screenCollider;

	[SerializeField]
	private Vector2Int screenResolution;

	[SerializeField]
	private AssetReferenceGameObject gamePrefabReference;

	[SerializeField]
	private PlayerActivityBalanceConfig fallbackBalanceConfig;

	private Vector3 _lastPosition = Vector3.zero;

	private AsyncOperationHandle<GameObject> _gameHandle;

	private IVideoGame _gameInstance;

	private PlayerActivityBalanceConfig _balanceConfig;

	private bool IsPlaying => PlayingInstance == this;

	public void Start()
	{
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		GlobalEvents.onHospitalRespawnStarts = (Action)Delegate.Combine(GlobalEvents.onHospitalRespawnStarts, new Action(Finish));
		InteriorDesignerUI.onOpenInteriorDesigner = (Action)Delegate.Combine(InteriorDesignerUI.onOpenInteriorDesigner, new Action(Finish));
	}

	public void OnDestroy()
	{
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		GlobalEvents.onHospitalRespawnStarts = (Action)Delegate.Remove(GlobalEvents.onHospitalRespawnStarts, new Action(Finish));
		InteriorDesignerUI.onOpenInteriorDesigner = (Action)Delegate.Remove(InteriorDesignerUI.onOpenInteriorDesigner, new Action(Finish));
		if (IsPlaying && !GameManager.isCitySceneBeingUnloaded)
		{
			Finish();
		}
	}

	private void OnExitBuilding(Address _)
	{
		Finish();
	}

	public void StartPlaying()
	{
		if (!PlayingInstance)
		{
			PlayingInstance = this;
			_lastPosition = PlayerHelper.GetPosition();
			InitialCamera = CameraHelper.GetCurrentCamera();
			InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
			EnergyHelper.RemoveEnergySpender("move");
			ThirdPersonCharacter character = InstanceBehavior<GameManager>.Instance.playerController.Character;
			Vector3 position = PlayerHelper.GetPosition();
			Quaternion rotation = Quaternion.LookRotation(base.transform.position - position);
			character.ForceToRotation(rotation);
			character.ToggleVisibility(show: false, includePhysics: false);
			_balanceConfig = GetBalanceConfig();
			_balanceConfig.EnableTemporalBoost(character);
			InstanceBehavior<OverlayManager>.Instance.HideOverlays();
			CinemachineVirtualCameraBase dummyCamera = InstanceBehavior<GameManager>.Instance.dummyCamera;
			dummyCamera.transform.DOKill();
			dummyCamera.transform.SetPositionAndRotation(cameraTransform.position, InitialCamera.transform.rotation);
			dummyCamera.transform.DORotate(cameraTransform.eulerAngles, 0.7f).SetLink(dummyCamera.gameObject);
			CameraHelper.SetCamera(dummyCamera);
			ComputerController componentInParent = GetComponentInParent<ComputerController>();
			if ((bool)componentInParent)
			{
				componentInParent.Occupied = true;
			}
			SetupScreen();
			LoadGamePrefab();
			InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.ComputerVideoGame);
			PlayerActivityUI.SetActivityWithoutUI(ActivityInstance);
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetMiniGameMode(ItemPanelLabel);
			InstanceBehavior<UIs>.Instance.playerHUD.currentBuildingUI.Toggle(on: false);
			InstanceBehavior<UIs>.Instance.tasksUI.ChangeVisibility(show: false);
			InstanceBehavior<UIs>.Instance.smartphoneUI.gameObject.SetActive(value: false);
		}
	}

	private void SetupScreen()
	{
		screenCollider.gameObject.SetActive(value: true);
		if ((bool)ScreenTexture)
		{
			ScreenTexture.Release();
		}
		ScreenTexture = new RenderTexture(screenResolution.x, screenResolution.y, 16, RenderTextureFormat.ARGB32);
		ScreenTexture.Create();
		if (!VideoGameVolumeInstance && (bool)InstanceBehavior<GlobalReferences>.Instance.videoGameVolumePrefab)
		{
			VideoGameVolumeInstance = UnityEngine.Object.Instantiate(InstanceBehavior<GlobalReferences>.Instance.videoGameVolumePrefab);
		}
	}

	public static bool IsAnyVideoGamePlaying()
	{
		return PlayingInstance;
	}

	public static void RequestFinish()
	{
		if ((bool)PlayingInstance)
		{
			PlayingInstance.Finish();
		}
	}

	public static void RepositionCamera()
	{
		if ((bool)PlayingInstance)
		{
			Transform transform = PlayingInstance.cameraTransform;
			CinemachineVirtualCameraBase dummyCamera = InstanceBehavior<GameManager>.Instance.dummyCamera;
			dummyCamera.transform.DOKill();
			dummyCamera.transform.SetPositionAndRotation(transform.position, transform.rotation);
		}
	}

	private void LoadGamePrefab()
	{
		if (!InstanceContainer)
		{
			InstanceContainer = new GameObject("VideoGameContainer");
			InstanceContainer.transform.position = new Vector3(0f, -1000f, 0f);
		}
		InstanceContainer.SetActive(value: false);
		AsyncOperationHandle<GameObject> asyncOperationHandle = gamePrefabReference.InstantiateAsync(InstanceContainer.transform);
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> handle)
		{
			if (handle.Status == AsyncOperationStatus.Succeeded)
			{
				_gameHandle = handle;
				InstanceContainer.SetActive(value: true);
				IVideoGame.CursorViewportPosition = new Vector3(0.5f, 0.5f);
				GameObject result = handle.Result;
				_gameInstance = result.GetComponent<IVideoGame>();
				if (_gameInstance == null)
				{
					Debug.LogError("The loaded video game prefab does not implement IVideoGame.");
				}
				else
				{
					Camera camera = _gameInstance.GetCamera();
					if (!camera)
					{
						Debug.LogError("The loaded video game prefab does not provide a camera.");
					}
					else
					{
						camera.targetTexture = ScreenTexture;
						GetComponentInParent<ScreenVideoController>().SetRenderTexture(ScreenTexture);
						_gameInstance.SetScreenResolution(ScreenTexture.width, ScreenTexture.height);
						_gameInstance.SetMusicState(!SaveGameManager.Current.minigameMusicDisabled, OnMusicToggle);
						AudioMixerGroup outputAudioMixerGroup = ((BuildingManager.IsInsideBuilding && !InstanceBehavior<BuildingManager>.Instance.building.IsHamptonsHouse()) ? InstanceBehavior<GlobalReferences>.Instance.indoorMixerGroup : InstanceBehavior<GlobalReferences>.Instance.foleyMixerGroup);
						AudioSource[] componentsInChildren = result.GetComponentsInChildren<AudioSource>(includeInactive: true);
						for (int i = 0; i < componentsInChildren.Length; i++)
						{
							componentsInChildren[i].outputAudioMixerGroup = outputAudioMixerGroup;
						}
						StartCoroutine(UpdateCoroutine());
					}
				}
			}
			else
			{
				Debug.LogError("Failed to load video game prefab.");
			}
		};
	}

	private static void OnMusicToggle(bool enableMusic)
	{
		SaveGameManager.Current.minigameMusicDisabled = !enableMusic;
	}

	private void Finish()
	{
		if (!IsPlaying)
		{
			return;
		}
		PlayingInstance = null;
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.ComputerVideoGame);
		ThirdPersonCharacter character = InstanceBehavior<GameManager>.Instance.playerController.Character;
		character.Reset();
		_balanceConfig?.DisableTemporalBoost(character);
		_balanceConfig = null;
		character.ToggleVisibility(show: true, includePhysics: false);
		if (_lastPosition != Vector3.zero)
		{
			character.WarpSafely(_lastPosition);
			_lastPosition = Vector3.zero;
		}
		if ((bool)InitialCamera)
		{
			CameraHelper.SetCamera(InitialCamera);
			InitialCamera = null;
		}
		if (_gameInstance != null)
		{
			MonoBehaviour monoBehaviour = (MonoBehaviour)_gameInstance;
			if ((bool)monoBehaviour)
			{
				UnityEngine.Object.Destroy(monoBehaviour.gameObject);
			}
			_gameInstance = null;
		}
		if (_gameHandle.IsValid())
		{
			Addressables.ReleaseInstance(_gameHandle);
			_gameHandle = default(AsyncOperationHandle<GameObject>);
		}
		if ((bool)ScreenTexture)
		{
			ScreenTexture.Release();
			ScreenTexture = null;
		}
		if ((bool)VideoGameVolumeInstance)
		{
			UnityEngine.Object.Destroy(VideoGameVolumeInstance);
			VideoGameVolumeInstance = null;
		}
		ComputerController componentInParent = GetComponentInParent<ComputerController>();
		if ((bool)componentInParent)
		{
			componentInParent.Occupied = false;
		}
		GetComponentInParent<ScreenVideoController>()?.Stop();
		screenCollider.gameObject.SetActive(value: false);
		PlayerActivityUI.SetActivityWithoutUI(null);
		InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.Toggle(isEnabled: false);
		if (BuildingManager.IsInsideBuilding)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.currentBuildingUI.Toggle(on: true);
		}
		InstanceBehavior<UIs>.Instance.tasksUI.ChangeVisibility(show: true);
		InstanceBehavior<UIs>.Instance.smartphoneUI.gameObject.SetActive(value: true);
	}

	private IEnumerator UpdateCoroutine()
	{
		Camera uiCamera = GameManager.GetMainCamera();
		while (IsPlaying)
		{
			Ray ray = uiCamera.ScreenPointToRay(Input.mousePosition);
			if (screenCollider.Raycast(ray, out var hitInfo, 100f))
			{
				Vector3 vector = screenCollider.transform.InverseTransformPoint(hitInfo.point);
				Vector3 size = screenCollider.size;
				IVideoGame.CursorViewportPosition = new Vector2(vector.x / size.x + 0.5f, vector.y / size.y + 0.5f);
			}
			yield return null;
		}
	}

	private PlayerActivityBalanceConfig GetBalanceConfig()
	{
		ComputerController componentInParent = GetComponentInParent<ComputerController>();
		if (!(componentInParent != null))
		{
			return fallbackBalanceConfig;
		}
		return componentInParent.VideoGameBalanceConfig;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		InitialCamera = null;
		PlayingInstance = null;
		ScreenTexture = null;
	}
}
