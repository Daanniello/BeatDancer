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
    public class Test
    {
        public Test()
        {

        }

        public Point RotateSaber(Point handPosition, double SaberLength, Quaternion quaternion)
        {
            var tipOffset = new Point { x = 0, y = 0, z = SaberLength };
            tipOffset = Rotate(tipOffset, quaternion);
            return AddPoints(tipOffset, handPosition);
        }

        public Point Rotate(Point point, Quaternion q)
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

        public double Magnitude(Quaternion q)
        {
            return Math.Sqrt(Math.Pow(q.w, 2) + Math.Pow(q.x, 2) + Math.Pow(q.y, 2) + Math.Pow(q.z, 2));
        }

        public Vector3D GetVector(Quaternion q)
        {
            return new Vector3D() { X = q.x, Y = q.y, Z = q.z };

        }

        public Vector3 ScaleVector(Vector3 vector, double factor)
        {
            return new Vector3()
            {
                X = (float)(vector.X * factor),
                Y = (float)(vector.Y * factor),
                Z = (float)(vector.Z * factor)
            };
        }

        public Vector3D AddVector(Vector3D vector, Vector3D vector2)
        {
            return new Vector3D()
            {
                X = (float)(vector.X + vector2.X),
                Y = (float)(vector.Y + vector2.Y),
                Z = (float)(vector.Z + vector2.Z)
            };
        }

        public Vector3 CrossProduct(Vector3 v1, Vector3 v2)
        {
            double x, y, z;
            x = v1.Y * v2.Z - v2.Y * v1.Z;
            y = (v1.X * v2.Z - v2.X * v1.Z) * -1;
            z = v1.X * v2.Y - v2.X * v1.Y;

            var rtnvector = new Vector3() { X = (float)x, Y = (float)y, Z = (float)z };
            return rtnvector;
        }

        public Quaternion Multiply(Quaternion q1, Quaternion q2)
        {
            
            var tempVector = AddVector(AddVector(Vector3D.Multiply(GetVector(q2), q1.w), Vector3D.Multiply(GetVector(q1), q2.w)), Vector3D.CrossProduct(GetVector(q1), GetVector(q2)));
            return new Quaternion { w = q1.w * q2.w - Vector3D.DotProduct(GetVector(q1), GetVector(q2)), x = tempVector.X, y = tempVector.Y, z = tempVector.Z };
        }

        public Quaternion Unit(Quaternion q)
        {
            var factor = 1 / Magnitude(q);
            return new Quaternion { w = q.w * factor, x = q.x * factor, y = q.y * factor, z = q.z * factor };
        }

        public Quaternion PureImaginaryQuaternion(Point point)
        {
            return new Quaternion { w = 0, x = point.x, y = point.y, z = point.z };
        }

        public Quaternion Conjugate(Quaternion q)
        {
            return new Quaternion { w = q.w, x = -q.x, y = -q.y, z = -q.z };
        }

        public Point AddPoints(Point p1, Point p2)
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
    }
}
