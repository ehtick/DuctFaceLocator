using Autodesk.Revit.DB;
using System.Diagnostics;

namespace DuctFaceLocator
{
  /// <summary>
  /// Round or rectangular cross section dimensions
  /// </summary>
  internal class XsecDimMm
  {
    public ConnectorProfileType Shape;
    public int Width;
    public int Height;

    public int Radius
    {
      get
      {
        Debug.Assert(!IsRect, "expected round cross section");
        return Width;
      }
    }

    /// <summary>
    /// Predicate to check whether rectangular cross section
    /// </summary>
    public bool IsRect
    {
      get
      {
        return ConnectorProfileType.Rectangular == Shape;
      }
    }

    /// <summary>
    /// Constructor from Revit MEP API `Connector` instance
    /// </summary>
    public XsecDimMm(Connector con)
    {
      Shape = con.Shape;
      if (IsRect)
      {
        Width = Util.FootToMmInt(con.Width);
        Height = Util.FootToMmInt(con.Height);
      }
      else if (ConnectorProfileType.Round == Shape)
      {
        Width = Util.FootToMmInt(con.Radius);
      }
      else
      {
        Debug.Assert(false, "expected rectangular or round cross section");
      }
    }

    public override string ToString()
    {
      return IsRect
        ? $"rectangular {Width}x{Height}"
        : $"round {Radius}";
    }
  }
}
