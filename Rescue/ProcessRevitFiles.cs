using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.RevitAddIns;
using Autodesk.Revit.DB.Architecture;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using IronPython.Runtime;
using MySql.Data.MySqlClient;
using System.Drawing;
using System.Threading;
using Org.BouncyCastle.Math.EC.Multiplier;
using System.Runtime.ExceptionServices;


namespace Window
{
    public class revitFile
    {
        private List files;
        public revitFile(List f)
        {
            files = f;
            /*
            String versionOfRvtFile = RevitFileUtils.GetVersion(filePath);
            Console.WriteLine("The version of the file: Revit " + versionOfRvtFile);
            */
            AddEnvironmentPaths(Searchs);
            foreach (String s in Searchs)
            {
                Console.WriteLine("The path of the Revit folder: " + s);
            }

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }
        static readonly string[] Searchs = RevitProductUtility.GetAllInstalledRevitProducts().Select(x => x.InstallLocation).ToArray();

        static void AddEnvironmentPaths(params string[] paths)
        {
            var path = new[] { Environment.GetEnvironmentVariable("PATH") ?? string.Empty };
            var newPath = string.Join(System.IO.Path.PathSeparator.ToString(), path.Concat(paths));
            //Set environment variables
            Environment.SetEnvironmentVariable("PATH", newPath);
        }
        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);

            //Find the relevant DLL in the installation path and load it
            foreach (var item in Searchs)
            {
                var file = string.Format("{0}.dll", System.IO.Path.Combine(item, assemblyName.Name));
                if (File.Exists(file))
                {
                    Console.WriteLine(file);
                    return Assembly.LoadFile(file);
                }
            }

