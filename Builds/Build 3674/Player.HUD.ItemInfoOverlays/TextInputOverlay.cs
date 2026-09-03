using System;
using Controllers;
using TMPro;
using UnityEngine;

namespace Player.HUD.ItemInfoOverlays;

public class TextInputOverlay : IOverlay
{
	[Header("Text Input Overlay")]
	[SerializeField]
	private TMP_InputField inputField;

	private ItemWithTextController _itemWithText;

	private void Start()
	{
		TMP_InputField tMP_InputField = inputField;
		tMP_InputField.onValidateInput = (TMP_InputField.OnValidateInput)Delegate.Combine(tMP_InputField.onValidateInput, (TMP_InputField.OnValidateInput)((string _, int index, char charInput) => (_itemWithText.maxTextLength > index) ? charInput : '\0'));
	}

	private void OnEnable()
	{
		inputField.onValueChanged.AddListener(OnInputValueChanged);
	}

	private void OnDisable()
	{
		inputField.onValueChanged.RemoveListener(OnInputValueChanged);
		_itemWithText = null;
	}

	public override bool IsValid(EntityController entityController)
	{
		return false;
	}

	public override bool ShouldShow(EntityController entityController)
	{
		return false;
	}

	public override void UpdateOverlay(EntityController entityController)
	{
		ItemWithTextController itemWithTextController = (_itemWithText = (ItemWithTextController)entityController);
		inputField.text = itemWithTextController.GetText();
	}

	private void OnInputValueChanged(string newValue)
	{
		if (!(_itemWithText == null))
		{
			_itemWithText.SetText(newValue);
		}
	}
}
