using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ChessArticle
{
    /// <summary>
    /// Drop-in replacement for the .NET Framework
    /// <c>System.Web.Script.Serialization.JavaScriptSerializer</c>, built on
    /// <c>System.Text.Json</c>.
    ///
    /// <c>DeserializeObject</c> returns the same loosely typed shape the
    /// original produced — <c>Dictionary&lt;string, object&gt;</c> for objects,
    /// <c>object[]</c> for arrays, and boxed primitives for the rest — so the
    /// calling code did not have to change.
    /// </summary>
    public sealed class JavaScriptSerializer
    {
        /// <summary>Kept for source compatibility; System.Text.Json has no such limit.</summary>
        public int MaxJsonLength { get; set; }

        public string Serialize(object value)
        {
            return JsonSerializer.Serialize(value);
        }

        public object DeserializeObject(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            using (JsonDocument doc = JsonDocument.Parse(json,
                       new JsonDocumentOptions { AllowTrailingCommas = true }))
            {
                return ToClr(doc.RootElement);
            }
        }

        private static object ToClr(JsonElement el)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Object:
                    var map = new Dictionary<string, object>(StringComparer.Ordinal);
                    foreach (JsonProperty p in el.EnumerateObject())
                        map[p.Name] = ToClr(p.Value);
                    return map;

                case JsonValueKind.Array:
                    var items = new List<object>();
                    foreach (JsonElement item in el.EnumerateArray())
                        items.Add(ToClr(item));
                    return items.ToArray();

                case JsonValueKind.String:
                    return el.GetString();

                case JsonValueKind.Number:
                    int i;
                    if (el.TryGetInt32(out i)) return i;
                    long l;
                    if (el.TryGetInt64(out l)) return l;
                    return el.GetDouble();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                default:
                    return null;
            }
        }
    }
}
