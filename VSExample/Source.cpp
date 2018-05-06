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

using namespace std;
HANDLE hVictimProcess;
BOOL FindProcess(PCWSTR exeName, DWORD& pid, vector<DWORD>& tids);
BOOL Dll_Injection(const wchar_t *funcarg, const wchar_t processname[]);

const wchar_t *GetWC(const char *c)
{
    if (!c) throw exception();
    size_t cSize = strlen(c) + 1;
    wchar_t* wc = new wchar_t[cSize];
    mbstowcs_s(&cSize, wc, cSize, c, cSize);
    return wc;
}

typedef HINSTANCE(CALLBACK* DLL_HWND)();
typedef int(CALLBACK* PORT)();
int main(int ac, char** av)
{
    // parse args    
    const wchar_t * dllpath = nullptr;
    const wchar_t * procname = nullptr;
    try 
    {
        dllpath = GetWC(av[1]);
        procname = GetWC(av[2]);
    }
    catch (...)
    {
        printf("Please enter injected dll full path as first argument\n");
        printf("Please enter process name as second argument\n");
        return 0;
    }

    // load dll to get hinstanse 
    HMODULE dll = LoadLibraryW(dllpath);
	if (dll == NULL)
    {
        printf("Incorrect dll\n");
        return 0;
    }
    DLL_HWND addr = (DLL_HWND)GetProcAddress(dll, "getDllHinstance");
	if (addr == NULL)
    {
        printf("Incorrect dll\n");
        return 0;
    }
    HINSTANCE mHandle = addr();
    PORT portFunc = (PORT)GetProcAddress(dll, "getSocketPort");
    if (portFunc == NULL)
    {
        printf("Incorrect dll\n");
        return 0;
    }
    int port = portFunc();
    // get adress of function
    LPVOID LoadLibAddress = (LPVOID)GetProcAddress(mHandle, "SetMsgHook");
    if (LoadLibAddress == NULL)
    {
        printf("Incorrect dll\n");
        return 0;
    }

    // inject dll to the notepad
    if (!Dll_Injection(dllpath, procname)) {
        printf("Injection error\n");
        return 0;
    }
        
    // call set hook function in dll
    auto hThreadId = CreateRemoteThread(hVictimProcess,
        nullptr,
        0,
        reinterpret_cast<LPTHREAD_START_ROUTINE>(LoadLibAddress),
        0,
        NULL,
        nullptr);
    if (hThreadId == NULL)
    {
        printf("Incorrect dll\n");
        return FALSE;
    }

    printf("Dll successfully injected! Listen port: %d\n", port);

    return 0;
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
    auto hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0); // Fucking lazy auto bullshit, also snap all processes
    if (hSnapshot == INVALID_HANDLE_VALUE)
    {
        return FALSE;
    }
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