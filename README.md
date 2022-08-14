# Disk Gazer

<div align="center"><img src="docs/images/gazer.svg" width=160 height=160></div>

Disk Gazer is a high-definition disk measuring tool which can measure the transfer rates of a physical disk in a small unit and at any location in the disk.

The original aim was to look into waves in hard disk drives. See [What Wave In Your Drive?](http://emoacht.github.io/DiskGazer/)

:book: [English](https://emoacht.github.io/DiskGazer/readme_en.html) | :book: [Japanese](https://emoacht.github.io/DiskGazer/readme_jp.html)
-|-

## Requirements

 * .NET Framework 4.8

## Download

:floppy_disk: [Latest release](https://github.com/emoacht/DiskGazer/releases/latest) 

## Development

This app consists of a combination of WPF app developed in C# and Win32 console app developed in C++. This console app is used by default to measure the transfer rates of disks in view of avoiding overhead of platform invoke (no significant difference though).

## History

Ver 1.2.0 2022-8-14

 - Add NVMe to storage bus types
 - Improve Per-Monitor DPI awareness
 - Change target framework to .NET Framework 4.8 

Ver 1.1.0 2015-8-22

 - Changed target framework to .NET Framework 4.5.2

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

## License

 - MIT License

## Other

 - Menu icons: [Visual Studio Image Library](http://msdn.microsoft.com/en-us/library/ms246582.aspx)

## Developer

 - emoacht (emotom[atmark]pobox.com)
 