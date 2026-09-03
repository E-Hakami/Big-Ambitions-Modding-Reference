using System.Collections.Generic;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public class ScheduleWorkstationModel
{
	public string workstationName;

	public string workstationId;

	public int customersPerHour;

	public List<string> attachedItems;

	public bool isFactoryMachine;

	public WorkShiftType shiftType;
}
