import sys
import win32con
from injector import Injector
from ctypes import wintypes
from mywin32enum import WIN_ID_TO_KEY
import ctypes

def print_winmsg(msg):
    print("hWnd:{}".format(str(msg.hWnd)))
    print("message:{}".format((WIN_ID_TO_KEY[msg.message] if msg.message in WIN_ID_TO_KEY else str(msg.message))))
    print("wParam:{}".format(str(msg.wParam)))
    print("lParam:{}".format(str(msg.lParam)))
    print("time:{}".format(str(msg.time)))
    print("pt:{}".format(str(msg.pt.x) + ',' + str(msg.pt.x)))

if (len(sys.argv) != 3):
    print("Usage: {} <PID> <Path To DLL>".format(sys.argv[0]))
    print("Eg: {} 1111 C:\\test\messagebox.dll".format(sys.argv[0]))
    sys.exit(0)

inj = Injector(sys.argv[1], sys.argv[2])
sock = inj.socket

while(1):
    msg = wintypes.MSG()
    m = sock.recvfrom(1024)
    ctypes.memmove(ctypes.pointer(msg),m[0],ctypes.sizeof(msg))
    print_winmsg(msg)
    if msg.message == win32con.WM_QUIT:
        break;
