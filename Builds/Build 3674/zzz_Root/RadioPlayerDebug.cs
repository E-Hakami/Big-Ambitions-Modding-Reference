using System.Text;
using UnityEngine;

public class RadioPlayerDebug : MonoBehaviour
{
	private const float ReferenceScreenHeight = 1080f;

	private GUIStyle _guiStyle;

	private void Start()
	{
		_guiStyle = new GUIStyle
		{
			fontSize = 14,
			normal = 
			{
				textColor = Color.white
			}
		};
	}

	private void OnGUI()
	{
		GUI.matrix = Matrix4x4.Scale(Vector3.one * ((float)Screen.height / 1080f));
		RadioPlayer radioPlayer = InstanceBehavior<GameManager>.Instance.radioPlayer;
		RadioStationData radioStationData = radioPlayer.GetRadioStationData(radioPlayer.GetCurrentStation());
		AudioClip currentClip = radioPlayer.currentClip;
		GUI.Box(new Rect(10f, 120f, 250f, 170f), "Radio Player Debug");
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"Current station: {radioPlayer.GetCurrentStation()}");
		stringBuilder.AppendLine("Current song: " + ((currentClip == null) ? "-" : currentClip.name));
		stringBuilder.AppendLine($"Is muted: {radioPlayer.IsMuted}");
		stringBuilder.AppendLine($"Is loading: {radioStationData.IsLoading}");
		stringBuilder.AppendLine((currentClip == null) ? "Time to end: -" : $"Time to end: {currentClip.length - radioPlayer.GetSongTime()}");
		stringBuilder.AppendLine($"Source / schedule time: {radioPlayer.GetSongTime():F1} / {radioStationData.CurrentClipProgressedTime:F1}");
		if (radioStationData.radioClips.Length != 0)
		{
			int num = ((radioStationData.currentClipIndex + 1 < radioStationData.radioClips.Length) ? (radioStationData.currentClipIndex + 1) : 0);
			stringBuilder.AppendLine("Next song: " + radioStationData.radioClips[num].Name);
		}
		GUI.Label(new Rect(20f, 150f, 230f, 20f), stringBuilder.ToString(), _guiStyle);
	}
}
