using System.Collections.Generic;
using Extensions;
using UnityEngine;

namespace UI.Smartphone.Apps.Persona;

public class EducationInfo : MonoBehaviour
{
	[SerializeField]
	private EducationInfoEntry educationEntryTemplate;

	private void OnEnable()
	{
		educationEntryTemplate.transform.ResetTemplate();
		foreach (DiplomaData sortedDiploma in GetSortedDiplomas())
		{
			Diploma diploma = EducationHelper.GetDiploma(sortedDiploma.diplomaName);
			educationEntryTemplate.transform.CreateElement().GetComponent<EducationInfoEntry>().Setup(diploma);
		}
	}

	private static List<DiplomaData> GetSortedDiplomas()
	{
		List<DiplomaData> list = new List<DiplomaData>(EducationHelper.AllDiplomas.Count);
		HashSet<DiplomaName> addedDiplomas = new HashSet<DiplomaName>();
		HashSet<DiplomaName> sortingDiplomas = new HashSet<DiplomaName>();
		foreach (DiplomaData allDiploma in EducationHelper.AllDiplomas)
		{
			AddDiplomaWithDependencies(allDiploma, list, addedDiplomas, sortingDiplomas);
		}
		return list;
	}

	private static void AddDiplomaWithDependencies(DiplomaData diplomaData, List<DiplomaData> sortedDiplomas, HashSet<DiplomaName> addedDiplomas, HashSet<DiplomaName> sortingDiplomas)
	{
		if (addedDiplomas.Contains(diplomaData.diplomaName) || !sortingDiplomas.Add(diplomaData.diplomaName))
		{
			return;
		}
		if (diplomaData.requiredDiploma != DiplomaName.Undefined)
		{
			DiplomaData diplomaData2 = EducationHelper.GetDiplomaData(diplomaData.requiredDiploma);
			if (diplomaData2 != null)
			{
				AddDiplomaWithDependencies(diplomaData2, sortedDiplomas, addedDiplomas, sortingDiplomas);
			}
		}
		sortingDiplomas.Remove(diplomaData.diplomaName);
		if (addedDiplomas.Add(diplomaData.diplomaName))
		{
			sortedDiplomas.Add(diplomaData);
		}
	}
}
