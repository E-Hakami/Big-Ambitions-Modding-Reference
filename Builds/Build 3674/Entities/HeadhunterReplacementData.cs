using System;
using UnityEngine;

namespace Entities;

[Serializable]
public class HeadhunterReplacementData
{
	private const int HeadhunterWorkingHours = 8;

	public EmployeeInstance employeeInstance;

	public int hoursUntilReplacement;

	public void SetHoursUntilReplacement(ReplacementReason replacementReason)
	{
		hoursUntilReplacement = replacementReason switch
		{
			ReplacementReason.Satisfaction => 8, 
			ReplacementReason.Retirement => 0, 
			ReplacementReason.Poached => UnityEngine.Random.Range(24, 41), 
			_ => throw new ArgumentOutOfRangeException("replacementReason", replacementReason, null), 
		};
	}
}
