using BigAmbitions.InteriorDesigner.Tools;
using Controllers;
using TMPro;
using UI.Components;
using UnityEngine;

namespace BigAmbitions.InteriorDesigner;

public class WorldTextProducerOverlay : IProducerOverlay
{
	[SerializeField]
	private TMP_InputField worldTextInput;

	private string _newText;

	private string _originalText;

	private ProducerOverlay _producerOverlay;

	private void Awake()
	{
		_producerOverlay = GetComponentInParent<ProducerOverlay>();
		worldTextInput.onValueChanged.AddListener(delegate(string text)
		{
			_newText = text;
		});
		KeyboardInputHelper.Configure(worldTextInput, delegate
		{
			_producerOverlay?.Close();
		}, autoFocus: false);
	}

	public override bool HasChanges()
	{
		return _originalText != _newText;
	}

	public override bool ShouldShow(ItemController itemController)
	{
		return itemController is ItemWithTextController;
	}

	public override void OnOpen(ItemController itemController)
	{
		ItemWithTextController itemWithTextController = itemController as ItemWithTextController;
		if (itemWithTextController == null)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		_originalText = itemWithTextController.GetText();
		_newText = _originalText;
		worldTextInput.text = _originalText;
		worldTextInput.characterLimit = itemWithTextController.maxTextLength;
		base.gameObject.SetActive(value: true);
		KeyboardInputHelper.FocusNextFrame(worldTextInput);
	}

	public override void ExecuteRevertibleAction()
	{
		IInteriorDesignerTool.executeActionThroughCode(new ProducerWorldTextRevertibleAction(IProducerOverlay.currentItemIndex, _originalText, _newText));
	}
}
