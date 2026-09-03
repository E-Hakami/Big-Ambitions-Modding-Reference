using System;
using System.Collections.Generic;
using Seasons;

namespace Scenes;

public static class GameScenesHelper
{
	public const GameScenes MainMenuScenes = GameScenes.MainMenu;

	public const GameScenes TransitionToSave = GameScenes.TransitionToSave;

	public const GameScenes IntroScene = GameScenes.Intro;

	public const GameScenes BlueprintCreatorScenes = GameScenes.LightsAndPostprocessing | GameScenes.Indoor | GameScenes.BlueprintCreator;

	private const GameScenes CityScenes = GameScenes.MainScene | GameScenes.UI | GameScenes.Buildings | GameScenes.ExteriorProps | GameScenes.LightsAndPostprocessing | GameScenes.Streets | GameScenes.Indoor | GameScenes.LowerManhattan | GameScenes.IndustryCity | GameScenes.TheHamptons;

	public static GameScenes GetAllCityScenes()
	{
		GameScenes baseCityScenes = GetBaseCityScenes();
		if (PlayerPrefSettings.SeasonalDecorations && SeasonHelper.CurrentSeason != null && SeasonHelper.CurrentSeason.hasSpecificScene && Enum.TryParse<GameScenes>(SeasonHelper.CurrentSeason.sceneName, out var result))
		{
			return baseCityScenes | result;
		}
		return baseCityScenes;
	}

	private static GameScenes GetBaseCityScenes()
	{
		return GameScenes.MainScene | GameScenes.UI | GameScenes.Buildings | GameScenes.ExteriorProps | GameScenes.LightsAndPostprocessing | GameScenes.Streets | GameScenes.Indoor | GameScenes.LowerManhattan | GameScenes.IndustryCity | GameScenes.TheHamptons;
	}

	public static GameScenes[] GetScenesFromMask(GameScenes scenesToLoad)
	{
		List<GameScenes> list = new List<GameScenes>();
		foreach (int value in Enum.GetValues(typeof(GameScenes)))
		{
			if (((uint)scenesToLoad & (uint)value) != 0)
			{
				list.Add((GameScenes)value);
			}
		}
		return list.ToArray();
	}
}
