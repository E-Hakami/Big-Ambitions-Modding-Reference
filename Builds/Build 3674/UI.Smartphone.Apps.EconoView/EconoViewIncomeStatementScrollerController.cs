using System;
using System.Collections;
using System.Collections.Generic;
using EnhancedUI.EnhancedScroller;
using Entities;
using Extensions;
using Helpers;
using Streets;
using UnityEngine;

namespace UI.Smartphone.Apps.EconoView;

public class EconoViewIncomeStatementScrollerController : MonoBehaviour, IEnhancedScrollerDelegate
{
	private const float FallbackCellSize = 100f;

	private static readonly List<float> RowValues = new List<float>(4);

	[SerializeField]
	private EnhancedScroller scroller;

	[SerializeField]
	private EconoViewIncomeStatementCellView cellViewTemplate;

	[SerializeField]
	private float cellSize;

	private readonly List<EconoViewIncomeStatementModel> _data = new List<EconoViewIncomeStatementModel>();

	private readonly List<EconoViewIncomeStatementModel> _visibleData = new List<EconoViewIncomeStatementModel>();

	private EconoView _econoView;

	private List<FinancialSummary> _financialSummaries;

	private Coroutine _reloadCoroutine;

	private void Awake()
	{
		EnsureScrollerDelegate();
	}

	private void OnEnable()
	{
		EnsureScrollerDelegate();
	}

	private void OnDisable()
	{
		if (_reloadCoroutine != null)
		{
			StopCoroutine(_reloadCoroutine);
			_reloadCoroutine = null;
		}
	}

	public int GetNumberOfCells(EnhancedScroller _)
	{
		return _visibleData.Count;
	}

	public float GetCellViewSize(EnhancedScroller _, int dataIndex)
	{
		return _visibleData[dataIndex].height;
	}

	public EnhancedScrollerCellView GetCellView(EnhancedScroller _, int dataIndex, int cellIndex)
	{
		EconoViewIncomeStatementCellView obj = scroller.GetCellView(cellViewTemplate) as EconoViewIncomeStatementCellView;
		EconoViewIncomeStatementModel model = _visibleData[dataIndex];
		obj.SetData(model, delegate
		{
			ToggleRow(model);
		});
		return obj;
	}

	public void Load(List<FinancialSummary> financialSummaries, EconoView econoView)
	{
		_financialSummaries = financialSummaries;
		_econoView = econoView;
		ResetData();
		if (cellViewTemplate == null || _financialSummaries == null || _financialSummaries.Count == 0)
		{
			RefreshRows();
			return;
		}
		LoadRows();
		RefreshRows();
	}

	private void LoadRows()
	{
		FinancialSummary financialSummary = _financialSummaries[0];
		List<float> values = CreateSummaryValues((FinancialSummary statement) => statement.totalBusinessProfit, out var total);
		EconoViewIncomeStatementModel businessesCategory = CreateRow(EconoViewRowType.Default, "econoview_row_businesses", values);
		if (financialSummary.businessIncomeStatements != null)
		{
			LoadBusinessRows(financialSummary, businessesCategory);
		}
		LoadSalaryRows();
		LoadRealEstateRows(financialSummary);
		LoadOngoingExpenseRows();
		LoadFeeRows();
		List<float> values2 = CreateSummaryValues((FinancialSummary summary) => summary.totalProfit, out total);
		CreateRow(EconoViewRowType.Total, "econoview_row_total", values2, autoSetValue1Color: false);
	}

	private void LoadBusinessRows(FinancialSummary latestSummary, EconoViewIncomeStatementModel businessesCategory)
	{
		Dictionary<Address, FinancialSummary.BusinessIncomeStatement>[] businessStatementLookups = GetBusinessStatementLookups(_financialSummaries);
		int num = 1;
		foreach (FinancialSummary.BusinessIncomeStatement businessIncomeStatement in latestSummary.businessIncomeStatements)
		{
			BuildingRegistration registration = BuildingHelper.GetBuildingRegistration(businessIncomeStatement.Address);
			EconoViewRowType rowType = EconoViewRowType.Warning;
			if (businessIncomeStatement.TotalProfit > 0f)
			{
				rowType = EconoViewRowType.Success;
			}
			if (businessIncomeStatement.TotalProfit < 0f)
			{
				rowType = EconoViewRowType.Danger;
			}
			List<float> values = CreateBusinessStatementValues(businessIncomeStatement, businessStatementLookups);
			CreateRow(rowType, string.IsNullOrEmpty(registration.BusinessName) ? registration.Address.ToFormattedString() : registration.BusinessName, values, autoSetValue1Color: true, businessesCategory, delegate
			{
				_econoView.SetBusiness(registration);
			}).name = $"Business{num}";
			num++;
		}
	}

