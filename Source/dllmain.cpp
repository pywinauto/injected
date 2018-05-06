#pragma once
#define _WINSOCKAPI_
#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#include <windows.h>
#include <fstream>
#include <comdef.h>
#include <string>
#include <sstream>
#include <thread>
#include <mutex>   
#include <condition_variable>
#include "TlHelp32.h"
#pragma comment (lib, "Ws2_32.lib")
#pragma warning(disable : 4996)

HHOOK g_hHook = NULL;
HINSTANCE g_hDll = NULL;

BOOL APIENTRY DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved);
extern "C" __declspec(dllexport) BOOL SetMsgHook(char* path);    // Exported
extern "C" __declspec(dllexport) BOOL UnsetMsgHook();
extern "C" __declspec(dllexport) BOOL OpenLogFile(char* path);
extern "C" __declspec(dllexport) BOOL InitSocket(int* port);
LRESULT CALLBACK SysMsgProc(int nCode, WPARAM wParam, LPARAM lParam);

SOCKET sock;
char buff[sizeof(MSG)];
struct sockaddr_in Sender_addr;
bool socketInitialized = false;
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

void messageToFile(LPMSG aMsg) {
    if (myfile.is_open()) {
        std::stringstream ss;
        ss << aMsg->time << ";";
        ss << aMsg->message << ";";
        ss << aMsg->hwnd << ";";
        ss << aMsg->lParam << ";";
        ss << aMsg->wParam << ";";
        ss << aMsg->pt.x << ";";
        ss << aMsg->pt.y << ";";
        printLog(ss.str());
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
    if (path)
        myfile.open(path);
    return myfile.is_open();
}

void infiniteWait() {
    std::condition_variable cv;
    std::mutex m;
    std::unique_lock<std::mutex> lock(m);
    cv.wait(lock, [] {return false; });
}

BOOL InitSocket(int* port) {
    //MessageBoxA(0, "Attach now", "InitSocketCall", 0);
    WSADATA wsaData;
    WSAStartup(MAKEWORD(2, 2), &wsaData);
    sock = socket(AF_INET, SOCK_DGRAM, 0);
    if (sock == INVALID_SOCKET) {
        return 0;
    }
    BOOL enabled = TRUE;
    if (setsockopt(sock, SOL_SOCKET, SO_BROADCAST, (char*)&enabled, sizeof(BOOL)) < 0) {
        return 0;
    }
    Sender_addr.sin_family = AF_INET;
    Sender_addr.sin_port = htons(*port);
    Sender_addr.sin_addr.s_addr = inet_addr("localhost");
    socketInitialized = true;
    return 1;
}

/*
    Call this function after dll injected
*/
BOOL SetMsgHook(char* path = nullptr)
{
    //MessageBoxA(0, "Attach now", "SetMsgHookCall", 0);
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
        return FALSE;
    }

    infiniteWait();
    return g_hHook != NULL;
}

BOOL UnsetMsgHook()
{
    if (g_hHook == NULL)
        return FALSE;

    BOOL isUnhooked = UnhookWindowsHookEx(g_hHook);
    if (isUnhooked)
    {
        printLog("Unhook");
        g_hHook = NULL;
    }

    if (myfile.is_open())
        myfile.close();

    closesocket(sock);

    return isUnhooked;
}

LRESULT CALLBACK SysMsgProc(int nCode, WPARAM wParam, LPARAM lParam)
{
    if (nCode < 0)
        return CallNextHookEx(g_hHook, nCode, wParam, lParam);

    LPMSG aMsg = (LPMSG)lParam;
    if (aMsg) {
        messageToFile(aMsg);
        memcpy(&buff, aMsg, sizeof(*aMsg));
        if (socketInitialized) {
            if (sendto(sock, buff, sizeof(*aMsg), 0, (sockaddr *)&Sender_addr, sizeof(Sender_addr)) < 0)
            {
                closesocket(sock);
                printLog("Socket closed!");
                return CallNextHookEx(g_hHook, nCode, wParam, lParam);
            }
        }
    }
    return CallNextHookEx(g_hHook, nCode, wParam, lParam);
}
