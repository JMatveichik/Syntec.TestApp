using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Syntec.TestApp.WPF.ViewModels;

namespace Syntec.TestApp.WPF.Log
{
    public class LogEntry
    {
        public DateTime Timestamp { get; }
        public NotificationType Type { get; }
        public string Source { get; }
        public string Message { get; }

        public string TimestampString => Timestamp.ToString("HH:mm:ss.fff");

        public LogEntry(string message, NotificationType type = NotificationType.Info, string source = "System")
        {
            Timestamp = DateTime.Now;
            Type = type;
            Source = source;
            Message = message;
        }
    }
}