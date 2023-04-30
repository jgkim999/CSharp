using Serilog.Core;
using Serilog.Events;

namespace Example1.Serilog
{
    public class CustomPolicy : IDestructuringPolicy
    {
        public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
        {
            result = null;

            if (value is LoginData loginData)
            {
                result = new StructureValue(
                    new List<LogEventProperty>
                    {
                        new("Username", new ScalarValue(loginData.Username))
                    });
            }

            return (result != null);
        }
    }
}