	private void LoadSalaryRows()
	{
		List<float> values = CreateSummaryValues((FinancialSummary summary) => summary.salaryIncome, out var total);
		if (!(total <= 0f))
		{
			EconoViewIncomeStatementModel groupRow = CreateRow(EconoViewRowType.Default, "econoview_row_other_income", values);
			CreateRow(EconoViewRowType.Success, "econoview_row_salary_income", values, autoSetValue1Color: true, groupRow);
		}
	}

	private void LoadRealEstateRows(FinancialSummary latestSummary)
	{
		List<float> values = CreateSummaryValues((FinancialSummary summary) => summary.totalRealEstate, out var total);
		if (total <= 0f)
		{
			return;
		}
		EconoViewIncomeStatementModel groupRow = CreateRow(EconoViewRowType.Default, "common_real_estate", values);
		if (latestSummary.realEstateStatements == null)
		{
			return;
		}
		Dictionary<Address, FinancialSummary.RealEstateStatement>[] realEstateStatementLookups = GetRealEstateStatementLookups(_financialSummaries);
		foreach (FinancialSummary.RealEstateStatement realEstateStatement in latestSummary.realEstateStatements)
		{
			BuildingRegistration registration = BuildingHelper.GetBuildingRegistration(realEstateStatement.Address);
			List<float> values2 = CreateRealEstateStatementValues(realEstateStatement, realEstateStatementLookups);
			CreateRow(EconoViewRowType.Success, registration.Address.ToFormattedString(), values2, autoSetValue1Color: true, groupRow, delegate
			{
				InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(registration.Address, "RealEstate");
			});
		}
	}

	private void LoadOngoingExpenseRows()
	{
		List<float> values = CreateSummaryValues((FinancialSummary summary) => summary.totalLoanExpenses + summary.totalHealthInsuranceExpenses + summary.totalUnassignedStaffWages - summary.totalResidentialExpenses, out var _);
		EconoViewIncomeStatementModel groupRow = CreateRow(EconoViewRowType.Default, "econoview_row_ongoing_expenses", values);
		List<float> values2 = CreateSummaryValues((FinancialSummary summary) => 0f - summary.totalResidentialExpenses, out var total2);
		if (total2 != 0f)
		{
			CreateRow(EconoViewRowType.Danger, "econoview_row_private_residences", values2, autoSetValue1Color: true, groupRow);
		}
		List<float> values3 = CreateSummaryValues((FinancialSummary summary) => summary.totalLoanExpenses, out var total3);
		if (total3 != 0f)
		{
			CreateRow(EconoViewRowType.Danger, "econoview_row_loans", values3, autoSetValue1Color: true, groupRow);
		}
		List<float> values4 = CreateSummaryValues((FinancialSummary summary) => summary.totalHealthInsuranceExpenses, out var total4);
		if (total4 != 0f)
		{
			CreateRow(EconoViewRowType.Danger, "ba:transaction_healthinsurance_label", values4, autoSetValue1Color: true, groupRow);
		}
		List<float> values5 = CreateSummaryValues((FinancialSummary summary) => summary.totalUnassignedStaffWages, out var total5);
		if (total5 != 0f)
		{
			CreateRow(EconoViewRowType.Danger, "econoview_row_unassigned_staff_wages", values5, autoSetValue1Color: true, groupRow);
		}
	}

