namespace GeometryKit.Core;

public readonly record struct Ray3d(Point3d Origin, Point3d Direction)
{
    public Point3d At(double parameter) => Origin + Direction * parameter;
}
