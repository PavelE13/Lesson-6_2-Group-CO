using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Structure;

namespace Lesson_6_2_Group_CO
{
    [TransactionAttribute(TransactionMode.Manual)]

    public class CreationModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            List<Level> listlevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .ToList();

            Level level1 = listlevel
               .Where(x => x.Name.Equals("Уровень 1"))
               .FirstOrDefault();
            Level level2 = listlevel
                .Where(x => x.Name.Equals("Уровень 2"))
                .FirstOrDefault();

            GetWallDrawMethod(doc, level1, level2);

            /*var res1 = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .OfType<WallType>()
                    .ToList();

            var res2 = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance))
                    .OfCategory(BuiltInCategory.OST_Doors)
                    .OfType<FamilyInstance>()
                    .Where(f => f.Name.Equals("0915 x 2134 мм"))
                    .ToList();

            var res3 = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .ToList();*/

            return Result.Succeeded;
        }

        public static void GetWallDrawMethod(Document doc, Level level1, Level level2)
        {
            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);
            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            Transaction transaction = new Transaction(doc, "Построение стен, дверей и крыши");
            transaction.Start();

            List<Wall> walls = new List<Wall>();
            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
            }
            AddDoor(doc, level1, walls[0]);
            AddRoof(doc, level2, walls);
            transaction.Commit();

            List<XYZ> pointcenterlist = new List<XYZ>();
            for (int b = 1; b < 4; b++)
            {
                AddWindow(doc, level1, walls[b]);
            }
        }

        public static void AddWindow(Document doc, Level level1, Wall wall)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_Windows)
                    .OfType<FamilySymbol>()
                    .Where(x => x.Name.Equals("0406 x 0610 мм"))
                    .Where(x => x.FamilyName.Equals("Фиксированные"))
                    .FirstOrDefault();

            Transaction transaction = new Transaction(doc, "Построение окон");
            transaction.Start();

            //XYZ correct = new XYZ(0, 0, 5);
            //BoundingBoxXYZ bounds = wall.get_BoundingBox(null);
            //LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ pointcenter = GetElementCenter(wall);
            //XYZ point1 = bounds.Max;
            //XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            //XYZ point2 = bounds.Min; 
            //XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            //XYZ pointw = ((point1 + point2) / 2); //+ correct;
            //XYZ pointc = (pointw + pointcenter) / 2;
            /*XYZ pointcenter = GetElementCenter(walls[b]);
            pointcenterlist.Add(pointcenter);*/

            if (!windowType.IsActive)
                windowType.Activate();

            doc.Create.NewFamilyInstance(pointcenter, windowType, wall, level1, StructuralType.NonStructural);

            transaction.Commit();
        }

        public static void AddDoor(Document doc, Level level1, Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_Doors)
                    .OfType<FamilySymbol>()
                    .Where(x => x.Name.Equals("0762 x 2134 мм"))
                    .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                    .FirstOrDefault();
            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ pointd = (point1 + point2) / 2;

            if (!doorType.IsActive)
                doorType.Activate();

            doc.Create.NewFamilyInstance(pointd, doorType, wall, level1, StructuralType.NonStructural);
        }

        private static void AddRoof(Document doc, Level level2, List<Wall> walls)
        {
            RoofType roofType = new FilteredElementCollector(doc)
                .OfClass(typeof(RoofType))
                .OfType<RoofType>()
                .Where(x => x.Name.Equals("Типовой - 400мм") && x.FamilyName.Equals("Базовая крыша"))
                .FirstOrDefault();

            double wallWidth = walls[0].Width;
            double dt = wallWidth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dt, -dt, 0));
            points.Add(new XYZ(dt, -dt, 0));
            points.Add(new XYZ(dt, dt, 0));
            points.Add(new XYZ(-dt, dt, 0));
            points.Add(new XYZ(-dt, -dt, 0));

            //Application application = doc.Application;
            CurveArray curvearray = new CurveArray();

            /*for (int i = 0; i < 4; i++)
            {
                LocationCurve curve = walls[i].Location as LocationCurve;
                footprint.Append(curve.Curve);
                XYZ p1 = curve.Curve.GetEndPoint(0);
                XYZ p2 = curve.Curve.GetEndPoint(1);
                curvearray.Append(Line.CreateBound(p1, p2));
                Line line = Line.CreateBound(p1 + points[i], p2 + points[i + 1]);
                footprint.Append(line);
            }*/

            curvearray.Append(Line.CreateBound(new XYZ(-17.74, -8.53, 14.12), new XYZ(-17.74, 0, 20)));
            curvearray.Append(Line.CreateBound(new XYZ(-17.74, 0, 20), new XYZ(17.74, 8.53, 14.12)));

            ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(0, 0, 0), new XYZ(0, 0, 16), new XYZ(0, 16, 0), doc.ActiveView);
            doc.Create.NewExtrusionRoof(curvearray, plane, level2, roofType, -16, 16);

            /*ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
            FootPrintRoof footprintRoof = doc.Create.NewFootPrintRoof(footprint, level2, roofType, out footPrintToModelCurveMapping);
            ModelCurveArrayIterator iterator = footPrintToModelCurveMapping.ForwardIterator();
            iterator.Reset();
            while (iterator.MoveNext())
            {
                ModelCurve modelCurve = iterator.Current as ModelCurve;
                footprintRoof.set_DefinesSlope(modelCurve, true);
                footprintRoof.set_SlopeAngle(modelCurve, 0.5);
            }*/
        }
        public static XYZ GetElementCenter(Element element)
        {
            BoundingBoxXYZ bounds = element.get_BoundingBox(null);
            return (bounds.Max + bounds.Min) / 2;
        }
    }
}
