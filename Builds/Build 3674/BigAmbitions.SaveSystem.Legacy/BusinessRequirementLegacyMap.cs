using System.Collections.Generic;

namespace BigAmbitions.SaveSystem.Legacy;

public sealed class BusinessRequirementLegacyMap : LegacyMapperBase
{
	public override List<string> Keys => new List<string> { "BusinessRequirement.businessRequirementName", "TodoTask.businessRequirement" };

	protected override Dictionary<int, string> Map => new Dictionary<int, string>
	{
		{ 0, "ba:businessrequirement_atleastoneproduct" },
		{ 1, "ba:businessrequirement_pointofsales" },
		{ 2, "ba:businessrequirement_workcomputer" },
		{ 3, "ba:businessrequirement_cashregister" },
		{ 4, "ba:businessrequirement_stackofshoppingbaskets" },
		{ 5, "ba:businessrequirement_scale" },
		{ 6, "ba:businessrequirement_loudspeaker" },
		{ 7, "ba:businessrequirement_djbooth" },
		{ 8, "ba:businessrequirement_hairdresserchair" },
		{ 9, "ba:businessrequirement_headwasher" },
		{ 10, "ba:businessrequirement_shelfwithhaircareproducts" },
		{ 11, "ba:businessrequirement_publicshower" },
		{ 12, "ba:businessrequirement_gymlockers" },
		{ 13, "ba:businessrequirement_workoutmachine" },
		{ 14, "ba:businessrequirement_fitnessplanningboard" },
		{ 15, "ba:businessrequirement_coatcheck" },
		{ 16, "ba:businessrequirement_toiletstalls" },
		{ 17, "ba:businessrequirement_sinks" },
		{ 18, "ba:businessrequirement_changingroom" },
		{ 19, "ba:businessrequirement_ticketbooth" },
		{ 20, "ba:businessrequirement_ticketkiosk" },
		{ 21, "ba:businessrequirement_cinemascreen" },
		{ 22, "ba:businessrequirement_projectionbooth" },
		{ 23, "ba:businessrequirement_dressingroom" },
		{ 24, "ba:businessrequirement_lightingbooth" },
		{ 25, "ba:businessrequirement_soundbooth" },
		{ 26, "ba:businessrequirement_costumebooth" },
		{ 27, "ba:businessrequirement_paidlicensingfees" },
		{ 28, "ba:businessrequirement_drinksfridge" }
	};
}
