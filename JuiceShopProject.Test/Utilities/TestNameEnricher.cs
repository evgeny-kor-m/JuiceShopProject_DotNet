using Serilog.Core;
using Serilog.Events;
using NUnit.Framework;

namespace JuiceShopProject.Test.Utilities
{
    public class TestNameEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            string testName = "NoTest";

            try
            {
                if (TestContext.CurrentContext?.Test?.MethodName != null)
                {
                    testName = TestContext.CurrentContext.Test.MethodName;
                }
            }
            catch
            {
                // Called outside of a test context
            }

            var property = propertyFactory.CreateProperty("TestName", testName);
            logEvent.AddPropertyIfAbsent(property);
        }
    }
}