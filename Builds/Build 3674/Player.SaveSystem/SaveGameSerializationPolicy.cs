using System;
using System.Reflection;
using OdinSerializer;
using OdinSerializer.Utilities;

namespace Player.SaveSystem;

public class SaveGameSerializationPolicy : ISerializationPolicy
{
	public string ID => "BigAmbitionsSaveGame";

	public bool AllowNonSerializableTypes => true;

	public bool ShouldSerializeMember(MemberInfo member)
	{
		if (member.IsDefined<ObsoleteAttribute>(inherit: true))
		{
			return false;
		}
		return SerializationPolicies.Unity.ShouldSerializeMember(member);
	}
}
