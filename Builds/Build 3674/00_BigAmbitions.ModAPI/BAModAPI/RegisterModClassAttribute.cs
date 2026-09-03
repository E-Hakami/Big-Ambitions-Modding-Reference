// BigAmbitions.ModAPI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BAModAPI.RegisterModClassAttribute
using System;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class RegisterModClassAttribute : Attribute
{
	public Type ModClassType { get; }

	public RegisterModClassAttribute(Type modClassType)
	{
		ModClassType = modClassType;
	}
}
