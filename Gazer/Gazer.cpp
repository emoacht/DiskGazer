// Entry point of console application

#include "stdafx.h"
#include <windows.h>
#include <tchar.h>
#include <iostream>
#include <string>

using namespace std;

int _tmain(int argc, _TCHAR* argv[])
{
	int physicalDrive = 0;    // Index number in PhysicalDrive
	int blockSize     = 1024; // Block size    (KiB)
	int blockOffset   = 0;    // Block offset  (KiB)
	int areaSize      = 1024; // Area size     (MiB)
	int areaLocation  = 0;    // Area location (MiB)
	int areaRatioInner = 16;  // Area ratio inner (numerator)
	int areaRatioOuter = 16;  // Area ratio outer (denominator)

	// ----------------
	// Check arguments.
	// ----------------
	switch (argc) // argv[0] is always empty.
	{
	case 2:
		if (_tcscmp(argv[1], _T("/?")) == 0)
		{
			cout << "Gazer syntax:" << endl;
			cout << "gazer [physical drive] [block size] [block offset] [area size] [area location]" << endl;
			cout << "- Unit of block size and block offset is KiB." << endl;
			cout << "- Unit of area size and area location is MiB." << endl;
			cout << "- Block size must be a power of 2 and no more than 1024." << endl;
			cout << "- Block offset must be no more than 1024." << endl;
			cout << "- Gazer requires administrator privilege." << endl;
		}
		return 0;
		break;

	case 6:
		physicalDrive = _tcstol(argv[1], NULL, 10);
		blockSize     = _tcstol(argv[2], NULL, 10);
		blockOffset   = _tcstol(argv[3], NULL, 10);
		areaSize      = _tcstol(argv[4], NULL, 10);
		areaLocation  = _tcstol(argv[5], NULL, 10);

		cout << "physical drive : " << physicalDrive << endl;
		cout << "block size     : " << blockSize << endl;
		cout << "block offset   : " << blockOffset << endl;
		cout << "area size      : " << areaSize << endl;
		cout << "area location  : " << areaLocation << endl;
		break;

	case 8:
		physicalDrive = _tcstol(argv[1], NULL, 10);
		blockSize = _tcstol(argv[2], NULL, 10);
		blockOffset = _tcstol(argv[3], NULL, 10);
		areaSize = _tcstol(argv[4], NULL, 10);
		areaLocation = _tcstol(argv[5], NULL, 10);
		areaRatioInner = _tcstol(argv[6], NULL, 10);
		areaRatioOuter = _tcstol(argv[7], NULL, 10);

		cout << "physical drive   : " << physicalDrive << endl;
		cout << "block size       : " << blockSize << endl;
		cout << "block offset     : " << blockOffset << endl;
		cout << "area size        : " << areaSize << endl;
		cout << "area location    : " << areaLocation << endl;
		cout << "area ratio inner : " << areaRatioInner << endl;
		cout << "area ratio outer : " << areaRatioOuter << endl;
		break;
	}

	string message = "";

	// Check physical drive.
	if (physicalDrive < 0)
	{
		message += "Invalid physical drive. ";
	}

	// Check block size.
	if ((blockSize <= 0) |
		(1024 < blockSize) |
		(1024 % blockSize != 0))
	{
		message += "Invalid block size. ";
	}

	// Check block offset.
	if ((blockOffset < 0) |
		(1024 < blockOffset))
	{
		message += "Invalid block offset. ";
	}

	// Check area size.
	if (areaSize <= 0)
	{
		message += "Invalid area size. ";
	}

	// Check area location.
	if (areaLocation < 0)
	{
		message += "Invalid area location. ";
	}

	// Check area ratio.
	if ((areaRatioInner < 0) || (areaRatioOuter < 0) || (areaRatioInner > areaRatioOuter))
	{
		message += "Invalid area ratio. ";
	}
	
	if (!message.empty())
	{
		cout << message << endl;
		return 1;
	}

	// ----------
	// Read disk.
	// ----------
	// This section is based on sequential read test of CrystalDiskMark (3.0.2)
	// created by hiyohiyo (http://crystalmark.info/).

	// Get handle to disk.
	TCHAR physicalDrivePath[32];

	_stprintf_s(physicalDrivePath, _T("\\\\.\\PhysicalDrive%d"), physicalDrive);

	HANDLE hFile = ::CreateFile(
		physicalDrivePath,
		GENERIC_READ, // Administrative privilege is required.
		0,
		NULL,
		OPEN_EXISTING,
		FILE_ATTRIBUTE_NORMAL | FILE_FLAG_NO_BUFFERING | FILE_FLAG_SEQUENTIAL_SCAN,
		NULL);

	if (hFile == INVALID_HANDLE_VALUE)
	{
		_tprintf_s(_T("Failed to get handle to disk (Code: %d)."), ::GetLastError());
		return 1;
	}

	// Prepare parameters.
	int areaSizeActual = areaSize; // Area size for actual reading
	if (0 < blockOffset)
	{
		areaSizeActual -= 1; // 1 is for the last MiB of area. If offset, it may exceed disk size.
	}

	long readNum = (areaSizeActual * 1024) / blockSize; // The number of reads

	int loopOuter = 1; // The number of outer loops
	int loopInner = readNum; // The number of inner loops

	if (areaRatioInner < areaRatioOuter)
	{
		loopOuter = (areaSizeActual * 1024) / (blockSize * areaRatioOuter);
		loopInner = areaRatioInner;

		readNum = loopInner * loopOuter;
	}

	LARGE_INTEGER areaLocationBytes; // Bytes
	areaLocationBytes.QuadPart = areaLocation;
	areaLocationBytes.QuadPart *= 1024;
	areaLocationBytes.QuadPart *= 1024;

	LARGE_INTEGER blockOffsetBytes; // Bytes
	blockOffsetBytes.QuadPart = blockOffset;
	blockOffsetBytes.QuadPart *= 1024;

	LARGE_INTEGER jumpBytes; // Bytes
	jumpBytes.QuadPart = blockSize;
	jumpBytes.QuadPart *= areaRatioOuter;
	jumpBytes.QuadPart *= 1024;

	areaLocationBytes.QuadPart += blockOffsetBytes.QuadPart;

	int bufSize = blockSize * 1024; // Buffer size (Bytes)
	char* buf = (char*) VirtualAlloc(NULL, bufSize, MEM_COMMIT, PAGE_READWRITE); // Buffer
	DWORD readSize;

	// Check high-resolution performance counter.
	LARGE_INTEGER frq;
	if (!QueryPerformanceFrequency(&frq))
	{
		_tprintf_s(_T("Can not use high-resolution performance counter."));
		return 1;
	}

	LARGE_INTEGER* lapTime;
	lapTime = new LARGE_INTEGER[readNum + 1]; // 1 is for starting time.
	QueryPerformanceCounter(&lapTime[0]); // Starting time

	for (int i = 0; i < loopOuter; i++)
	{
		if (0 < i)
		{
			areaLocationBytes.QuadPart += jumpBytes.QuadPart;
		}

		// Move pointer.
		BOOL result1 = ::SetFilePointerEx(
			hFile,
			areaLocationBytes,
			NULL,
			FILE_BEGIN);

		if (result1 == false)
		{
			_tprintf_s(_T("Failed to move pointer (Code: %d)."), ::GetLastError());
			return 1;
		}

		// Measure disk transfer rate (sequential read).
		for (int j = 1; j <= loopInner; j++)
		{
			BOOL result2 = ::ReadFile(
				hFile,
				buf,
				bufSize,
				&readSize,
				NULL);

			if (result2 == false)
			{
				_tprintf_s(_T("Failed to measure disk transfer rate (Code: %d)."), ::GetLastError());
				return 1;
			}

			QueryPerformanceCounter(&lapTime[i * loopInner + j]);
		}
	}

	VirtualFree(buf, bufSize, MEM_DECOMMIT);
	buf = NULL;

	CloseHandle(hFile);

	// ----------------
	// Process results.
	// ----------------
	// Calculate each transfer rate.
	double* data;
	data = new double[readNum];

	for (int i = 1; i <= readNum; i++)
	{
		double timeEach = (double)(lapTime[i].QuadPart - lapTime[i - 1].QuadPart) / frq.QuadPart; // Second
		double scoreEach = floor(bufSize / timeEach) / 1000000.0; // MB/s

		data[i - 1] = scoreEach;
	}

	// Calculate total transfer rate (just for reference).
	double totalTime = (double)(lapTime[readNum].QuadPart - lapTime[0].QuadPart) / frq.QuadPart; // Second
	double totalRead = (double)blockSize * (double)readNum * 1024.0; // Bytes

	double totalScore = floor(totalRead / totalTime) / 1000000.0; // MB/s

	delete[] lapTime;
	lapTime = NULL;

	// Show outcome.
	_tprintf_s(_T("[Start data]\n"));

	int k = 0;
	for (int i = 0; i < readNum; i++)
	{
		_tprintf_s(_T("%0.6f "), data[i]); // Data have 6 decimal places.

		k++;
		if ((k == 6) |
			(i == readNum - 1))
		{
			k = 0;
			_tprintf_s(_T("\n"));
		}
	}

	_tprintf_s(_T("[End data]\n"));
	_tprintf_s(_T("Total %0.6f MB/s"), totalScore);

	delete[] data;
	data = NULL;

	return 0;
}