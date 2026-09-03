using System;
using System.Collections.Generic;
using Buildings.Office.Headquarters;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using UI.Elements;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan.PricingManagers;

public class PricingManagerPlanUI : MonoBehaviour
{
	private const string SelectNeighborhoodKey = "bizman_pricingmanagers_select_neighborhood";

	public NoManagerAssignedPopUp noManagerAssignedPopUp;

	[SerializeField]
	private Dropdown neighborhoodDropdown;

	[SerializeField]
	private PricingManagerProductsScrollerController productsScroller;

	private PricingManagerPlan _currentPlan;

	private List<string> _neighborhoods;

	public event Action<string> onNeighborhoodChanged;

	private void OnEnable()
	{
		NoManagerAssignedPopUp obj = noManagerAssignedPopUp;
		obj.deletePlan = (Action)Delegate.Combine(obj.deletePlan, new Action(DeletePlan));
		neighborhoodDropdown.onOptionSelected.AddListener(OnChangedNeighborhood);
	}

	private void OnDisable()
	{
		NoManagerAssignedPopUp obj = noManagerAssignedPopUp;
		obj.deletePlan = (Action)Delegate.Remove(obj.deletePlan, new Action(DeletePlan));
		neighborhoodDropdown.onOptionSelected.RemoveListener(OnChangedNeighborhood);
	}

	public void LoadPlan(PricingManagerPlan plan)
	{
		_currentPlan = plan;
		_currentPlan.RecomputeSuggestions();
		SetUpNeighborhoodDropdown(plan);
		if (plan.AnalystInstance == null)
		{
			noManagerAssignedPopUp.Show();
		}
		else
		{
			noManagerAssignedPopUp.Hide();
		}
		productsScroller.LoadPlan(plan);
		base.gameObject.SetActive(value: true);
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
		noManagerAssignedPopUp.Hide();
	}

	private void SetUpNeighborhoodDropdown(PricingManagerPlan plan)
	{
		_neighborhoods = GetSelectableNeighborhoods(plan);
		List<string> list = new List<string>(_neighborhoods.Count + 1) { "bizman_pricingmanagers_select_neighborhood" };
		list.AddRange(_neighborhoods);
		int selectedOption = _neighborhoods.IndexOf(plan.supervisedNeighborhood) + 1;
		neighborhoodDropdown.SetOptions(list, localize: true, selectedOption);
	}

	private static List<string> GetSelectableNeighborhoods(PricingManagerPlan plan)
	{
		List<string> list = new List<string>();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (PricingManagerHelper.IsManageableBusiness(buildingRegistration) && !list.Contains(buildingRegistration.Neighborhood) && !PricingManagerHelper.IsNeighborhoodSupervised(buildingRegistration.Neighborhood, plan.id))
			{
				list.Add(buildingRegistration.Neighborhood);
			}
		}
		if (!plan.supervisedNeighborhood.IsNullOrEmpty() && !list.Contains(plan.supervisedNeighborhood))
		{
			list.Add(plan.supervisedNeighborhood);
		}
		return list;
	}

	private void OnChangedNeighborhood(int neighborhoodIndex)
	{
		string newNeighborhood = ((neighborhoodIndex == 0) ? null : _neighborhoods[neighborhoodIndex - 1]);
		if (newNeighborhood == _currentPlan.supervisedNeighborhood)
		{
			return;
		}
		if (_currentPlan.originalStorePrices.Count > 0)
		{
			LanguageChangeEventDataHolder bodyData = "bizman_pricingmanagers_change_neighborhood_confirm".Localize();
			HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
			{
				ChangeNeighborhood(newNeighborhood);
			}, delegate
			{
				neighborhoodDropdown.SelectOption(_neighborhoods.IndexOf(_currentPlan.supervisedNeighborhood) + 1);
			});
		}
		else
		{
			ChangeNeighborhood(newNeighborhood);
		}
	}

	private void ChangeNeighborhood(string newNeighborhood)
	{
		_currentPlan.SetSupervisedNeighborhood(newNeighborhood);
		onNeighborhoodChanged?.Invoke(newNeighborhood);
		productsScroller.LoadPlan(_currentPlan);
		SaveGameManager.MarkChange();
	}

	private void DeletePlan()
	{
		if (_currentPlan == null)
		{
			Debug.LogError("No plan selected");
			return;
		}
		PricingManagerHelper.DeletePlan(_currentPlan.id);
		SaveGameManager.MarkChange();
	}
}
