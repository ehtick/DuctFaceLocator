using Autodesk.Revit.DB;
using System;

namespace DuctFaceLocator
{
  class Util
  {

    #region Unit Handling

    private const double _inchToMm = 25.4;
    private const double _footToMm = 12 * _inchToMm;
    private const double _footToMeter = _footToMm * 0.001;
    private const double _sqfToSqm = _footToMeter * _footToMeter;
    private const double _cubicFootToCubicMeter = _footToMeter * _sqfToSqm;

    /// <summary>
    ///     Convert a given length in feet to millimetres.
    /// </summary>
    public static double FootToMm(double length)
    {
      return length * _footToMm;
    }

    #endregion // Unit Handling

    #region Formatting

    /// <summary>
    ///     Return an English plural suffix for the given
    ///     number of items, i.e. 's' for zero or more
    ///     than one, and nothing for exactly one.
    /// </summary>
    public static string PluralSuffix(int n)
    {
      return 1 == n ? "" : "s";
    }

    /// <summary>
    ///     Return an English plural suffix 'ies' or
    ///     'y' for the given number of items.
    /// </summary>
    public static string PluralSuffixY(int n)
    {
      return 1 == n ? "y" : "ies";
    }

    /// <summary>
    ///     Return a dot (full stop) for zero
    ///     or a colon for more than zero.
    /// </summary>
    public static string DotOrColon(int n)
    {
      return 0 < n ? ":" : ".";
    }

    /// <summary>
    ///     Return a string for a real number
    ///     formatted to two decimal places.
    /// </summary>
    public static string RealString(double a)
    {
      return a.ToString("0.##");
    }

    /// <summary>
    ///     Return a string representation in degrees
    ///     for an angle given in radians.
    /// </summary>
    public static string AngleString(double angle)
    {
      return $"{RealString(angle * 180 / Math.PI)} degrees";
    }

    /// <summary>
    ///     Return a string for a length in millimetres
    ///     formatted as an integer value.
    /// </summary>
    public static string MmString(double length)
    {
      //return RealString( FootToMm( length ) ) + " mm";
      return $"{Math.Round(FootToMm(length))} mm";
    }

    /// <summary>
    ///     Return a string for a UV point
    ///     or vector with its coordinates
    ///     formatted to two decimal places.
    /// </summary>
    public static string PointString(
        UV p,
        bool onlySpaceSeparator = false)
    {
      var format_string = onlySpaceSeparator
          ? "{0} {1}"
          : "({0},{1})";

      return string.Format(format_string,
          RealString(p.U),
          RealString(p.V));
    }

    /// <summary>
    ///     Return a string for an XYZ point
    ///     or vector with its coordinates
    ///     formatted to two decimal places.
    /// </summary>
    public static string PointString(
        XYZ p,
        bool onlySpaceSeparator = false)
    {
      var format_string = onlySpaceSeparator
          ? "{0} {1} {2}"
          : "({0},{1},{2})";

      return string.Format(format_string,
          RealString(p.X),
          RealString(p.Y),
          RealString(p.Z));
    }

    #endregion // Formatting
  }
}
