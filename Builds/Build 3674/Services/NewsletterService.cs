using System;
using System.Collections;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Services;

public static class NewsletterService
{
	[Serializable]
	private class SubscribeRequest
	{
		public int gameId;

		public string email;

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string location;
	}

	private const string SubscribeUrl = "https://gametools.hovgaard.com/api/newsletter/subscribe";

	private const string ApiKey = "1b8f48879f4b4b328fe7ed79d136c3aa";

	private const int GameId = 1;

	private const int MaxRateLimitRetries = 3;

	private const float DefaultRateLimitBackoffSeconds = 15f;

	public static IEnumerator Subscribe(string email, string location = null)
	{
		if (string.IsNullOrWhiteSpace(email))
		{
			yield break;
		}
		string s = JsonConvert.SerializeObject(new SubscribeRequest
		{
			gameId = 1,
			email = email.Trim(),
			location = (string.IsNullOrWhiteSpace(location) ? null : location.Trim())
		});
		byte[] bodyRaw = Encoding.UTF8.GetBytes(s);
		for (int attempt = 0; attempt <= 3; attempt++)
		{
			using UnityWebRequest request = new UnityWebRequest("https://gametools.hovgaard.com/api/newsletter/subscribe", "POST");
			request.uploadHandler = new UploadHandlerRaw(bodyRaw);
			request.downloadHandler = new DownloadHandlerBuffer();
			request.SetRequestHeader("Content-Type", "application/json");
			request.SetRequestHeader("X-Api-Key", "1b8f48879f4b4b328fe7ed79d136c3aa");
			yield return request.SendWebRequest();
			long responseCode = request.responseCode;
			switch (responseCode)
			{
			case 200L:
				yield break;
			case 403L:
				Debug.LogWarning("Newsletter subscribe rejected for suppressed address: " + request.downloadHandler.text);
				yield break;
			case 429L:
			{
				if (attempt >= 3)
				{
					break;
				}
				float retryAfterSeconds = GetRetryAfterSeconds(request);
				Debug.LogWarning($"Newsletter subscribe rate limited; retrying after {retryAfterSeconds}s.");
				yield return new WaitForSecondsRealtime(retryAfterSeconds);
				goto end_IL_00b4;
			}
			}
			Debug.LogException(new Exception($"Newsletter subscribe failed ({responseCode}): {request.error} {request.downloadHandler.text}"));
			break;
			end_IL_00b4:;
		}
	}

	private static float GetRetryAfterSeconds(UnityWebRequest request)
	{
		string responseHeader = request.GetResponseHeader("Retry-After");
		if (!string.IsNullOrEmpty(responseHeader) && float.TryParse(responseHeader, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && result > 0f)
		{
			return result;
		}
		return 15f;
	}
}
