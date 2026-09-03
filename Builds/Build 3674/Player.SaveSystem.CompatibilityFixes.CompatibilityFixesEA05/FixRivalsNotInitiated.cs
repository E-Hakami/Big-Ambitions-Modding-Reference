using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Rivals;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class FixRivalsNotInitiated : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		bool flag = gameInstance.rivalStates == null || gameInstance.rivalStates.Count < 19;
		int num = 0;
		foreach (RivalState rivalState in gameInstance.rivalStates)
		{
			if (RivalsHelper.GetRivalData(rivalState.rivalId).WeeklyIncome < 0.01f)
			{
				num++;
			}
		}
		IEnumerable<SpecialRivalState> source = from x in RivalsHelper.GetSpecialRivals()
			select RivalsHelper.GetSpecialRivalState(x.rivalData.id);
		if (num == 19 && !source.All((SpecialRivalState x) => x.isDefeated))
		{
			flag = true;
		}
		if (flag)
		{
			AddDailyIncomesToAiBusinesses addDailyIncomesToAiBusinesses = new AddDailyIncomesToAiBusinesses();
			new PopulateRivals().Apply(gameInstance);
			addDailyIncomesToAiBusinesses.Apply(gameInstance);
		}
	}
}
