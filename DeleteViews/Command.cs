#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Input;

#endregion

namespace DeleteViews
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // create list of views to be deleted
            List<View> viewsToDelete = new List<View>();

            // create list of views to keep
            List<View> viewsToKeep = new List<View>();

            // get all views in the project
            FilteredElementCollector viewColl = new FilteredElementCollector(doc);
            viewColl.OfCategory(BuiltInCategory.OST_Views);

            // get all sheets in the project
            FilteredElementCollector sheetColl = new FilteredElementCollector(doc);
            sheetColl.OfClass(typeof(ViewSheet));

            // make sure there are sheets in the project
            if (sheetColl.Count() < 1)
            {
                // alert the user
                TaskDialog.Show("Error", "There are no sheets in the project. Please add some.");
                return Result.Cancelled;
            }

            // loop through views
            foreach (View curView in viewColl)
            {
                // check if view name has prefix
                if (curView.Name.Contains("working_") == false)
                {
                    // check if view is already on sheet
                    if (Viewport.CanAddViewToSheet(doc, sheetColl.FirstElementId(), curView.Id))
                    {

                        // check if view has dependent views
                        if (curView.GetDependentViewIds().Count() == 0)
                        {
                            // add view to list of views to be deleted
                            viewsToDelete.Add(curView);
                        }

                    }
                }
            }

            // start a transaction
            Transaction curTrans = new Transaction(doc, "Delete unused views");
            curTrans.Start();

            // loop through list of views to delete
            try
            {
                foreach (View deleteView in viewsToDelete)
                {
                    // delete the view
                    doc.Delete(deleteView.Id);
                }
            }
            catch (Exception)
            {
                TaskDialog.Show("Error", "Could not delete view.");
            }

            // close the transaction
            curTrans.Commit();
            curTrans.Dispose();

            // alert the user
            TaskDialog.Show("Complete", "Deleted " + viewsToDelete.Count().ToString() + " views.");

            return Result.Succeeded;
        }
    }
}
