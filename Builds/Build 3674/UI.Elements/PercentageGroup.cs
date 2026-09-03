using System.Collections.Generic;
using Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Elements;

public class PercentageGroup : MonoBehaviour
{
	public List<PercentageGroupEntry> entries = new List<PercentageGroupEntry>(6);

	public Transform entryTemplate;

	private void Start()
	{
		entryTemplate.gameObject.SetActive(value: false);
	}

	public void SetEntries(List<PercentageGroupEntry> entries)
	{
		entryTemplate.ResetTemplate();
		float width = GetComponent<RectTransform>().rect.width;
		foreach (PercentageGroupEntry entry in entries)
		{
			float x = width * (float)entry.percentage / 100f;
			Transform obj = Object.Instantiate(entryTemplate, base.transform);
			obj.name = entry.gradient.ToString();
			RectTransform component = obj.GetComponent<RectTransform>();
			obj.GetComponent<Image>().sprite = entry.gradient;
			component.sizeDelta = new Vector2(x, component.sizeDelta.y);
			obj.gameObject.SetActive(value: true);
		}
	}
}
