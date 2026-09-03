using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Blueprints;

public static class BlueprintsWorkshopHelper
{
	public static async Task<(Texture2D, Sprite)> DownloadBlueprintThumbnail(string thumbnailURL)
	{
		using UnityWebRequest www = UnityWebRequestTexture.GetTexture(thumbnailURL);
		UnityWebRequestAsyncOperation asyncOp = www.SendWebRequest();
		while (!asyncOp.isDone)
		{
			await Task.Delay(33);
		}
		if (www.result != UnityWebRequest.Result.Success)
		{
			return (null, null);
		}
		Texture2D content = DownloadHandlerTexture.GetContent(www);
		Sprite item = Sprite.Create(content, new Rect(0f, 0f, 1920f, 1080f), new Vector2(0.5f, 0.5f));
		return (content, item);
	}
}
