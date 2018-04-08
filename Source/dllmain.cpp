#include <fstream>
#include <comdef.h>
#include <string>
#include <sstream>
#include <thread>
#include <mutex>   
#include <condition_variable>
#include "windows.h"
#include "TlHelp32.h"
#pragma warning(disable : 4996)

HHOOK g_hHook = NULL;
HINSTANCE g_hDll = NULL;

BOOL APIENTRY DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved);
extern "C" __declspec(dllexport) BOOL SetMsgHook(char* path);    // Exported
extern "C" __declspec(dllexport) BOOL UnsetMsgHook();   
extern "C" __declspec(dllexport) BOOL OpenLogFile(char* path);
LRESULT CALLBACK SysMsgProc(int nCode, WPARAM wParam, LPARAM lParam);

std::ofstream myfile;

extern "C" __declspec(dllexport) HINSTANCE getDllHinstance() {
	return g_hDll;
}

void printLog(const std::string& message)
{
    if (myfile.is_open()) {
        myfile << message << std::endl;
    }
}

BOOL APIENTRY DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
	switch (fdwReason)
	{
	case DLL_PROCESS_ATTACH:
		g_hDll = hinstDLL;
		return true;
    case DLL_PROCESS_DETACH:
        UnsetMsgHook();
        return true;
	}
}

BOOL OpenLogFile(char* path) {
    myfile.open(path);
    return myfile.is_open();
}

void infineWait() {
    std::condition_variable cv;
    std::mutex m;
    std::unique_lock<std::mutex> lock(m);
    cv.wait(lock, [] {return false; });
}

/*
    Call this function after dll injected
*/
BOOL SetMsgHook(char* path)
{
    OpenLogFile(path);
	g_hHook = SetWindowsHookEx(WH_GETMESSAGE, SysMsgProc, g_hDll, 0);
	if (g_hHook != NULL)
	{   
        printLog("Hook successfully set!");
        printLog("time;hwnd;lParam;wParam;message;pt.x;pt.y");
	}
	else 
    {
		HRESULT hr = HRESULT_FROM_WIN32(GetLastError());
		_com_error err(hr);
		LPCTSTR errMsg = err.ErrorMessage();
        printLog("Error when hook set");        
	}
    infineWait();
	return g_hHook != NULL;
}

BOOL UnsetMsgHook()
{	
	if (g_hHook == NULL)
		return false;

    if (myfile.is_open())
        myfile.close();

	BOOL isUnhooked = UnhookWindowsHookEx(g_hHook);
    if (isUnhooked)
    {
        printLog("Unhook");
        g_hHook = NULL;
    }

	return isUnhooked;
}

LRESULT CALLBACK SysMsgProc(int nCode, WPARAM wParam, LPARAM lParam)
{
	if (nCode < 0)
		return CallNextHookEx(g_hHook, nCode, wParam, lParam);

	LPMSG aMsg = (LPMSG)lParam;
    if (aMsg) {
        //TODO: socket binary massage send
        std::stringstream ss;
        ss << aMsg->time << ";" << aMsg->hwnd << ";" << aMsg->lParam << ";" << aMsg->wParam
           << ";" << aMsg->message << ";" << aMsg->pt.x << ";" << aMsg->pt.y;
        printLog(ss.str());
    }
	return CallNextHookEx(g_hHook, nCode, wParam, lParam);
}
