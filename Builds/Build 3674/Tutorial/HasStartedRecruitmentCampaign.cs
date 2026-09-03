using System.Linq;
using Entities;
using HGAttributes;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Marketing/HasStartedRecruitmentCampaign")]
public class HasStartedRecruitmentCampaign : QuestRequirement
{
	[AutocompleteDropdown("Skills")]
	public string[] skillNames;

	public override bool CheckIfCompleted()
	{
		return SaveGameManager.Current.RecruitmentCampaigns.Any((RecruitmentCampaign x) => skillNames.Contains(x.skillRequirement.skillName));
	}
}
