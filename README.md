# FireRevit

![Revit API](https://img.shields.io/badge/Revit%20API-2019-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-lightgray.svg)
![.NET](https://img.shields.io/badge/.NET-4.7-blue.svg)

Latest Note(5/12/2024):  
This project consists of two sub-projects:   
1. Get some window data from the Revit file(corresponding to folder Data converting and Room finding).  
2. Get more kinds of data from the Revit file and compute an escape route(corresponding to folder Rescue and Path finding).  
  
The first step of these two projects is the same: using the same way to get the data from the Revit file.  
Due to authorization problems, the Autodesk team removed and modified some code in the first sub-project. So basically, the first sub-project(get window data) can not be run as mentioned in the tech report.  

However, the second sub-project remains the same and should be working correctly.  
You can try to run the second project as their basic structure is almost the same, the difference is that the second sub-project has several more steps that will not directly store the data we get but will do some more computations and then store it as we mentioned(so it is more complicated than the first one and I think you will probably get some inspiration).  

--------------------------------------------------------------------------------------  
Revit secondary development project.  
Using Revit Files to Identify the Location of Fire Danger and Escape Routes.  
Under the guidance of Prof. Dennis Shasha.  

Please refer to [NYU technical report](https://cs.nyu.edu/media/publications/RevitToDatabase.pdf) for more information.


## <a name="versions"></a> Versions

The most up-to-date version provided here is for Revit 2019.


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
