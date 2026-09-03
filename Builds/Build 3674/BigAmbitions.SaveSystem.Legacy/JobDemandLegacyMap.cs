using System.Collections.Generic;

namespace BigAmbitions.SaveSystem.Legacy;

public sealed class JobDemandLegacyMap : LegacyMapperBase
{
	private static readonly HashSet<string> _legacyEnumTypeNames = new HashSet<string> { "Entities.Employee.JobDemands.JobDemandName, BigAmbitions", "Entities.JobDemandName, Assembly-CSharp" };

	public override List<string> Keys => new List<string> { "JobDemand.demandName", "EmployeeInstance.demands", "UnfulfilledDemandsComplaint.demandName", "HeadhunterDealBreakerData.applicableJobDemands", "AiBusinessEmployeeData.hoursPerWeekDemandName" };

	public override HashSet<string> LegacyEnumTypeNames => _legacyEnumTypeNames;

	protected override Dictionary<int, string> Map => new Dictionary<int, string>
	{
		{ 0, "ba:jobdemand_parttime" },
		{ 1, "ba:jobdemand_fulltime" },
		{ 2, "ba:jobdemand_fourdaysweek" },
		{ 3, "ba:jobdemand_fivedaysweek" },
		{ 4, "ba:jobdemand_nomornings" },
		{ 5, "ba:jobdemand_noevenings" },
		{ 6, "ba:jobdemand_nonights" },
		{ 7, "ba:jobdemand_freeweekends" },
		{ 8, "ba:jobdemand_nocleaning" },
		{ 9, "ba:jobdemand_peacefulworkenvironment" },
		{ 10, "ba:jobdemand_seatedatofficechair2" },
		{ 11, "ba:jobdemand_seatedatmultipurposechair" },
		{ 12, "ba:jobdemand_seatedatofficedesk2" },
		{ 13, "ba:jobdemand_standardfridge" },
		{ 14, "ba:jobdemand_largemeetingtable" },
		{ 15, "ba:jobdemand_sofa" },
		{ 16, "ba:jobdemand_coffeemachine" },
		{ 17, "ba:jobdemand_cleanworkplace" },
		{ 18, "ba:jobdemand_bronzehealthinsurance" },
		{ 19, "ba:jobdemand_silverhealthinsurance" },
		{ 20, "ba:jobdemand_goldhealthinsurance" },
		{ 21, "ba:jobdemand_hasgraphictablet" },
		{ 22, "ba:jobdemand_hasgraphictabletwithscreen" },
		{ 23, "ba:jobdemand_hasmousepad" },
		{ 24, "ba:jobdemand_hasphone" },
		{ 25, "ba:jobdemand_hasofficephone" },
		{ 27, "ba:jobdemand_noafternoons" },
		{ 28, "ba:jobdemand_watercooler" },
		{ 29, "ba:jobdemand_seatedatofficedesk1" },
		{ 30, "ba:jobdemand_seatedatofficechair" },
		{ 31, "ba:jobdemand_hascomputermonitor" },
		{ 32, "ba:jobdemand_hasprinter" },
		{ 33, "ba:jobdemand_hascalculator" },
		{ 34, "ba:jobdemand_hasdeskglobe" },
		{ 35, "ba:jobdemand_hasdeskcalendar" }
	};
}