	private void LoadFeeRows()
	{
		List<float> values = CreateSummaryValues((FinancialSummary summary) => summary.totalHeadhunterReplacementFees + summary.parkingFees, out var total);
		if (total != 0f)
		{
			EconoViewIncomeStatementModel groupRow = CreateRow(EconoViewRowType.Default, "econoview_row_fees", values);
			List<float> values2 = CreateSummaryValues((FinancialSummary summary) => summary.totalHeadhunterReplacementFees, out var total2);
			if (total2 != 0f)
			{
				CreateRow(EconoViewRowType.Danger, "ba:transaction_headhunterreplacement_label", values2, autoSetValue1Color: true, groupRow);
			}
			List<float> values3 = CreateSummaryValues((FinancialSummary summary) => summary.parkingFees, out var total3);
			if (total3 != 0f)
			{
				CreateRow(EconoViewRowType.Danger, "econoview_row_parking_fees", values3, autoSetValue1Color: true, groupRow);
			}
		}
	}

	private EconoViewIncomeStatementModel CreateRow(EconoViewRowType rowType, string rowName = "econoview_row_undefined", List<float> values = null, bool autoSetValue1Color = true, EconoViewIncomeStatementModel groupRow = null, Action clickAction = null)
	{
		EconoViewIncomeStatementModel econoViewIncomeStatementModel = new EconoViewIncomeStatementModel(rowType, rowName, values, autoSetValue1Color, groupRow, clickAction, GetCellSize());
		_data.Add(econoViewIncomeStatementModel);
		return econoViewIncomeStatementModel;
	}

	private void ResetData()
	{
		_data.Clear();
		_visibleData.Clear();
		if (!(cellViewTemplate == null))
		{
			cellViewTemplate.ClearRowRelationships();
			cellViewTemplate.gameObject.SetActive(value: false);
		}
	}

	private void RefreshRows(float scrollPosition = 0f, bool keepScrollPosition = false)
	{
		_visibleData.Clear();
		for (int i = 0; i < _data.Count; i++)
		{
			EconoViewIncomeStatementModel econoViewIncomeStatementModel = _data[i];
			if (IsRowVisible(econoViewIncomeStatementModel))
			{
				_visibleData.Add(econoViewIncomeStatementModel);
			}
		}
		ReloadScroller(scrollPosition, keepScrollPosition);
	}

	private void ReloadScroller(float scrollPosition = 0f, bool keepScrollPosition = false)
	{
		if (scroller == null)
		{
			return;
		}
		EnsureScrollerDelegate();
		if (scroller.Container == null)
		{
			ScheduleReload(scrollPosition, keepScrollPosition);
			return;
		}
		scroller.ReloadData();
		if (keepScrollPosition)
		{
			scroller.SetScrollPositionImmediately(scrollPosition);
		}
	}

	private void ScheduleReload(float scrollPosition, bool keepScrollPosition)
	{
		if (_reloadCoroutine != null)
		{
			StopCoroutine(_reloadCoroutine);
		}
		_reloadCoroutine = StartCoroutine(ReloadWhenReady(scrollPosition, keepScrollPosition));
	}

	private IEnumerator ReloadWhenReady(float scrollPosition, bool keepScrollPosition)
	{
		while (scroller != null && scroller.Container == null)
		{
			yield return null;
		}
		_reloadCoroutine = null;
		if (!(scroller == null))
		{
			EnsureScrollerDelegate();
			scroller.ReloadData();
			if (keepScrollPosition)
			{
				scroller.SetScrollPositionImmediately(scrollPosition);
			}
		}
	}

	private void ToggleRow(EconoViewIncomeStatementModel row)
	{
		if (!HasChildRows(row))
		{
			row.InvokeClick();
			return;
		}
		row.isExpanded = !row.isExpanded;
		float scrollPosition = ((scroller != null) ? scroller.ScrollPosition : 0f);
		RefreshRows(scrollPosition, keepScrollPosition: true);
	}

