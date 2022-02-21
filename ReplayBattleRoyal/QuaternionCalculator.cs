using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace ReplayBattleRoyal
{
    public class QuaternionCalculator
    {
        public QuaternionCalculator()
        {

        }

        public static Point RotateSaber(Point handPosition, double SaberLength, Quaternion quaternion)
        {
            var tipOffset = new Point { x = 0, y = 0, z = SaberLength };
            tipOffset = Rotate(tipOffset, quaternion);
            return AddPoints(tipOffset, handPosition);
        }

        public static Point Rotate(Point point, Quaternion q)
        {
            if (Math.Round(Magnitude(q), 4) != 0)
            {
                q = Unit(q);
            }

            var qImaginary = PureImaginaryQuaternion(point);
            var qConjugate = Conjugate(q);

            var q2 = Multiply(Multiply(q, qImaginary), qConjugate);
            return new Point { x = q2.x, y = q2.y, z = q2.z };

        }

        public static double Magnitude(Quaternion q)
        {
            return Math.Sqrt(Math.Pow(q.w, 2) + Math.Pow(q.x, 2) + Math.Pow(q.y, 2) + Math.Pow(q.z, 2));
        }

        public static Vector3D GetVector(Quaternion q)
        {
            return new Vector3D() { X = q.x, Y = q.y, Z = q.z };

        }

        public static Vector3 ScaleVector(Vector3 vector, double factor)
        {
            return new Vector3()
            {
                X = (float)(vector.X * factor),
                Y = (float)(vector.Y * factor),
                Z = (float)(vector.Z * factor)
            };
        }

        public static Vector3D AddVector(Vector3D vector, Vector3D vector2)
        {
            return new Vector3D()
            {
                X = (float)(vector.X + vector2.X),
                Y = (float)(vector.Y + vector2.Y),
                Z = (float)(vector.Z + vector2.Z)
            };
        }

        public static Quaternion Multiply(Quaternion q1, Quaternion q2)
        {
            
            var tempVector = AddVector(AddVector(Vector3D.Multiply(GetVector(q2), q1.w), Vector3D.Multiply(GetVector(q1), q2.w)), Vector3D.CrossProduct(GetVector(q1), GetVector(q2)));
            return new Quaternion { w = q1.w * q2.w - Vector3D.DotProduct(GetVector(q1), GetVector(q2)), x = tempVector.X, y = tempVector.Y, z = tempVector.Z };
        }

        public static Quaternion Unit(Quaternion q)
        {
            var factor = 1 / Magnitude(q);
            return new Quaternion { w = q.w * factor, x = q.x * factor, y = q.y * factor, z = q.z * factor };
        }

        public static Quaternion PureImaginaryQuaternion(Point point)
        {
            return new Quaternion { w = 0, x = point.x, y = point.y, z = point.z };
        }

        public static Quaternion Conjugate(Quaternion q)
        {
            return new Quaternion { w = q.w, x = -q.x, y = -q.y, z = -q.z };
        }

        public static Point AddPoints(Point p1, Point p2)
        {
            return new Point
            {
                x = p1.x + p2.x,
                y = p1.y + p2.y,
                z = p1.z + p2.z,
            };
        }

        public class Point
        {
            public double x { get; set; }
            public double y { get; set; }
            public double z { get; set; }
        }

        public class Quaternion
        {
            public double x { get; set; }
            public double y { get; set; }
            public double z { get; set; }
            public double w { get; set; }
        }

        public class EulerAngles
        {
            public double roll; // x
            public double pitch; // y
            public double yaw; // z
        }

        public static EulerAngles ToEulerAngles(Quaternion q)
        {
            EulerAngles angles = new();

            // roll (x-axis rotation)
            double sinr_cosp = 2 * (q.w * q.x + q.y * q.z);
            double cosr_cosp = 1 - 2 * (q.x * q.x + q.y * q.y);
            angles.roll = Math.Atan2(sinr_cosp, cosr_cosp);

            // pitch (y-axis rotation)
            double sinp = 2 * (q.w * q.y - q.z * q.x);
            if (Math.Abs(sinp) >= 1)
            {
                angles.pitch = Math.CopySign(Math.PI / 2, sinp);
            }
            else
            {
                angles.pitch = Math.Asin(sinp);
            }

            // yaw (z-axis rotation)
            double siny_cosp = 2 * (q.w * q.z + q.x * q.y);
            double cosy_cosp = 1 - 2 * (q.y * q.y + q.z * q.z);
            angles.yaw = Math.Atan2(siny_cosp, cosy_cosp);

            return angles;
        }
    }
}