            return args.RequestingAssembly;
        }
        public List<ModelCurve> ModelCurve_Of_FloorBoundary(Autodesk.Revit.DB.Document doc, Floor floor)
        {
            List<ModelCurve> mclist = new List<ModelCurve>();
            //Find board edges by deleting
            Transaction transTemp = new Transaction(doc); transTemp.Start("tempDelete");
            ICollection<ElementId> flooredge_idlist = doc.Delete(floor.Id);
            transTemp.RollBack();

            foreach (ElementId id in flooredge_idlist)
            {
                Element e = doc.GetElement(id);
                if (e is ModelLine)
                {
                    //Modellines with 16 or more parameters are boundaries, 12 are slope arrows, and 6 are span directions
                    if ((e as ModelLine).Parameters.Size > 12)
                        mclist.Add(e as ModelLine);
                }
                if (e is ModelArc || e is ModelEllipse || e is ModelNurbSpline)
                    mclist.Add(e as ModelCurve);
            }
            return mclist;
        }
        public void convertRevitFiles()
        {
            Product _product = Product.GetInstalledProduct();

            var clientId = new ClientApplicationId(Guid.NewGuid(), "LK", "BIMAPI");

            //"I am authorized by Autodesk to use this UI-less functionality." Can only be this string.
            _product.Init(clientId, "I am authorized by Autodesk to use this UI-less functionality.");

            foreach (RevitFileUtils file in files)
            {
                convertRevitFile(file.getFilePath(), _product, file.getBuildingName());
            }
            _product?.Exit();
        }

        public void convertRevitFile(string filepath, Product _product, string building_name)
        {
            Console.WriteLine(filepath);
            var Application = _product.Application;
            Console.WriteLine("Current Revit version: " + Application.VersionName);

            Document doc = Application.OpenDocumentFile(filepath);
            Console.WriteLine("RVT FILE OPENED");
            Console.WriteLine("Current filename: " + doc.Title);

            ScriptRuntime pyRumTime = Python.CreateRuntime();
            dynamic obj = pyRumTime.UseFile("..\\..\\..\\room_finder.py");
            Console.WriteLine(obj.welcome("Test function in my Python"));

            Console.WriteLine("----------------------------------------START GETTING VIEW-------------------------------------------");
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            var it = collector.OfClass(typeof(View)).ToArray();
            List<View> views = new List<View>();
            View3D largestView3D = null;
            int elementNumber = -1;
            foreach (Element v in it)
            {
                View3D view3D = v as View3D;
                if (view3D != null)
                {
                    if(null != view3D.CropBox)
                    {
                        FilteredElementCollector viewCollector = new FilteredElementCollector(doc, view3D.Id);
                        viewCollector.OfCategory(BuiltInCategory.OST_Doors);
                        if (viewCollector.ToElementIds().Count >= elementNumber)
                        {
                            elementNumber = viewCollector.ToElementIds().Count;
                            largestView3D = view3D;
                        }
                    }   
                }
            }
            views.Add(largestView3D);
            Console.WriteLine("----------------------------------------GETTING VIEW SUCCEED-------------------------------------------");
            Console.WriteLine();
            Console.WriteLine("----------------------------------------START GETTING FIRST LEVEL-----------------------------------------");
            FilteredElementCollector levelCollector = new FilteredElementCollector(doc);
            var levels = levelCollector.OfClass(typeof(Level)).ToArray();
            ElementId level_id = levels[0].Id;
            Level groundFloorLevel;
            foreach (Level l in levels)
            {
                int floor_number = obj.getDigit(l.Name);
                if (floor_number == 1)
                {
                    level_id = l.Id;
                    groundFloorLevel = l;
                }
            }
            Console.WriteLine("----------------------------------------GETTING FIRST LEVEL SUCCEED---------------------------------------");
            Console.WriteLine();
            Console.WriteLine("----------------------------------------START GETTING DOOR CONNECTION-------------------------------------");
            FilteredElementCollector doorCollector = new FilteredElementCollector(doc);
            var doors = doorCollector.OfCategory(BuiltInCategory.OST_Doors).OfClass(typeof(FamilyInstance)).ToArray();

            List<List> roomEdges = new List<List>();
            List exitRoom = new List();
            List exitDoor = new List();

            foreach (FamilyInstance door in doors)
            {
                Room room = door.Room;
                Room room1 = door.FromRoom;
                Room room2 = door.ToRoom;

                List roomList = new List();
                if(room!=null)
                { 
                    roomList.Add(room);
                }
                if (room1 != null)
                {
                    roomList.Add(room1);
                }
                if (room2 != null)
                {
                    roomList.Add(room2);
                }
                if(roomList.Count == 0)
                {
                    continue;
                }
                List roomList1 = new List();
                roomList1.Add(roomList[0]);
                foreach (Room r in roomList)
                {
                    foreach(Room r1 in roomList1)
                    {
                        if(r.Name == r1.Name)
                        {
                            break;
                        }
                        if(r1 == roomList1[roomList1.Count - 1])
                        {
                            roomList1.Add(r);
                        }
                    }
                }
                if(roomList1.Count > 1)
                {
                    roomList1.Add(door);
                    roomEdges.Add(roomList1);
                }
                else if(roomList1.Count == 1)
                {
                    if ((roomList1[0] as Room).LevelId == level_id)
                    {
                        exitDoor.Add(door);
                    }
                }
            }
            Console.WriteLine("----------------------------------------GETTING DOOR CONNECTION SUCCEED-------------------------------------------");
            Console.WriteLine();
            Console.WriteLine("----------------------------------------START GETTING STAIR CONNECTION(This takes a long time)---------------------------");
            FilteredElementCollector roomCollector = new FilteredElementCollector(doc);
            var rooms = roomCollector.OfCategory(BuiltInCategory.OST_Rooms).ToArray();

            FilteredElementCollector stairCollector = new FilteredElementCollector(doc);
            var stairs = stairCollector.OfCategory(BuiltInCategory.OST_Stairs).ToArray();

            List s = new List();
            foreach(Element stair in stairs)
            {
                s.Add(stair);
            }
            foreach (Element stair in s)
            {
                BoundingBoxXYZ b = stair.get_BoundingBox(views[0]);
                if(b == null)
                {
                    continue;
                }
                List stairConnectRooms = new List();
                double current_height = b.Min.Z;
                double end_height = b.Max.Z;

                for(int i = 0; i < 10000; i++)
                {
                    if (current_height + 6 > end_height)
                    {
                        current_height = end_height + 1;
                    }

                    Room currentRoom = null;
                    XYZ possiblePoint0 = new XYZ(b.Max.X, b.Max.Y, current_height);
                    XYZ possiblePoint1 = new XYZ((b.Max.X + b.Min.X) / 2, (b.Max.Y + b.Min.Y) / 2, current_height);

                    Room possibleRoom0 = doc.GetRoomAtPoint(possiblePoint0);
                    Room possibleRoom1 = doc.GetRoomAtPoint(possiblePoint1);

                    if (possibleRoom0 != null)
                    {
                        currentRoom = possibleRoom0;
                    }
                    else
                    {
                        currentRoom = possibleRoom1;
                    }

                    if(currentRoom == null)
                    {
                        continue;
                    }
                    if (stairConnectRooms.Count == 0)
                    {
                        stairConnectRooms.Add(currentRoom);
                    }
                    else
                    {
                        int roomCount = 0;
                        foreach(Room r in stairConnectRooms)
                        {
                            roomCount = roomCount + 1;
                            if(currentRoom.Name == r.Name && currentRoom.LevelId == r.LevelId)
                            {
                                break;
                            }
                            if(roomCount == stairConnectRooms.Count)
                            {
                                stairConnectRooms.Add(currentRoom);
                            }
                        }
                    }
                    current_height = current_height + 6;
                    if (current_height > end_height)
                    {
                        break;
                    }
                }
                List possibleRoomList = new List();
                Room possibleTop = doc.GetRoomAtPoint(b.Max);
                Room possibleTop1 = doc.GetRoomAtPoint(new XYZ(b.Min.X, b.Min.Y, b.Max.Z + 1));
                Room possibleTop2 = doc.GetRoomAtPoint(new XYZ((b.Max.X + b.Min.X) / 2, (b.Max.Y + b.Min.Y) / 2, b.Max.Z + 1));

                possibleRoomList.Add(possibleTop);
                possibleRoomList.Add(possibleTop1);
                possibleRoomList.Add(possibleTop2);

                foreach (Room p in possibleRoomList)
                {
                    if(p != null)
                    {
                        int roomCount = 0;
                        foreach (Room r in stairConnectRooms)
                        {
                            roomCount = roomCount + 1;
                            if (p.Name == r.Name && p.LevelId == r.LevelId)
                            {
                                break;
                            }
                            if (roomCount == stairConnectRooms.Count)
                            {
                                stairConnectRooms.Add(p);
                                break;
                            }
                        }
                    }
                }
                if (stairConnectRooms.Count < 2)
                {
                    continue;
                }
                else
                {
                    for(int j = 0; j < stairConnectRooms.Count - 1; j++)
                    {
                        Room r1 = stairConnectRooms[j] as Room;
                        Room r2 = stairConnectRooms[j+1] as Room;

                        if (r1 != null && r2 != null && r1.Name != r2.Name)
                        {
                            List roomlist = new List();
                            roomlist.Add(r1);
                            roomlist.Add(r2);
                            roomlist.Add(stair);
                            roomEdges.Add(roomlist);
                        }
                    }
                }  
            }
            Console.WriteLine("----------------------------------------GETTING STAIR CONNECTION SUCCEED-------------------------------------------");
            Console.WriteLine();

            List room_boundary_no_walls = new List();
            List walls = new List();
            foreach (Room room in rooms)
            { 
                SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions();
                IList<IList<BoundarySegment>> segments = (room as Room).GetBoundarySegments(opt);
                if (null != segments & room.Location != null)
                {
                    foreach (IList<BoundarySegment> segmentList in segments)
                    {
                        foreach (BoundarySegment boundarySegment in segmentList)
                        {
                            Wall wall = doc.GetElement(boundarySegment.ElementId) as Wall;
                            if (wall != null)
                            {
                                List w = new List();
                                XYZ start = boundarySegment.GetCurve().GetEndPoint(0);
                                XYZ end = boundarySegment.GetCurve().GetEndPoint(1);
                                w.Add(room);
                                w.Add(start);
                                w.Add(end);
                                walls.Add(w);
                            }
                            else
                            {
                                List w = new List();
                                XYZ start = boundarySegment.GetCurve().GetEndPoint(0);
                                XYZ end = boundarySegment.GetCurve().GetEndPoint(1);
                                w.Add(room);
                                w.Add(start);
                                w.Add(end);
                                walls.Add(w);
                                double length = (start.X - end.X) * (start.X - end.X) + (start.Y - end.Y) * (start.Y - end.Y) + (start.Z - end.Z) * (start.Z - end.Z);
                                if (length > 1)
                                {
                                    List room_boundary_no_wall = new List();
                                    room_boundary_no_wall.Add(room);
                                    room_boundary_no_wall.Add(start);
                                    room_boundary_no_wall.Add(end);
                                    room_boundary_no_walls.Add(room_boundary_no_wall);
                                }
                                
                            }
                        }
                    }
                }
            }
            foreach (List room_1 in room_boundary_no_walls)
            {
                foreach (List room_2 in room_boundary_no_walls)
                {
                    if((room_1[0] as Room).LevelId == (room_2[0] as Room).LevelId)
                    {
                        double slope1;
                        double slope2;
                        if (Math.Abs((room_1[1] as XYZ).X - (room_1[2] as XYZ).X) > 0.1)
                        {
                            slope1 = ((room_1[1] as XYZ).Y - (room_1[2] as XYZ).Y) / ((room_1[1] as XYZ).X - (room_1[2] as XYZ).X);
                        }
                        else
                        {
                            slope1 = 10000000;
                        }
                        if (Math.Abs((room_2[1] as XYZ).X - (room_2[2] as XYZ).X) > 0.1)
                        {
                            slope2 = ((room_2[1] as XYZ).Y - (room_2[2] as XYZ).Y) / ((room_2[1] as XYZ).X - (room_2[2] as XYZ).X);
                        }
                        else
                        {
                            slope2 = 10000000;
                        }

                        if (Math.Abs(slope1-slope2)<0.05 || Math.Abs(slope1 / slope2 - 1) < 0.05)
                        {
                            double length = Math.Pow(Math.Pow((room_2[1] as XYZ).X - (room_2[2] as XYZ).X, 2) + Math.Pow((room_2[1] as XYZ).Y - (room_2[2] as XYZ).Y, 2), 0.5);
                            double distance = Math.Abs(((room_1[1] as XYZ).X - (room_2[1] as XYZ).X) * ((room_2[2] as XYZ).Y - (room_2[1] as XYZ).Y) - ((room_1[1] as XYZ).Y - (room_2[1] as XYZ).Y) * ((room_2[2] as XYZ).X - (room_2[1] as XYZ).X)) / length;

                            if ((room_1[0] as Room).Name != (room_2[0] as Room).Name)
                            {
                                if (distance < 2)
                                {
                                    double length1 = Math.Max((room_1[1] as XYZ).X, (room_1[2] as XYZ).X) - Math.Min((room_2[1] as XYZ).X, (room_2[2] as XYZ).X);
                                    double length2 = Math.Max((room_2[1] as XYZ).X, (room_2[2] as XYZ).X) - Math.Min((room_1[1] as XYZ).X, (room_1[2] as XYZ).X);

                                    double length3 = Math.Max((room_1[1] as XYZ).Y, (room_1[2] as XYZ).Y) - Math.Min((room_2[1] as XYZ).Y, (room_2[2] as XYZ).Y);
                                    double length4 = Math.Max((room_2[1] as XYZ).Y, (room_2[2] as XYZ).Y) - Math.Min((room_1[1] as XYZ).Y, (room_1[2] as XYZ).Y);

                                    if (length1 > 0 && length2 > 0)
                                    {
                                        double ratio = Math.Min(length1, length2) / Math.Max(length1, length2);
                                        if (ratio > 0.1)
                                        {
                                            List roomlist1 = new List();
                                            roomlist1.Add((room_1[0] as Room));
                                            roomlist1.Add((room_2[0] as Room));

                                            roomlist1.Add("no wall");
                                            roomEdges.Add(roomlist1);
                                            break;
                                        }
                                    }
                                    if (length3 > 0 && length4 > 0)
                                    {
                                        double ratio1 = Math.Min(length3, length4) / Math.Max(length3, length4);
                                        if (ratio1 > 0.1)
                                        {
                                            List roomlist1 = new List();
                                            roomlist1.Add((room_1[0] as Room));
                                            roomlist1.Add((room_2[0] as Room));

                                            roomlist1.Add("no wall");
                                            roomEdges.Add(roomlist1);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Console.WriteLine("----------------------------------------GETTING BOUNDARY CONNECTION SUCCEED-------------------------------------------");
            Console.WriteLine();
            Console.WriteLine("----------------------------------------START GETTING BOUNDARY-------------------------------------------");

            List boundaryList = new List();
            foreach (Room room in rooms)
            {
                if(room.LevelId == level_id)
                {
                    SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions();
                    IList<IList<BoundarySegment>> segments = (room as Room).GetBoundarySegments(opt);
                    if (null != segments & room.Location != null)
                    {
                        foreach (IList<BoundarySegment> segmentList in segments)
                        {
                            foreach (BoundarySegment boundarySegment in segmentList)
                            {
                                List boundary = new List();
                                XYZ start = boundarySegment.GetCurve().GetEndPoint(0);
                                XYZ end = boundarySegment.GetCurve().GetEndPoint(1);
                                boundary.Add(room);
                                boundary.Add(start);
                                boundary.Add(end);
                                boundaryList.Add(boundary);
                            }
                        }
                    }
                }
            }
            

            List boundaryOutLine = new List();
            int indexRoom1 = 0;
            foreach (List room_1 in boundaryList)
            {
                indexRoom1 = indexRoom1 + 1;
                int indexBoundary = 0;
                foreach (List room_2 in boundaryList)
                {
                    indexBoundary = indexBoundary + 1;
                    if (indexBoundary != indexRoom1)
                    {
                        //if ((room_2[0] as Room).Name != (room_1[0] as Room).Name)
                        //{
                            double slope1;
                            double slope2;
                            if(Math.Abs((room_1[1] as XYZ).X - (room_1[2] as XYZ).X) > 0.1)
                            {
                                slope1 = ((room_1[1] as XYZ).Y - (room_1[2] as XYZ).Y) / ((room_1[1] as XYZ).X - (room_1[2] as XYZ).X);
                            }
                            else
                            {
                                slope1 = 100000000;
                            }
                            if(Math.Abs((room_2[1] as XYZ).X - (room_2[2] as XYZ).X) > 0.1)
                            {
                                slope2 = ((room_2[1] as XYZ).Y - (room_2[2] as XYZ).Y) / ((room_2[1] as XYZ).X - (room_2[2] as XYZ).X);
                            }
                            else
                            {
                                slope2 = 100000000;
                            }
                            if (Math.Abs(slope1/slope2 - 1) < 0.05 || Math.Abs(slope1 - slope2 ) < 0.05)
                            {
                                double length = Math.Pow(Math.Pow((room_2[1] as XYZ).X - (room_2[2] as XYZ).X, 2) + Math.Pow((room_2[1] as XYZ).Y - (room_2[2] as XYZ).Y, 2), 0.5);
                                double distance = Math.Abs(((room_1[1] as XYZ).X - (room_2[1] as XYZ).X) * ((room_2[2] as XYZ).Y - (room_2[1] as XYZ).Y) - ((room_1[1] as XYZ).Y - (room_2[1] as XYZ).Y) * ((room_2[2] as XYZ).X - (room_2[1] as XYZ).X)) / length;

                                if (distance < 3)
                                {
                                    double length1 = Math.Max((room_1[1] as XYZ).X, (room_1[2] as XYZ).X) - Math.Min((room_2[1] as XYZ).X, (room_2[2] as XYZ).X);
                                    double length2 = Math.Max((room_2[1] as XYZ).X, (room_2[2] as XYZ).X) - Math.Min((room_1[1] as XYZ).X, (room_1[2] as XYZ).X);

                                    double length3 = Math.Max((room_1[1] as XYZ).Y, (room_1[2] as XYZ).Y) - Math.Min((room_2[1] as XYZ).Y, (room_2[2] as XYZ).Y);
                                    double length4 = Math.Max((room_2[1] as XYZ).Y, (room_2[2] as XYZ).Y) - Math.Min((room_1[1] as XYZ).Y, (room_1[2] as XYZ).Y);

                                    if (length1 > 0 && length2 > 0)
                                    {
                                        double ratio = Math.Min(length1, length2) / Math.Max(length1, length2);
                                        if (ratio > 0.4)
                                        {
                                            break;
                                        }
                                    }
                                    if (length3 > 0 && length4 > 0)
                                    {
                                        double ratio1 = Math.Min(length3, length4) / Math.Max(length3, length4);
                                        if (ratio1 > 0.4)
                                        {
                                            break;
                                        }
                                    }  
                                }
                            }
                        //}
                    }
                    if (indexBoundary == boundaryList.Count)
                    {
                        double length1 = Math.Pow(Math.Pow((room_1[1] as XYZ).X - (room_1[2] as XYZ).X, 2) + Math.Pow((room_1[1] as XYZ).Y - (room_1[2] as XYZ).Y, 2), 0.5);
                        if (length1 > 3.281)
                        {
                            List boundaryList1 = new List();
                            boundaryList1.Add(room_1[0] as Room);
                            boundaryList1.Add(room_1[1]);
                            boundaryList1.Add(room_1[2]);
                            boundaryOutLine.Add(boundaryList1);
                        }
                    }
                    
                }
            }
            Console.WriteLine("----------------------------------------GETTING BOUNDARY SUCCEED-------------------------------------------");
            Console.WriteLine();
            Console.WriteLine("----------------------------------------START GETTING EXIT ROOM-------------------------------------------");
            foreach (FamilyInstance door in exitDoor)
            {
                double edge_x = (door.get_BoundingBox(views[0]).Max.X + door.get_BoundingBox(views[0]).Min.X) / 2;
                double edge_y = (door.get_BoundingBox(views[0]).Max.Y + door.get_BoundingBox(views[0]).Min.Y) / 2;
                double edge_z = (door.get_BoundingBox(views[0]).Max.Z + door.get_BoundingBox(views[0]).Min.Z) / 2;
                XYZ doorPoint = new XYZ(edge_x, edge_y, edge_z);
                Room possibleExit = doc.GetRoomAtPoint(doorPoint);

                Room roomOfDoor = null;
                Room roomOfDoor0 = door.Room;
                Room roomOfDoor1 = door.FromRoom;
                Room roomOfDoor2 = door.ToRoom;

                if(roomOfDoor0 != null)
                {
                    roomOfDoor = roomOfDoor0;
                }
                else if (roomOfDoor1 != null)
                {
                    roomOfDoor = roomOfDoor1;
                }
                else
                {
                    roomOfDoor = roomOfDoor2;
                }
                if(roomOfDoor == null)
                {
                    continue;
                }

                if (possibleExit == null)
                {
                    foreach (List boundryRoom in boundaryOutLine)
                    {
                        double length = Math.Pow(Math.Pow((boundryRoom[1] as XYZ).X - (boundryRoom[2] as XYZ).X, 2) + Math.Pow((boundryRoom[1] as XYZ).Y - (boundryRoom[2] as XYZ).Y, 2), 0.5);
                        double distance = Math.Abs((doorPoint.X - (boundryRoom[1] as XYZ).X) * ((boundryRoom[2] as XYZ).Y - (boundryRoom[1] as XYZ).Y) - (doorPoint.Y - (boundryRoom[1] as XYZ).Y) * ((boundryRoom[2] as XYZ).X - (boundryRoom[1] as XYZ).X)) / length;
                        if (distance < 3)
                        {
                            if ((boundryRoom[0] as Room).Name == roomOfDoor.Name)
                            {
                                List exit = new List();
                                exit.Add(boundryRoom[0] as Room);
                                exit.Add(doorPoint);
                                exitRoom.Add(exit);
                            }
                        }
                    }
                }
                else
                {
                    foreach (List boundryRoom in boundaryOutLine)
                    {
                        double length = Math.Pow(Math.Pow((boundryRoom[1] as XYZ).X - (boundryRoom[2] as XYZ).X, 2) + Math.Pow((boundryRoom[1] as XYZ).Y - (boundryRoom[2] as XYZ).Y, 2), 0.5);
                        double distance = Math.Abs((doorPoint.X - (boundryRoom[1] as XYZ).X) * ((boundryRoom[2] as XYZ).Y - (boundryRoom[1] as XYZ).Y) - (doorPoint.Y - (boundryRoom[1] as XYZ).Y) * ((boundryRoom[2] as XYZ).X - (boundryRoom[1] as XYZ).X)) / length;
                        if (distance < 3)
                        {
                            if (roomOfDoor2 != null)
                            {
                                if ((boundryRoom[0] as Room).Name == roomOfDoor2.Name && (boundryRoom[0] as Room).Name == possibleExit.Name && roomOfDoor1 == null)
                                {
                                    List exit = new List();
                                    exit.Add(boundryRoom[0] as Room);
                                    exit.Add(doorPoint);
                                    exitRoom.Add(exit);
                                }
                            }
                        }
                    }
                }
            }
            Console.WriteLine("----------------------------------------GETTING EXIT ROOM SUCCEED-------------------------------------------");
            Console.WriteLine();

            String connetStr = "server=" + config.host + ";port= "+ config.port +";user=" + config.USER + ";password=" + config.PASSWORD + "; database=" + config.DATABASE +";";
            MySqlConnection conn = new MySqlConnection(connetStr);
            try
            {
                conn.Open();
            }
            catch (MySqlException ex)
            {
                if (ex.Message == "Unable to connect to any of the specified MySQL hosts.")
                {
                    Console.WriteLine("Unable to connect to any of the specified MySQL hosts.");
                }
                else if (ex.Message == "Authentication to host '" + config.host +"' for user '" + config.USER + "' using method 'caching_sha2_password' failed with message: Unknown database '" + config.DATABASE+ "'")
                {
                    String connetStr1 = "server=" + config.host + ";port=" + config.port + ";user=" + config.USER + ";password=" + config.PASSWORD + "";
                    MySqlConnection conn1 = new MySqlConnection(connetStr1);
                    conn1.Open();
                    string sql_createDatabase = "CREATE SCHEMA `" + config.DATABASE + "` ";
                    MySqlCommand cmd = new MySqlCommand(sql_createDatabase, conn1);
                    int result = cmd.ExecuteNonQuery();

                    string sql_create_table_edge = "CREATE TABLE `" + config.DATABASE + "`.`edge` (" +
                                                   "`id_edge` INT NOT NULL AUTO_INCREMENT," +
                                                   "`building_name` VARCHAR(100) NOT NULL," +
                                                   "`node1` VARCHAR(100) NOT NULL," +
                                                   "`node2` VARCHAR(100) NOT NULL," +
                                                   "`length` DOUBLE NOT NULL," +
                                                   "`edge_type` VARCHAR(100) NOT NULL," +
                                                   "`edge_location` VARCHAR(100) NOT NULL," +
                                                   "PRIMARY KEY(`id_edge`))";

                    string sql_create_table_node = "CREATE TABLE `" + config.DATABASE + "`.`node` (" +
                                                   "`id_node` INT NOT NULL AUTO_INCREMENT," +
                                                   "`building_name` VARCHAR(100) NOT NULL," +
                                                   "`room_name` VARCHAR(100) NOT NULL," +
                                                   "`room_location` VARCHAR(100) NOT NULL," +
                                                   "`is_exit` VARCHAR(100) NOT NULL," +
                                                   "`exit_location` VARCHAR(100) NOT NULL," +
                                                   "PRIMARY KEY(`id_node`));";

                    string sql_create_table_wall = "CREATE TABLE `" + config.DATABASE + "`.`wall` (" +
                                                    "`id_wall` INT NOT NULL AUTO_INCREMENT," +
                                                    "`building_name` VARCHAR(100) NOT NULL," +
                                                    "`room_name` VARCHAR(100) NOT NULL," +
                                                    "`start_point` VARCHAR(100) NOT NULL," +
                                                    "`end_point` VARCHAR(100) NOT NULL," +
                                                    "PRIMARY KEY(`id_wall`));";

                    string sql_create_table_fire = "CREATE TABLE `" + config.DATABASE + "`.`fire` (" +
                                                    "`id_fire` INT NOT NULL," +
                                                    "`building_name` VARCHAR(100) NOT NULL," +
                                                    "`room_name` VARCHAR(100) NOT NULL," +
                                                    "PRIMARY KEY(`id_fire`));";

                    MySqlCommand cmd1 = new MySqlCommand(sql_create_table_edge, conn1);
                    cmd1.ExecuteNonQuery();

                    MySqlCommand cmd2 = new MySqlCommand(sql_create_table_node, conn1);
                    cmd2.ExecuteNonQuery();

                    MySqlCommand cmd3 = new MySqlCommand(sql_create_table_wall, conn1);
                    cmd3.ExecuteNonQuery();

                    MySqlCommand cmd4 = new MySqlCommand(sql_create_table_fire, conn1);
                    cmd4.ExecuteNonQuery();

                    Console.WriteLine("Create database succeed!");
                }
            }
            finally
            {
                conn.Close();
            }

            try
            {
                conn.Open();
                Console.WriteLine("Connection established");
                string sql_insert = "";
                string sql_insert1 = "";
                string sql_insert2 = "";

                foreach (List edge in roomEdges)
                {
                    XYZ room1_location = ((edge[0] as Room).Location as LocationPoint).Point;
                    XYZ room2_location = ((edge[1] as Room).Location as LocationPoint).Point;

                    double dis = Math.Pow(room1_location.X - room2_location.X, 2) + Math.Pow(room1_location.Y - room2_location.Y, 2) + Math.Pow(room1_location.Z - room2_location.Z, 2);
                    dis = Math.Pow(dis, 0.5);

                    string edge_type = "";
                    string edge_location = "";
                    if (edge[2].GetType().Name == "FamilyInstance")
                    {
                        edge_type = "door";
                        if((edge[2] as FamilyInstance).get_BoundingBox(views[0]) != null)
                        {
                            double edge_x = ((edge[2] as FamilyInstance).get_BoundingBox(views[0]).Max.X + (edge[2] as FamilyInstance).get_BoundingBox(views[0]).Min.X) / 2;
                            double edge_y = ((edge[2] as FamilyInstance).get_BoundingBox(views[0]).Max.Y + (edge[2] as FamilyInstance).get_BoundingBox(views[0]).Min.Y) / 2;
                            double edge_z = (edge[2] as FamilyInstance).get_BoundingBox(views[0]).Min.Z;
                            edge_location = "(" + edge_x.ToString() + ", " + edge_y.ToString() + ", " + edge_z.ToString() + ")";
                        }
                        else
                        {
                            edge_location = "null";
                        }
                    }
                    else if(edge[2].GetType().Name == "Stairs" || edge[2].GetType().Name == "Element")
                    {
                        edge_type = "stair";
                        edge_location = "null";
                    }
                    else if (edge[2].GetType().Name == "String")
                    {
                        edge_type = "boundary";
                        edge_location = "null";
                    }
                    else
                    {
                        edge_type = "null";
                        edge_location = "null";
                    }
                    sql_insert += "INSERT INTO `" + config.DATABASE + "`.`edge` (`building_name`, `node1`, `node2`, `length`, `edge_type`, `edge_location`) VALUES ('" + doc.Title + "', '" + (edge[0] as Room).Name +" | "+ (edge[0] as Room).Level.Name+ "', '" + (edge[1] as Room).Name +" | " + (edge[1] as Room).Level.Name + "', '" + dis + "', '" + edge_type + "', '" + edge_location + "');";
                }

                foreach(Room room in rooms)
                {
                    if(room.Location != null)
                    {
                        int i = 0;
                        foreach (List r in exitRoom)
                        {
                            i++;
                            if ((r[0] as Room).Location != null)
                            {
                                if ((r[0] as Room).Name == room.Name && room.LevelId == level_id)
                                {
                                    sql_insert1 += "INSERT INTO `" + config.DATABASE + "`.`node` (`building_name`, `room_name`, `room_location`, `is_exit`, `exit_location`) VALUES ('" + doc.Title + "', '" + room.Name + " | " + room.Level.Name + "', '" + (room.Location as LocationPoint).Point + "', 'true', '" + r[1].ToString() + "'); ";
                                    break;
                                }
                            }
                            if(i == exitRoom.Count)
                            {
                                sql_insert1 += "INSERT INTO `" + config.DATABASE + "`.`node` (`building_name`, `room_name`, `room_location`, `is_exit`, `exit_location`) VALUES ('" + doc.Title + "', '" + room.Name + " | " + room.Level.Name + "', '" + (room.Location as LocationPoint).Point + "', 'false', 'null');";
                            }
                        }
                    }
                }
                foreach(List e in walls)
                {
                    sql_insert2 += "INSERT INTO `" + config.DATABASE + "`.`wall` (`building_name`, `room_name`, `start_point`, `end_point`) VALUES ('" + doc.Title + "' , '" + (e[0] as Room).Name + " | " + (e[0] as Room).Level.Name + "', '" + e[1] + "', '" + e[2] + "');";
                }

                MySqlCommand cmd = new MySqlCommand(sql_insert, conn);
                int result = cmd.ExecuteNonQuery();
                Console.WriteLine(result);

                MySqlCommand cmd1 = new MySqlCommand(sql_insert1, conn);
                int result1 = cmd1.ExecuteNonQuery();
                Console.WriteLine(result1);

                MySqlCommand cmd2 = new MySqlCommand(sql_insert2, conn);
                int result2 = cmd2.ExecuteNonQuery();
                Console.WriteLine(result2);
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                
                string txtPath = "..\\..\\..\\rvtFiles/converted_files.txt";
                StreamWriter wlog;
                wlog = File.AppendText(txtPath);
                wlog.WriteLine("{0}", building_name);
                wlog.Flush();
                wlog.Close();
                conn.Close();
                Console.WriteLine("File " + doc.Title + " finished!");
            }
            doc.Close();
            /*
            _product?.Exit();
            */
        }
    }
}