using System;
using System.Linq;
using BigAmbitions.Characters.Skills;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;

public class FixHRManagerNotHavingCorrectClass : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		for (int i = 0; i < gameInstance.EmployeeInstances.Count; i++)
		{
			if (gameInstance.EmployeeInstances[i].characterData.skills.Exists((Skill x) => x.name == "ba:skill_hrmanager") && !(gameInstance.EmployeeInstances[i] is HRManager))
			{
				EmployeeInstance oldEmployeeInstance = gameInstance.EmployeeInstances[i].Copy();
				gameInstance.EmployeeInstances[i] = new HRManager
				{
					id = oldEmployeeInstance.id,
					characterData = oldEmployeeInstance.characterData,
					dayHired = oldEmployeeInstance.dayHired,
					demands = oldEmployeeInstance.demands,
					hourlyWage = oldEmployeeInstance.hourlyWage,
					assignedAddress = oldEmployeeInstance.assignedAddress,
					satisfaction = oldEmployeeInstance.satisfaction,
					trainingSession = oldEmployeeInstance.trainingSession,
					nextSickDay = oldEmployeeInstance.nextSickDay,
					isAbsent = oldEmployeeInstance.isAbsent,
					isReplaced = oldEmployeeInstance.isReplaced,
					temporalId = oldEmployeeInstance.temporalId,
					workedHoursToday = oldEmployeeInstance.workedHoursToday,
					assignedHRManager = oldEmployeeInstance.assignedHRManager,
					presetId = oldEmployeeInstance.presetId,
					hasSendQuitWarning = oldEmployeeInstance.hasSendQuitWarning,
					sendRetirementNotice = oldEmployeeInstance.sendRetirementNotice,
					isTrainingDay = oldEmployeeInstance.isTrainingDay,
					candidateInfo = oldEmployeeInstance.candidateInfo,
					assignedEmployees = (from x in gameInstance.EmployeeInstances
						where x.assignedHRManager == oldEmployeeInstance.id
						select x.id).ToList()
				};
			}
		}
	}
}
