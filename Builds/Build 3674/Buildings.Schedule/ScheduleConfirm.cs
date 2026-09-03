using System;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace Buildings.Schedule;

public class ScheduleConfirm : MonoBehaviour
{
	public TextLocalizationComponent warninglabel;

	public Toggle fastToggle;

	private Action<bool> _onConfirm;

	private Action _onCancel;

	private void Start()
	{
		fastToggle.isOn = SaveGameManager.Current.PlayerDefaults.fastScheduleAutoFill;
	}

	public void ShowConfirm(string key, Action<bool> onConfirm, Action onCancel = null)
	{
		warninglabel.Key = key;
		base.gameObject.SetActive(value: true);
		_onConfirm = onConfirm;
		_onCancel = onCancel;
	}

	public void ClickCancel()
	{
		_onCancel?.Invoke();
		_onCancel = null;
		_onConfirm = null;
		base.gameObject.SetActive(value: false);
	}

	public void ClickConfirm()
	{
		base.gameObject.SetActive(value: false);
		SaveGameManager.Current.PlayerDefaults.fastScheduleAutoFill = fastToggle.isOn;
		_onConfirm?.Invoke(fastToggle.isOn);
	}
}
