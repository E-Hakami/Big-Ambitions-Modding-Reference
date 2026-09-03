using UI.Elements;
using UnityEngine;

namespace UI.Overlays;

public interface IOverlay
{
	Vector3 GetTargetPosition();

	LabelInfo GetFirstLineLabel();

	LabelInfo GetSecondLineLeftLabel();

	LabelInfo GetSecondLineRightLabel();

	ButtonInfo[] GetButtons();
}
