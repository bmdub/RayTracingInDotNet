using System;
using System.Numerics;

namespace RayTracingInDotNet
{
	static class Matrix4x4Extensions
    {
        public static Quaternion ToQuaternion(this Matrix4x4 mat)
        {
            Quaternion q = new Quaternion();
            q.W = MathF.Sqrt(MathF.Max(0, 1 + mat.M11 + mat.M22 + mat.M33)) / 2;
            q.X = MathF.Sqrt(MathF.Max(0, 1 + mat.M11 - mat.M22 - mat.M33)) / 2;
            q.Y = MathF.Sqrt(MathF.Max(0, 1 - mat.M11 + mat.M22 - mat.M33)) / 2;
            q.Z = MathF.Sqrt(MathF.Max(0, 1 - mat.M11 - mat.M22 + mat.M33)) / 2;
            q.X *= -MathF.Sign(q.X * (mat.M32 - mat.M23));
            q.Y *= -MathF.Sign(q.Y * (mat.M13 - mat.M31));
            q.Z *= -MathF.Sign(q.Z * (mat.M21 - mat.M12));
            return q;
        }

        public static Vector4 Multiply(this in Matrix4x4 mat, in Vector4 vec)
        {
            return new Vector4(
                (mat.M11 * vec.X) + (mat.M12 * vec.Y) + (mat.M13 * vec.Z) + (mat.M14 * vec.W),
                (mat.M21 * vec.X) + (mat.M22 * vec.Y) + (mat.M23 * vec.Z) + (mat.M24 * vec.W),
                (mat.M31 * vec.X) + (mat.M32 * vec.Y) + (mat.M33 * vec.Z) + (mat.M34 * vec.W),
                (mat.M41 * vec.X) + (mat.M42 * vec.Y) + (mat.M43 * vec.Z) + (mat.M44 * vec.W));
        }

        public static Vector4 MultiplyColumnOriented(this in Matrix4x4 mat, in Vector4 vec)
        {
            return new Vector4(
                (mat.M11 * vec.X) + (mat.M21 * vec.Y) + (mat.M31 * vec.Z) + (mat.M41 * vec.W),
                (mat.M12 * vec.X) + (mat.M22 * vec.Y) + (mat.M32 * vec.Z) + (mat.M42 * vec.W),
                (mat.M13 * vec.X) + (mat.M23 * vec.Y) + (mat.M33 * vec.Z) + (mat.M43 * vec.W),
                (mat.M14 * vec.X) + (mat.M24 * vec.Y) + (mat.M34 * vec.Z) + (mat.M44 * vec.W));
        }

        public static Matrix4x4 SetTranslation(this Matrix4x4 mat, in Vector3 vec) =>
            new Matrix4x4(
                mat.M11, mat.M12, mat.M13, mat.M14,
                mat.M21, mat.M22, mat.M23, mat.M24,
                mat.M31, mat.M32, mat.M33, mat.M34,
                vec.X, vec.Y, vec.Z, mat.M44);

        public static Matrix4x4 MoveBy(this Matrix4x4 mat, in Vector3 vec) =>
            SetTranslation(mat, mat.Translation + vec);

        public static Matrix4x4 RotateBy(this Matrix4x4 mat, in Vector3 rot)
        {
            var tv = mat.Translation;
            mat = mat.SetTranslation(Vector3.Zero);
            mat *= Matrix4x4.CreateRotationX(rot.X);
            mat *= Matrix4x4.CreateRotationY(rot.Y);
            mat *= Matrix4x4.CreateRotationZ(rot.Z);
            mat = mat.SetTranslation(tv);
            return mat;
        }

        public static Matrix4x4 RotateBy(this Matrix4x4 mat, in Vector3 rot, in Vector3 origin)
        {
            mat = mat.SetTranslation(mat.Translation - origin);
            mat *= Matrix4x4.CreateRotationX(rot.X);
            mat *= Matrix4x4.CreateRotationY(rot.Y);
            mat *= Matrix4x4.CreateRotationZ(rot.Z);
            mat = mat.SetTranslation(mat.Translation + origin);
            return mat;
        }
    }
}
