using UnityEngine;

[CreateAssetMenu(fileName = "DiplomaData", menuName = "BigAmbitions/Diploma data")]
public class DiplomaData : ScriptableObject
{
	public DiplomaName diplomaName;

	public int pricePerHour;

	public DiplomaName requiredDiploma;

	public int requiredMinutes;
}
