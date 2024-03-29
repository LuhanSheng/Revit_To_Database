﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
using MySql.Data.MySqlClient;

namespace RevitToDatabase
{
    [Transaction(TransactionMode.Manual)]

    class ConvertCommand : IExternalCommand
    {
        #region Constants
        static string CONVERTED_FILES_PATH = @"rvtFiles\converted_files.txt";
        static string ROOM_FINDER_SCRIPT_FILE = "room_finder.py";
        #endregion

        #region Constructors
        static ConvertCommand()
        {
            assemblyLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }
        #endregion

        #region IExternalCommand members
        /// <summary>
        /// Executes Refresh the Unit command within Revit.
        /// </summary>
        /// <param name="commandData">An ExternalCommandData object which contains reference to Application and View needed by external command.</param>
        /// <param name="message">Error message can be returned by external command. This will be displayed only if the command status was "Failed".
        /// There is a limit of 1023 characters for this message; strings longer than this will be truncated.</param>
        /// <param name="elements">Element set indicating problem elements to display in the failure dialog.
        /// This will be used only if the command status was "Failed".</param>
        /// <returns>Execution result.</returns>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            foreach (RevitFileUtils file in GetRevitFiles())
            {
                Console.WriteLine(file.getFilePath());
                Document document = null;
                try
                {
                    document = commandData.Application.Application.OpenDocumentFile(file.getFilePath());
                    ConvertRevitFile(document, file.getBuildingName());
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    document?.Close(false);
                    document?.Dispose();
                }
            }

            return Result.Succeeded;
        }
        #endregion

        #region Private logic
        private List GetRevitFiles()
        {
            Console.WriteLine(assemblyLocation);
            DirectoryInfo mydir = new DirectoryInfo(assemblyLocation);
            List files = new List();
            foreach (FileSystemInfo fsi in mydir.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                if (CheckFile(fsi.ToString()))
                {
                    Console.WriteLine(fsi);
                    string building_name = "";
                    foreach (char c in fsi.ToString())
                    {
                        if (c == 46)
                        {
                            break;
                        }
                        building_name += c;
                    }
                    RevitFileUtils file = new RevitFileUtils(fsi.FullName, building_name);

                    files.Add(file);
                    /*
                     * file.GetVersion();
                    if (file.getFileVersion() == "2015" || file.getFileVersion() == "2016" || file.getFileVersion() == "2017" || file.getFileVersion() == "2018" || file.getFileVersion() == "2019" || file.getFileVersion() == "2020")
                    {
                        files.Add(file);
                    }
                    */
                }
            }

            return files;
        }

        private static void ConvertRevitFile(Document doc, string buildingName)
        {
            Console.WriteLine("RVT FILE OPENED");

            ScriptRuntime pyRumTime = Python.CreateRuntime();
            dynamic obj = pyRumTime.UseFile(Path.Combine(assemblyLocation, ROOM_FINDER_SCRIPT_FILE));
            Console.WriteLine(obj.welcome("Test function in my Python"));

            ElementCategoryFilter siteCategoryfilter = new ElementCategoryFilter(BuiltInCategory.OST_ProjectBasePoint);

            FilteredElementCollector collector = new FilteredElementCollector(doc);

            SiteLocation site = doc.SiteLocation;
            const double angleRatio = Math.PI / 180;
            string prompt = "\nCurrent project's Site location information:";
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

            FilteredElementCollector windowCollector = new FilteredElementCollector(doc);

            var windows = windowCollector.OfCategory(BuiltInCategory.OST_Windows).OfClass(typeof(FamilyInstance)).ToArray();

            FilteredElementCollector levelCollector = new FilteredElementCollector(doc);
            var levels = levelCollector.OfClass(typeof(Level)).ToArray();
            double elevation = (double)bias[2];
            foreach (Level l in levels)
            {
                int floor_number = obj.getDigit(l.Name);
                if (floor_number == 1)
                {
                    elevation = l.Elevation;
                }
            }
            double first_floor_height = elevation - (double)bias[2];

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

                XYZ windowLocationXyz = (window.Location as LocationPoint).Point;
                windowLocations.Add(buildingName);
                windowLocations.Add(result.Name + " | " + result.Level.Name);
                windowLocations.Add(windowLocationXyz.X);
                windowLocations.Add(windowLocationXyz.Y);
                windowLocations.Add(windowLocationXyz.Z - first_floor_height);
                windowData.Add(windowLocations);
            }

