using System.Collections.Generic;

namespace Entities;

public class AdditionalMessageData
{
	public List<TextMessage.ContextButtonData> contextButtonData;

	public List<string> listOfLabels;

	public Taxes taxes;

	public float backTaxesOwed;
}
