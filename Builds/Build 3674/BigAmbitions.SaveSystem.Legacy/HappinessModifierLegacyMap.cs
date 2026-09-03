using System.Collections.Generic;

namespace BigAmbitions.SaveSystem.Legacy;

public sealed class HappinessModifierLegacyMap : LegacyMapperBase
{
	private static readonly HashSet<string> _legacyEnumTypeNames = new HashSet<string> { "Helpers.HappinessModifierType, BigAmbitions" };

	public override List<string> Keys => new List<string>
	{
		"GameInstance.usedHappinessModifiers", "HappinessModifier.type", "HappinessModifier.nonTemporalType", "HappinessModifierData.type", "EntertainDevice.happinessModifierType", "HygieneEnvironment.happinessModifierType", "PaidActivityEnvironment.happinessModifierType", "RestEnvironment.happinessModifierType", "SleepEnvironment.happinessModifierType", "WorkoutExercise.happinessModifierType",
		"ChangeHappinessModifier.happinessModifierType", "HasHappinessModifier.type", "LocationHappinessTrigger.regularBoost", "LocationHappinessTrigger.temporalBoost"
	};

	public override HashSet<string> LegacyEnumTypeNames => _legacyEnumTypeNames;

	protected override Dictionary<int, string> Map => new Dictionary<int, string>
	{
		{ 0, "ba:happinessmodifier_cheat" },
		{ 1, "ba:happinessmodifier_started_a_headquarters" },
		{ 2, "ba:happinessmodifier_slept_in_the_car" },
		{ 3, "ba:happinessmodifier_walked_in_the_park" },
		{ 4, "ba:happinessmodifier_went_to_hospital" },
		{ 5, "ba:happinessmodifier_no_home" },
		{ 6, "ba:happinessmodifier_first_day_on_ny" },
		{ 7, "ba:happinessmodifier_first_apartment" },
		{ 8, "ba:happinessmodifier_firstjob" },
		{ 9, "ba:happinessmodifier_first_employee" },
		{ 10, "ba:happinessmodifier_completed_personal_goal" },
		{ 11, "ba:happinessmodifier_positive_revenue" },
		{ 12, "ba:happinessmodifier_gambled" },
		{ 13, "ba:happinessmodifier_played_videogames" },
		{ 14, "ba:happinessmodifier_a_fresh_start" },
		{ 15, "ba:happinessmodifier_watched_tv" },
		{ 16, "ba:happinessmodifier_rested_on_a_boat" },
		{ 17, "ba:happinessmodifier_exercised" },
		{ 18, "ba:happinessmodifier_went_to_nightclub" },
		{ 19, "ba:happinessmodifier_djed" },
		{ 20, "ba:happinessmodifier_walking_in_the_park" },
		{ 21, "ba:happinessmodifier_playing_videogames" },
		{ 22, "ba:happinessmodifier_watching_tv" },
		{ 23, "ba:happinessmodifier_djing" },
		{ 24, "ba:happinessmodifier_in_a_nightclub" },
		{ 25, "ba:happinessmodifier_exercising" },
		{ 26, "ba:happinessmodifier_reading" },
		{ 27, "ba:happinessmodifier_read" },
		{ 28, "ba:happinessmodifier_went_to_the_skate_park" },
		{ 29, "ba:happinessmodifier_in_the_skate_park" },
		{ 30, "ba:happinessmodifier_personalized_workout_plan" },
		{ 31, "ba:happinessmodifier_wet" },
		{ 32, "ba:happinessmodifier_hygiene" },
		{ 33, "ba:happinessmodifier_swimming" },
		{ 34, "ba:happinessmodifier_went_swimming" },
		{ 35, "ba:happinessmodifier_partying_at_the_pier" },
		{ 36, "ba:happinessmodifier_partied_at_the_pier" },
		{ 37, "ba:happinessmodifier_riding_pier_rides" },
		{ 38, "ba:happinessmodifier_rode_pier_rides" },
		{ 39, "ba:happinessmodifier_watched_show" },
		{ 40, "ba:happinessmodifier_donated" },
		{ 41, "ba:happinessmodifier_watching_show" }
	};
}
