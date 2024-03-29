#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

#endregion

namespace DuctFaceLocator
{
  [Transaction(TransactionMode.Manual)]
  public class Command : IExternalCommand
  {
    /// <summary>
    /// Get the connected connector of one connector
    /// </summary>
    /// <param name="connector">The connector to be analysed</param>
    /// <returns>The connected connector</returns>
    static Connector GetConnectedConnector(Connector connector)
    {
      Connector connectedConnector = null;
      ConnectorSet allRefs = connector.AllRefs;
      foreach (Connector c in allRefs)
      {
        // Ignore non-EndConn connectors and connectors of the current element
        if (c.ConnectorType != ConnectorType.End 
          || c.Owner.Id == connector.Owner.Id)
        {
          continue;
        }
        connectedConnector = c;
        break;
      }
      return connectedConnector;
    }

    /// <summary>
    /// Return the primary connector 
    /// from the given connector set.
    /// </summary>
    Connector GetPrimaryConnector(ConnectorSet conset)
    {
      Connector primary_connector = null;
      foreach (Connector c in conset)
      {
        MEPConnectorInfo info = c.GetMEPConnectorInfo();
        if (info.IsPrimary)
        {
          primary_connector = c;
          break;
        }
      }
      return primary_connector;
    }

    /// <summary>
    /// Return the secondary connector 
    /// from the given connector set.
    /// </summary>
    Connector GetSecondaryConnector(ConnectorSet conset)
    {
      Connector secondary_connector = null;
      foreach (Connector c in conset)
      {
        MEPConnectorInfo info = c.GetMEPConnectorInfo();
        if (info.IsSecondary)
        {
          secondary_connector = c;
          break;
        }
      }
      return secondary_connector;
    }

    /// <summary>
    /// Return the first non-primary and non-secondary 
    /// connector from the given connector manager.
    /// </summary>
    Connector GetFirstNonPrimaryOrSecondaryConnector(
      ConnectorSet conset)
    {
      Connector connector = null;
      foreach (Connector c in conset)
      {
        MEPConnectorInfo info = c.GetMEPConnectorInfo();
        if (!info.IsPrimary && !info.IsSecondary)
        {
          connector = c;
          break;
        }
      }
      return connector;
    }

    /// <summary>
    /// Return the primary connector 
    /// from the given connector manager.
    /// </summary>
    Connector GetPrimaryConnector(ConnectorManager cm)
    {
      return GetPrimaryConnector(cm.Connectors);
    }

    /// <summary>
    /// Return the secondary connector 
    /// from the given connector manager.
    /// </summary>
    Connector GetSecondaryConnector(ConnectorManager cm)
    {
      return GetSecondaryConnector(cm.Connectors);
    }

    /// <summary>
    /// Return the first non-primary and non-secondary 
    /// connector from the given connector manager.
    /// </summary>
    Connector GetFirstNonPrimaryOrSecondaryConnector(ConnectorManager cm)
    {
      return GetFirstNonPrimaryOrSecondaryConnector(cm.Connectors);
    }

    void f( Document doc, List<ElementId> ids, double dToEnd, double dSpacing )
    {
      foreach (ElementId id in ids)
      {
        FabricationPart part = doc.GetElement(id) as FabricationPart;
        ConnectorManager cm = part.ConnectorManager;
        Connector start = GetPrimaryConnector(cm);
        Connector end = GetSecondaryConnector(cm);
        if (null != end)
        {
          XYZ ps = start.Origin;
          XYZ pe = end.Origin;
          double length = ps.DistanceTo(pe);
          double d2 = 2 * dToEnd;
          if (Util.IsEqual(d2, length) || d2 > length)
          {
            // No space for two, so place hanger 
            // in the middle if possible

            try
            {
              //FabricationPart.CreateHanger(doc, hanger_button,
              //  button_condition, id, start, 0.5 * length,
              //  attach_to_structure);
            }
            catch (InvalidOperationException ex)
            {
              Debug.Assert(ex.Message.Equals(
                "Cannot place hanger on the host."),
                "expected problem placing hanger on host");
            }
          }
          else
          {
            // Place start and middle hangers

            double t = dToEnd;
            double dEnd = length - dToEnd;
            while (t < dEnd || Util.IsEqual(t, dEnd))
            {
              //FabricationPart.CreateHanger(doc, hanger_button,
              //  button_condition, id, start, t,
              //  attach_to_structure);

              t += dSpacing;
            }

            // Place end hanger

            //FabricationPart.CreateHanger(doc, hanger_button,
            //  button_condition, id, end, dToEnd,
            //  attach_to_structure);
          }
        }
      }

    }

    void DetermineInsertionFaceAndLocation( 
      int index,
      Connector con,
      out int iface,
      out XYZ p )
    {
      p = null;
      iface = -1;
      if (ConnectorType.Physical == con.ConnectorType)
      {
        p = con.Origin;
        Debug.Print("{0}: {1}", index, Util.PointString(p));
      }
    }

