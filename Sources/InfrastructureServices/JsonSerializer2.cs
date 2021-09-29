using Serilog;

namespace System.Text.Json
{
    public static class JsonSerializer2
    {
        public static T DeserializeRequired<T>(string jsonString, ILogger logger)
        {
            try
            {
                var typedObject = JsonSerializer.Deserialize<T>(jsonString);

                if (typedObject == null)
                    throw new NotSupportedException("Nullable deserialization");

                return typedObject;
            }
            catch (Exception e)
            {
                logger.Error(e, "Error of deserialization {jsonString} to type {type}", jsonString, typeof(T).FullName);
                throw;
            }
        }
    }
}