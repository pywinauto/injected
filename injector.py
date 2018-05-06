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

class Injector(object):

    """Class for injections dll and set hook on windows messages"""

    def __init__(self, process_name, dll_path, is_unicode = False):
        """Constructor inject dll, set socket and hook (one process - one class instanse"""
        self.dll_path = os.path.abspath(dll_path)
        self.app = Desktop(backend="win32")[process_name]
        self.is_unicode = is_unicode
        self.pid = processid(self.app.handle)
        self.h_process = self._get_process_handle(self.pid)
        if (int(sysinfo.is_x64_Python()) + int(is64bitprocess(self.pid))) % 2 != 0:
            raise RuntimeError("Application and python version must be same versions (x32 or x64)")

        self._inject_dll_to_process()

        self.sock = socket(AF_INET, SOCK_DGRAM)
        self.sock.bind(('',0))
        port = self.sock.getsockname()[1]

        self._remote_call_int_param_func("InitSocket", port)
        self._remote_call_void_func("SetMsgHook")

    @property
    def socket(self):
        """Returns datagram socket"""
        return self.sock

    @property
    def application(self):
        """Returns hooked application"""
        return self.app

    def _get_process_handle(self, pid):
        return cfuncs.OpenProcess(cfuncs.PROCESS_ALL_ACCESS, False, pid)

    def _create_remote_thread_with_timeout(self, proc_address, arg_address, timeout_ms = 1000, call_err_text = "", timeout_err_text = ""):
        thread_handle = cfuncs.CreateRemoteThread(self.h_process, None, 0, proc_address, arg_address, 0, None)
        if not thread_handle:
            raise RuntimeError(call_err_text)
        # TODO: add timeout to pywinauto timings and replace
        ret = cfuncs.WaitForSingleObject(thread_handle, ctypes.wintypes.DWORD(timeout_ms))
        if ret == cfuncs.WAIT_TIMEOUT:
            raise TimeoutError(timeout_err_text)

    def _inject_dll_to_process(self):
        # Get dll path length
        c_dll_path = ctypes.create_unicode_buffer(self.dll_path) if self.is_unicode else ctypes.create_string_buffer(self.dll_path)

        # Allocate space for DLL path
        arg_address = cfuncs.VirtualAllocEx(self.h_process, 0, ctypes.sizeof(c_dll_path), cfuncs.VIRTUAL_MEM, cfuncs.PAGE_READWRITE)

        # Write DLL path to allocated space
        if not cfuncs.WriteProcessMemory(self.h_process, arg_address, ctypes.byref(c_dll_path), ctypes.sizeof(c_dll_path), 0):
            raise AttributeError("Couldn't write data to process memory, check python acceess.")

        # Resolve LoadLibraryA Address
        h_kernel32 = cfuncs.GetModuleHandleW("kernel32.dll") if self.is_unicode else cfuncs.GetModuleHandleA("kernel32.dll")
        h_loadlib = cfuncs.GetProcAddress(h_kernel32, "LoadLibraryW" if self.is_unicode else "LoadLibraryA")

        # Now we createRemoteThread with entrypoiny set to LoadLibraryA and pointer to DLL path as param
        self._create_remote_thread_with_timeout(h_loadlib, arg_address, 1000,
            "Couldn't create remote thread, application and python version must be same versions (x32 or x64)",
            "Inject time out")

    def _get_dll_proc_address(self, proc_name):
        lib = cfuncs.LoadLibraryW(self.dll_path) if self.is_unicode else cfuncs.LoadLibraryA(self.dll_path)
        return cfuncs.GetProcAddress(lib, proc_name)

    def _remote_call_void_func(self, func_name):
        proc_address = self._get_dll_proc_address(func_name)

        if not cfuncs.CreateRemoteThread(self.h_process, None, 0, proc_address, 0, 0, None):
            raise RuntimeError("Couldn't create remote thread, dll not injected, inject and try again!")

    def _remote_call_int_param_func(self, func_name, param):
        import ctypes

        # resolve paramtype for different applications
        a = ctypes.c_int64(param) if is64bitprocess(self.pid) else ctypes.c_int32(param)

        arg_address = cfuncs.VirtualAllocEx(self.h_process, 0, ctypes.sizeof(a), cfuncs.VIRTUAL_MEM, cfuncs.PAGE_READWRITE)
        if not cfuncs.WriteProcessMemory(self.h_process, arg_address, ctypes.byref(a), ctypes.sizeof(a), 0):
            raise AttributeError("Couldn't write data to process memory, check python acceess.")

        proc_address = self._get_dll_proc_address(func_name)

        self._create_remote_thread_with_timeout(proc_address, arg_address, 1000,
            "Couldn't create remote thread, dll not injected, inject and try again!",
            "{0}(int) function call time out".format(func_name))
