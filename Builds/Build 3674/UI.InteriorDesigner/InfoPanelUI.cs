using UnityEngine;

namespace UI.InteriorDesigner;

public abstract class InfoPanelUI : MonoBehaviour
{
	public abstract bool ShouldShow();

	public abstract void OnEnterInteriorDesignerMode();

	public abstract void OnExitInteriorDesignerMode();
}