    /// <summary>
    /// Analyse a given rectangular duct fabrication part's 
    /// side face connectors. Return false if it has none.
    /// </summary>
    bool AnalyzeDuctFaceConnectors(
      List<string> report,
      FabricationPart part)
    {
      ConnectorManager conmgr = part.ConnectorManager;
      ConnectorSet conset = conmgr.Connectors;
      int n = conset.Size;
      bool rc = false;

      if(3 > n)
      {
        Debug.Print("duct <{0}> has only one or two connectors",
          part.Id.IntegerValue /*Util.ElementDescription(part)*/ );
      }
      else
      {
        Connector start = GetPrimaryConnector(conset);
        Connector end = GetSecondaryConnector(conset);

        XYZ ps = start.Origin;
        XYZ pe = end.Origin;
        XYZ vz = pe - ps;
        double length = vz.GetLength();
        vz = vz.Normalize();

        // Transform from local duct to world coordinate system

        Transform twcs = start.CoordinateSystem;
        Debug.Assert(Util.IsEqual(1, twcs.Determinant), "expected start connector unity transform");

        // Flip so that Z axis points into duct, not out of it

        twcs.BasisY = -(twcs.BasisY);
        twcs.BasisZ = -(twcs.BasisZ);

        // Duct width, height and orientation

        XsecDimMm xsecdim = new XsecDimMm(start);
        XYZ vw = twcs.BasisX;
        XYZ vh = twcs.BasisY;

        string duct_description = string.Format(
          "Duct <{0}> at {1} --> {2} {3} "
          + "vw {4} vh {5} length {6} {7}",
          part.Id.IntegerValue /*Util.ElementDescription(part)*/,
          Util.PointStringMm(ps), Util.PointStringMm(pe),
          xsecdim.ToString(),
          Util.PointStringInt(vw), Util.PointStringInt(vh),
          Util.FootToMmInt(length), Util.PointStringInt(vz));

        Debug.Print(duct_description 
          + $" has {n} connector{Util.PluralSuffix(n)}{Util.DotOrColon(n)}");

        if (!Util.IsEqual(vz, twcs.BasisZ))
        {
          Debug.Print( "start connector does not align with end connector");
        }
        else
        {
          string condesc = "<empty>";

          // Transform from world to local duct coordinate system

          Transform tlcs = twcs.Inverse;
          XYZ pslcs = tlcs.OfPoint(ps);
          Debug.Assert(Util.IsEqual(pslcs, XYZ.Zero));

          int i = -1;

          foreach (Connector c in conset)
          {
            ++i;
            ConnectorType ctyp = c.ConnectorType;
            if (ConnectorType.Curve != ctyp
              && ConnectorType.End != ctyp )
            {
              condesc = string.Format("{0} {1}", i, ctyp.ToString());
            }
            else
            {
              MEPConnectorInfo info = c.GetMEPConnectorInfo();
              string psx = info.IsPrimary ? "P" : (info.IsSecondary ? "S" : "X");
              //int iface;
              Transform tx = c.CoordinateSystem;
              //XYZ pcwcs = c.Origin; // on duct centre line curve
              XYZ pwcs = tx.Origin;
              XYZ vzwcs = tx.BasisZ;
              //Debug.Assert(Util.IsEqual(vzwcs, vw) || Util.IsEqual(vzwcs, vh),
              //  "expected tap location in w or h direction");
              XYZ plcs = tlcs.OfPoint(pwcs);
              XYZ vzlcs = tlcs.OfVector(vzwcs);
              XYZ vd = plcs - pslcs;
              condesc = string.Format(
                "{0} {1} {2}: {3}+{4} {5}+{6} vd {7}", 
                i, psx, ctyp.ToString(),
                Util.PointStringMm(pwcs), Util.PointStringInt(vzwcs),
                Util.PointStringMm(plcs), Util.PointStringInt(vzlcs),
                Util.PointStringMm(vd));

              // Connector location is on duct centre line, not 
              // on a face, so we cannot use that. Connector Z
              // direction points along the duct centre line,
              // not to a face, so we cannot use that either.

              // Third attempt: find the connected connector 
              // and use its origin.

              Connector c2 = GetConnectedConnector(c);
              string c2data = "<null>";
              if (null != c2)
              {
                XYZ p2w = c2.Origin;
                XYZ p2l = tlcs.OfPoint(p2w);
                XYZ v2d = p2l - pslcs;

                c2data = string.Format("{0} {1} {2}", Util.PointStringMm(p2w),
                  Util.PointStringMm(p2l), Util.PointStringMm(v2d));

                if (xsecdim.IsRect 
                  && !(Util.IsZero(v2d.X) && Util.IsZero(v2d.Y)))
                {
                  if( null != duct_description)
                  {
                    report.Add(duct_description + ":");
                    duct_description = null;
                  }
                  XsecDimMm xsecdim2 = new XsecDimMm(c2);
                  report.Add(string.Format(
                    $"  tap {xsecdim2} at {Util.PointStringMm(v2d)}"));
                }
              }
              condesc += " connected to " + c2data;
            }
            Debug.Print(condesc);
          }
        }
      }
      return rc;
    }

    /// <summary>
    /// Analyse all pre- or post-selected 
    /// fabrication part duct faces
    /// </summary>
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements)
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      List<ElementId> ids = new FabricationPartSelector(uidoc).Ids;

      int n = ids.Count;

      if( 0 == n )
      {
        return Result.Cancelled;
      }

      int nDuctFaceParts = 0;
      int nNonDuctFaceParts = 0;
      List<string> report = new List<string>();

      foreach (ElementId id in ids)
      {
        FabricationPart part 
          = doc.GetElement(id) as FabricationPart;

        if(AnalyzeDuctFaceConnectors(report, part))
        {
          ++nDuctFaceParts;
        }
        else
        {
          ++nNonDuctFaceParts;
        }
      }
      string s = $"{nDuctFaceParts + nNonDuctFaceParts} parts, "
        + $"{nDuctFaceParts} with face connectors, {nNonDuctFaceParts} without:";

      Util.InfoMsg3(s, report);

      return Result.Succeeded;
    }
  }
}

// what is the  meaning of GetFabricationConnectorInfo.FabricationIndex?
// -1 means thast it's a tap, apparently

