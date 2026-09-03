using System;
using System.Collections.Generic;

namespace BigAmbitions.Rivals;

[Serializable]
public class SpecialRivalState
{
	public string rivalId;

	public bool isActive;

	public bool isDefeated;

	public List<string> completedTimelineEntryIds;

	public List<DefenseState> defenseStates;

	public List<string> sentMessageKeys;
}