            string connetStr = "server=" + config.host + ";port= " + config.port + ";user=" + config.USER + ";password=" + config.PASSWORD + "; database=" + config.DATABASE + ";";
            MySqlConnection conn = new MySqlConnection(connetStr);
            try
            {
                conn.Open();
            }
            catch (MySqlException ex)
            {
                if (ex.Message == "Unable to connect to any of the specified MySQL hosts.")
                {
                    Console.WriteLine("Creat connection");
                }
                else if (ex.Message == "Authentication to host '127.0.0.1' for user 'root' using method 'caching_sha2_password' failed with message: Unknown database 'window'")
                {
                    string connetStr1 = "server=" + config.host + ";port=" + config.port + ";user=" + config.USER + ";password=" + config.PASSWORD + ";";
                    MySqlConnection conn1 = new MySqlConnection(connetStr1);
                    conn1.Open();
                    string sql_createDatabase = "CREATE SCHEMA `" + config.DATABASE + "` ";
                    MySqlCommand cmd = new MySqlCommand(sql_createDatabase, conn1);
                    int result = cmd.ExecuteNonQuery();

                    string sql_caeateTable = "CREATE TABLE `" + config.DATABASE + "`.`window` (" +
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
                    Console.WriteLine("Create database");
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

                List basePointLocation = new List();
                for (int i = 0; i < 5; i++)
                {
                    basePointLocation.Add(0);
                }
                windowData.Add(basePointLocation);

                foreach (List window in windowData)
                {
                    List point = new List();
                    List target_point = new List();
                    target_point.Add(window[2]);
                    target_point.Add(window[3]);
                    target_point.Add(window[4]);
                    List r = obj.getCoordinate(survey_point, target_point, bias);
                    if (window[0].ToString() != "0" && window[1].ToString() != "0")
                    {
                        String sql_data = "('" + window[0] + "', '" + window[1] + "', '" + r[0] + "', '" + r[1] + "', '" + r[2] + "');";
                        sql_insert += "INSERT INTO `window` (`building_name`, `room_name`, `latitude`, `longitude`, `height`) VALUES " + sql_data;
                    }
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
                    b1 = -(a1 * r_lat_small);
                    b2 = -(a1 * r_long_small);
                    int index = 0;
                    foreach (List poi in draw)
                    {
                        index = index + 1;
                        if (index == draw.Count)
                        {
                            double y = 1000 - ((double)poi[0] * a1 + b1 + 50);
                            double x = (double)poi[1] * a1 + b2 + 50;

                            Brush br = new SolidBrush(System.Drawing.Color.Red);
                            System.Drawing.Rectangle rec = new System.Drawing.Rectangle((int)x - 8, (int)y - 8, 16, 16);
                            graphics.FillRectangle(br, rec);
                            break;
                        }
                        if ((double)poi[0] != (double)survey_point[0] && (double)poi[1] != (double)survey_point[1])
                        {
                            double y = 1000 - ((double)poi[0] * a1 + b1 + 50);
                            double x = (double)poi[1] * a1 + b2 + 50;
                            System.Drawing.Rectangle rec = new System.Drawing.Rectangle((int)x - 4, (int)y - 4, 8, 8);
                            graphics.FillRectangle(b, rec);
                        }
                    }
                    double y_base = 1000 - ((double)survey_point[0] * a1 + b1 + 50);
                    double x_base = (double)survey_point[1] * a1 + b2 + 50;


                    Brush r = new SolidBrush(System.Drawing.Color.Red);
                    System.Drawing.Rectangle rec_base = new System.Drawing.Rectangle((int)x_base - 8, (int)y_base - 8, 16, 16);
                    graphics.FillRectangle(r, rec_base);
                }
                else
                {
                    a1 = 900 / long_difference;
                    b1 = -(a1 * r_long_small);
                    b2 = -(a1 * r_lat_small);
                    int index = 0;
                    foreach (List poi in draw)
                    {

                        index = index + 1;
                        if (index == draw.Count)
                        {
                            double yb = 1000 - ((double)poi[0] * a1 + b2 + 50);
                            double xb = (double)poi[1] * a1 + b1 + 50;

                            Brush br = new SolidBrush(System.Drawing.Color.Red);
                            System.Drawing.Rectangle recb = new System.Drawing.Rectangle((int)xb - 8, (int)yb - 8, 16, 16);
                            graphics.FillRectangle(br, recb);
                            break;
                        }
                        double y = 1000 - ((double)poi[0] * a1 + b2 + 50);
                        double x = (double)poi[1] * a1 + b1 + 50;
                        System.Drawing.Rectangle rec = new System.Drawing.Rectangle((int)x - 4, (int)y - 4, 8, 8);
                        graphics.FillRectangle(b, rec);
                    }
                    double y_base = 1000 - ((double)survey_point[0] * a1 + b2 + 50);
                    double x_base = (double)survey_point[1] * a1 + b1 + 50;

                    Brush r = new SolidBrush(System.Drawing.Color.Red);
                    System.Drawing.Rectangle rec_base = new System.Drawing.Rectangle((int)x_base - 8, (int)y_base - 8, 16, 16);
                    graphics.FillRectangle(r, rec_base);
                }
                myimage.Save("..\\..\\..\\images/" + buildingName + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);


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
                string txtPath = Path.Combine(assemblyLocation, CONVERTED_FILES_PATH);

                StreamWriter wlog;
                wlog = File.AppendText(txtPath);
                wlog.WriteLine("{0}", buildingName);
                wlog.Flush();
                wlog.Close();
                conn.Close();
                Console.WriteLine("File " + doc.Title + " finished!");
            }
            ////doc.Close();
            /*
            _product?.Exit();
            */
        }

        public static bool CheckFile(string filename)
        {
            string extension = Path.GetExtension(filename);
            if (extension != ".rvt")
            {
                return false;
            }
            else
            {
                string buildingName = "";
                foreach (char c in filename)
                {
                    if (c == 46)
                    {
                        break;
                    }

                    buildingName += c;
                }

                if (NameInCSV(buildingName))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool NameInCSV(string buildingName)
        {
            var path = Path.Combine(assemblyLocation, CONVERTED_FILES_PATH);
            StreamReader mysr = new StreamReader(path, System.Text.Encoding.Default);

            string strline;
            while ((strline = mysr.ReadLine()) != null)
            {
                if (buildingName == strline)
                {
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Fields
        private static string assemblyLocation;
        #endregion
    }
}