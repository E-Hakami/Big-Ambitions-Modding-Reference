using BaTable;
using TMPro;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan.HRManagers;

public class HeadhunterEmployeeCellView : BaTableCellView<HeadhunterEmployeeModel>
{
	[SerializeField]
	private TMP_Text employeeName;

	[SerializeField]
	private TMP_Text primarySkill;

	[SerializeField]
	private TMP_Text businessName;

	[SerializeField]
	private TMP_Text replacementReason;

	public override void SetData(HeadhunterEmployeeModel data)
	{
		employeeName.text = data.EmployeeName;
		primarySkill.text = data.PrimarySkill;
		businessName.text = data.BusinessName;
		replacementReason.text = data.ReplacementReason;
	}
}
