namespace Buildings.Indoors;

public class IndoorSpotlightController : IndoorLightController
{
	protected override void OnLightsStatusChanged(bool _)
	{
		base.OnLightsStatusChanged(lightsOn: true);
	}
}
