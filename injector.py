from pywinauto.handleprops import processid
from pywinauto.handleprops import is64bitprocess
from pywinauto.timings import TimeoutError as WaitError
from pywinauto import sysinfo
from .channel import Pipe
from . import cfuncs
import ctypes
import os

# Relative path, would be changed after adding to pywinauto
dll_path = "./dll_to_inject/"

class Injector(object):

    """Class for injections dll and set hook on windows messages"""

    def __init__(self, app, is_unicode = False):
        """Constructor inject dll, set socket and hook (one application - one class instanse)"""
        self.app = app
        self.is_unicode = is_unicode
        self.pid = processid(self.app.handle)
        if not sysinfo.is_x64_Python() == is64bitprocess(self.pid):
            raise RuntimeError("Application and Python must be both 32-bit or both 64-bit")
        self.h_process = self._get_process_handle(self.pid)
        self.dll_path = os.path.abspath("{0}pywinmsg{1}{2}.dll".format(dll_path,
            "64" if is64bitprocess(self.pid) else "32",
            "u" if self.is_unicode else ""))
        self._inject_dll_to_process()

        # self._remote_call_int_param_func("InitSocket", port)
        # self._remote_call_void_func("SetMsgHook")

    @property
    def application(self):
        """Return hooked application"""
        return self.app

    @staticmethod
    def _get_process_handle(pid):
        return cfuncs.OpenProcess(cfuncs.PROCESS_ALL_ACCESS, False, pid)

    def _create_remote_thread_with_timeout(self, proc_address, arg_address, timeout_ms = 1000, call_err_text = "", timeout_err_text = ""):
        thread_handle = cfuncs.CreateRemoteThread(self.h_process, None, 0, proc_address, arg_address, 0, None)
        if not thread_handle:
            raise RuntimeError(call_err_text)
        # TODO: add timeout to pywinauto timings and replace
        ret = cfuncs.WaitForSingleObject(thread_handle, ctypes.wintypes.DWORD(timeout_ms))
        if ret == cfuncs.WAIT_TIMEOUT:
            raise WaitError(timeout_err_text)

    def _inject_dll_to_process(self):
        # Get dll path length
        c_dll_path = ctypes.create_unicode_buffer(self.dll_path) if self.is_unicode else ctypes.create_string_buffer(self.dll_path)

        # Allocate space for DLL path
        arg_address = cfuncs.VirtualAllocEx(self.h_process, 0, ctypes.sizeof(c_dll_path), cfuncs.VIRTUAL_MEM, cfuncs.PAGE_READWRITE)

        # Write DLL path to allocated space
        if not cfuncs.WriteProcessMemory(self.h_process, arg_address, ctypes.byref(c_dll_path), ctypes.sizeof(c_dll_path), 0):
            raise AttributeError("Couldn't write data to process memory, check python acceess.")

        # Resolve LoadLibraryA(W) Address
        h_kernel32 = cfuncs.GetModuleHandleW("kernel32.dll") if self.is_unicode else cfuncs.GetModuleHandleA("kernel32.dll")
        h_loadlib = cfuncs.GetProcAddress(h_kernel32, "LoadLibraryW" if self.is_unicode else "LoadLibraryA")

        # Now call createRemoteThread with entry point set to LoadLibraryA(W) and pointer to DLL path as param
        self._create_remote_thread_with_timeout(h_loadlib, arg_address, 1000,
            "Couldn't create remote thread, application and Python must be both 32-bit or both 64-bit",
            "Inject time out")

    def _get_dll_proc_address(self, proc_name):
        lib = cfuncs.LoadLibraryW(self.dll_path) if self.is_unicode else cfuncs.LoadLibraryA(self.dll_path)
        return cfuncs.GetProcAddress(lib, proc_name)

    def _remote_call_void_func(self, func_name):
        proc_address = self._get_dll_proc_address(func_name)

        if not cfuncs.CreateRemoteThread(self.h_process, None, 0, proc_address, 0, 0, None):
            raise RuntimeError("Couldn't create remote thread, dll not injected, inject and try again!")

    def _remote_call_int_param_func(self, func_name, param):
        # Resolve paramtype for different applications
        a = ctypes.c_int64(param) if is64bitprocess(self.pid) else ctypes.c_int32(param)

        arg_address = cfuncs.VirtualAllocEx(self.h_process, 0, ctypes.sizeof(a), cfuncs.VIRTUAL_MEM, cfuncs.PAGE_READWRITE)
        if not cfuncs.WriteProcessMemory(self.h_process, arg_address, ctypes.byref(a), ctypes.sizeof(a), 0):
            raise AttributeError("Couldn't write data to process memory, check python acceess.")

        proc_address = self._get_dll_proc_address(func_name)

        self._create_remote_thread_with_timeout(proc_address, arg_address, 1000,
            "Couldn't create remote thread, dll not injected, inject and try again!",
            "{0}(int) function call time out".format(func_name))


def inject(pid):
    dll_path = (os.path.dirname(__file__) + os.sep + r'x64\bootstrap.dll').encode('utf-8')
    #dll_path = rb'D:\repo\pywinauto\dotnetguiauto\Debug\bootstrap.dll'

    h_process = cfuncs.OpenProcess(cfuncs.PROCESS_ALL_ACCESS, False, int(pid))
    #print(f'{h_process=}')
    if not h_process:
        #print('')
        sys.exit(-1)

    arg_address = cfuncs.VirtualAllocEx(h_process, 0, len(dll_path), cfuncs.VIRTUAL_MEM, cfuncs.PAGE_READWRITE)
    #print(f'{hex(arg_address)=}')

    cfuncs.WriteProcessMemory(h_process, arg_address, dll_path, len(dll_path), 0)
    #print(f'{written=}')

    h_kernel32 = cfuncs.GetModuleHandleA(b"kernel32.dll")
    #print(f'{h_kernel32=}')
    h_loadlib = cfuncs.GetProcAddress(h_kernel32, b"LoadLibraryA")
    #print(f'{h_loadlib=}')

    thread_id = ctypes.c_ulong(0)

    if not cfuncs.CreateRemoteThread(
        h_process,
        None,
        0,
        h_loadlib,
        arg_address,
        0,
        ctypes.byref(thread_id)
    ):
        print('err inject')

    #print(f'{thread_id=}')

def create_pipe(pid):
    pipe = Pipe(f'pywinauto_{pid}')
    if pipe.connect(n_attempts=1):
        return pipe
    else:
        inject(pid)
        #print('Inject successful, connecting to the server...')
        pipe.connect()
        return pipe
