using System;
using HSVPicker;
using Helpers;
using UI.InteriorDesigner;
using UnityEngine;

public class CustomColorPicker : MonoBehaviour
{
	public static bool isOpen;

	[SerializeField]
	private ColorPicker picker;

	public Action<Color> onColorPick;

	private Color _initialColor;

	private Color _colorSelected;

	private Action<Color> _onColorChanged;

	private void Awake()
	{
		picker.onValueChanged.AddListener(OnColorChanged);
	}

	public void Open(Action<Color> onColorChanged, Color initialColor)
	{
		InteriorDesignerUI.blockInput = true;
		_initialColor = initialColor;
		_onColorChanged = onColorChanged;
		picker.CurrentColor = _initialColor;
		_colorSelected = _initialColor;
		base.gameObject.SetActive(value: true);
		isOpen = true;
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
		isOpen = false;
		InteriorDesignerUI.blockInput = false;
	}

	public void Add()
	{
		PlayerSettingsHelper.AddPlayerColor(_colorSelected);
		onColorPick?.Invoke(_colorSelected);
		Close();
	}

	public void Cancel()
	{
		_onColorChanged?.Invoke(_initialColor);
		Close();
	}

	private void OnColorChanged(Color color)
	{
		_colorSelected = color;
		_onColorChanged?.Invoke(_colorSelected);
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		isOpen = false;
	}
}
