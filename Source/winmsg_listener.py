from socket import *
import ctypes
from ctypes import wintypes
import win32con
from mywin32enum import WIN_ID_TO_KEY

def print_winmsg(msg):
    print "hWnd:" +  str(msg.hWnd)
    print "message:" + (WIN_ID_TO_KEY[msg.message] if msg.message in WIN_ID_TO_KEY else str(msg.message))
    print "wParam:" +  str(msg.wParam)
    print "lParam:" +  str(msg.lParam)
    print "time:" +  str(msg.time)
    print "pt:" + str(msg.pt.x) + ',' + str(msg.pt.x)

s = socket(AF_INET, SOCK_DGRAM)
s.bind(('',17078))

while(1):
    msg = wintypes.MSG()
    m = s.recvfrom(1024)
    ctypes.memmove(ctypes.pointer(msg),m[0],ctypes.sizeof(msg))
    print_winmsg(msg)
    if msg.message == win32con.WM_QUIT:
    	break;
