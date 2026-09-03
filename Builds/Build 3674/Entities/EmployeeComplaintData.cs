using System;
using AI.Employees;
using UnityEngine;

namespace Entities;

[Serializable]
public class EmployeeComplaintData
{
	public const int BonusComplaintHoursIncrease = 720;

	public const int MaxComplaintsPerWeek = 2;

	public bool isComplaining;

	public int hoursUntilNextComplaint;

	public Complaint currentComplaint;

	public int complaintDeadlineHours;

	public bool hasRival;

	public void ResetHoursUntilNextComplaint()
	{
		int daysPerYear = SaveGameManager.Current.gameVariables.daysPerYear;
		int num = Mathf.FloorToInt(UnityEngine.Random.Range((float)daysPerYear * 0.5f, (float)daysPerYear * 1.5f));
		hoursUntilNextComplaint = num * 24;
		complaintDeadlineHours = 0;
		isComplaining = false;
		hasRival = false;
	}

	public void UpdateHoursUntilNextComplaintDueToSatisfaction(float satisfaction)
	{
		if (satisfaction > 75f)
		{
			hoursUntilNextComplaint += 12;
		}
		else if (satisfaction < 25f)
		{
			hoursUntilNextComplaint -= 12;
		}
	}

	public void UpdateHoursUntilNextComplaintHourly()
	{
		hoursUntilNextComplaint--;
	}

	public void UpdateHoursUntilNextComplaintDueToBonus()
	{
		if (hoursUntilNextComplaint < 0)
		{
			hoursUntilNextComplaint = 720;
		}
		else
		{
			hoursUntilNextComplaint += 720;
		}
	}

	public void SetDataFromComplaint(Complaint complaint, bool complaintHasRival)
	{
		ResetHoursUntilNextComplaint();
		complaintDeadlineHours = complaint.hoursToHandleComplaint;
		isComplaining = true;
		currentComplaint = complaint;
		hasRival = complaintHasRival;
	}
}
