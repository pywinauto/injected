#include "pch.h"

#pragma comment(lib, "mscoree.lib")
#pragma comment(lib, "Shlwapi.lib")

extern "C" __declspec(dllexport) int LoadWorkerDll();
bool initWorkerDllAbsolutePath(HMODULE hCurrentDll, wchar_t* buf, size_t len, const wchar_t* workerDllName);

wchar_t workerDllPath[MAX_PATH] = { 0 };

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

        if (!initWorkerDllAbsolutePath(hModule, workerDllPath, sizeof(workerDllPath) / sizeof(wchar_t), L"worker.dll"))
        {
            return FALSE;
        }
        
        Log().Get() << "create LoadWorkerDll() thread ...";
        auto Thread = CreateThread(0, 0, (LPTHREAD_START_ROUTINE)LoadWorkerDll, 0, 0, 0);
        if (!Thread) {
            Log().Get() << "failed to create LoadWorkerDll() thread";
            return FALSE;
        }
        Log().Get() << "LoadWorkerDll() thread is created";

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


bool initWorkerDllAbsolutePath(HMODULE hCurrentDll, wchar_t* buf, size_t len, const wchar_t* workerDllName) {
    Log().Get() << "getting absolute path to worker dll...";
    int ret = GetModuleFileNameW(hCurrentDll, buf, (DWORD)len);
    if (!ret) {
        Log().LogLastError();
        Log().Get() << "Error: GetModuleFileNameW failed with " << ret;
        return false;
    }
    Log().Get() << "module path is " << buf;
    PathRemoveFileSpecW(buf);
    Log().Get() << "module folder is " << buf;
    wcscat_s(buf, len, L"\\");
    wcscat_s(buf, len, workerDllName);
    Log().Get() << "worker dll path is " << buf;

    return true;
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

                Log().Get() << "load worker dll and start server - " << workerDllPath;
                DWORD pReturnValue;
                int ret = runtimeHost->ExecuteInDefaultAppDomain(workerDllPath, L"InjectedWorker.Main", L"StartServer", L"", &pReturnValue);
                if (ret == S_OK) {
                    Log().Get() << "worker dll: server is finished";
                }
                else {
                    Log().LogLastError();
                    Log().Get() << "ExecuteInDefaultAppDomain failed with return value " << ret;
                }

                //OPTIONAL: You can keep the CLR Opened depending on your needs
                runtimeHost->Release();
            }
            else {
                Log().LogLastError();
                Log().Get() << "GetInterface() failed";
            }
            runtimeInfo->Release();
        }
        else {
            Log().LogLastError();
            Log().Get() << "GetRuntime() failed";
        }
        metaHost->Release();
    }
    else {
        Log().LogLastError();
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