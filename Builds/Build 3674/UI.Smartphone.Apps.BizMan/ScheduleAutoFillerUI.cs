using Buildings.Schedule;
using Localizor.LanguageChangeEvent;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan;

public class ScheduleAutoFillerUI : MonoBehaviour
{
	[SerializeField]
	private ProgressBar progressBar;

	[SerializeField]
	private Button cancelButton;

	private ScheduleAutoFiller _scheduleAutoFiller;

	private bool _isSingleDay;

	public void Show(ScheduleAutoFiller scheduleAutoFiller)
	{
		_isSingleDay = scheduleAutoFiller.IsSingleDay;
		base.gameObject.SetActive(value: true);
		progressBar.SetValue01(scheduleAutoFiller.CurrentProgress);
		cancelButton.onClick.AddListener(OnCancelRequested);
		_scheduleAutoFiller = scheduleAutoFiller;
		_scheduleAutoFiller.onProgress.AddListener(OnProgressChanged);
		_scheduleAutoFiller.onCompleted.AddListener(OnCompleted);
		_scheduleAutoFiller.inhibitSuccessNotification = false;
	}

	private void OnCancelRequested()
	{
		if (_scheduleAutoFiller != null)
		{
			_scheduleAutoFiller.RequestCancel();
			_scheduleAutoFiller.onProgress.RemoveListener(OnProgressChanged);
			_scheduleAutoFiller.onCompleted.RemoveListener(OnCompleted);
			_scheduleAutoFiller = null;
		}
		base.gameObject.SetActive(value: false);
	}

	private void OnProgressChanged(ScheduleAutoFiller scheduleAutoFiller, float progress)
	{
		if (_scheduleAutoFiller == scheduleAutoFiller)
		{
			progressBar.SetValue01(progress);
		}
	}

	private void OnCompleted(ScheduleAutoFiller scheduleAutoFiller, bool success)
	{
		if (_scheduleAutoFiller == scheduleAutoFiller)
		{
			if (((_scheduleAutoFiller != null) & success) && !_scheduleAutoFiller.isOptimal)
			{
				LanguageChangeEventDataHolder bodyData = new LanguageChangeEventDataHolder
				{
					Key = (_isSingleDay ? "bizman_schedule_auto_fill_day_partial" : "bizman_schedule_auto_fill_partial")
				};
				HudConfirm.Show(new LanguageChangeEventDataHolder
				{
					Key = "bizman_schedule_auto_fill_partial_title"
				}, bodyData, null, null, "common_ok");
			}
			_scheduleAutoFiller = null;
			base.gameObject.SetActive(value: false);
		}
	}
}
