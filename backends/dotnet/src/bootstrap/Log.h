#include <string>
#include <sstream>

// https://www.drdobbs.com/cpp/logging-in-c/201804215
class Log
{
public:
    Log() {};
    virtual ~Log();
    std::wstringstream& Get();
    void LogLastError();
protected:
    std::wstringstream os;
private:
    Log(const Log&);
    Log& operator =(const Log&);
};