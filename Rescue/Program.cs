using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.RevitAddIns;
using Autodesk.Revit.DB.Architecture;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using IronPython.Runtime;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Window;

namespace ProceeRvtFile
{
    class Program
    {
        public static bool nameInCSV(string buildingName)
        {
            string strpath = "..\\..\\..\\rvtFiles/converted_files.txt";

            StreamReader mysr = new StreamReader(strpath, System.Text.Encoding.Default);

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
        public static bool checkFile(string filename)
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
                    if ((int)c == 46)
                    {
                        break;
                    }
                    buildingName += c;
                }
                if (nameInCSV(buildingName))
                {
                    return false;
                }
            }
            return true;
        }

        static String folderPath = "..\\..\\..\\rvtFiles";

        [STAThread]
        static void Main(string[] args)
        {
            string str = System.AppDomain.CurrentDomain.BaseDirectory;
            Console.WriteLine(str);
            Console.WriteLine(folderPath);
            DirectoryInfo mydir = new DirectoryInfo(folderPath);
            List files = new List();
            foreach (FileSystemInfo fsi in mydir.GetFileSystemInfos())
            {
                string extension = Path.GetExtension(fsi.ToString());
                if (checkFile(fsi.ToString()))
                {
                    Console.WriteLine(fsi);
                    string building_name = "";
                    foreach (char c in fsi.ToString())
                    {
                        if ((int)c == 46)
                        {
                            break;
                        }
                        building_name += c;
                    }
                    RevitFileUtils file = new RevitFileUtils(folderPath + "/" + fsi.ToString(), building_name);
                    files.Add(file);
                    /*
                    file.GetVersion();
                    if (file.getFileVersion() == "2015" || file.getFileVersion() == "2016" || file.getFileVersion() == "2017" || file.getFileVersion() == "2018" || file.getFileVersion() == "2019" || file.getFileVersion() == "2020")
                    {
                        files.Add(file);
                    }
                    */
                }
            }
            foreach (RevitFileUtils f in files)
            {
                Console.WriteLine(f.getFilePath());
            }
            revitFile r = new revitFile(files);
            r.convertRevitFiles();
            /*
            revitFile r = new revitFile(filePath);
            r.convertRevitFile();
            */
        }
    }
}
