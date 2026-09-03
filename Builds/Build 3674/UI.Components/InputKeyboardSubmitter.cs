using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Components;

[DisallowMultipleComponent]
public class InputKeyboardSubmitter : MonoBehaviour
{
	[SerializeField]
	private TMP_InputField inputField;

	[SerializeField]
	private Button button;

	[SerializeField]
	private bool autoFocus;

	[SerializeField]
	private bool refocusInput;

	private void Awake()
	{
		KeyboardInputHelper.Configure(inputField, Submit, autoFocus, refocusInput);
		if (refocusInput)
		{
			button.onClick.AddListener(RefocusInput);
		}
	}

	private void OnDestroy()
	{
		if ((bool)button)
		{
			button.onClick.RemoveListener(RefocusInput);
		}
	}

	private void Submit()
	{
		if ((bool)button && button.isActiveAndEnabled && button.interactable)
		{
			button.onClick.Invoke();
		}
	}

	private void RefocusInput()
	{
		KeyboardInputHelper.FocusNextFrame(inputField);
	}
}
