using System;

[Serializable]
public class Satisfaction
{
	public int customerService;

	public int pricing;

	public int cleanliness;

	public int facility;

	public int overall;

	public Satisfaction()
	{
		customerService = 50;
		pricing = 50;
		cleanliness = 50;
		overall = 50;
		facility = 50;
	}
}
