using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EnhancedUI.EnhancedScroller;
using Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BaTable;

public abstract class BaTable<TCellView, TModel> : MonoBehaviour, IEnhancedScrollerDelegate where TCellView : BaTableCellView<TModel>
{
	public List<TModel> data = new List<TModel>();

	public TCellView prefab;

	public EnhancedScroller scroller;

	private string _order;

	public bool orderByAsc;

	private Transform _headers;

	private TCellView _currentSelectedRow;

	public bool allowRowSelection;

	public UnityEvent<TModel> onRowSelected = new UnityEvent<TModel>();

	private string _currentSelectedRowDataId;

	protected string CurrentSortColumn => _order;

	public virtual void Start()
	{
		scroller.Delegate = this;
		_headers = base.transform.Find("Headers");
		if (!(_headers != null))
		{
			return;
		}
		Button[] componentsInChildren = _headers.GetComponentsInChildren<Button>();
		foreach (Button button in componentsInChildren)
		{
			if (IsSortableColumn(button.name))
			{
				button.onClick.AddListener(delegate
				{
					ChangeSortOrder(button.name);
				});
			}
		}
	}

	public void ResetFilters()
	{
		if (!string.IsNullOrEmpty(_order))
		{
			Transform obj = _headers.Find(_order);
			obj.Find("ArrowDown").gameObject.SetActive(value: false);
			obj.Find("ArrowUp").gameObject.SetActive(value: false);
			_order = null;
		}
	}

	public virtual void ChangeSortOrder(string propertyName = null, bool flipOrder = true)
	{
		if (data == null || data.Count == 0)
		{
			return;
		}
		if (propertyName == null)
		{
			propertyName = _order;
		}
		FieldInfo fieldInfo;
		PropertyInfo propertyInfo;
		if (!string.IsNullOrEmpty(propertyName))
		{
			if ((_order == propertyName) & flipOrder)
			{
				orderByAsc = !orderByAsc;
			}
			if (_order != propertyName)
			{
				orderByAsc = true;
			}
			if (!string.IsNullOrEmpty(_order))
			{
				Transform obj = _headers.Find(_order);
				obj.Find("ArrowDown").gameObject.SetActive(value: false);
				obj.Find("ArrowUp").gameObject.SetActive(value: false);
			}
			_order = propertyName;
			fieldInfo = typeof(TModel).GetField(propertyName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
			propertyInfo = typeof(TModel).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
			if (fieldInfo == null && propertyInfo == null)
			{
				Debug.LogError("Sorting failed for " + propertyName + ". Mismatch between table headers and data model.");
				return;
			}
			data = (orderByAsc ? data.OrderBy(GetValue) : data.OrderByDescending(GetValue)).ToList();
			Transform obj2 = _headers.Find(propertyName);
			obj2.Find("ArrowDown").gameObject.SetActive(!orderByAsc);
			obj2.Find("ArrowUp").gameObject.SetActive(orderByAsc);
			OnDataOrdered();
		}
		object GetValue(TModel model)
		{
			if (!(fieldInfo != null))
			{
				return propertyInfo.GetValue(model);
			}
			return fieldInfo.GetValue(model);
		}
	}

	private static bool IsSortableColumn(string columnName)
	{
		FieldInfo field = typeof(TModel).GetField(columnName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
		PropertyInfo property = typeof(TModel).GetProperty(columnName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
		if ((object)field == null)
		{
			return (object)property != null;
		}
		return true;
	}

	protected virtual void OnDataOrdered()
	{
		scroller.ReloadData();
	}

	public int GetNumberOfCells(EnhancedScroller scroller)
	{
		return data.Count;
	}

	public abstract float GetCellViewSize(EnhancedScroller scroller, int dataIndex);

	public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
	{
		TCellView cellView = scroller.GetCellView(prefab) as TCellView;
		cellView.SetData(data[dataIndex]);
		if (allowRowSelection)
		{
			Button orAddComponent = cellView.gameObject.GetOrAddComponent<Button>();
			orAddComponent.onClick.RemoveAllListeners();
			cellView.dataIndex = dataIndex;
			string dataId = GetDataId(data[dataIndex]);
			orAddComponent.onClick.AddListener(delegate
			{
				SelectRow(cellView, dataId);
			});
			cellView.VisualizeSelected(selected: false);
			cellView.ResetVisuals();
			if (_currentSelectedRowDataId == dataId)
			{
				_currentSelectedRow = cellView;
				_currentSelectedRow.VisualizeSelected(selected: true);
			}
		}
		return cellView;
	}

	protected virtual string GetDataId(TModel model)
	{
		return (string)typeof(TModel).GetField("Id").GetValue(model);
	}

	public void SelectRow(TCellView row, string dataId)
	{
		if ((bool)_currentSelectedRow)
		{
			_currentSelectedRow.VisualizeSelected(selected: false);
		}
		_currentSelectedRow = row;
		_currentSelectedRowDataId = dataId;
		if ((bool)row)
		{
			_currentSelectedRow.VisualizeSelected(selected: true);
			onRowSelected.Invoke(data[row.cellIndex]);
		}
	}

	protected string GetSelectedRowDataId()
	{
		return _currentSelectedRowDataId;
	}

	protected TCellView GetSelectedRow()
	{
		return _currentSelectedRow;
	}

	protected void ResetSelectedRow()
	{
		_currentSelectedRow = null;
		_currentSelectedRowDataId = null;
	}
}
