using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Player.HUD.ControlHints;

public class InlineControlsHintRenderer : MonoBehaviour
{
	private const string KeyboardBindingGroup = "Keyboard";

	[SerializeField]
	private TMP_Text textTemplate;

	[SerializeField]
	private ControlsHintBindingUI bindingTemplate;

	[SerializeField]
	private Transform container;

	private readonly List<TMP_Text> _textElements = new List<TMP_Text>();

	private readonly List<ControlsHintBindingUI> _bindingElements = new List<ControlsHintBindingUI>();

	private readonly List<ControlsHintBinding> _displayedBindings = new List<ControlsHintBinding>();

	private IReadOnlyList<ControlsHintBinding> _bindings;

	private string _text;

	private int _visibleTextCount;

	private int _visibleBindingCount;

	private void Awake()
	{
		textTemplate.gameObject.SetActive(value: false);
		bindingTemplate.gameObject.SetActive(value: false);
	}

	private void OnEnable()
	{
		GlobalEvents.onBindingsChanged = (Action)Delegate.Combine(GlobalEvents.onBindingsChanged, new Action(RefreshBindings));
		RefreshBindings();
	}

	private void OnDisable()
	{
		GlobalEvents.onBindingsChanged = (Action)Delegate.Remove(GlobalEvents.onBindingsChanged, new Action(RefreshBindings));
	}

	public void SetContent(string text, IReadOnlyList<ControlsHintBinding> bindings)
	{
		if (_bindings == bindings && string.Equals(_text, text, StringComparison.Ordinal))
		{
			RefreshBindings();
			return;
		}
		_text = text;
		_bindings = bindings;
		_visibleTextCount = 0;
		_visibleBindingCount = 0;
		_displayedBindings.Clear();
		int num = 0;
		int num2 = 0;
		while (num2 < text.Length)
		{
			int num3 = text.IndexOf('{', num2);
			if (num3 < 0)
			{
				break;
			}
			if (!TryReadPlaceholder(text, num3, out var closeIndex, out var bindingIndex) || bindingIndex >= bindings.Count)
			{
				num2 = num3 + 1;
				continue;
			}
			ShowText(text.Substring(num, num3 - num));
			ShowBinding(bindings[bindingIndex]);
			num = closeIndex + 1;
			num2 = num;
		}
		ShowText(text.Substring(num));
		for (int i = _visibleTextCount; i < _textElements.Count; i++)
		{
			_textElements[i].gameObject.SetActive(value: false);
		}
		for (int j = _visibleBindingCount; j < _bindingElements.Count; j++)
		{
			_bindingElements[j].gameObject.SetActive(value: false);
		}
	}

	private void RefreshBindings()
	{
		for (int i = 0; i < _displayedBindings.Count; i++)
		{
			SetBindingText(_bindingElements[i], _displayedBindings[i]);
		}
	}

	private void ShowText(string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			TMP_Text tMP_Text;
			if (_visibleTextCount < _textElements.Count)
			{
				tMP_Text = _textElements[_visibleTextCount];
			}
			else
			{
				tMP_Text = UnityEngine.Object.Instantiate(textTemplate, container);
				_textElements.Add(tMP_Text);
			}
			tMP_Text.text = value;
			tMP_Text.gameObject.SetActive(value: true);
			tMP_Text.transform.SetAsLastSibling();
			_visibleTextCount++;
		}
	}

	private void ShowBinding(ControlsHintBinding binding)
	{
		ControlsHintBindingUI controlsHintBindingUI;
		if (_visibleBindingCount < _bindingElements.Count)
		{
			controlsHintBindingUI = _bindingElements[_visibleBindingCount];
		}
		else
		{
			controlsHintBindingUI = UnityEngine.Object.Instantiate(bindingTemplate, container);
			_bindingElements.Add(controlsHintBindingUI);
		}
		SetBindingText(controlsHintBindingUI, binding);
		controlsHintBindingUI.gameObject.SetActive(value: true);
		controlsHintBindingUI.transform.SetAsLastSibling();
		_displayedBindings.Add(binding);
		_visibleBindingCount++;
	}

	private static void SetBindingText(ControlsHintBindingUI element, ControlsHintBinding binding)
	{
		element.SetText(binding.GetDisplayText("Keyboard"));
	}

	private static bool TryReadPlaceholder(string text, int openIndex, out int closeIndex, out int bindingIndex)
	{
		closeIndex = -1;
		bindingIndex = 0;
		int i = openIndex + 1;
		if (i >= text.Length || !char.IsDigit(text[i]))
		{
			return false;
		}
		for (; i < text.Length && char.IsDigit(text[i]); i++)
		{
			int num = text[i] - 48;
			if (bindingIndex > (int.MaxValue - num) / 10)
			{
				return false;
			}
			bindingIndex = bindingIndex * 10 + num;
		}
		if (i >= text.Length || text[i] != '}')
		{
			return false;
		}
		closeIndex = i;
		return true;
	}
}
