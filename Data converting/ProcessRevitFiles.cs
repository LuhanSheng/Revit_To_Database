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

namespace Window
{
    public class drawPicture
    {

    }
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

            //在安装路径中查找相关dll并加载
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
        public void convertRevitFiles()
        {
            Product _product = Product.GetInstalledProduct();

            var clientId = new ClientApplicationId(Guid.NewGuid(), "LK", "BIMAPI");

            //"I am authorized by Autodesk to use this UI-less functionality."只能为该字符串
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
            /*
             * OfClass(typeof(Wall))
             * 1507_DREXEL PSLAMS_CENTRAL_190327.rvt
             * three_layers.rvt
             */
            ScriptRuntime pyRumTime = Python.CreateRuntime();
            dynamic obj = pyRumTime.UseFile("..\\..\\..\\room_finder.py");
            Console.WriteLine(obj.welcome("Test function in my Python"));

            ElementCategoryFilter siteCategoryfilter = new ElementCategoryFilter(BuiltInCategory.OST_ProjectBasePoint);

            FilteredElementCollector collector = new FilteredElementCollector(doc);

            SiteLocation site = doc.SiteLocation;
            const double angleRatio = Math.PI / 180;
            String prompt = "\nCurrent project's Site location information:";
            prompt += "\n\t" + "Latitude: " + site.Latitude / angleRatio;
            prompt += "\n\t" + "Longitude: " + site.Longitude / angleRatio;
            prompt += "\n\t" + "TimeZone: " + site.TimeZone;
            Console.WriteLine(prompt);

            List survey_point = new List();
            survey_point.Add(site.Latitude / angleRatio);
            survey_point.Add(site.Longitude / angleRatio);

            IList<Element> siteElements = collector.WherePasses(siteCategoryfilter).ToElements();

            List bias = new List();
            foreach (BasePoint ele in siteElements)
            {
                var px = ele.get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM).AsDouble();
                var py = ele.get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM).AsDouble();
                var pz = ele.get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM).AsDouble();
                var pa = ele.get_Parameter(BuiltInParameter.BASEPOINT_ANGLETON_PARAM).AsDouble();

                Console.WriteLine("\nCurrent project's Base point information:");
                Console.WriteLine("\teast/west: " + px);
                Console.WriteLine("\tnorth/south: " + py);
                Console.WriteLine("\televation: " + pz);
                Console.WriteLine("\tAngle to north: " + pa);
                XYZ projectBasePoint = new XYZ(px, py, pz);

                bias.Add(px);
                bias.Add(py);
                bias.Add(pz);
                bias.Add(pa);
            }

            Console.WriteLine("--------------------------------------------------------------------------------------------");
            FilteredElementCollector windowCollector = new FilteredElementCollector(doc);

            var windows = windowCollector.OfCategory(BuiltInCategory.OST_Windows).OfClass(typeof(FamilyInstance)).ToArray();
            Console.WriteLine(windows);
            List<List> windowData = new List<List>();
            foreach (FamilyInstance window in windows)
            {
                List windowLocations = new List();
                Room room = window.Room;
                Room room1 = window.FromRoom;
                Room room2 = window.ToRoom;
                List rooms = new List();
                rooms.Add(room2);
                rooms.Add(room1);
                rooms.Add(room);

                Room result = null;
                foreach (Room r in rooms)
                {
                    if (r != null)
                    {
                        result = r;
                    }
                }
                if (result == null)
                {
                    continue;
                }

                Console.WriteLine(result);
                Console.WriteLine(window.Location);
                XYZ windowLocationXyz = (window.Location as LocationPoint).Point;
                Console.WriteLine(windowLocationXyz);
                windowLocations.Add(building_name);
                windowLocations.Add(result.Name);
                windowLocations.Add(windowLocationXyz.X);
                windowLocations.Add(windowLocationXyz.Y);
                windowLocations.Add(windowLocationXyz.Z);
                windowData.Add(windowLocations);
            }
            /*
            obj.storeWindowData(windowData);
            */
            String connetStr = "server=127.0.0.1;port=3306;user=root;password=0106259685; database=window;";
            MySqlConnection conn = new MySqlConnection(connetStr);
            try
            {
                conn.Open();
            }
            catch (MySqlException ex)
            {
                if (ex.Message == "Unable to connect to any of the specified MySQL hosts.")
                {
                    Console.WriteLine("创建连接");
                }
                else if (ex.Message == "Authentication to host '127.0.0.1' for user 'root' using method 'caching_sha2_password' failed with message: Unknown database 'window'")
                {
                    String connetStr1 = "server=127.0.0.1;port=3306;user=root;password=0106259685";
                    MySqlConnection conn1 = new MySqlConnection(connetStr1);
                    conn1.Open();
                    string sql_createDatabase = "CREATE SCHEMA `window` ";
                    MySqlCommand cmd = new MySqlCommand(sql_createDatabase, conn1);
                    int result = cmd.ExecuteNonQuery();

                    string sql_caeateTable = "CREATE TABLE `window`.`window` (" +
                                             "`idwindow` INT(11) NOT NULL AUTO_INCREMENT," +
                                             "`building_name` VARCHAR(200) NOT NULL," +
                                             "`room_name` VARCHAR(100) NOT NULL," +
                                             "`latitude` DOUBLE NOT NULL," +
                                             "`longitude` DOUBLE NOT NULL," +
                                             "`height` DOUBLE NOT NULL," +
                                             "PRIMARY KEY(`idwindow`))";
                    MySqlCommand cmd1 = new MySqlCommand(sql_caeateTable, conn1);
                    int result1 = cmd1.ExecuteNonQuery();

                    Console.WriteLine(result1);
                    Console.WriteLine("创建数据库");
                }
            }
            finally
            {
                conn.Close();
            }

            try
            {
                conn.Open();//打开通道，建立连接，可能出现异常,使用try catch语句
                Console.WriteLine("已经建立连接");
                //在这里使用代码对数据库进行增删查改
                string sql = "INSERT INTO `window` (`building_name`, `room_name`, `lat`, `long`, `height`) VALUES ('three layers', 'MECHANIC ROOM', '50.5656', '20.0001', '20.55')";
                string sql_insert = "";

                Image myimage = new Bitmap(1000, 1000);
                Graphics graphics = Graphics.FromImage(myimage);
                graphics.Clear(System.Drawing.Color.White);
                Brush b = new SolidBrush(System.Drawing.Color.Black);
                Pen p = new Pen(b, 1);

                List draw = new List();
                draw.Add(survey_point);
                double r_lat_big = -200;
                double r_long_big = -200;
                double r_lat_small = 200;
                double r_long_small = 200;
                foreach (List window in windowData)
                {
                    List point = new List();
                    List target_point = new List();
                    target_point.Add(window[2]);
                    target_point.Add(window[3]);
                    target_point.Add(window[4]);
                    List r = obj.getCoordinate(survey_point, target_point, bias);
                    String sql_data = "('" + window[0] + "', '" + window[1] + "', '" + r[0] + "', '" + r[1] + "', '" + r[2] + "');";
                    sql_insert += "INSERT INTO `window` (`building_name`, `room_name`, `latitude`, `longitude`, `height`) VALUES " + sql_data;

                    if (r_lat_big < (double)r[0])
                    {
                        r_lat_big = (double)r[0];
                    }
                    if (r_long_big < (double)r[1])
                    {
                        r_long_big = (double)r[1];
                    }
                    if (r_lat_small > (double)r[0])
                    {
                        r_lat_small = (double)r[0];
                    }
                    if (r_long_small > (double)r[1])
                    {
                        r_long_small = (double)r[1];
                    }

                    point.Add(r[0]);
                    point.Add(r[1]);
                    draw.Add(point);
                }
                double lat_difference = r_lat_big - r_lat_small;
                double long_difference = r_long_big - r_long_small;
                double a1;
                double b1;
                double b2;
                if (lat_difference > long_difference)
                {
                    a1 = 900 / lat_difference;
                    Console.WriteLine("a1:" + a1);
                    b1 = -(a1 * r_lat_small);
                    Console.WriteLine("b1:" + b1);
                    b2 = -(a1 * r_long_small);
                    Console.WriteLine("b1:" + b2);
                    foreach (List poi in draw)
                    {
                        double y = 1000 - ((double)poi[0] * a1 + b1 + 50);
                        double x = (double)poi[1] * a1 + b2 + 50;
                        System.Drawing.Rectangle rec = new System.Drawing.Rectangle((int)x - 4, (int)y - 4, 8, 8);
                        Console.WriteLine((int)x);
                        Console.WriteLine((int)y);
                        graphics.FillRectangle(b, rec);
                    }
                    double y_base = 1000 - ((double)survey_point[0] * a1 + b2 + 50);
                    double x_base = (double)survey_point[1] * a1 + b1 + 50;
                    Brush r = new SolidBrush(System.Drawing.Color.Red);
                    System.Drawing.Rectangle rec_base = new System.Drawing.Rectangle((int)x_base - 8, (int)y_base - 8, 16, 16);
                    graphics.FillRectangle(r, rec_base);
                }
                else
                {
                    a1 = 900 / long_difference;
                    Console.WriteLine("a1:" + a1);
                    b1 = -(a1 * r_long_small);
                    Console.WriteLine("b1:" + b1);
                    b2 = -(a1 * r_lat_small);
                    Console.WriteLine("b1:" + b2);
                    foreach (List poi in draw)
                    {
                        double y = 1000- ((double)poi[0] * a1 + b2 + 50);
                        double x = (double)poi[1] * a1 + b1 + 50;
                        System.Drawing.Rectangle rec = new System.Drawing.Rectangle((int)x - 4, (int)y - 4, 8, 8);
                        Console.WriteLine((int)x);
                        Console.WriteLine((int)y);
                        graphics.FillRectangle(b, rec);
                    }
                    double y_base = 1000 - ((double)survey_point[0] * a1 + b2 + 50);
                    double x_base = (double)survey_point[1]* a1 + b1 + 50;
                    Console.WriteLine("x_base:" + x_base);
                    Console.WriteLine("y_base:" + y_base);
                    Brush r = new SolidBrush(System.Drawing.Color.Red);
                    System.Drawing.Rectangle rec_base = new System.Drawing.Rectangle((int)x_base - 8, (int)y_base - 8, 16, 16);
                    graphics.FillRectangle(r, rec_base);
                }
                
                Console.WriteLine("=======================================================");
                myimage.Save("..\\..\\..\\images/" + building_name + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

                MySqlCommand cmd = new MySqlCommand(sql_insert, conn);
                int result = cmd.ExecuteNonQuery();
                Console.WriteLine(result);
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
            }
            doc.Close();
            /*
            _product?.Exit();
            */
        }
    }
}
