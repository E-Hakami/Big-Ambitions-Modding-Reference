using System;
using Localizor.LanguageChangeEvent;
using NaughtyAttributes;
using UnityEngine;

namespace UI.MainMenu;

public abstract class CustomGameOption<T> : MonoBehaviour
{
	[Header("Header")]
	[SerializeField]
	private TextLocalizationComponent headerLabel;

	[SerializeField]
	private string headerKey;

	[Header("Tooltip")]
	[SerializeField]
	private bool showTooltip;

	[ShowIf("showTooltip")]
	[SerializeField]
	private BasicTooltip tooltip;

	[ShowIf("showTooltip")]
	[SerializeField]
	private string tooltipKey;

	public Action<T> onValueChanged;

	protected virtual void Awake()
	{
		if (headerLabel != null)
		{
			headerLabel.Key = headerKey;
		}
		if (showTooltip && tooltip != null)
		{
			tooltip.descriptionKey = tooltipKey;
			tooltip.gameObject.SetActive(value: true);
		}
		else if (tooltip != null)
		{
			tooltip.gameObject.SetActive(value: false);
		}
	}

	public abstract void SetValue(T value);
}
