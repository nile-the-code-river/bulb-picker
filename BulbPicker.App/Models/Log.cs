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

        // Robot Arm Commands & Data Sending
        RobotArmPointsSent,
        RobotArmCommunication,

        // Robot Arm Offset Settings
        SettingFileUpdated,

        FOR_TEST
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
