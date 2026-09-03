using System;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

public class AcknowledgeWarningUi : MonoBehaviour
{
	[SerializeField]
	private RectTransform container;

	[SerializeField]
	private TextLocalizationComponent headerLabel;

	[SerializeField]
	private TextLocalizationComponent bodyLabel;

	[SerializeField]
	private TextLocalizationComponent confirmLabel;

	[SerializeField]
	private Toggle acknowledgeToggle;

	private Action _onConfirmAction;

	private string _value;

	private void Start()
	{
		container.gameObject.SetActive(value: false);
		AcknowledgeWarning.onShow = (Action<string, string, string, string, Action>)Delegate.Combine(AcknowledgeWarning.onShow, new Action<string, string, string, string, Action>(Show));
	}

	private void OnDestroy()
	{
		AcknowledgeWarning.isOpen = false;
		AcknowledgeWarning.onShow = (Action<string, string, string, string, Action>)Delegate.Remove(AcknowledgeWarning.onShow, new Action<string, string, string, string, Action>(Show));
	}

	private void Show(string value, string headerKey, string bodyKey, string confirmKey, Action onConfirmAction)
	{
		headerLabel.Key = headerKey;
		bodyLabel.Key = bodyKey;
		confirmLabel.Key = confirmKey;
		_value = value;
		_onConfirmAction = onConfirmAction;
		if (acknowledgeToggle != null)
		{
			acknowledgeToggle.SetIsOnWithoutNotify(value: false);
		}
		container.gameObject.SetActive(value: true);
		AcknowledgeWarning.isOpen = true;
	}

	public void ClickCancel()
	{
		if (AcknowledgeWarning.isOpen)
		{
			_onConfirmAction = null;
			_value = null;
			container.gameObject.SetActive(value: false);
			AcknowledgeWarning.isOpen = false;
		}
	}

	public void ClickConfirm()
	{
		if (AcknowledgeWarning.isOpen)
		{
			if (acknowledgeToggle != null && acknowledgeToggle.isOn)
			{
				AcknowledgeWarning.Acknowledge(_value);
			}
			Action onConfirmAction = _onConfirmAction;
			_onConfirmAction = null;
			_value = null;
			container.gameObject.SetActive(value: false);
			AcknowledgeWarning.isOpen = false;
			onConfirmAction?.Invoke();
		}
	}
}
