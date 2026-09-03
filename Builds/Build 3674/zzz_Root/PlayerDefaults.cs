using System;
using UI.Smartphone.Apps.Contacts;
using UnityEngine.Serialization;

[Serializable]
public class PlayerDefaults
{
	public int sleepHours = 8;

	public int sleepMinutes;

	public int playHours = 2;

	public int watchTvHours = 2;

	public int playMinutes = 30;

	public int watchTvMinutes = 30;

	public int djMinutes = 30;

	public int readMinutes = 30;

	[FormerlySerializedAs("workOutMinutes")]
	public int workoutMinutes = 30;

	public int sleepInBedMinutes = 480;

	public int sleepInCarMinutes = 30;

	public int sleepInBenchMinutes = 30;

	public int sleepInBoatMinutes = 30;

	public int restOnChairMinutes = 30;

	public int hygieneMinutes = 15;

	public int swimmingMinutes = 30;

	public int golfMinutes = 30;

	public int tennisMinutes = 30;

	public string contactsLastName;

	public ContactCategoryName contactsLastCategoryName;

	public bool fastScheduleAutoFill;
}
