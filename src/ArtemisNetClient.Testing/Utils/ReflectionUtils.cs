using System.Reflection;

namespace ActiveMQ.Artemis.Client.Testing.Utils;

internal static class ReflectionUtils
{
    public static T? GetPropValue<T>(object obj, string propertyName)
    {
        var value = obj.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj, null);
        if (value == null)
        {
            return default;
        }

        return (T) value;
    }

    public static void SetPropValue<T>(object obj, string propertyName, T propertyValue)
    {
        obj.GetType().GetProperty(propertyName)?.SetValue(obj, propertyValue, BindingFlags.Instance | BindingFlags.NonPublic, null, null, null);
    }
}