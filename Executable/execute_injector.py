import sys
from injector import inject_dll_to_process
from injector import remote_call_void_func
from pywinauto import Desktop

# This code should be incapsulate to some module or class in library

# NOTE: mb call thats script as subprocess with 64 or 32 bit python
# MSG structure also has different sizes depends of pyhon type

if (len(sys.argv) != 3):
    print("Usage: {} <PID> <Path To DLL>".format(sys.argv[0]))
    print("Eg: {} 1111 C:\\test\messagebox.dll".format(sys.argv[0]))
    sys.exit(0)

try:
    dll_path = sys.argv[2]

    app = Desktop(backend="win32")[sys.argv[1]]

    inject_dll_to_process(app, dll_path)
    remote_call_void_func(app, dll_path, "SetMsgHook")
    # will be used in data listener or rewriten
    # sock_port = ctypes.cdll.LoadLibrary(dll_path).getSocketPort()
except (RuntimeError, AttributeError) as exc:
    print(exc)
except IndexError:
    print("Invalid process name.")
