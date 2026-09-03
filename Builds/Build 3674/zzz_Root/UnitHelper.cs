using Localizor;

public static class UnitHelper
{
	public static bool useImperial;

	public const float MetersToFeet = 3.28084f;

	public const float SquareMetersToSquareFeet = 10.76391f;

	public static string ToFormattedDistance(this float distance)
	{
		if (!useImperial)
		{
			return LocalizorManager.GetLocalization("common_distance_meters", new
			{
				distance = $"{distance:N0}"
			});
		}
		return LocalizorManager.GetLocalization("common_distance_feet", new
		{
			distance = $"{distance * 3.28084f:N0}"
		});
	}

	public static string ToFormattedDistance(this int distance)
	{
		if (!useImperial)
		{
			return LocalizorManager.GetLocalization("common_distance_meters", new
			{
				distance = $"{distance:N0}"
			});
		}
		return LocalizorManager.GetLocalization("common_distance_feet", new
		{
			distance = $"{(float)distance * 3.28084f:N0}"
		});
	}

	public static string ToFormattedArea(this float area)
	{
		if (!useImperial)
		{
			return LocalizorManager.GetLocalization("common_area_meters", new
			{
				area = $"{area:N0}"
			});
		}
		return LocalizorManager.GetLocalization("common_area_feet", new
		{
			area = $"{area * 10.76391f:N0}"
		});
	}

	public static string ToFormattedArea(this int area)
	{
		if (!useImperial)
		{
			return LocalizorManager.GetLocalization("common_area_meters", new
			{
				area = $"{area:N0}"
			});
		}
		return LocalizorManager.GetLocalization("common_area_feet", new
		{
			area = $"{(float)area * 10.76391f:N0}"
		});
	}

	public static string GetAreaUnit()
	{
		if (!useImperial)
		{
			return "common_area_meter_unit".GetLocalization();
		}
		return "common_area_feet_unit".GetLocalization();
	}

	public static string GetDistanceUnit()
	{
		if (!useImperial)
		{
			return "common_distance_meter_unit".GetLocalization();
		}
		return "common_distance_feet_unit".GetLocalization();
	}
}
