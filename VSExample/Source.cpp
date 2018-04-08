#include <Windows.h>
#include <ostream>
#include <iostream>
#include <TlHelp32.h>
#include <tchar.h>
#include <Winternl.h>
#include <fstream>
#include <future>
#include <filesystem>
#include <Winternl.h>
#include <string>
#include <tuple>
#include <vector>
#include <memory>

#define RTN_OK 0
#define RTN_USAGE 1
#define RTN_ERROR 13

#define DEBUG
using namespace std;
HANDLE hVictimProcess;

VOID DbgPrint(const char *msg)
{

#ifdef DEBUG
	DWORD eMsgLen, errNum = GetLastError();
	LPTSTR lpvSysMsg;

	if (msg)
		printf("%s: ", msg);
	eMsgLen = FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER |
		FORMAT_MESSAGE_FROM_SYSTEM,
		NULL, errNum, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
		(LPTSTR)&lpvSysMsg, 0, NULL);
	if (eMsgLen > 0)
		_ftprintf(stderr, _T("%d %s\n"), errNum, lpvSysMsg);
	else
		_ftprintf(stderr, _T("Error %d\n"), errNum);
	if (lpvSysMsg != NULL)
		LocalFree(lpvSysMsg);
#endif
}

BOOL FindProcess(PCWSTR exeName, DWORD& pid, vector<DWORD>& tids) {
	auto hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS | TH32CS_SNAPTHREAD, 0);
	if (hSnapshot == INVALID_HANDLE_VALUE)
		return FALSE;

	pid = 0;

	PROCESSENTRY32 pe = { sizeof(pe) };
	if (::Process32First(hSnapshot, &pe)) {
		do {
			if (_wcsicmp(pe.szExeFile, exeName) == 0) {
				pid = pe.th32ProcessID;
				THREADENTRY32 te = { sizeof(te) };
				if (Thread32First(hSnapshot, &te)) {
					do {
						if (te.th32OwnerProcessID == pid) {
							tids.push_back(te.th32ThreadID);
						}
					} while (Thread32Next(hSnapshot, &te));
				}
				break;
			}
		} while (Process32Next(hSnapshot, &pe));
	}

	CloseHandle(hSnapshot);
	return pid > 0 && !tids.empty();
}

