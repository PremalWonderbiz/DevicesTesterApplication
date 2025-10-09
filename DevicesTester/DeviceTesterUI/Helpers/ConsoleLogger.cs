using SchneiderElectric.Automation.Sodb.Common;

namespace DeviceTesterUI.Helpers
{
    public class ConsoleLogger : ISodbLogger
    {
        public void LogInformation(string message)
        {
            Console.WriteLine("Info  : " + message);
        }

        public void LogDebug(string message)
        {
            Console.WriteLine("Debug : " + message);
        }

        public void LogWarning(string message)
        {
            Console.WriteLine("Warn  : " + message);
        }

        public void LogError(string message)
        {
            Console.WriteLine("Error : " + message);
        }

        public void LogTrace(string message)
        {
            Console.WriteLine("Trace : " + message);
        }
    }
}
