using BigAmbitions.Characters.Skills;
using BigAmbitions.Tags;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

public class EnablePartAndFullTimeToRecruitmentCampaigns : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (RecruitmentCampaign recruitmentCampaign in gameInstance.RecruitmentCampaigns)
		{
			if (SkillHelper.GetData(recruitmentCampaign.skillRequirement.skillName).HasTag(TagRef.Skilltag.hashoursperweekdemand))
			{
				recruitmentCampaign.partTime = true;
				recruitmentCampaign.fullTime = true;
			}
		}
	}
}
