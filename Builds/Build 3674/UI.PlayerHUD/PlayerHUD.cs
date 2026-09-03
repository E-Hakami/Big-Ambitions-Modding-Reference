using Buildings;
using DG.Tweening;
using Localizor;
using Localizor.LanguageChangeEvent;
using Streets;
using TMPro;
using UI.CurrentBuilding;
using UI.CurrentJob;
using UI.CustomUI;
using UI.Dialog;
using UI.ItemPanel;
using UI.Load;
using UI.MergeCargo;
using UI.Purchase;
using UI.PurchaseVehicle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI.PlayerHUD;

public class PlayerHUD : MonoBehaviour
{
	public DialogUI dialogUI;

	public UI.CurrentJob.CurrentJob currentJobUI;

	public ItemPanelUI itemPanelUI;

	public JobOffer jobOfferPanel;

	public ManageCargoUi manageCargoUI;

	public PurchaseVehicleUI purchaseVehicleUI;

	public PurchaseUI purchaseUI;

	public CurrentBuildingUI currentBuildingUI;

	public TextLocalizationComponent nearestBuildingText;

	public TMP_Text currentNeighborhoodText;

	public RectTransform currentNeighborhoodRectTransform;

	public TMP_Text currentDeviceText;

	private string _lastNeighborhood;

	private Building _lastClosestBuilding;

	private Sequence _neighbourhoodScaleSequence;

	private void Start()
	{
		CreateNeighbourhoodScaleSequence();
	}

	public void ShowJob(Job job)
	{
		InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.JobOfferPanel);
		jobOfferPanel.job = job;
		jobOfferPanel.gameObject.SetActive(value: true);
	}

	private void LateUpdate()
	{
		if (LoadScene.isLoading || InstanceBehavior<GameManager>.Instance.playerController.awaitingRepositioning)
		{
			return;
		}
		currentDeviceText.text = "Current device: " + ((Gamepad.current != null) ? Gamepad.current.name : "Keyboard");
		Building building = ClosestBuildingFromPlayer.Get();
		if (building == _lastClosestBuilding)
		{
			return;
		}
		NeighborhoodHelper.CurrentNeighborhood = building.Neighbourhood;
		if (building == CasinoBoatManager.CasinoBuilding)
		{
			nearestBuildingText.Key = "common_casino";
			currentNeighborhoodText.text = "common_international_waters".GetLocalization();
			_lastNeighborhood = string.Empty;
		}
		else
		{
			nearestBuildingText.SetValue(building.Address.ToFormattedString(), clearKey: true);
			if (_lastNeighborhood != building.Neighbourhood)
			{
				UpdateNeighbourhoodLabel(building.Neighbourhood);
			}
		}
		_lastClosestBuilding = building;
	}

	private void CreateNeighbourhoodScaleSequence()
	{
		_neighbourhoodScaleSequence = DOTween.Sequence();
		_neighbourhoodScaleSequence.Append(currentNeighborhoodRectTransform.DOScale(1.25f, 0.5f).SetEase(Ease.OutQuad));
		_neighbourhoodScaleSequence.Append(currentNeighborhoodRectTransform.DOScale(1f, 0.5f).SetEase(Ease.InQuad));
		_neighbourhoodScaleSequence.SetAutoKill(autoKillOnCompletion: false);
	}

	private void UpdateNeighbourhoodLabel(string neighborhood)
	{
		_lastNeighborhood = neighborhood;
		currentNeighborhoodText.text = neighborhood.GetLocalization();
		_neighbourhoodScaleSequence.Restart();
	}
}
