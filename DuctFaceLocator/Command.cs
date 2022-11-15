#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;

#endregion

namespace DuctFaceLocator
{
  [Transaction(TransactionMode.Manual)]
  public class Command : IExternalCommand
  {
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
    /// Analyse a given fabrication part's connectors
    /// </summary>
    /// <param name="part"></param>
    void AnalyzeFabricationPart(FabricationPart part)
    {
      ConnectorManager conmgr = part.ConnectorManager;
      ConnectorSet conset = conmgr.Connectors;
      int n = conset.Size;

      if(3 > n)
      {
        Debug.Print("only one or two connectors");
      }
      else
      {
        Debug.Print("{0} connector{1}", n, Util.PluralSuffix(n), Util.DotOrColon(n));

        Connector start = GetPrimaryConnector(conset);
        Connector end = GetSecondaryConnector(conset);

        XYZ ps = start.Origin;
        XYZ pe = end.Origin;
        XYZ v = (pe - ps).Normalize();

        // Transform from local duct to world coordinate system

        Transform twcs = start.CoordinateSystem;
        Debug.Assert(Util.IsEqual(1, twcs.Determinant), "expected start connector unity transform");
        //Debug.Assert(Util.IsParallel(v, twcs.BasisZ), "expected start connector aligned with end connector");

        // Flip so that Z axis points into duct, not out of it

        twcs.BasisY = -(twcs.BasisY);
        twcs.BasisZ = -(twcs.BasisZ);
        //Debug.Assert(Util.IsEqual(v, twcs.BasisZ), "expected start connector aligned with end connector");

        if(!Util.IsEqual(v, twcs.BasisZ))
        {
          Debug.Print( "start connector does not align with end connector");
        }
        else
        {
          int w = Util.FootToMmInt(start.Width);
          int h = Util.FootToMmInt(start.Height);

          // Transform from world to local duct coordinate system

          Transform tlcs = twcs.Inverse;
          XYZ pslcs = tlcs.OfPoint(ps);
          Debug.Assert(Util.IsEqual(pslcs, XYZ.Zero));

          int i = -1;

          foreach (Connector c in conset)
          {
            ++i;
            MEPConnectorInfo info = c.GetMEPConnectorInfo();
            if (!info.IsPrimary && !info.IsSecondary)
            {
              int iface;
              XYZ pwcs = c.Origin;
              XYZ plcs = tlcs.OfPoint(pwcs);
              v = plcs - pslcs;
              //XYZ w = tlcs.OfVector(v);
              //Debug.Print("");
              Debug.Print("{0}: {1} {2} {3}", i, Util.PointStringMm(pwcs), Util.PointStringMm(plcs), Util.PointStringMm(v));

              //DetermineInsertionFaceAndLocation(t, i, c, out iface, out p);
            }
          }
        }
      }
    }

    /// <summary>
    /// Analyse all selected or pre-selected fabrication parts
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

      foreach (ElementId id in ids)
      {
        FabricationPart part 
          = doc.GetElement(id) as FabricationPart;

        AnalyzeFabricationPart(part);
      }

      return Result.Succeeded;
    }
  }
}

// what is the  meaning of GetFabricationConnectorInfo.FabricationIndex?
// -1 means thast it's a tap, apparently

