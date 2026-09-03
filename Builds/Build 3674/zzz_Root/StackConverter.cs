using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

public class StackConverter : JsonConverter
{
	public override bool CanWrite => false;

	private static Type StackParameterType(Type objectType)
	{
		while (objectType != null)
		{
			if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Stack<>))
			{
				return objectType.GetGenericArguments()[0];
			}
			objectType = objectType.BaseType;
		}
		return null;
	}

	public override bool CanConvert(Type objectType)
	{
		return StackParameterType(objectType) != null;
	}

	private object ReadJsonGeneric<T>(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
		{
			return null;
		}
		List<T> list = serializer.Deserialize<List<T>>(reader);
		Stack<T> stack = (existingValue as Stack<T>) ?? ((Stack<T>)serializer.ContractResolver.ResolveContract(objectType).DefaultCreator());
		for (int num = list.Count - 1; num >= 0; num--)
		{
			stack.Push(list[num]);
		}
		return stack;
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
		{
			return null;
		}
		try
		{
			Type type = StackParameterType(objectType);
			return GetType().GetMethod("ReadJsonGeneric", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).MakeGenericMethod(type).Invoke(this, new object[4] { reader, objectType, existingValue, serializer });
		}
		catch (TargetInvocationException innerException)
		{
			throw new JsonSerializationException("Failed to deserialize " + objectType, innerException);
		}
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}
}
