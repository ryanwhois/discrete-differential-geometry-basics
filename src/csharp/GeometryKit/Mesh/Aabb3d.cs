using GeometryKit.Core;

namespace GeometryKit.Mesh;

public readonly record struct Aabb3d(Point3d Min, Point3d Max)
{
    public static Aabb3d Empty => new(
        new(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity),
        new(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity));

    public static Aabb3d FromPoint(Point3d point) => new(point, point);
    public Point3d Centre => (Min + Max) * 0.5;
    public Point3d Extents => Max - Min;
    public bool IsEmpty => Min.X > Max.X || Min.Y > Max.Y || Min.Z > Max.Z;

    public Aabb3d Encapsulate(Point3d point) => new(
        new(Math.Min(Min.X, point.X), Math.Min(Min.Y, point.Y), Math.Min(Min.Z, point.Z)),
        new(Math.Max(Max.X, point.X), Math.Max(Max.Y, point.Y), Math.Max(Max.Z, point.Z)));

    public Aabb3d Encapsulate(Aabb3d other) => IsEmpty ? other : other.IsEmpty ? this :
        new Aabb3d(
            new(Math.Min(Min.X, other.Min.X), Math.Min(Min.Y, other.Min.Y), Math.Min(Min.Z, other.Min.Z)),
            new(Math.Max(Max.X, other.Max.X), Math.Max(Max.Y, other.Max.Y), Math.Max(Max.Z, other.Max.Z)));

    public double DistanceSquared(Point3d point)
    {
        static double AxisDistance(double value, double min, double max) => value < min ? min - value : value > max ? value - max : 0;
        var x = AxisDistance(point.X, Min.X, Max.X);
        var y = AxisDistance(point.Y, Min.Y, Max.Y);
        var z = AxisDistance(point.Z, Min.Z, Max.Z);
        return x * x + y * y + z * z;
    }

    public bool Intersects(Ray3d ray, double maximumParameter)
    {
        var min = 0.0;
        var max = maximumParameter;
        return IntersectsAxis(ray.Origin.X, ray.Direction.X, Min.X, Max.X, ref min, ref max)
            && IntersectsAxis(ray.Origin.Y, ray.Direction.Y, Min.Y, Max.Y, ref min, ref max)
            && IntersectsAxis(ray.Origin.Z, ray.Direction.Z, Min.Z, Max.Z, ref min, ref max);
    }

    private static bool IntersectsAxis(double origin, double direction, double minBound, double maxBound, ref double minimum, ref double maximum)
    {
        if (Math.Abs(direction) < 1e-15) return origin >= minBound && origin <= maxBound;
        var inverse = 1.0 / direction;
        var t0 = (minBound - origin) * inverse;
        var t1 = (maxBound - origin) * inverse;
        if (t0 > t1) (t0, t1) = (t1, t0);
        minimum = Math.Max(minimum, t0);
        maximum = Math.Min(maximum, t1);
        return minimum <= maximum;
    }
}
