using System.Collections.Generic;

namespace UI.Smartphone.Apps.BizMan;

public static class BusinessStatusFilter
{
	public const string Businesses = "common_businesses";

	public const string Empty = "ba:businesstype_empty";

	public const string RealEstate = "common_real_estate";

	public static readonly IReadOnlyList<string> All = new string[3] { "common_businesses", "ba:businesstype_empty", "common_real_estate" };
}
