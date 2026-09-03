using System.Linq;
using BigAmbitions.Characters.Skills;
using BigAmbitions.Tags;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

public class EnsureAllFullTimeEmployeesHaveFullTimeDemand : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (EmployeeInstance employeeInstance in gameInstance.EmployeeInstances)
		{
			if (employeeInstance.characterData.skills.Any((Skill x) => SkillHelper.GetData(x).HasTag(TagRef.Skilltag.forcefulltime)) && employeeInstance.demands.Count != 0)
			{
				employeeInstance.demands[0] = "ba:jobdemand_fulltime";
			}
		}
		foreach (EmployeeInstance candidateEmployeeInstance in gameInstance.CandidateEmployeeInstances)
		{
			if (candidateEmployeeInstance.characterData.skills.Any((Skill x) => SkillHelper.GetData(x.name).HasTag(TagRef.Skilltag.forcefulltime)) && candidateEmployeeInstance.demands.Count != 0)
			{
				candidateEmployeeInstance.demands[0] = "ba:jobdemand_fulltime";
			}
		}
	}
}
