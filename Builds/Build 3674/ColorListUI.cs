using System;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

public class ColorListUI : MonoBehaviour
{
	[SerializeField]
	private CustomColorPicker customColorPicker;

	[SerializeField]
	private Color[] colors;

	[SerializeField]
	private Transform container;

	[SerializeField]
	private Transform addColorButton;

	[SerializeField]
	private Transform playerColorTemplate;

	[SerializeField]
	private Transform colorTemplate;

	[SerializeField]
	private bool highlightSelection;

	public Color[] defaultColorsOverride;

	public Func<Color> getInitialColor;

	public Action<Color> onColorChanged;

	public Action<ColorListUI> onRefresh;

	private GameObject _previousHighlight;

	public Action<Color> onSelectColor;

	private void Awake()
	{
		playerColorTemplate.gameObject.SetActive(value: false);
		colorTemplate.gameObject.SetActive(value: false);
	}

	public void SetUp()
	{
		ResetColors();
		Color[] array = defaultColorsOverride;
		Color[] array2 = ((array != null && array.Length > 0) ? defaultColorsOverride : colors);
		Color? obj;
		if (!highlightSelection)
		{
			obj = null;
		}
		else
		{
			Func<Color> func = getInitialColor;
			obj = ((func != null) ? new Color?(func()) : ((array2.Length != 0) ? new Color?(array2[0]) : ((Color?)null)));
		}
		Color? initialColor = obj;
		foreach (Color playerColor in PlayerSettingsHelper.GetPlayerColors())
		{
			SetUpPlayerColor(playerColor, initialColor);
		}
		array = array2;
		foreach (Color color in array)
		{
			SetUpPresetColor(color, initialColor);
		}
	}

	public void AddNewColor()
	{
		customColorPicker.onColorPick = ConfirmCustomColor;
		customColorPicker.Open(PreviewColor, GetInitialColor());
	}

	private void ResetColors()
	{
		foreach (Transform item in container)
		{
			if (item != addColorButton && item != playerColorTemplate && item != colorTemplate)
			{
				UnityEngine.Object.Destroy(item.gameObject);
			}
		}
	}

	private void SetUpPlayerColor(Color color, Color? initialColor)
	{
		Transform entry = UnityEngine.Object.Instantiate(playerColorTemplate, playerColorTemplate.parent);
		entry.GetComponent<Image>().color = color;
		entry.GetComponent<Button>().onClick.AddListener(delegate
		{
			SelectColor(color, entry);
		});
		entry.GetComponent<PlayerColor>().onRemove.AddListener(delegate
		{
			RemovePlayerColor(color);
		});
		entry.gameObject.SetActive(value: true);
		if (initialColor.HasValue && color.Approximately(initialColor.Value))
		{
			OnSelectedEntry(entry);
		}
	}

	private void SetUpPresetColor(Color color, Color? initialColor)
	{
		Transform entry = UnityEngine.Object.Instantiate(colorTemplate, colorTemplate.parent);
		color.a = 1f;
		entry.GetComponent<Image>().color = color;
		entry.GetComponent<Button>().onClick.AddListener(delegate
		{
			SelectColor(color, entry);
		});
		entry.gameObject.SetActive(value: true);
		if (initialColor.HasValue && color.Approximately(initialColor.Value))
		{
			OnSelectedEntry(entry);
		}
	}

	private void SelectColor(Color color, Transform selectedEntry = null)
	{
		if (highlightSelection && selectedEntry != null)
		{
			OnSelectedEntry(selectedEntry);
		}
		onSelectColor?.Invoke(color);
	}

	private void OnSelectedEntry(Transform selectedEntry)
	{
		if ((bool)selectedEntry)
		{
			if ((bool)_previousHighlight)
			{
				_previousHighlight.SetActive(value: false);
			}
			Transform transform = selectedEntry.Find("Selected");
			if ((bool)transform)
			{
				_previousHighlight = transform.gameObject;
				transform.gameObject.SetActive(value: true);
			}
		}
	}

	private void PreviewColor(Color color)
	{
		onColorChanged?.Invoke(color);
	}

	private void ConfirmCustomColor(Color color)
	{
		SelectColor(color);
		RefreshColors();
	}

	private Color GetInitialColor()
	{
		return getInitialColor?.Invoke() ?? Color.white;
	}

	private void RemovePlayerColor(Color color)
	{
		LanguageChangeEventDataHolder bodyData = "colorlist_hud_confirm_remove_color".Localize();
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			PlayerSettingsHelper.RemovePlayerColor(color);
			RefreshColors();
		});
	}

	private void RefreshColors()
	{
		SetUp();
		onRefresh?.Invoke(this);
	}
}
