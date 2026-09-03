using System.Linq;
using BigAmbitions.Rivals;
using Extensions;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;

public class InitializeOldGymAsARegularCompetitor : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		RivalsHelper.FillData(gameInstance.rivalStates.Select((RivalState x) => x.rivalId).ToList());
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(new Address("ba:street_fourthavenue", 28));
		AiBusinessDefault aiBusinessDefault = CompetitionHelper.GetBusinessDefaultsByType("ba:businesstype_gym").FirstOrDefault((AiBusinessDefault x) => x.businessName == "Keep Calm and Squat On");
		if (aiBusinessDefault == null)
		{
			aiBusinessDefault = CompetitionHelper.GetBusinessDefaultsByType("ba:businesstype_gym").GetRandomBusinessDefault(buildingRegistration);
		}
		string text = aiBusinessDefault?.corporationRivalId;
		if (string.IsNullOrEmpty(text))
		{
			text = RivalsHelper.GetNonSpecialRivals().GetRandom().id;
		}
		CompetitionHelper.StartNewCompetitorBusiness("ba:businesstype_gym", buildingRegistration, impactMarket: false, aiBusinessDefault, text);
	}
}
