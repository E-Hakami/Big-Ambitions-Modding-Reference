using BigAmbitions.DayNightCycle;
using Buildings.Office.Headquarters;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;

public class FixNextRecruitDayFromHeadhunterPlans : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (HeadhunterPlan headhunterPlan in gameInstance.headhunterPlans)
		{
			if (!headhunterPlan.isRecruiting || headhunterPlan.nextRecruit == null)
			{
				continue;
			}
			DayOfWeekOrdered dayOfWeek = TimeHelper.GetDayOfWeek(gameInstance.Day);
			if (dayOfWeek == DayOfWeekOrdered.Friday && gameInstance.Hour >= 15)
			{
				headhunterPlan.nextRecruit.Day = gameInstance.Day + 3;
				continue;
			}
			switch (dayOfWeek)
			{
			case DayOfWeekOrdered.Saturday:
				headhunterPlan.nextRecruit.Day = gameInstance.Day + 2;
				break;
			case DayOfWeekOrdered.Sunday:
				headhunterPlan.nextRecruit.Day = gameInstance.Day + 1;
				break;
			default:
				headhunterPlan.nextRecruit.Day = gameInstance.Day;
				break;
			}
		}
	}
}
