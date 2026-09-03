using Buildings.Schedule;

namespace Entities.Employee.JobDemands.Requirements;

public interface IWorkStationFilter
{
	bool AcceptsWorkStation(WorkStationInfo workStation, BuildingRegistration buildingRegistration);
}
