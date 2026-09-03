using System.Collections.Generic;
using Helpers;
using IngameDebugConsole;
using Localizor;
using UI.Notification;

public static class EducationHelper
{
	public const string AddressableLabel = "Diploma";

	public static readonly List<DiplomaData> AllDiplomas = new List<DiplomaData>();

	public static void OnDiplomasLoaded(IList<DiplomaData> diplomas)
	{
		AllDiplomas.Clear();
		AllDiplomas.AddRange(diplomas);
	}

	public static bool HasCompletedDiploma(DiplomaName name)
	{
		return GetDiploma(name).completed;
	}

	public static Diploma GetDiploma(DiplomaName name)
	{
		Diploma diploma = SaveGameManager.Current?.PlayerDiplomas.Find((Diploma x) => x.name == name);
		if (diploma != null)
		{
			return diploma;
		}
		diploma = new Diploma
		{
			name = name
		};
		SaveGameManager.Current?.PlayerDiplomas.Add(diploma);
		return diploma;
	}

	public static DiplomaData GetDiplomaData(DiplomaName name)
	{
		return AllDiplomas.Find((DiplomaData x) => x.diplomaName == name);
	}

	public static void UnlockAllCourses()
	{
		foreach (DiplomaData allDiploma in AllDiplomas)
		{
			Diploma diploma = GetDiploma(allDiploma.diplomaName);
			diploma.completed = true;
			diploma.minutesStudied = allDiploma.requiredMinutes;
		}
	}

	public static void ShowCourseRequiredNotification(string businessType, DiplomaName diplomaRequired)
	{
		Dictionary<string, string> notificationData = new Dictionary<string, string>
		{
			{
				"name",
				diplomaRequired.GetLocalization()
			},
			{
				"businessName",
				businessType.GetLocalization()
			}
		};
		Notifications.Show(NotificationType.Error, "education_helper_notification_business_type_course_required", notificationData);
	}

	[ConsoleMethod("courses", "Completes a specific course/diploma", new string[] { })]
	public static void Command_CompleteCourse(DiplomaName diplomaName, bool completed = true)
	{
		Diploma diploma = GetDiploma(diplomaName);
		DiplomaData diplomaData = GetDiplomaData(diplomaName);
		diploma.completed = completed;
		diploma.minutesStudied = (completed ? diplomaData.requiredMinutes : 0);
	}

	[ConsoleMethod("UnlockAllCourses", "Unlocks all courses", new string[] { })]
	public static void Command_UnlockAllCourses()
	{
		UnlockAllCourses();
	}
}
