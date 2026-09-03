using System;

namespace Blueprints;

[Serializable]
public class BlueprintDataElement
{
	public DataElement dataElement;

	public string value;

	public BlueprintDataElement(DataElement dataElement, string value)
	{
		this.dataElement = dataElement;
		this.value = value;
	}
}
