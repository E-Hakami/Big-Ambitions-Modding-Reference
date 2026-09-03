namespace UI.Smartphone.Apps.Persona;

public struct PersonalWealthData
{
	public float cash;

	public float totalInvestments;

	public float totalLoans;

	public float totalAssets;

	public float CurrentWealth => cash + totalInvestments - totalLoans + totalAssets;

	public float WealthBeforeLoans => cash + totalInvestments + totalAssets;
}
