import pywintypes
import time
import win32file
import win32pipe
import winerror
import sys
from pywinauto.actionlogger import ActionLogger


class BrokenPipeError(Exception):
    pass

class Pipe(object):
    def __init__(self, name):
        self.name = name
        self.handle = None

    def connect(self, n_attempts=30, delay=1):
        for i in range(n_attempts):
            try:
                self.handle = win32file.CreateFile(
                    r'\\.\pipe\{}'.format(self.name),
                    win32file.GENERIC_READ | win32file.GENERIC_WRITE,
                    0,
                    None,
                    win32file.OPEN_EXISTING,
                    0,
                    None
                )
                ret = win32pipe.SetNamedPipeHandleState(
                        self.handle,
                        win32pipe.PIPE_READMODE_MESSAGE | win32pipe.PIPE_WAIT,
                        None,
                        None,
                    )
                ActionLogger().log('Connected to the pipe {}'.format(self.name))
                break
            except pywintypes.error as e:
                if e.args[0] == winerror.ERROR_FILE_NOT_FOUND:
                    ActionLogger().log('Attempt {}/{}: failed to connect to the pipe {}'.format(i+1, n_attempts,
                                                                                                self.name))
                    time.sleep(delay)
                else:
                    raise BrokenPipeError('Unexpected pipe error: {}'.format(e))
        if self.handle is not None:
            return True
        return False

    def transact(self, string):
        try:
            win32file.WriteFile(self.handle, string.encode('utf-8'))
            win32file.FlushFileBuffers(self.handle)
            resp = win32file.ReadFile(self.handle, 64 * 1024)
            return resp[1].decode(sys.getdefaultencoding())
        except pywintypes.error as e:
            if e.args[0] == winerror.ERROR_BROKEN_PIPE:
                raise BrokenPipeError("Broken pipe")
            else:
                raise BrokenPipeError('Unexpected pipe error: {}'.format(e))
            return ''

    def close(self):
        win32file.CloseHandle(self.handle)

