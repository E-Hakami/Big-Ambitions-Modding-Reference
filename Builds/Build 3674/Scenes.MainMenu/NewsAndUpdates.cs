using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Scenes.MainMenu;

public class NewsAndUpdates : MonoBehaviour
{
	[SerializeField]
	private int numberOfPostsToLoad = 10;

	[SerializeField]
	private Image displayImage;

	[SerializeField]
	private TMP_Text postTitleLabel;

	[SerializeField]
	private TMP_Text postDescriptionLabel;

	[SerializeField]
	private Texture2D defaultDisplayImage;

	[SerializeField]
	private GameObject previousButton;

	[SerializeField]
	private GameObject nextButton;

	private List<Post> _posts;

	private int _currentPostIndex;

	private readonly List<Texture2D> _textures = new List<Texture2D>();

	private Sprite _currentPostSprite;

	private void Start()
	{
		StartCoroutine(LoadPosts());
	}

	private IEnumerator LoadPosts()
	{
		_posts = new List<Post>();
		using UnityWebRequest request = UnityWebRequest.Get("https://www.bigambitionsgame.com/api/posts.html");
		yield return request.SendWebRequest();
		if (request.result != UnityWebRequest.Result.Success)
		{
			Debug.Log(request.error);
			yield break;
		}
		JArray jArray = JArray.Parse(request.downloadHandler.text);
		List<IEnumerator> list = new List<IEnumerator>();
		foreach (JToken item in jArray)
		{
			JObject jObject = item.ToObject<JObject>();
			if (jObject != null)
			{
				string url = string.Format("https://www.bigambitionsgame.com{0}", jObject["image"]);
				Post post = new Post
				{
					title = jObject["title"]?.ToString(),
					description = jObject["description"]?.ToString(),
					steamUrl = jObject["steamUrl"]?.ToString()
				};
				list.Add(DownloadThumbnail(post, url));
				_posts.Add(post);
				if (_posts.Count >= numberOfPostsToLoad)
				{
					break;
				}
			}
		}
		foreach (IEnumerator item2 in list)
		{
			yield return item2;
		}
		if (_posts.Count > 0)
		{
			SetPost(0);
		}
		else
		{
			SetErrorPost();
		}
	}

	private IEnumerator DownloadThumbnail(Post post, string url)
	{
		if (string.IsNullOrEmpty(url))
		{
			yield break;
		}
		using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			post.displayImage = DownloadHandlerTexture.GetContent(request);
			_textures.Add(post.displayImage);
			yield break;
		}
		string text = "Error downloading News thumbnail: " + request.result;
		if (!string.IsNullOrEmpty(request.downloadHandler.error))
		{
			text = text + "\n" + request.downloadHandler.error;
		}
		Debug.Log(text);
	}

	public void NextPost()
	{
		if (_posts.Count != 0)
		{
			_currentPostIndex++;
			if (_currentPostIndex >= _posts.Count)
			{
				_currentPostIndex = 0;
			}
			SetPost(_currentPostIndex);
		}
	}

	public void PreviousPost()
	{
		if (_posts.Count != 0)
		{
			_currentPostIndex--;
			if (_currentPostIndex < 0)
			{
				_currentPostIndex = _posts.Count - 1;
			}
			SetPost(_currentPostIndex);
		}
	}

	private void SetPost(int i)
	{
		postTitleLabel.text = _posts[i].title;
		postDescriptionLabel.text = _posts[i].description;
		if (_currentPostSprite != null)
		{
			Object.Destroy(_currentPostSprite);
		}
		Texture2D texture2D = ((_posts[i].displayImage != null) ? _posts[i].displayImage : defaultDisplayImage);
		displayImage.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), Vector2.one / 2f);
		_currentPostSprite = displayImage.sprite;
		Button component = displayImage.GetComponent<Button>();
		component.onClick.RemoveAllListeners();
		component.onClick.AddListener(delegate
		{
			OpenPost(_posts[i]);
		});
		_currentPostIndex = i;
		previousButton.SetActive(i != 0);
		nextButton.SetActive(i != _posts.Count - 1);
	}

	private void OpenPost(Post post)
	{
		if (Singleton<SteamAPI>.Instance.steamApiEnabled)
		{
			SteamFriends.OpenWebOverlay(post.steamUrl);
		}
		else
		{
			Application.OpenURL(post.steamUrl);
		}
	}

	private void SetErrorPost()
	{
		postTitleLabel.text = "Oups!";
		postDescriptionLabel.text = "Seems like we couldn't load the updates. It may be related to your internet connection.";
		displayImage.sprite = Sprite.Create(defaultDisplayImage, new Rect(0f, 0f, defaultDisplayImage.width, defaultDisplayImage.height), Vector2.one / 2f);
	}

	public void CleanupTextures()
	{
		foreach (Texture2D texture in _textures)
		{
			Object.Destroy(texture);
		}
		_textures.Clear();
	}
}
