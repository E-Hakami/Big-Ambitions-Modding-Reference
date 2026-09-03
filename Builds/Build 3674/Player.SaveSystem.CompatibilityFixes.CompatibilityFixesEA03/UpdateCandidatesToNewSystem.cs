using System.Collections.Generic;
using System.Linq;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

public class UpdateCandidatesToNewSystem : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		gameInstance.CandidateEmployeeInstances = new HashSet<EmployeeInstance>(gameInstance.CandidateEmployeeInstances).ToList();
		foreach (EmployeeInstance candidate in gameInstance.CandidateEmployeeInstances)
		{
			if (candidate.hired || candidate.declined)
			{
				continue;
			}
			Address address = gameInstance.Contacts.FirstOrDefault((Contact x) => x.messagesQueue.Any((TextMessage y) => y.contextAction?.employeeInstanceId == candidate.id))?.Address;
			if (address == new Address("ba:street_firstavenue", 2))
			{
				address = new Address("ba:street_fourthavenue", 41);
			}
			candidate.candidateInfo = new CandidateInfo
			{
				sourceAddress = address,
				hoursUntilExpiring = 168
			};
		}
		gameInstance.CandidateEmployeeInstances.RemoveAll((EmployeeInstance x) => x.hired || x.declined);
	}
}
