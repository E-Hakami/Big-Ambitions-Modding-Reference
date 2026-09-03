using TMPro;
using UnityEngine;

namespace Character.Customization;

public class Age : MonoBehaviour
{
	[SerializeField]
	private int minimumAge;

	[SerializeField]
	private TMP_InputField ageField;

	[HideInInspector]
	public int currentAge;

	private void Start()
	{
		currentAge = int.Parse(ageField.text);
	}

	public void UpdateAge()
	{
		currentAge = int.Parse(ageField.text);
		if (currentAge < minimumAge)
		{
			currentAge = minimumAge;
			ageField.text = currentAge.ToString();
		}
	}

	public void DecrementAge()
	{
		if (currentAge > minimumAge)
		{
			currentAge--;
			ageField.text = currentAge.ToString();
		}
	}

	public void IncrementAge()
	{
		if (currentAge < 99)
		{
			currentAge++;
			ageField.text = currentAge.ToString();
		}
	}
}
