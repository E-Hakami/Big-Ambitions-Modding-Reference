using System;

[Serializable]
public class Loan
{
	public float totalAmount;

	public float remainingAmount;

	public int dailyInterest;

	public int dailyPayment;

	public Address bankAddress;

	public float PaidAmount => totalAmount - remainingAmount;

	public Loan()
	{
	}

	public Loan(float amount, int dailyInterest, int dailyPayment, Address bankAddress)
	{
		totalAmount = amount;
		remainingAmount = amount;
		this.dailyInterest = dailyInterest;
		this.dailyPayment = dailyPayment;
		this.bankAddress = bankAddress;
	}
}
