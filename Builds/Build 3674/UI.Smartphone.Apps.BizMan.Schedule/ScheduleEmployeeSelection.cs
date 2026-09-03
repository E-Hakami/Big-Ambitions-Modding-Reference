using Localizor.LanguageChangeEvent;
using Player.HUD.ItemInfoOverlays;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public class ScheduleEmployeeSelection : MonoBehaviour
{
	[SerializeField]
	private GameObject container;

	[SerializeField]
	private TextLocalizationComponent headerLabel;

	[SerializeField]
	private ScheduleEmployeeScrollerController scrollerController;

	private int _hour;

	private string _workstationId;

	public bool IsOpen => container.activeSelf;

	private void Awake()
	{
		ScheduleHelper.OnRequestEmployeeSelection.AddListener(Show);
		container.SetActive(value: false);
	}

	public void Hide()
	{
		container.SetActive(value: false);
		_hour = -1;
		_workstationId = string.Empty;
	}

	public void Show(int hour, string workstationId)
	{
		_hour = hour;
		_workstationId = workstationId;
		bool flag = !string.IsNullOrEmpty(workstationId);
		if (flag)
		{
			string aliasHeader = OverlayHelper.GetAliasHeader(ScheduleHelper.WorkstationsById[_workstationId]);
			headerLabel.Arguments = new
			{
				workstationName = aliasHeader
			};
		}
		headerLabel.Key = (flag ? "bizman_schedule_employee_header" : "bizman_schedule_employee_header_no_workstation");
		ScheduleHelper.RegenerateSimulatedScheduleDays();
		container.SetActive(value: true);
		scrollerController.LoadList(OnEmployeeSelected, _hour, _workstationId);
	}

	private void OnEmployeeSelected(string employeeId)
	{
		if (_hour != -1 && !string.IsNullOrEmpty(_workstationId))
		{
			ScheduleHelper.AddWorkShift(_hour, _workstationId, employeeId);
			Hide();
		}
	}
}
