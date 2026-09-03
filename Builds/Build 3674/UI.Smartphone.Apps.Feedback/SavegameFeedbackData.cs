using System.IO;
using Helpers;
using UnityEngine;

namespace UI.Smartphone.Apps.Feedback;

public class SavegameFeedbackData : IFeedbackData
{
	private const string FeedbackSavegameFileName = "feedback.hsg";

	private const string FeedbackSavegameFieldName = "savegame";

	private const string MidnightSavegameFileName = "feedbackmidnight.hsg";

	private const string MidnightSavegameFieldName = "savegamemidnight";

	private const string SavedMidnightSavegameFileName = "Recover Midnight.hsg";

	private const string MimeType = "application/hsg";

	public void AddToForm(ref WWWForm formData)
	{
		if (SaveGameManager.Current != null)
		{
			string path = Path.Combine(IFeedbackData.FeedbackFolder, "feedback.hsg");
			if (SaveGameSerializationHelper.SerializeBinaryData(path, SaveGameManager.Current))
			{
				formData.AddBinaryData("savegame", File.ReadAllBytes(path), "feedback.hsg", "application/hsg");
			}
			string path2 = Path.Combine(SaveGamePathHelper.CurrentVersionFolderPath(), SaveGameManager.Current.characterId, "Recover Midnight.hsg");
			if (File.Exists(path2))
			{
				formData.AddBinaryData("savegamemidnight", File.ReadAllBytes(path2), "feedbackmidnight.hsg", "application/hsg");
			}
		}
	}

	public void GatherData()
	{
		if (SaveGameManager.Current != null)
		{
			PlayerHelper.SaveCurrentPosition();
			VehicleHelper.SaveAllVehiclePositions();
			SaveGameManager.Current.buildNumberAtLastSave = GameVersion.GetCurrent().buildNumber;
		}
	}

	public void GatherDataDelayed()
	{
	}
}
