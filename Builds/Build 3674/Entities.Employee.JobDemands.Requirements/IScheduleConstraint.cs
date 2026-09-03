using System.Collections.Generic;
using Buildings.Schedule;

namespace Entities.Employee.JobDemands.Requirements;

public interface IScheduleConstraint
{
	void ApplyConstraint(ScheduleAutoFiller scheduler, EmployeeInstance employee, List<WorkStationInfo> workStations);
}
