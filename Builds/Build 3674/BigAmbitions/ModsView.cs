using System.Collections.Generic;
using BigAmbitions.InputSystem;
using BigAmbitions.ModsInternal;
using DG.Tweening;
using UnityEngine;

namespace BigAmbitions;

public class ModsView : MonoBehaviour
{
	private const string ModsWarningValue = "community_mods_warning";

	private const float ShowStartScale = 0.92f;

	private const float PopInDuration = 0.22f;

	private const float FadeInDuration = 0.14f;

	private static readonly Vector2 ShowPositionOffset = new Vector2(0f, -10f);

	[SerializeField]
	private RectTransform modsPanel;

	[SerializeField]
	private RectTransform uploadPanel;

	[SerializeField]
	private RectTransform background;

	[SerializeField]
	private CanvasGroup modsPanelCanvasGroup;

	[SerializeField]
	private CanvasGroup uploadPanelCanvasGroup;

	[SerializeField]
	private CanvasGroup backgroundCanvasGroup;

	private bool _isModsPanelOpen;

	private bool _isUploadPanelOpen;

	private void Awake()
	{
		ModManifest.Initialize();
	}

	private void Start()
	{
		modsPanel.gameObject.SetActive(value: false);
		uploadPanel.gameObject.SetActive(value: false);
		background.gameObject.SetActive(value: false);
	}

	private void Update()
	{
		if (PlayerAction.Cancel.Pressed())
		{
			if (_isUploadPanelOpen)
			{
				ShowUploadPanel(isOn: false);
			}
			else if (_isModsPanelOpen)
			{
				ShowModsPanel(isOn: false);
			}
		}
	}

	public void ShowModsPanel(bool isOn)
	{
		if (_isModsPanelOpen == isOn)
		{
			return;
		}
		if (isOn)
		{
			AcknowledgeWarning.Show("community_mods_warning", OpenModsPanel, "main_menu_mods_warning_header", "main_menu_mods_warning_body");
			return;
		}
		HidePanel(modsPanel, modsPanelCanvasGroup);
		if (ModManifest.HasChangedSinceSnapshot())
		{
			OnManifestChanged();
		}
		HideBackground();
		_isModsPanelOpen = isOn;
	}

	private void OpenModsPanel()
	{
		if (!_isModsPanelOpen)
		{
			ModManifest.TakeSnapshot();
			ShowPanel(modsPanel, modsPanelCanvasGroup);
			ShowBackground();
			_isModsPanelOpen = true;
		}
	}

	public void ShowUploadPanel(bool isOn)
	{
		if (_isUploadPanelOpen != isOn)
		{
			if (isOn)
			{
				HidePanel(modsPanel, modsPanelCanvasGroup);
				ShowPanel(uploadPanel, uploadPanelCanvasGroup);
			}
			else
			{
				HidePanel(uploadPanel, uploadPanelCanvasGroup);
				ShowPanel(modsPanel, modsPanelCanvasGroup);
			}
			_isUploadPanelOpen = isOn;
		}
	}

	private static void ShowPanel(RectTransform panelTransform, CanvasGroup canvasGroup)
	{
		panelTransform.gameObject.SetActive(value: true);
		panelTransform.DOKill(complete: true);
		canvasGroup.DOKill(complete: true);
		canvasGroup.alpha = 0f;
		canvasGroup.blocksRaycasts = true;
		canvasGroup.interactable = true;
		Vector2 anchoredPosition = panelTransform.anchoredPosition;
		panelTransform.localScale = Vector3.one * 0.92f;
		panelTransform.anchoredPosition = anchoredPosition + ShowPositionOffset;
		Sequence s = DOTween.Sequence().SetUpdate(isIndependentUpdate: true);
		s.Join(canvasGroup.DOFade(1f, 0.14f).SetEase(Ease.OutQuad));
		s.Join(panelTransform.DOScale(1f, 0.22f).SetEase(Ease.OutBack));
		s.Join(panelTransform.DOAnchorPos(anchoredPosition, 0.22f).SetEase(Ease.OutBack));
	}

	private static void HidePanel(RectTransform panelTransform, CanvasGroup canvasGroup)
	{
		Vector2 vector = new Vector2(0f, -6f);
		GameObject panelObject = panelTransform.gameObject;
		if (panelObject.activeSelf)
		{
			panelTransform.DOKill(complete: true);
			canvasGroup.DOKill(complete: true);
			canvasGroup.blocksRaycasts = false;
			canvasGroup.interactable = false;
			Vector2 originalAnchoredPosition = panelTransform.anchoredPosition;
			Sequence sequence = DOTween.Sequence().SetUpdate(isIndependentUpdate: true);
			sequence.Join(canvasGroup.DOFade(0f, 0.12f).SetEase(Ease.InQuad));
			sequence.Join(panelTransform.DOScale(0.92f, 0.16f).SetEase(Ease.InQuad));
			sequence.Join(panelTransform.DOAnchorPos(originalAnchoredPosition + vector, 0.16f).SetEase(Ease.InQuad));
			sequence.OnComplete(delegate
			{
				panelTransform.anchoredPosition = originalAnchoredPosition;
				panelObject.SetActive(value: false);
			});
		}
	}

	private void ShowBackground()
	{
		background.gameObject.SetActive(value: true);
		background.DOKill(complete: true);
		backgroundCanvasGroup.DOKill(complete: true);
		backgroundCanvasGroup.alpha = 0f;
		backgroundCanvasGroup.blocksRaycasts = true;
		backgroundCanvasGroup.interactable = true;
		backgroundCanvasGroup.DOFade(1f, 0.14f).SetEase(Ease.OutQuad).SetUpdate(isIndependentUpdate: true);
	}

	private void HideBackground()
	{
		GameObject backgroundObject = background.gameObject;
		if (backgroundObject.activeSelf)
		{
			background.DOKill(complete: true);
			backgroundCanvasGroup.DOKill(complete: true);
			backgroundCanvasGroup.blocksRaycasts = false;
			backgroundCanvasGroup.interactable = false;
			backgroundCanvasGroup.DOFade(0f, 0.12f).SetEase(Ease.InQuad).SetUpdate(isIndependentUpdate: true)
				.OnComplete(delegate
				{
					backgroundObject.SetActive(value: false);
				});
		}
	}

	private static async void OnManifestChanged()
	{
		HashSet<ulong> addedSinceSnapshot = ModManifest.GetAddedSinceSnapshot();
		HashSet<ulong> removedSinceSnapshot = ModManifest.GetRemovedSinceSnapshot();
		ModEnumDefinitions.Clear();
		await ModDiscoveryRegistry.DiscoverSteamModsByIdsAsync(addedSinceSnapshot);
		await ModLifecycleLoader.ApplyScopeChangesAsync(addedSinceSnapshot, removedSinceSnapshot);
		ModDiscoveryRegistry.RemoveDiscoveredSteamMods(removedSinceSnapshot);
	}
}
