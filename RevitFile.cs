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
using System.Drawing;

namespace Window
{
    public class RevitFileUtils
    {
        private const string MatchVersion = @"((?<=Autodesk Revit )20\d{2})|((?<=Format: )20\d{2})";
        /// <summary>
        /// 获取revit文件版本号[采用流方式]返回结果（eg:2018,2019）
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>返回结果（eg:2018,2019）</returns>
        /// 
        /// 
        private string filePath;
        private string fileVersion;
        private string building_name;
        public RevitFileUtils(string path, string b)
        {
            filePath = path;
            building_name = b;
        }
        public string getBuildingName()
        {
            return building_name;
        }
        public string getFilePath()
        {
            return filePath;
        }
        public string getFileVersion()
        {
            return fileVersion;
        }

        public void GetVersion()
        {

            var version = string.Empty;
            Encoding useEncoding = Encoding.Unicode;
            using (FileStream file = new FileStream(filePath, FileMode.Open))
            {
                //匹配字符有20个(最长的匹配字符串18版本的有20个)，为了防止分割对匹配造成的影响，需要验证20次偏移结果
                for (int i = 0; i < 20; i++)
                {
                    byte[] buffer = new byte[2000];
                    file.Seek(i, SeekOrigin.Begin);
                    while (file.Read(buffer, 0, buffer.Length) != 0)
                    {
                        var head = useEncoding.GetString(buffer);
                        Regex regex = new Regex(MatchVersion);
                        var match = regex.Match(head);
                        if (match.Success)
                        {

                            version = match.ToString();
                            fileVersion = version;
                        }
                    }
                }
            }
            fileVersion = version;
        }

    }
}
