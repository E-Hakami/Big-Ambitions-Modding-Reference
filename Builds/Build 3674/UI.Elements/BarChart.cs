using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Elements;

public class BarChart : MonoBehaviour
{
	[Serializable]
	public class BarChartEntry
	{
		public string Label;

		public float Amount;
	}

	public List<BarChartEntry> entries;

	public RectTransform entryTemplate;

	private void Start()
	{
		SetEntries(entries);
	}

	public void SetEntries(List<BarChartEntry> newEntries)
	{
		entries = newEntries;
		entryTemplate.ResetTemplate();
		if (newEntries.Count == 0)
		{
			return;
		}
		float width = entryTemplate.parent.GetComponent<RectTransform>().rect.width;
		float num = entries.Max((BarChartEntry x) => x.Amount);
		for (int num2 = 0; num2 < entries.Count; num2++)
		{
			if (num2 + 1 > InstanceBehavior<GlobalReferences>.Instance.chartColors.Length)
			{
				Debug.LogWarning("BarChart limited. Add more colors.");
				break;
			}
			BarChartEntry barChartEntry = entries[num2];
			Color32 color = InstanceBehavior<GlobalReferences>.Instance.chartColors[num2];
			RectTransform rectTransform = UnityEngine.Object.Instantiate(entryTemplate, entryTemplate.parent);
			rectTransform.GetComponent<Image>().color = color;
			float num3 = Mathf.Floor(barChartEntry.Amount * 100f / num);
			TextMeshProUGUI labelByName = rectTransform.transform.GetLabelByName("Label");
			labelByName.text = barChartEntry.Label;
			rectTransform.transform.GetLabelByName("Amount").text = Mathf.RoundToInt(barChartEntry.Amount).ToString();
			rectTransform.sizeDelta = new Vector2(Math.Max(num3 * width / 100f, labelByName.GetComponent<RectTransform>().rect.width), rectTransform.sizeDelta.y);
			rectTransform.gameObject.SetActive(value: true);
		}
	}
}
