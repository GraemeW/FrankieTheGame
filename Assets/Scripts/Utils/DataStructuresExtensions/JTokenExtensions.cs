using Newtonsoft.Json.Linq;

namespace Frankie.Utils
{
    public static class JTokenExtensions
    {

        public static bool TryToObject<T>(this JToken token, out T value)
        {
            // Note:  Null entries return false, which is expected behaviour for this save system
            // i.e. we do not allow saving null state, it is effectively ignored (defaults used via RestoreState)
            if (token is null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
            {
                value = default;
                return false;
            }
            
            if (token is JValue jValue)
            {
                // Exact match:  no conversion needed
                if (jValue.Value is T exact)
                {
                    value = exact;
                    return true;
                }

                try
                {
                    // Scalar conversion:  numeric widening/narrowing, string<->enum, Guid,  etc.
                    value = jValue.Value<T>();
                    return value != null;
                }
                catch
                {
                    value = default;
                    return false;
                }
            }

            // Slow path:  objects/arrays use the full serializer
            try
            {
                value = token.ToObject<T>();
                return value != null;
            }
            catch
            {
                value = default;
                return false;
            }
        }
    }
}
