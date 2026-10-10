namespace GeometryKit.Core;

public readonly record struct Plane3d(Point3d Origin, Point3d Normal)
{
    public Plane3d Normalized() => new(Origin, Normal.Normalized());
    public double SignedDistance(Point3d point) => Point3d.Dot(point - Origin, Normal);
}
