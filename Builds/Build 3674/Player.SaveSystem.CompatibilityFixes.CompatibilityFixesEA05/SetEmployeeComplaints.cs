using System.Collections.Generic;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class SetEmployeeComplaints : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		gameInstance.daysWithComplaints = new Queue<int>();
		foreach (EmployeeInstance employeeInstance in gameInstance.EmployeeInstances)
		{
			employeeInstance.complaintData = new EmployeeComplaintData();
			employeeInstance.complaintData.ResetHoursUntilNextComplaint();
		}
		foreach (EmployeeInstance candidateEmployeeInstance in gameInstance.CandidateEmployeeInstances)
		{
			candidateEmployeeInstance.complaintData = new EmployeeComplaintData();
		}
	}
}
