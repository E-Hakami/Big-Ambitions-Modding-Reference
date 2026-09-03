using System.IO;
using UnityEngine;

namespace UI.Smartphone.Apps.Feedback;

public class ScreenshotFeedbackData : IFeedbackData
{
	private const string TakenScreenshotFileName = "Feedback.jpg";

	private const string SentScreenshotFileName = "screenshot.jpg";

	private const string FormFieldName = "screenshot";

	private const string MimeType = "image/jpeg";

	private string _fileName;

	public static Texture2D CursorTexture { get; set; }

	public void AddToForm(ref WWWForm formData)
	{
		formData.AddBinaryData("screenshot", File.ReadAllBytes(Path.Combine(IFeedbackData.FeedbackFolder, "Feedback.jpg")), "screenshot.jpg", "image/jpeg");
	}

	public void GatherData()
	{
		string feedbackFolder = IFeedbackData.FeedbackFolder;
		Directory.CreateDirectory(feedbackFolder);
		_fileName = Path.Combine(feedbackFolder, "Feedback.jpg");
		if (File.Exists(_fileName))
		{
			File.Delete(_fileName);
		}
	}

	public void GatherDataDelayed()
	{
		TakeScreenshot(_fileName);
	}

	private static void TakeScreenshot(string screenshotPath)
	{
		Texture2D texture2D = ScreenCapture.CaptureScreenshotAsTexture();
		DrawCursor(texture2D);
		texture2D.Apply();
		byte[] bytes = texture2D.EncodeToJPG();
		File.WriteAllBytes(screenshotPath, bytes);
		Object.Destroy(texture2D);
	}

	private static void DrawCursor(Texture2D screenshotTexture)
	{
		for (int i = 0; i < CursorTexture.height; i++)
		{
			for (int j = 0; j < CursorTexture.width; j++)
			{
				Color pixel = CursorTexture.GetPixel(j, i);
				if (pixel.a > 0f)
				{
					screenshotTexture.SetPixel((int)Input.mousePosition.x + j, (int)Input.mousePosition.y + i, pixel);
				}
			}
		}
	}
}
