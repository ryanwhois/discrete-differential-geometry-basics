namespace GeometryKit.Core;

/// <summary>Double-precision point/vector value used by GeometryKit's public API.</summary>
public readonly record struct Point3d(double X, double Y, double Z)
{
    public static Point3d Zero => new(0, 0, 0);

    public static Point3d operator +(Point3d a, Point3d b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Point3d operator -(Point3d a, Point3d b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Point3d operator -(Point3d value) => new(-value.X, -value.Y, -value.Z);
    public static Point3d operator *(Point3d value, double scalar) => new(value.X * scalar, value.Y * scalar, value.Z * scalar);
    public static Point3d operator *(double scalar, Point3d value) => value * scalar;
    public static Point3d operator /(Point3d value, double scalar) => new(value.X / scalar, value.Y / scalar, value.Z / scalar);

    public double LengthSquared => X * X + Y * Y + Z * Z;
    public double Length => Math.Sqrt(LengthSquared);

    public Point3d Normalized()
    {
        var length = Length;
        return length == 0 ? Zero : this / length;
    }

    public static double Dot(Point3d a, Point3d b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    public static Point3d Cross(Point3d a, Point3d b) => new(
        a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X);
    public static double DistanceSquared(Point3d a, Point3d b) => (a - b).LengthSquared;
    public static double Distance(Point3d a, Point3d b) => Math.Sqrt(DistanceSquared(a, b));
}
