using System.Linq;
using Buildings.Office.Headquarters;
using DG.Tweening;
using Localizor;
using Localizor.LanguageChangeEvent;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.LogisticsManagers;

public class LogisticsManagerDestinationUI : MonoBehaviour
{
	public RectTransform productsElementTemplate;

	public LogisticsManagerListSortable productsList;

	[SerializeField]
	private TextLocalizationComponent nameText;

	[SerializeField]
	private Button collapseButton;

	[SerializeField]
	private Button removeButton;

	[SerializeField]
	private RectTransform productsHeader;

	[SerializeField]
	private RectTransform noProducts;

	[SerializeField]
	private RectTransform arrowIcon;

	[SerializeField]
	private UI.Elements.Dropdown businessTargetDropdown;

	private LogisticsManagerPlanUI _logisticsManagerPlanUI;

	private LogisticsManagerPlan _currentPlan;

	private int _destinationIndex;

	private CanvasGroup _cachedCanvasGroup;

	public void SetUp(LogisticsManagerPlanUI logisticsManagerPlanUI, LogisticsManagerPlan currentPlan, int destinationIndex, bool isExceeding)
	{
		_logisticsManagerPlanUI = logisticsManagerPlanUI;
		_currentPlan = currentPlan;
		_destinationIndex = destinationIndex;
		nameText.SetData("bizman_logisticsmanagers_destination_number".Localize(new
		{
			number = _destinationIndex + 1
		}));
		removeButton.onClick.AddListener(delegate
		{
			HudConfirm.Show(null, "deliveryplans_hud_confirm_discard_destination", delegate
			{
				_currentPlan.destinations.RemoveAt(_destinationIndex);
				_logisticsManagerPlanUI.LoadPlan(_currentPlan);
				SaveGameManager.MarkChange();
			});
		});
		collapseButton.onClick.AddListener(delegate
		{
			bool flag = !_currentPlan.destinations[_destinationIndex].isUiCollapsed;
			_logisticsManagerPlanUI.ChangeProductsVisibility(_destinationIndex, !flag);
		});
		int selectedOption = _logisticsManagerPlanUI.Destinations.FindIndex((BuildingRegistration x) => x.Address == _currentPlan.destinations[_destinationIndex].deliveryTargetAddress);
		businessTargetDropdown.SetPlaceholder("common_unassigned");
		businessTargetDropdown.SetOptions(_logisticsManagerPlanUI.Destinations.Select((BuildingRegistration x) => x.GetDisplayName()).ToList(), localize: false, selectedOption);
		businessTargetDropdown.onOptionSelected.AddListener(delegate(int option)
		{
			Address address = _logisticsManagerPlanUI.Destinations[option].Address;
			_logisticsManagerPlanUI.UpdateSelectedBusiness(_destinationIndex, address);
		});
		UpdateProductsVisibility();
		if ((bool)_cachedCanvasGroup || TryGetComponent<CanvasGroup>(out _cachedCanvasGroup))
		{
			_cachedCanvasGroup.alpha = (isExceeding ? 0.5f : 1f);
		}
	}

	public void ChangeProductsVisibility(bool isVisible)
	{
		_currentPlan.destinations[_destinationIndex].isUiCollapsed = !isVisible;
		UpdateProductsVisibility();
	}

	public void UpdateProductsVisibility()
	{
		bool flag = !_currentPlan.destinations[_destinationIndex].isUiCollapsed;
		bool flag2 = !productsList.IsEmpty();
		arrowIcon.DORotate(new Vector3(0f, 0f, flag ? 90 : 180), 0.2f).SetUpdate(isIndependentUpdate: true);
		productsHeader.gameObject.SetActive(flag & flag2);
		productsList.gameObject.SetActive(flag & flag2);
		noProducts.gameObject.SetActive(flag && !flag2);
	}
}
