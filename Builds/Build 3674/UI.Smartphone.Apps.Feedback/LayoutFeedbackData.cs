using System.Text;
using Buildings.Indoors.InteriorDesign;
using BusinessLayoutSets;
using UI.InteriorDesigner;
using UnityEngine;

namespace UI.Smartphone.Apps.Feedback;

public class LayoutFeedbackData : IFeedbackData
{
	private const string LayoutFileName = "layout.json";

	private const string LayoutFieldName = "layout";

	private const string MimeType = "application/json";

	private byte[] _layoutBytes;

	public void AddToForm(ref WWWForm formData)
	{
		if (_layoutBytes != null && _layoutBytes.Length != 0)
		{
			formData.AddBinaryData("layout", _layoutBytes, "layout.json", "application/json");
		}
	}

	public void GatherData()
	{
		if (InteriorDesignerHelper.BlueprintCreatorMode && InteriorDesignerUI.IsOpen)
		{
			string s = JsonUtility.ToJson(BusinessLayoutSetHelper.Collect(collectDirtSpots: false));
			_layoutBytes = Encoding.UTF8.GetBytes(s);
		}
	}

	public void GatherDataDelayed()
	{
	}
}
