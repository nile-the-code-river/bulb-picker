namespace BulbPicker.App.Models
{
    public enum LogType
    {
        // Camera & RobotArm
        Connected,
        // Camera & RobotArm
        Disconnected,

        // Camera Image Composition
        ImageCombined, // TODO: and saved when testing!

        // Robot Arm Communication
        RobotArmPointsSent,
        RobotArmProgramCommandsSent,

        // Robot Arm Offset Settings
        ChangeSaved
    }

    public class Log
    {
        public string Message { get; init; }
        public DateTime LoggedAt { get; set; }
        public LogType Type { get; init; }
        public Log(string message, LogType type)
        {
            Message = message;
            Type = type;
            LoggedAt = DateTime.Now;
        }
    }
}
