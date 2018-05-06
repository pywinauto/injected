import sys
import win32con
from injector import *
from ctypes import wintypes
from mywin32enum import WIN_ID_TO_KEY

def print_winmsg(msg):
    print "hWnd:" +  str(msg.hWnd)
    print "message:" + (WIN_ID_TO_KEY[msg.message] if msg.message in WIN_ID_TO_KEY else str(msg.message))
    print "wParam:" +  str(msg.wParam)
    print "lParam:" +  str(msg.lParam)
    print "time:" +  str(msg.time)
    print "pt:" + str(msg.pt.x) + ',' + str(msg.pt.x)

if (len(sys.argv) != 3):
    print("Usage: {} <PID> <Path To DLL>".format(sys.argv[0]))
    print("Eg: {} 1111 C:\\test\messagebox.dll".format(sys.argv[0]))
    sys.exit(0)

sock = execute_workflow(sys.argv[1], sys.argv[2])

while(1):
    msg = wintypes.MSG()
    m = sock.recvfrom(1024)
    ctypes.memmove(ctypes.pointer(msg),m[0],ctypes.sizeof(msg))
    print_winmsg(msg)
    if msg.message == win32con.WM_QUIT:
        break;
