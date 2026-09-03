using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Localizor;
using Localizor.LanguageChangeEvent;
using LogicUI.FancyTextRendering;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using UnityEngine.UI.Extensions.HelpSystem;
using UnityEngine.Video;

public class HelpPageRenderer : MonoBehaviour
{
	private class UiEntry
	{
		public UiType UiType;

		public string Text;

		public Sprite Sprite;

		public VideoClip VideoClip;
	}

	private enum UiType
	{
		Text,
		Image,
		Video
	}

	public MarkdownRenderer markdownTemplate;

	public TextLinkHelper textLinkHelper;

	public VideoPlayer videoTemplate;

	public Image imageTemplate;

	public TextLocalizationComponent categoryNameLabel;

	public TextLocalizationComponent pageNameLabel;

	private readonly List<AsyncOperationHandle<Sprite>> _spriteHandles = new List<AsyncOperationHandle<Sprite>>();

	private readonly List<AsyncOperationHandle<VideoClip>> _clipHandles = new List<AsyncOperationHandle<VideoClip>>();

	private void Start()
	{
		markdownTemplate.gameObject.SetActive(value: false);
		videoTemplate.gameObject.SetActive(value: false);
		imageTemplate.gameObject.SetActive(value: false);
	}

	private void OnDestroy()
	{
		ReleaseHandles();
	}

	private void LinkClicked(string slug)
	{
		InstanceBehavior<HelpSystem>.Instance.OpenLink(slug);
	}

	public void RenderPage(string slug)
	{
		ReleaseHandles();
		IEnumerable<UiEntry> enumerable = ConvertMarkdownToUiEntries(("help_" + slug + "_content").GetLocalization());
		categoryNameLabel.Key = "menu_options_none";
		pageNameLabel.Key = slug;
		ClearPage();
		foreach (UiEntry item in enumerable)
		{
			switch (item.UiType)
			{
			case UiType.Text:
			{
				MarkdownRenderer markdownRenderer = Object.Instantiate(markdownTemplate, base.transform);
				markdownRenderer.gameObject.AddComponent<TextLinkHelper>().OnLinkClicked += LinkClicked;
				markdownRenderer.Source = item.Text;
				markdownRenderer.gameObject.SetActive(value: true);
				break;
			}
			case UiType.Image:
			{
				Image image = Object.Instantiate(imageTemplate, base.transform);
				image.sprite = item.Sprite;
				image.gameObject.SetActive(value: true);
				break;
			}
			case UiType.Video:
			{
				VideoPlayer videoPlayer = Object.Instantiate(videoTemplate, base.transform);
				videoPlayer.clip = item.VideoClip;
				videoPlayer.gameObject.SetActive(value: true);
				break;
			}
			}
		}
		InstanceBehavior<HelpSystem>.Instance.currentSlugChanged.Invoke(slug);
	}

	public void RenderPage(HelpStructureGroupEntry helpPageCategory, HelpStructurePageEntry helpPageEntry)
	{
		ReleaseHandles();
		IEnumerable<UiEntry> enumerable = ConvertMarkdownToUiEntries(("help_" + helpPageEntry.PageLocalizorKeyPrefix + "_content").GetLocalization());
		categoryNameLabel.Key = helpPageCategory.CategoryLocalizorKey;
		pageNameLabel.Key = helpPageEntry.PageLocalizorKeyPrefix;
		ClearPage();
		foreach (UiEntry item in enumerable)
		{
			switch (item.UiType)
			{
			case UiType.Text:
			{
				MarkdownRenderer markdownRenderer = Object.Instantiate(markdownTemplate, base.transform);
				markdownRenderer.gameObject.AddComponent<TextLinkHelper>().OnLinkClicked += LinkClicked;
				markdownRenderer.Source = item.Text;
				markdownRenderer.gameObject.SetActive(value: true);
				break;
			}
			case UiType.Image:
			{
				Image image = Object.Instantiate(imageTemplate, base.transform);
				image.sprite = item.Sprite;
				image.gameObject.SetActive(value: true);
				break;
			}
			case UiType.Video:
			{
				VideoPlayer videoPlayer = Object.Instantiate(videoTemplate, base.transform);
				videoPlayer.clip = item.VideoClip;
				videoPlayer.gameObject.SetActive(value: true);
				break;
			}
			}
		}
		InstanceBehavior<HelpSystem>.Instance.currentSlugChanged.Invoke(helpPageEntry.Slug);
	}

	private IEnumerable<UiEntry> ConvertMarkdownToUiEntries(string content)
	{
		string pattern = "!\\[[^\\]]*\\]\\((?<filename>.*?)(?=\\\"|\\))(\\\".*\\\")?\\)";
		string pattern2 = "<video.*src=[\"'](.+?)[\"'].*>";
		string mediaTagPattern = "\\[(image|video),(.+?)\\]";
		content = Regex.Replace(content, pattern, (Match match) => "__UISPLIT[image," + match.Groups[2].Value + "]");
		content = Regex.Replace(content, pattern2, (Match match) => "__UISPLIT[video," + match.Groups[1].Value + "]", RegexOptions.Singleline);
		string[] array = content.Split("__UISPLIT");
		foreach (string text in array)
		{
			string clean = text;
			Match media = Regex.Match(text, mediaTagPattern);
			if (media.Success)
			{
				if (media.Groups[1].Value == "video")
				{
					string value = media.Groups[2].Value;
					AsyncOperationHandle<VideoClip> item = Addressables.LoadAssetAsync<VideoClip>("HelpContent/" + Path.GetFileName(value).Trim());
					_clipHandles.Add(item);
					VideoClip videoClip = item.WaitForCompletion();
					yield return new UiEntry
					{
						UiType = UiType.Video,
						VideoClip = videoClip
					};
				}
				else
				{
					AsyncOperationHandle<Sprite> item2 = Addressables.LoadAssetAsync<Sprite>("HelpContent/" + Path.GetFileName(media.Groups[2].Value).Trim());
					_spriteHandles.Add(item2);
					Sprite sprite = item2.WaitForCompletion();
					yield return new UiEntry
					{
						UiType = UiType.Image,
						Sprite = sprite
					};
				}
				clean = clean.Replace(media.Value, "");
			}
			yield return new UiEntry
			{
				UiType = UiType.Text,
				Text = clean
			};
		}
	}

	private void ClearPage()
	{
		foreach (Transform item in base.transform)
		{
			if (item != markdownTemplate.transform && item != videoTemplate.transform && item != imageTemplate.transform && item != categoryNameLabel.transform.parent.transform)
			{
				Object.Destroy(item.gameObject);
			}
		}
	}

	private void ReleaseHandles()
	{
		foreach (AsyncOperationHandle<Sprite> spriteHandle in _spriteHandles)
		{
			Addressables.Release(spriteHandle);
		}
		foreach (AsyncOperationHandle<VideoClip> clipHandle in _clipHandles)
		{
			Addressables.Release(clipHandle);
		}
		_spriteHandles.Clear();
		_clipHandles.Clear();
	}
}
