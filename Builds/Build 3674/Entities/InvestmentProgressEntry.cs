using System;

namespace Entities;

[Serializable]
public class InvestmentProgressEntry
{
	public int day;

	public float change;

	public float newBalance;

	public InvestmentProgressEntry(int day, float change, float newBalance)
	{
		this.day = day;
		this.change = change;
		this.newBalance = newBalance;
	}

	public InvestmentProgressEntry()
	{
	}
}
