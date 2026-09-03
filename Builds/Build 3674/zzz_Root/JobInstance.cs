using System;

[Serializable]
public class JobInstance
{
	public Address address;

	public bool hired;

	public bool fired;

	public int warnings;

	public int lastWarningDay;

	public int hiringDay;

	public int firedDay;
}
