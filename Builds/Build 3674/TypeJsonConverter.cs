using System;
using BusinessLayoutSets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public sealed class TypeJsonConverter<T> : JsonConverter
{
	private const string TypePropertyName = "type";

	public override bool CanConvert(Type objectType)
	{
		return typeof(T).IsAssignableFrom(objectType);
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value == null)
		{
			writer.WriteNull();
			return;
		}
		JsonSerializer jsonSerializer = CreateSerializerWithoutThisConverter(serializer);
		JObject jObject = JObject.FromObject(value, jsonSerializer);
		jObject["type"] = value.GetType().AssemblyQualifiedName;
		jObject.WriteTo(writer);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
		{
			return null;
		}
		JObject jObject = JObject.Load(reader);
		JToken jToken = jObject["type"];
		Type type = null;
		if (jToken != null && jToken.Type == JTokenType.String)
		{
			type = Type.GetType(jToken.Value<string>());
		}
		if (type == null || !typeof(Item).IsAssignableFrom(type))
		{
			type = typeof(Item);
		}
		Item item = (Item)Activator.CreateInstance(type);
		serializer.Populate(jObject.CreateReader(), item);
		return item;
	}

	private static JsonSerializer CreateSerializerWithoutThisConverter(JsonSerializer serializer)
	{
		JsonSerializer jsonSerializer = new JsonSerializer
		{
			Context = serializer.Context,
			Culture = serializer.Culture,
			ContractResolver = serializer.ContractResolver,
			ConstructorHandling = serializer.ConstructorHandling,
			CheckAdditionalContent = serializer.CheckAdditionalContent,
			DateFormatHandling = serializer.DateFormatHandling,
			DateFormatString = serializer.DateFormatString,
			DateParseHandling = serializer.DateParseHandling,
			DateTimeZoneHandling = serializer.DateTimeZoneHandling,
			DefaultValueHandling = serializer.DefaultValueHandling,
			EqualityComparer = serializer.EqualityComparer,
			FloatFormatHandling = serializer.FloatFormatHandling,
			FloatParseHandling = serializer.FloatParseHandling,
			Formatting = serializer.Formatting,
			MaxDepth = serializer.MaxDepth,
			MetadataPropertyHandling = serializer.MetadataPropertyHandling,
			MissingMemberHandling = serializer.MissingMemberHandling,
			NullValueHandling = serializer.NullValueHandling,
			ObjectCreationHandling = serializer.ObjectCreationHandling,
			PreserveReferencesHandling = serializer.PreserveReferencesHandling,
			ReferenceLoopHandling = serializer.ReferenceLoopHandling,
			StringEscapeHandling = serializer.StringEscapeHandling,
			TraceWriter = serializer.TraceWriter,
			TypeNameAssemblyFormatHandling = serializer.TypeNameAssemblyFormatHandling,
			TypeNameHandling = serializer.TypeNameHandling
		};
		for (int i = 0; i < serializer.Converters.Count; i++)
		{
			JsonConverter jsonConverter = serializer.Converters[i];
			if (!(jsonConverter is TypeJsonConverter<T>))
			{
				jsonSerializer.Converters.Add(jsonConverter);
			}
		}
		return jsonSerializer;
	}
}
