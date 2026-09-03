namespace AI.Employees.SalaryNegotiation;

public struct CandidateSalaryOffer
{
	public const int HoursPerMonth = 160;

	private const int HoursPerYear = 1920;

	public float hourlyWage;

	public float signingBonus;

	public readonly bool fromCandidate;

	public float Total => hourlyWage + GetHourlyValueForSigningBonus(signingBonus);

	public static float GetHourlyValueForSigningBonus(float signingBonus)
	{
		return signingBonus / 1920f;
	}

	public CandidateSalaryOffer(float hourlyWage, float signingBonus, bool fromCandidate)
	{
		this.hourlyWage = hourlyWage;
		this.signingBonus = signingBonus;
		this.fromCandidate = fromCandidate;
	}
}
