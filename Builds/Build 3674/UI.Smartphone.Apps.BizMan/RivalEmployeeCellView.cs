using System.Collections.Generic;
using BaTable;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan;

public class RivalEmployeeCellView : BaTableCellView<RivalEmployeeModel>
{
	[SerializeField]
	private TMP_Text employeeName;

	[SerializeField]
	private TextLocalizationComponent primarySkill;

	[SerializeField]
	private Button negotiateButton;

	private RivalEmployeeModel _data;

	public override void SetData(RivalEmployeeModel data)
	{
		_data = data;
		employeeName.text = data.EmployeeName;
		if ((bool)primarySkill)
		{
			primarySkill.Arguments = new
			{
				skill = data.PrimarySkill.Item1,
				value = Mathf.RoundToInt(data.PrimarySkill.Item2)
			};
		}
		if (data.EmployeeData.HasActiveSalaryNegotiation)
		{
			negotiateButton.interactable = false;
			return;
		}
		negotiateButton.interactable = true;
		negotiateButton.onClick.RemoveAllListeners();
		negotiateButton.onClick.AddListener(OnNegotiateButtonClicked);
	}

	private void OnNegotiateButtonClicked()
	{
		if (!EducationHelper.HasCompletedDiploma(DiplomaName.BasicHr))
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string> { 
			{
				"name",
				DiplomaName.BasicHr.GetLocalizeKey().Localize().ToString()
			} };
			Notifications.Show(NotificationType.Error, "notification_missing_diploma_poach_employee", notificationData);
		}
		else
		{
			_data.OnNegotiate(_data.EmployeeData);
		}
	}
}
