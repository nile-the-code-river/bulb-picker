using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulbPicker.App.Models
{
    enum LogLevel
    {
        Error,
        BulbDetected,
        RobotArmOn,
        RobotArmOff,
        RobotArmRunning,
    }

    class LogMessage
    {
        public string Message { get; init; }
        public LogLevel Level { get; init; }
        public LogMessage(string message, LogLevel level)
        {
            Message = message;
            Level = level;
        }
    }
}
