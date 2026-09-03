using System.Collections.Generic;
using System.Linq;
using Extensions;
using Localizor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.EconoView;

public class EconoViewIncomeStatement : MonoBehaviour
{
	public enum RowType
	{
		Danger,
		Warning,
		Success,
		Default,
		Total
	}

	public class RowRelationship
	{
		public Transform Parent;

		public Transform Child;
	}

	public Transform defaultEntry;

	public Transform successEntry;

	public Transform warningEntry;

	public Transform dangerEntry;

	public Transform totalEntrySuccess;

	public Transform totalEntryDanger;

	private List<RowRelationship> _rowRelationships = new List<RowRelationship>();

	public void Reset()
	{
		List<Transform> list = new List<Transform> { defaultEntry, successEntry, warningEntry, dangerEntry, totalEntrySuccess, totalEntryDanger };
		list.ForEach(delegate(Transform x)
		{
			x.gameObject.SetActive(value: false);
		});
		foreach (Transform item in defaultEntry.parent.transform)
		{
			if (!list.Contains(item))
			{
				Object.Destroy(item.gameObject);
			}
		}
		_rowRelationships.Clear();
	}

	public Transform CreateRow(RowType rowType, string rowName = "econoview_row_undefined", List<float> values = null, bool autoSetValue1Color = true, Transform groupRow = null)
	{
		if (values == null)
		{
			values = new List<float> { 0f, 0f, 0f, 0f };
		}
		Transform entry;
		switch (rowType)
		{
		case RowType.Danger:
			entry = Object.Instantiate(dangerEntry, defaultEntry.parent);
			break;
		case RowType.Success:
			entry = Object.Instantiate(successEntry, defaultEntry.parent);
			break;
		case RowType.Warning:
			entry = Object.Instantiate(successEntry, warningEntry.parent);
			break;
		default:
			entry = Object.Instantiate(defaultEntry, defaultEntry.parent);
			break;
		}
		SetRowData(entry, rowName, values, autoSetValue1Color);
		entry.gameObject.SetActive(value: true);
		entry.GetComponentInChildren<Button>().onClick.AddListener(delegate
		{
			foreach (Transform item in from x in _rowRelationships
				where x.Parent == entry
				select x.Child)
			{
				item.gameObject.SetActive(!item.gameObject.activeSelf);
			}
		});
		if (groupRow != null)
		{
			_rowRelationships.Add(new RowRelationship
			{
				Parent = groupRow,
				Child = entry
			});
		}
		return entry;
	}

	public void SetTotal(string label, List<float> values)
	{
		if (values == null)
		{
			values = new List<float> { 0f, 0f, 0f, 0f };
		}
		Transform transform = Object.Instantiate((values != null && values[0] >= 0f) ? totalEntrySuccess : totalEntryDanger, totalEntrySuccess.parent);
		SetRowData(transform, label, values, autoSetValue1Color: false);
		transform.gameObject.SetActive(value: true);
	}

	public void SetRowData(Transform obj, string rowName, List<float> values, bool autoSetValue1Color = true)
	{
		if (values == null)
		{
			values = new List<float> { 0f, 0f, 0f, 0f };
		}
		if (LocalizorManager.IsLocalizedKey(rowName))
		{
			obj.GetLanguageChangeEventByName("Name/Title").Key = rowName;
		}
		else
		{
			obj.GetLanguageChangeEventByName("Name/Title").Key = null;
			obj.GetLabelByName("Name/Title").text = rowName;
		}
		TextMeshProUGUI component = obj.Find("Name/Value1").GetComponent<TextMeshProUGUI>();
		component.text = values[0].ToCurrencyFormat();
		if (autoSetValue1Color)
		{
			component.color = ((values[0] >= 0f) ? InstanceBehavior<GlobalReferences>.Instance.colors.green : InstanceBehavior<GlobalReferences>.Instance.colors.red);
		}
		obj.Find("Value2").GetComponentInChildren<TextMeshProUGUI>().text = values[1].ToCurrencyFormat();
		obj.Find("Value3").GetComponentInChildren<TextMeshProUGUI>().text = values[2].ToCurrencyFormat();
		obj.Find("Value4").GetComponentInChildren<TextMeshProUGUI>().text = values[3].ToCurrencyFormat();
	}
}
