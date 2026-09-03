using BigAmbitions.InteriorDesigner;
using BigAmbitions.InteriorDesigner.Tools;
using UI.InteriorDesigner;
using UnityEngine;

namespace Buildings.Indoors.InteriorDesign;

public class TimeOfDayToolSetup : ToolSetup
{
	public override IInteriorDesignerTool Tool { get; protected set; }

	public override ToolName ToolName => ToolName.TimeOfDay;

	public override void Setup(ActionPanelUI actionPanel, MonoBehaviour overlay)
	{
		Tool = new TimeOfDayTool();
		if (!(InteriorDesignerHelper.TimeOfDayController != null))
		{
			return;
		}
		GlobalEvents.RegisterOnGameLoadedCallback(delegate
		{
			TimeOfDayTool.onEnvironmentPeriodChanged = delegate(float timeInHours)
			{
				InteriorDesignerHelper.TimeOfDayController.SetEnvironmentSettings(timeInHours);
				InteriorDesignerHelper.TimeOfDayController.UpdateHourlyValues(timeInHours);
			};
		});
	}
}