	private bool HasChildRows(EconoViewIncomeStatementModel row)
	{
		for (int i = 0; i < _data.Count; i++)
		{
			if (_data[i].parent == row)
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsRowVisible(EconoViewIncomeStatementModel row)
	{
		for (EconoViewIncomeStatementModel parent = row.parent; parent != null; parent = parent.parent)
		{
			if (!parent.isExpanded)
			{
				return false;
			}
		}
		return true;
	}

	private float GetCellSize()
	{
		if (cellSize > 0f)
		{
			return cellSize;
		}
		RectTransform rectTransform = ((cellViewTemplate != null) ? (cellViewTemplate.transform as RectTransform) : null);
		if (rectTransform != null && rectTransform.rect.height > 0f)
		{
			return rectTransform.rect.height;
		}
		return 100f;
	}

	private void EnsureScrollerDelegate()
	{
		if (scroller != null)
		{
			scroller.Delegate = this;
		}
	}

	private List<float> CreateSummaryValues(Func<FinancialSummary, float> selector, out float total)
	{
		_financialSummaries.MapToListAndSum(RowValues, selector, out total);
		return RowValues;
	}

	private static Dictionary<Address, FinancialSummary.BusinessIncomeStatement>[] GetBusinessStatementLookups(List<FinancialSummary> financialSummaries)
	{
		Dictionary<Address, FinancialSummary.BusinessIncomeStatement>[] array = new Dictionary<Address, FinancialSummary.BusinessIncomeStatement>[financialSummaries.Count];
		for (int i = 1; i < financialSummaries.Count; i++)
		{
			List<FinancialSummary.BusinessIncomeStatement> businessIncomeStatements = financialSummaries[i].businessIncomeStatements;
			if (businessIncomeStatements != null)
			{
				Dictionary<Address, FinancialSummary.BusinessIncomeStatement> dictionary = new Dictionary<Address, FinancialSummary.BusinessIncomeStatement>(businessIncomeStatements.Count);
				for (int j = 0; j < businessIncomeStatements.Count; j++)
				{
					FinancialSummary.BusinessIncomeStatement businessIncomeStatement = businessIncomeStatements[j];
					dictionary.TryAdd(businessIncomeStatement.Address, businessIncomeStatement);
				}
				array[i] = dictionary;
			}
		}
		return array;
	}

	private static Dictionary<Address, FinancialSummary.RealEstateStatement>[] GetRealEstateStatementLookups(List<FinancialSummary> financialSummaries)
	{
		Dictionary<Address, FinancialSummary.RealEstateStatement>[] array = new Dictionary<Address, FinancialSummary.RealEstateStatement>[financialSummaries.Count];
		for (int i = 1; i < financialSummaries.Count; i++)
		{
			List<FinancialSummary.RealEstateStatement> realEstateStatements = financialSummaries[i].realEstateStatements;
			if (realEstateStatements != null)
			{
				Dictionary<Address, FinancialSummary.RealEstateStatement> dictionary = new Dictionary<Address, FinancialSummary.RealEstateStatement>(realEstateStatements.Count);
				for (int j = 0; j < realEstateStatements.Count; j++)
				{
					FinancialSummary.RealEstateStatement realEstateStatement = realEstateStatements[j];
					dictionary.TryAdd(realEstateStatement.Address, realEstateStatement);
				}
				array[i] = dictionary;
			}
		}
		return array;
	}

	private static List<float> CreateBusinessStatementValues(FinancialSummary.BusinessIncomeStatement statement, Dictionary<Address, FinancialSummary.BusinessIncomeStatement>[] statementLookups)
	{
		PrepareRowValues(statementLookups.Length);
		RowValues.Add(statement.TotalProfit);
		for (int i = 1; i < statementLookups.Length; i++)
		{
			Dictionary<Address, FinancialSummary.BusinessIncomeStatement> dictionary = statementLookups[i];
			float item = ((dictionary != null && dictionary.TryGetValue(statement.Address, out var value)) ? value.TotalProfit : 0f);
			RowValues.Add(item);
		}
		return RowValues;
	}

	private static List<float> CreateRealEstateStatementValues(FinancialSummary.RealEstateStatement statement, Dictionary<Address, FinancialSummary.RealEstateStatement>[] statementLookups)
	{
		PrepareRowValues(statementLookups.Length);
		RowValues.Add(statement.Amount);
		for (int i = 1; i < statementLookups.Length; i++)
		{
			Dictionary<Address, FinancialSummary.RealEstateStatement> dictionary = statementLookups[i];
			float item = ((dictionary != null && dictionary.TryGetValue(statement.Address, out var value)) ? value.Amount : 0f);
			RowValues.Add(item);
		}
		return RowValues;
	}

	private static void PrepareRowValues(int capacity)
	{
		RowValues.Clear();
		if (RowValues.Capacity < capacity)
		{
			RowValues.Capacity = capacity;
		}
	}
}
