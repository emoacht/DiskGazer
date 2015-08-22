Disk Gazer
==========

Disk Gazer is a benchmark tool specialized for measuring transfer rate of physical disk in a small unit and at any location.

##Requirements

 * Windows 7 or newer
 * .NET Framework 4.5.2

##Development

This app consists of a WPF app developed in C# and a Win32 console app developed in C++ with Visual Studio Professional 2013.

The logic for sequential read test is based on that of [CrystalDiskMark][1] (3.0.2) created by hiyohiyo.

##History

Ver 1.1.0 2015-8-22

 - Changed target framework to 4.5.2

Ver 1.0.1 2015-6-14

 - Fixed possible exception

Ver 1.0.0 2015-3-31

 - First decent release

Ver 0.4.5 2014-09-19

 - Refactoring

Ver 0.4.4 2014-06-18

 - Enabled cancellation of read operation
 - Improved analyze operation

Ver 0.4.2 2014-06-12

 - Refactoring

Ver 0.4.0 2014-04-26

 - Added area ratio function

Ver 0.3.1 2014-04-25

 - Refactoring

Ver 0.3.0 2014-04-21

 - Modified internal code drastically
 - Removed the limitation of area size

Ver 0.2.1 2013-03-17

 - Initial release

##License

 - MIT License

##Other

 - Library: [WPF Monitor Aware Window][2]
 - Menu icons: [Visual Studio Image Library][3]

[1]: http://crystalmark.info/
[2]: https://github.com/emoacht/WpfMonitorAware
[3]: http://msdn.microsoft.com/en-us/library/ms246582.aspx
