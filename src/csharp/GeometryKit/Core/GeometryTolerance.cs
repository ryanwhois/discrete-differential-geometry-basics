namespace GeometryKit.Core;

/// <summary>
/// Numerical contract for a single operation. Values are in the model's native units.
/// The default deliberately avoids a global, unitless modelling tolerance.
/// </summary>
public readonly record struct GeometryTolerance(double Distance, double Relative)
{
    public static GeometryTolerance Default => new(1e-9, 1e-12);

    public double ForScale(double scale) => Math.Max(Distance, Relative * Math.Max(1.0, scale));
    public bool IsZero(double value, double scale = 1.0) => Math.Abs(value) <= ForScale(scale);
}
