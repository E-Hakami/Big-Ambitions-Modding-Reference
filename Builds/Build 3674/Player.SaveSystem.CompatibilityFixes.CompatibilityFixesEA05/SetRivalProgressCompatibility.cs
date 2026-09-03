using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Rivals;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class SetRivalProgressCompatibility : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (SpecialRival rival in RivalsHelper.GetSpecialRivals())
		{
			(List<BuildingRegistration>, float) playerValues = rival.timeline.GetPlayerValues(rival);
			List<BuildingRegistration> playerBusinesses = playerValues.Item1;
			float item = playerValues.Item2;
			float weeklyIncomePercentage = item / rival.rivalData.WeeklyIncome * 100f;
			List<TimelineEntry> list = (from x in rival.timeline.allEntries
				where !x.IsCompleted && playerBusinesses.Count >= x.businesses && weeklyIncomePercentage >= (float)x.weeklyIncomePercentage
				orderby x.businesses descending
				select x).ToList();
			if (list.Count > 0)
			{
				list.RemoveAt(0);
			}
			SpecialRivalState rivalState = gameInstance.specialRivalStates.FirstOrDefault((SpecialRivalState state) => state.rivalId == rival.rivalData.id);
			if (rivalState == null)
			{
				rivalState = new SpecialRivalState();
			}
			SpecialRivalState specialRivalState = rivalState;
			if (specialRivalState.completedTimelineEntryIds == null)
			{
				specialRivalState.completedTimelineEntryIds = new List<string>();
			}
			foreach (TimelineEntry item2 in list.Where((TimelineEntry entry) => !rivalState.completedTimelineEntryIds.Contains(entry.id)))
			{
				rivalState.completedTimelineEntryIds.Add(item2.id);
			}
			specialRivalState = rivalState;
			if (specialRivalState.sentMessageKeys == null)
			{
				specialRivalState.sentMessageKeys = new List<string>();
			}
			bool flag = RivalsHelper.HasMessageBeenSent(rival.rivalData.id, rival.entranceMessageKey);
			if (!flag && playerBusinesses.Count > 0)
			{
				RivalsHelper.SendMessageWithoutNotification(rival.rivalData.id, rival.entranceMessageKey);
				rivalState.sentMessageKeys.Add(rival.entranceMessageKey);
				flag = true;
			}
			if (flag && !rivalState.isActive && playerBusinesses.Count >= 3)
			{
				string activationMessageKey = rival.timeline.activationMessageKey;
				RivalsHelper.SendMessageWithoutNotification(rival.rivalData.id, activationMessageKey);
				rivalState.sentMessageKeys.Add(activationMessageKey);
				rivalState.isActive = true;
				rival.GetRivalContact().SendMessage(new TextMessage("ba:messagetype_rivalry_activated", null, RivalsHelper.HasMessageBeenSent(rival.rivalData.id, activationMessageKey), isNewInteraction: false, isSpecialMessage: true));
			}
		}
	}
}
