using System;

namespace Scenes;

[Serializable]
[Flags]
public enum GameScenes
{
	Intro = 1,
	MainMenu = 2,
	MainScene = 4,
	UI = 8,
	Buildings = 0x10,
	ExteriorProps = 0x20,
	LightsAndPostprocessing = 0x40,
	Streets = 0x80,
	Indoor = 0x100,
	TransitionToSave = 0x200,
	ChristmasStreetDecorations = 0x400,
	LowerManhattan = 0x800,
	IndustryCity = 0x1000,
	BlueprintCreator = 0x2000,
	TheHamptons = 0x4000
}
