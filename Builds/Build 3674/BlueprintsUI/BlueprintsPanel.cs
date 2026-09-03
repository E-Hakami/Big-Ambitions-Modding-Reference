using System;
using System.Collections.Generic;
using System.Threading;
using BigAmbitions.InputSystem;
using Blueprints;
using CameraControllers;
using DG.Tweening;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Localizor.LanguageChangeEvent;
using UI.Load;
using UnityEngine;
using UnityEngine.UI;

namespace BlueprintsUI;

public class BlueprintsPanel : MonoBehaviour
{
	public static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

	[SerializeField]
	private Transform categoryTemplate;

	[SerializeField]
	private BlueprintsListUI blueprintsListUI;

	[SerializeField]
	private RectTransform splitterIndicator;

	[SerializeField]
	private float indicatorAnimationTime = 0.2f;

	[SerializeField]
	private Button openBlueprintCreatorButton;

	[SerializeField]
	private GameObject blueprintCreatorTooltip;

	private const BlueprintCategory InitialBlueprintCategory = BlueprintCategory.Gallery;

	private readonly Dictionary<BlueprintCategory, RectTransform> _categoryButtonRectTransforms = new Dictionary<BlueprintCategory, RectTransform>();

	public static bool IsOpen { get; private set; }

	public bool IsBlueprintInfoOpen => blueprintsListUI.IsSelectedBlueprintPanelOpen;

	public bool IsWorkshopConfirmOpen => blueprintsListUI.IsWorkshopConfirmPanelOpen;

	private void Awake()
	{
		BlueprintsFolderLoader.Init();
	}

	private void Update()
	{
		if (PlayerAction.Cancel.Pressed() && (bool)InstanceBehavior<MainMenuController>.Instance)
		{
			if (IsWorkshopConfirmOpen)
			{
				CloseWorkshopConfirm();
			}
			else if (IsBlueprintInfoOpen)
			{
				CloseBlueprintInfo();
			}
			else
			{
				Hide();
			}
		}
	}

	private void OnDestroy()
	{
		IsOpen = false;
		PedestrianCam.blockCameraZoom = false;
	}

	public void Show(BlueprintCategory category = BlueprintCategory.Gallery)
	{
		cancellationTokenSource.Dispose();
		cancellationTokenSource = new CancellationTokenSource();
		PedestrianCam.blockCameraZoom = true;
		base.gameObject.SetActive(value: true);
		IsOpen = true;
		openBlueprintCreatorButton.interactable = InstanceBehavior<MainMenuController>.Instance != null;
		blueprintCreatorTooltip.SetActive(InstanceBehavior<MainMenuController>.Instance == null);
		SetUpCategories();
		if (!cancellationTokenSource.Token.IsCancellationRequested)
		{
			SelectCategory(category);
		}
	}

	public static void OnBlueprintsLoadingCancelled()
	{
		LoadingSpinner.Hide();
	}

	private void SetUpCategories()
	{
		categoryTemplate.ResetTemplate();
		_categoryButtonRectTransforms.Clear();
		foreach (BlueprintCategory blueprintCategory in Enum.GetValues(typeof(BlueprintCategory)))
		{
			if ((blueprintCategory != BlueprintCategory.DevBusinessLayouts && blueprintCategory != BlueprintCategory.DevInteriorDesigns && blueprintCategory != BlueprintCategory.Feedback) || GameManager.IsDevMode)
			{
				Transform obj = categoryTemplate.CreateElement();
				obj.GetComponent<TextLocalizationComponent>().Key = blueprintCategory.GetLocalizeKey();
				Button component = obj.GetComponent<Button>();
				component.onClick.AddListener(delegate
				{
					SelectCategory(blueprintCategory);
				});
				_categoryButtonRectTransforms.Add(blueprintCategory, component.GetRectTransform());
			}
		}
	}

	private void SelectCategory(BlueprintCategory blueprintCategory)
	{
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			RectTransform rectTransform = _categoryButtonRectTransforms[blueprintCategory];
			splitterIndicator.sizeDelta = new Vector2(rectTransform.rect.width, splitterIndicator.sizeDelta.y);
			splitterIndicator.DOMoveX(rectTransform.position.x, indicatorAnimationTime).SetLink(splitterIndicator.gameObject).SetUpdate(isIndependentUpdate: true);
		});
		blueprintsListUI.Open(blueprintCategory);
	}

	public async void SyncToWorkshop()
	{
		if (!LoadingSpinner.isLoading)
		{
			LoadingSpinner.Show();
			await blueprintsListUI.ReloadBlueprints(clearCaches: true);
			if (!cancellationTokenSource.Token.IsCancellationRequested)
			{
				SelectCategory(BlueprintCategory.Gallery);
				LoadingSpinner.Hide();
			}
		}
	}

	public void Hide()
	{
		cancellationTokenSource.Cancel();
		blueprintsListUI.Close();
		categoryTemplate.ResetTemplate();
		base.gameObject.SetActive(value: false);
		IsOpen = false;
		PedestrianCam.blockCameraZoom = false;
		GameObject.Find("UI")?.transform.Find("StartView")?.gameObject.SetActive(value: true);
	}

	public void CloseWorkshopConfirm()
	{
		blueprintsListUI.CloseWorkshopConfirm();
	}

	public void CloseBlueprintInfo()
	{
		blueprintsListUI.CloseBlueprintInfo();
	}

	public void OpenBlueprintCreator()
	{
		IsOpen = false;
		LoadScene.LoadBlueprintCreator();
	}
}
