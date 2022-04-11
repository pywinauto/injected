#include "pch.h"

#pragma comment(lib, "mscoree.lib")

extern "C" __declspec(dllexport) int LoadWorkerDll();


// https://www.drdobbs.com/cpp/logging-in-c/201804215
class Log
{
public:
    Log() {};
    virtual ~Log();
    std::wstringstream& Get();
protected:
    std::wstringstream os;
private:
    Log(const Log&);
    Log& operator =(const Log&);
};
std::wstringstream& Log::Get()
{
    os << "bootstrap dll (" << GetCurrentProcessId() << "): ";
    return os;
}
Log::~Log()
{
    os << std::endl;
    OutputDebugStringW(os.str().c_str());
}


BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    {
        Log().Get() << "DLL_PROCESS_ATTACH";
        Log().Get() << "create LoadWorkerDll() thread ...";
        // start LoadWorkerDll()
        auto Thread = CreateThread(0, 0, (LPTHREAD_START_ROUTINE)LoadWorkerDll, 0, 0, 0);
        if (!Thread) {
            Log().Get() << "LoadWorkerDll() thread failed";
            return FALSE;
        }
        Log().Get() << "LoadWorkerDll() thread success";
        break;
    }
    case DLL_THREAD_ATTACH:
    {
        Log().Get() << "DLL_THREAD_ATTACH";
        break;
    }
    case DLL_THREAD_DETACH:
    {
        Log().Get() << "DLL_THREAD_DETACH";
        break; 
    }
    case DLL_PROCESS_DETACH:
    {
        Log().Get() << "DLL_PROCESS_DETACH";
        break;
    }
    }
    return TRUE;
}


void debugPrintLastError() {
    DWORD dLastError = GetLastError();
    LPSTR strErrorMessage = NULL;

    FormatMessage(
        FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS | FORMAT_MESSAGE_ARGUMENT_ARRAY | FORMAT_MESSAGE_ALLOCATE_BUFFER,
        NULL,
        dLastError,
        0,
        strErrorMessage,
        0,
        NULL);

    Log().Get() << "GetLastError() is " << dLastError << " - " << strErrorMessage;
}


extern "C" __declspec(dllexport) 
int LoadWorkerDll() {
    Log().Get() << "LoadWorkerDll() called";

    // TODO CorBindToRuntimeEx for .NET < 4.0
    ICLRMetaHost* metaHost = nullptr;
    if (CLRCreateInstance(CLSID_CLRMetaHost, IID_ICLRMetaHost, (LPVOID*)&metaHost) == S_OK) {
        // TODO EnumerateLoadedRuntimes ?
        const wchar_t* runtimeVersion = L"v4.0.30319";
        ICLRRuntimeInfo* runtimeInfo = nullptr;
        if (metaHost->GetRuntime(runtimeVersion, IID_ICLRRuntimeInfo, (LPVOID*)&runtimeInfo) == S_OK) {
            ICLRRuntimeHost* runtimeHost = nullptr;
            if (runtimeInfo->GetInterface(CLSID_CLRRuntimeHost, IID_ICLRRuntimeHost, (LPVOID*)&runtimeHost) == S_OK) {
                Log().Get() << "runtimeHost->Start()...";

                runtimeHost->Start();

                DWORD pReturnValue;
                // TODO dll path + "worker.dll"
                const wchar_t* path = L"D:\\repo\\pywinauto\\dotnetguiauto\\x64\\Debug\\worker.dll";
                Log().Get() << "load worker dll and start server - " << path;
                int ret = runtimeHost->ExecuteInDefaultAppDomain(path, L"InjectedWorker.Main", L"StartServer", L"", &pReturnValue);
                if (ret == S_OK) {
                    Log().Get() << "worker dll: server is finished";
                }
                else {
                    debugPrintLastError();
                    Log().Get() << "ExecuteInDefaultAppDomain failed with return value " << ret;
                }

                //OPTIONAL: You can keep the CLR Opened depending on your needs
                runtimeHost->Release();
            }
            else {
                debugPrintLastError();
                Log().Get() << "GetInterface() failed";
            }
            runtimeInfo->Release();
        }
        else {
            debugPrintLastError();
            Log().Get() << "GetRuntime() failed";
        }
        metaHost->Release();
    }
    else {
        debugPrintLastError();
        Log().Get() << "bootstrap: CLRCreateInstance() failed";
    }

    return 0;
}





/*

https://stackoverflow.com/questions/6275693/what-governs-the-version-of-the-net-clr-that-gets-loaded-by-corbindtoruntimeex
https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/hosting/corbindtoruntimeex-function
https://docs.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/01918c6x(v=vs.100)?redirectedfrom=MSDN
https://stackoverflow.com/questions/20338895/replacement-equivalent-for-the-corbindtoruntimeex-function
*/