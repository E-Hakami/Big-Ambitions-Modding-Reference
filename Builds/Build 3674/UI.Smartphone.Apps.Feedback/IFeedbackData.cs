using System.IO;
using UnityEngine;

namespace UI.Smartphone.Apps.Feedback;

public interface IFeedbackData
{
	protected static string FeedbackFolder => Path.Combine(Application.persistentDataPath, "BugReports");

	void AddToForm(ref WWWForm formData);

	void GatherData();

	void GatherDataDelayed();
}
