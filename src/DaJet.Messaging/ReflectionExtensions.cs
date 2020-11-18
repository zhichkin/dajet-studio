using System.Collections.Generic;
using System.Reflection;

namespace DaJet.Messaging
{
    public static class ReflectionExtensions
    {
        public static bool IsList(this PropertyInfo @this)
        {
            return @this.PropertyType.IsGenericType
                && @this.PropertyType.GetGenericTypeDefinition() == typeof(List<>);
        }
    }
}