BOOL Dll_Injection(const wchar_t *funcarg, const wchar_t processname[])
{
	//TCHAR lpdllpath[MAX_PATH];
	//GetFullPathName(dll_name, MAX_PATH, lpdllpath, nullptr);

	/* Snapshot of processes */
	DWORD processId{};
	DbgPrint("[+] creating process snapshot");
	auto hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0); // Fucking lazy auto bullshit, also snap all processes
	if (hSnapshot == INVALID_HANDLE_VALUE)
	{
		DbgPrint("[!] failed to create process snapshot");
		return FALSE;
	}
	DbgPrint("[+] Created process snapshot\n\n");
	PROCESSENTRY32 pe{}; /* Describes an entry from a list of the processes residing
						 in the system address space when a snapshot was taken.
						 The size of the structure, in bytes. Before calling the
						 Process32First function, set this member to sizeof(PROCESSENTRY32).
						 If you do not initialize dwSize, Process32First fails. (msdn) */

	pe.dwSize = sizeof PROCESSENTRY32;
	/* MSDN:
	The size of the structure, in bytes. Before calling the Process32First
	function, set this member to sizeof(PROCESSENTRY32).
	If you do not initialize dwSize, Process32First fails.*/

	BOOL isProcessFound = FALSE;
	if (Process32First(hSnapshot, &pe) == FALSE)  //Get first "link" I guess
	{
		CloseHandle(hSnapshot);
		return FALSE;
	}

	if (_wcsicmp(pe.szExeFile, processname) == 0) // if pe.szExeFile and Processname are the same
	{
		CloseHandle(hSnapshot);
		processId = pe.th32ProcessID;
		isProcessFound = TRUE;
	}

	/* End get first PID */

	/* Get the rest and process like the first */
	while (Process32Next(hSnapshot, &pe))
	{
		if (_wcsicmp(pe.szExeFile, processname) == 0)
		{
			CloseHandle(hSnapshot);
			processId = pe.th32ProcessID;
			break;
		}
	}

	//Check if process was found
	if (isProcessFound)
	{
		return FALSE;
	}

	/* this portion get it and puts it in the memory of the remote process */
	// get size of the dll's path
	auto size = wcslen(funcarg) * sizeof(wchar_t);

	// open selected process
	hVictimProcess = OpenProcess(PROCESS_ALL_ACCESS, 0, processId);
	if (hVictimProcess == NULL) // check if process open failed
	{
		return FALSE;
	}

	// allocate memory in the remote process
	auto pNameInVictimProcess = VirtualAllocEx(hVictimProcess,
		nullptr,
		size,
		MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
	if (pNameInVictimProcess == NULL) //Check if allocation failed
	{
		return FALSE;
	}
	auto bStatus = WriteProcessMemory(hVictimProcess,
		pNameInVictimProcess,
		funcarg,
		size,
		nullptr);

	if (bStatus == 0)
	{
		return FALSE;
	}

	auto hKernel32 = GetModuleHandle(L"kernel32.dll");
	if (hKernel32 == NULL)
	{
		return FALSE;
	}

	auto LoadLibraryAddress = GetProcAddress(hKernel32, "LoadLibraryW");
	if (LoadLibraryAddress == NULL) //Check if GetProcAddress works; if not try some ugly as sin correction code
	{
		if ((LoadLibraryAddress = GetProcAddress(hKernel32, "LoadLibraryA")) == NULL)
		{
			return FALSE;
		}
	}

	// Using the above objects execute the DLL in the remote process
	auto hThreadId = CreateRemoteThread(hVictimProcess,
		nullptr,
		0,
		reinterpret_cast<LPTHREAD_START_ROUTINE>(LoadLibraryAddress),
		pNameInVictimProcess,
		NULL,
		nullptr);
	if (hThreadId == NULL)
	{
		return FALSE;
	}

	WaitForSingleObject(hThreadId, INFINITE);

	//CloseHandle(hVictimProcess);
	VirtualFreeEx(hVictimProcess, pNameInVictimProcess, size, MEM_RELEASE);

	return TRUE;
}

typedef HINSTANCE(CALLBACK* DLL_HWND)();
int main()
{
    // log file path
    const char* logfile = "C:\\Users\\Drinker\\Desktop\\tmp2.txt";
	
    // load dll to get hinstanse
	HMODULE dll = LoadLibraryW(L"winmsg_catcher.dll");
	if (dll == NULL) {
		printf("The DLL could not be found.\n");
		getchar();
		return -1;
	}    
	DLL_HWND addr = (DLL_HWND)GetProcAddress(dll, "getDllHinstance");
	if (addr == NULL) {
		printf("The function was not found.\n");
		getchar();
		return -1;
	}
    HINSTANCE mHandle = addr();
    
    // get adress of function
	LPVOID LoadLibAddress = (LPVOID)GetProcAddress(mHandle, "SetMsgHook");

    // inject dll to the notepad
    if (!Dll_Injection(L"C:\\Users\\Drinker\\Source\\Repos\\winmsg_catcher\\x64\\Debug\\winmsg_catcher.dll", L"notepad.exe"))
        return 0;

    // allocate mem for log file path
    int size = strlen(logfile) * sizeof(char);
    auto pNameInVictimProcess = VirtualAllocEx(hVictimProcess,
        nullptr,
        size,
        MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
    if (pNameInVictimProcess == NULL) //Check if allocation failed
    {
        return FALSE;
    }    
    auto bStatus = WriteProcessMemory(hVictimProcess,
        pNameInVictimProcess,
        logfile,
        size,
        nullptr);

    // call sethook function in notepad process with logfile path argument
	auto hThreadId = CreateRemoteThread(hVictimProcess,
		nullptr,
		0,
		reinterpret_cast<LPTHREAD_START_ROUTINE>(LoadLibAddress),
        pNameInVictimProcess,
		NULL,
		nullptr);
	if (hThreadId == NULL)
	{
		return FALSE;
	}
	
	system("pause");
	return 0;
}