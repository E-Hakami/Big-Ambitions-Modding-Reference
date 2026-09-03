using System;
using System.Collections.Generic;
using System.Linq;
using JimmysUnityUtilities;
using TMPro;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan.LogisticsManagers;

public class LogisticsManagerListSortable : MonoBehaviour
{
	[SerializeField]
	private List<Transform> listHeaders;

	private List<LogisticsManagerListEntryData> _productsData;

	private LogisticsManagerListOption _currentSortOption;

	private bool _invertedSort;

	public void SetUp(List<LogisticsManagerListEntryData> productData)
	{
		_productsData = productData;
	}

	public void SortBy(string optionInText)
	{
		if (!Enum.TryParse(typeof(LogisticsManagerListOption), optionInText, out var result))
		{
			Debug.LogError("LogisticsManagerListOption " + optionInText + " not found");
			return;
		}
		LogisticsManagerListOption option = (LogisticsManagerListOption)result;
		Transform transform = ((_currentSortOption == LogisticsManagerListOption.None) ? null : listHeaders.First((Transform x) => x.name == _currentSortOption.ToString()));
		if (_currentSortOption == option)
		{
			_invertedSort = !_invertedSort;
			if (transform != null)
			{
				transform.Find("Arrow").GetRectTransform().eulerAngles = new Vector3(0f, 0f, _invertedSort ? 90 : (-90));
			}
		}
		else
		{
			_currentSortOption = option;
			_invertedSort = false;
			if (transform != null)
			{
				transform.Find("Arrow").gameObject.SetActive(value: false);
				TMP_Text component = transform.GetComponent<TMP_Text>();
				if ((bool)component)
				{
					component.margin = new Vector4(0f, 0f, 0f, 0f);
				}
			}
			Transform obj = listHeaders.First((Transform x) => x.name == _currentSortOption.ToString());
			Transform obj2 = obj.Find("Arrow");
			obj2.GetRectTransform().eulerAngles = new Vector3(0f, 0f, -90f);
			obj2.gameObject.SetActive(value: true);
			TMP_Text component2 = obj.GetComponent<TMP_Text>();
			if ((bool)component2)
			{
				component2.margin = new Vector4(40f, 0f, 0f, 0f);
			}
		}
		List<LogisticsManagerListEntryData> list = (_invertedSort ? _productsData.OrderBy((LogisticsManagerListEntryData x) => x.data[option]).ToList() : _productsData.OrderByDescending((LogisticsManagerListEntryData x) => x.data[option]).ToList());
		for (int num = list.Count; num > 0; num--)
		{
			list[num - 1].entryTransform.SetSiblingIndex(num);
		}
	}

	public bool IsEmpty()
	{
		if (_productsData != null)
		{
			return _productsData.Count == 0;
		}
		return true;
	}
}
