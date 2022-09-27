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
    void DetermineInsertionFaceAndLocation( 
      Connector con,
      out int iface,
      out XYZ p )
    {
      iface = -1;
      p = con.Origin;
      Debug.Print(Util.PointString(p));
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
      Debug.Print("{0} connector{1}{2}", n, Util.PluralSuffix(n), Util.DotOrColon(n));

      foreach (Connector con in conset)
      {
        int iface;
        XYZ p;
        DetermineInsertionFaceAndLocation(con, out iface, out p);
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
