using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulbPicker.App.Models
{
    public class Bulb
    {
    }

    public class BulbPickUpPoint
    {
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Z { get; private set; }
        public RobotArmPosition CorrespondingRobotArm { get; private set; }

        public void SetX(float x) => X = x;
        public void SetY(float y) => Y = y;
        public void SetZ(float z) => Z = z;
        public void SetCorrespondingRobotArm(RobotArmPosition position) => CorrespondingRobotArm = position;
    }

    public class BulbBoundingBox
    {
        public float X1 { get; init; }
        public float Y1 { get; init; }
        public float X2 { get; init; }
        public float Y2 { get; init; }
        public float XCenter { get; init; }
        public float YCenter { get; init; }
        public BulbBoundingBox(float x1, float y1, float x2, float y2, float xCenter, float yCenter)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
            XCenter = xCenter;
            YCenter = yCenter;
        }
    }
}
