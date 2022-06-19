#include "pch.h"

#pragma comment(lib, "mscoree.lib")
#pragma comment(lib, "Shlwapi.lib")

extern "C" __declspec(dllexport) int LoadWorkerDll();
bool initWorkerDllAbsolutePath(HMODULE hCurrentDll, wchar_t* buf, size_t len, const wchar_t* workerDllName);
ICLRRuntimeInfo* FindAvailableClrSince4(ICLRMetaHost* metaHost);

wchar_t workerDllPath[MAX_PATH] = { 0 };

// TODO add functions to enable/disable logging, set log level, unload dlls

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

        if (!initWorkerDllAbsolutePath(hModule, workerDllPath, sizeof(workerDllPath) / sizeof(wchar_t), L"worker_wpf.dll"))
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
        break;
    }
    case DLL_THREAD_DETACH:
    {
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


bool OK(HRESULT res, const char* name) {
    if (res != S_OK) {
        Log().LogLastError();
        Log().Get() << name << " failed with return value " << res;
        return false;
    }
    return true;
}


/* The code below is inspired by these articles:
* https://web.archive.org/web/20101224064236/http:/codingthewheel.com/archives/how-to-inject-a-managed-assembly-dll
* https://blog.adamfurmanek.pl/2016/04/16/dll-injection-part-4/
* https://www.unknowncheats.me/forum/general-programming-and-reversing/332825-inject-net-dll-using-clr-hosting.html
* 
* Some useful info about the compatibility with .NET versions can be found here:
* https://stackoverflow.com/questions/6275693/what-governs-the-version-of-the-net-clr-that-gets-loaded-by-corbindtoruntimeex
* https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/hosting/corbindtoruntimeex-function
* https://docs.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/01918c6x(v=vs.100)?redirectedfrom=MSDN
* https://stackoverflow.com/questions/20338895/replacement-equivalent-for-the-corbindtoruntimeex-function
*/
extern "C" __declspec(dllexport) 
int LoadWorkerDll() {
    Log().Get() << "LoadWorkerDll() called";

    // TODO CorBindToRuntimeEx for .NET < 4.0
    ICLRMetaHost* metaHost = nullptr;
    if (OK(CLRCreateInstance(CLSID_CLRMetaHost, IID_ICLRMetaHost, (LPVOID*)&metaHost), "CLRCreateInstance")) {
        Log().Get() << "Looking for CLR >= 4.0 ...";
        ICLRRuntimeInfo* runtimeInfo = FindAvailableClrSince4(metaHost);
        if (runtimeInfo) {
            ICLRRuntimeHost* runtimeHost = nullptr;
            if (OK(runtimeInfo->GetInterface(CLSID_CLRRuntimeHost, IID_ICLRRuntimeHost, (LPVOID*)&runtimeHost), "GetInterface")) {
                Log().Get() << "runtimeHost->Start()...";
                runtimeHost->Start();

                Log().Get() << "load worker dll and start server - " << workerDllPath;
                DWORD pReturnValue;
                if (OK(runtimeHost->ExecuteInDefaultAppDomain(workerDllPath, L"InjectedWorker.Server", L"Start", L"", &pReturnValue), "ExecuteInDefaultAppDomain")) {
                    Log().Get() << "worker dll: server is finished";
                }

                runtimeHost->Release();
            }
            runtimeInfo->Release();
        }
        else {
            Log().Get() << "Error: CLR >= 4.0 not found!";
        }
        metaHost->Release();
    }

    return 0;
}

ICLRRuntimeInfo* GetCLRWithMajorVersionSince4(IEnumUnknown* runtimes) {
    ICLRRuntimeInfo* runtime = nullptr;

    DWORD versionBufLen = 30;
    wchar_t* versionBuf = new wchar_t[versionBufLen];
    ULONG fetched;
    ICLRRuntimeInfo* rt = nullptr;
    while (!runtime && OK(runtimes->Next(1, (IUnknown**)&rt, &fetched), "Next")) {
        if (OK(rt->GetVersionString(versionBuf, &versionBufLen), "GetVersionString")) {
            Log().Get() << "Available runtime: " << versionBuf;

            // example version string: v4.0.30319, check major version
            if (_wtoi(versionBuf + 1) >= 4) {
                Log().Get() << "Selected runtime: " << versionBuf;
                runtime = rt;
            }
            else {
                rt->Release();
            }
        }
    }

    delete[] versionBuf;
    return runtime;
}

ICLRRuntimeInfo* FindAvailableClrSince4(ICLRMetaHost* metaHost) {
    ICLRRuntimeInfo* runtime = nullptr;

    IEnumUnknown* runtimes = nullptr;
    if (OK(metaHost->EnumerateLoadedRuntimes(GetCurrentProcess(), &runtimes), "EnumerateLoadedRuntimes")) {
        runtime = GetCLRWithMajorVersionSince4(runtimes);
        runtimes->Release();
    }

    if (!runtime) {
        Log().Get() << "CLR >= 4.0 is not loaded, trying to find it in installed";
        if (OK(metaHost->EnumerateInstalledRuntimes(&runtimes), "EnumerateInstalledRuntimes")) {
            runtime = GetCLRWithMajorVersionSince4(runtimes);
            runtimes->Release();
        }
    }

    return runtime;
}