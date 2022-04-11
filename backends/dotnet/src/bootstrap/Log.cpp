#include "Log.h"

#include <windows.h>

void Log::LogLastError() {
    DWORD dLastError = GetLastError();
    LPWSTR strErrorMessage = NULL;

    FormatMessageW(
        FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS | FORMAT_MESSAGE_ARGUMENT_ARRAY | FORMAT_MESSAGE_ALLOCATE_BUFFER,
        NULL,
        dLastError,
        0,
        (LPWSTR)&strErrorMessage,
        0,
        NULL);

    this->Get() << "GetLastError() is " << dLastError << " - " << strErrorMessage;
}
std::wstringstream& Log::Get()
{
    os << "bootstrap dll (" << GetCurrentProcessId() << "): ";
    return os;
}
Log::~Log()
{
    os << std::endl;
    OutputDebugStringW(os.str().c_str());
}