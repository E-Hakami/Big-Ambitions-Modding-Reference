using CameraControllers;

namespace Buildings.Indoors.InteriorDesign;

public static class InteriorDesignerHelper
{
	public static TimeOfDayController TimeOfDayController { get; private set; }

	public static PlacementCam PlacementCam { get; private set; }

	public static bool BlueprintCreatorMode { get; set; }

	public static void Init(TimeOfDayController timeOfDayController, PlacementCam placementCam, bool blueprintCreatorMode)
	{
		PlacementCam = placementCam;
		TimeOfDayController = timeOfDayController;
		BlueprintCreatorMode = blueprintCreatorMode;
		if (blueprintCreatorMode)
		{
			timeOfDayController.OnEnterBlueprintCreator();
		}
	}
}
