using BigAmbitions.Characters.Appearance;

public class ToiletController : HygieneItemController
{
	private static readonly AppearanceElementType[] NakedElementTypes = new AppearanceElementType[2]
	{
		AppearanceElementType.Legs,
		AppearanceElementType.LegsAccessory
	};

	protected override void OnBeginUse(ThirdPersonCharacter tpc)
	{
		base.OnBeginUse(tpc);
		tpc.appearanceSetter.SetPixelatedPlaneActive(active: true);
		AppearanceElementType[] nakedElementTypes = NakedElementTypes;
		foreach (AppearanceElementType nakedElement in nakedElementTypes)
		{
			tpc.appearanceSetter.SetNakedElement(nakedElement);
		}
		Employee component = tpc.GetComponent<Employee>();
		if ((bool)component && (bool)component.employeeStationController && component.employeeStationController.employeeInstance == component.employeeInstance)
		{
			component.employeeStationController.SetEmployeeAppearance(tpc, NakedElementTypes);
		}
		else
		{
			tpc.appearanceSetter.UpdateVisuals();
		}
	}

	protected override void OnEndUse(ThirdPersonCharacter tpc)
	{
		base.OnEndUse(tpc);
		tpc.appearanceSetter.SetPixelatedPlaneActive(active: false);
	}
}
