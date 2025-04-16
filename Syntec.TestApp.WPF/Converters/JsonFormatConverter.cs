using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Windows.Data;
using Syntec.TestApp.WPF.ViewModels;

namespace Syntec.TestApp.WPF.Converters
{
    public class JsonFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "null";

            try
            {
                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    Converters = new JsonConverter[] { new ParameterJsonConverter() },
                    NullValueHandling = NullValueHandling.Include,
                    DefaultValueHandling = DefaultValueHandling.Include
                };

                return JsonConvert.SerializeObject(value, settings);
            }
            catch (Exception ex)
            {
                return $"Ошибка форматирования JSON: {ex.Message}";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Кастомный конвертер для типа Parameter
    public class ParameterJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Parameter);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var parameter = (Parameter)value;
            var obj = new JObject
            {
                ["Name"] = parameter.Name,
                ["Type"] = parameter.Type?.FullName,
                ["Value"] = parameter.Value != null ? JToken.FromObject(parameter.Value) : null,
                ["Description"] = parameter.Description
            };
            obj.WriteTo(writer);
        }
    }
}