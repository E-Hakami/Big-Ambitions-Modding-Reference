using System;
using TMPro;
using UnityEngine;

namespace UI.Components;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_InputField))]
public sealed class KeyboardInputHandler : MonoBehaviour
{
	private TMP_InputField _inputField;

	private Action _onSubmit;

	private bool _autoFocus;

	private bool _refocusAfterSubmit;

	private void Awake()
	{
		_inputField = GetComponent<TMP_InputField>();
		_inputField.onSubmit.AddListener(OnSubmit);
	}

	private void OnEnable()
	{
		if (_autoFocus)
		{
			KeyboardInputHelper.FocusNextFrame(_inputField);
		}
	}

	public void Configure(Action onSubmit, bool autoFocus, bool refocusAfterSubmit)
	{
		_onSubmit = onSubmit;
		_autoFocus = autoFocus;
		_refocusAfterSubmit = refocusAfterSubmit;
		if (_autoFocus && base.isActiveAndEnabled)
		{
			KeyboardInputHelper.FocusNextFrame(_inputField);
		}
	}

	private void OnSubmit(string _)
	{
		_onSubmit?.Invoke();
		if (_refocusAfterSubmit)
		{
			KeyboardInputHelper.FocusNextFrame(_inputField);
		}
	}
}
