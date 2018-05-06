from pywinauto.handleprops import processid
from pywinauto.handleprops import is64bitprocess
from pywinauto.timings import TimeoutError
from pywinauto import Desktop
from pywinauto import sysinfo
from socket import *
import win32event
import cfuncs
import ctypes
import sys
import os

#TODO: incapsulate to class

is_unicode = False

def get_process_handle(pid):
    return cfuncs.OpenProcess(cfuncs.PROCESS_ALL_ACCESS, False, pid)

def create_remote_thread_with_timeout(h_process, proc_address, arg_address, timeout_ms = 1000, call_err_text = "", timeout_err_text = ""):
    thread_handle = cfuncs.CreateRemoteThread(h_process, None, 0, proc_address, arg_address, 0, None)
    if not thread_handle:
        raise RuntimeError(call_err_text)
    # TODO: add timeout to pywinauto timings and replace
    ret = cfuncs.WaitForSingleObject(thread_handle, ctypes.wintypes.DWORD(timeout_ms))
    if ret == cfuncs.WAIT_TIMEOUT:
        raise TimeoutError(timeout_err_text)

def inject_dll_to_process(app, dll_path):
    # Get dll path length
    c_dll_path = ctypes.create_unicode_buffer(dll_path) if is_unicode else ctypes.create_string_buffer(dll_path)
    h_process = get_process_handle(processid(app.handle))

    # Allocate space for DLL path
    arg_address = cfuncs.VirtualAllocEx(h_process, 0, ctypes.sizeof(c_dll_path), cfuncs.VIRTUAL_MEM, cfuncs.PAGE_READWRITE)

    # Write DLL path to allocated space
    if not cfuncs.WriteProcessMemory(h_process, arg_address, ctypes.byref(c_dll_path), ctypes.sizeof(c_dll_path), 0):
        raise AttributeError("Couldn't write data to process memory, check python acceess.")

    # Resolve LoadLibraryA Address
    h_kernel32 = cfuncs.GetModuleHandleW("kernel32.dll") if is_unicode else cfuncs.GetModuleHandleA("kernel32.dll")
    h_loadlib = cfuncs.GetProcAddress(h_kernel32, "LoadLibraryW" if is_unicode else "LoadLibraryA")

    # Now we createRemoteThread with entrypoiny set to LoadLibraryA and pointer to DLL path as param
    create_remote_thread_with_timeout(h_process, h_loadlib, arg_address, 1000,
        "Couldn't create remote thread, application and python version must be same versions (x32 or x64)",
        "Inject time out")

def get_dll_proc_address(dll_path, proc_name):
    lib = cfuncs.LoadLibraryW(dll_path) if is_unicode else cfuncs.LoadLibraryA(dll_path)
    return cfuncs.GetProcAddress(lib, proc_name)

def remote_call_void_func(app, dll_path, func_name):
    h_process = get_process_handle(processid(app.handle))
    proc_address = get_dll_proc_address(dll_path, func_name)

    if not cfuncs.CreateRemoteThread(h_process, None, 0, proc_address, 0, 0, None):
        raise RuntimeError("Couldn't create remote thread, dll not injected, inject and try again!")

def remote_call_int_param_func(app, dll_path, func_name, param):
    import ctypes
    pid = processid(app.handle)
    h_process = get_process_handle(pid)

    # resolve paramtype for different applications
    a = ctypes.c_int64(param) if is64bitprocess(pid) else ctypes.c_int32(param)

    arg_address = cfuncs.VirtualAllocEx(h_process, 0, ctypes.sizeof(a), cfuncs.VIRTUAL_MEM, cfuncs.PAGE_READWRITE)
    if not cfuncs.WriteProcessMemory(h_process, arg_address, ctypes.byref(a), ctypes.sizeof(a), 0):
        raise AttributeError("Couldn't write data to process memory, check python acceess.")

    proc_address = get_dll_proc_address(dll_path, func_name)

    create_remote_thread_with_timeout(h_process, proc_address, arg_address, 1000,
        "Couldn't create remote thread, dll not injected, inject and try again!",
        "{0}(int) function call time out".format(func_name))

# inected pywinauto's dll to the process and returns socket for listhen info
def execute_workflow(process_name, dll_path):
    # resolve arguments
    dll_path = os.path.abspath(dll_path)
    app = Desktop(backend="win32")[process_name]

    pid = processid(app.handle)
    h_process = get_process_handle(pid)
    if (int(sysinfo.is_x64_Python()) + int(is64bitprocess(pid))) % 2 != 0:
        raise RuntimeError("Application and python version must be same versions (x32 or x64)")

    inject_dll_to_process(app, dll_path)

    s = socket(AF_INET, SOCK_DGRAM)
    s.bind(('',0))
    port = s.getsockname()[1]

    remote_call_int_param_func(app, dll_path, "InitSocket", port)
    remote_call_void_func(app, dll_path, "SetMsgHook")

    return s
