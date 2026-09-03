using System;
using System.Collections.Generic;

[Serializable]
public class Job
{
	public List<DiplomaName> requiredDiplomas;

	public string employerName;

	public List<ScheduleDay> scheduleDays;

	public float hourlySalary;

	public string localizeKey;

	public int minDaysBeforeRehireAfterFiring = 30;
}
