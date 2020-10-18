# FireRevit

![Revit API](https://img.shields.io/badge/Revit%20API-2020-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-lightgray.svg)
![.NET](https://img.shields.io/badge/.NET-4.7-blue.svg)

Revit secondary development project.  
Using Revit Files to Identify the Location of Fire Danger and Escape Routes.  
Under the guidance of Prof.Dennis Shasha.  

Please refer to [NYU technical report](https://cs.nyu.edu/dynamic/reports/) for more information. Look down the page a little.


## <a name="versions"></a> Versions

The most up-to-date version provided here is for Revit 2019/2020.


## Install

The IDE we used is Visual Studio(VS), So the installation shown here are based on VS.  
If you use another IDE, you can just regard the part related to VS as a reference.  

Please install Revit 2019/2020 and MySQL on the machine.  
FireRevit requires python version >= 3.5, packages of PyMySQL (v0.9.3), matplotlib (v3.1.1), ironPython (v2.7.10) and MySql.Data (v8.0.21).  
Besides, please add the references of:  
• RevitAPI.dll  
• RevitAPIUI.dll  
• RevitNET.dll  
• RevitAddInUtility.dll (the property of Copy Local should be set to True)  
Find these assemblies in the installation directory of Revit.  

## Author

Implemented by Luhan Sheng and Dennis Shasha.

Maintained by Luhan Sheng.

## Contact

wc36170565@gmail.com

## License

This project is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT).
Please see the [LICENSE](LICENSE) file for full details.
