from pywinauto.handleprops import processid

import cfuncs

def get_process_handle(pid):
    return cfuncs.OpenProcess(cfuncs.PROCESS_ALL_ACCESS, False, pid)

def inject_dll_to_process(app, dll_path):
    # Get dll path length
    dll_len = len(dll_path)

    h_process = get_process_handle(processid(app.handle))

    # Allocate space for DLL path
    arg_address = cfuncs.VirtualAllocEx(h_process, 0, dll_len, cfuncs.VIRTUAL_MEM, cfuncs.PAGE_READWRITE)

    # Write DLL path to allocated space
    if not cfuncs.WriteProcessMemory(h_process, arg_address, dll_path, dll_len, 0):
        raise AttributeError("Couldn't write data to process memory, check python acceess.")

    # Resolve LoadLibraryA Address
    h_kernel32 = cfuncs.GetModuleHandle("kernel32.dll")
    h_loadlib = cfuncs.GetProcAddress(h_kernel32, "LoadLibraryA")

    # Now we createRemoteThread with entrypoiny set to LoadLibraryA and pointer to DLL path as param
    if not cfuncs.CreateRemoteThread(h_process, None, 0, h_loadlib, arg_address, 0, None):
        raise RuntimeError("Couldn't create remote thread, application and python version must have same version (x32 or x64)")

def remote_call_void_func(app, dll_path, func_name):
    h_process = get_process_handle(processid(app.handle))
    lib = cfuncs.LoadLibraryA(dll_path)

    proc_address = cfuncs.GetProcAddress(lib, func_name)

    if not cfuncs.CreateRemoteThread(h_process, None, 0, proc_address, 0, 0, None):
        raise RuntimeError("Couldn't create remote thread, application and python version must have same version (x32 or x64)")
