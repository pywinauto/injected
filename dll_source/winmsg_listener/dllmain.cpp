#pragma once
#define _WINSOCKAPI_
#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#include <windows.h>
#include <thread>
#include <vector>
#include <mutex>
#include <set>
#include <condition_variable>

#pragma comment (lib, "Ws2_32.lib")

static HHOOK     g_hook_handle_sys = nullptr;
static HHOOK     g_hook_handle_wnd = nullptr;
static HINSTANCE g_hDll = nullptr;

BOOL    APIENTRY DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved);
LRESULT CALLBACK SysMsgProc(int nCode, WPARAM wParam, LPARAM lParam);
LRESULT CALLBACK CallWndProc(int nCode, WPARAM wParam, LPARAM lParam);
extern "C" __declspec(dllexport) BOOL Initialize();
extern "C" __declspec(dllexport) BOOL SetApprovedList(int* list);
extern "C" __declspec(dllexport) HINSTANCE getDllHinstance() {
    return g_hDll;
}

class PipeManager {
    const static size_t m_msg_size = sizeof(MSG);
    std::string m_pipe_name = "\\\\.\\pipe\\pywinauto_recorder_pipe";

    HANDLE m_h_pipe;
    DWORD  m_access_flags     = GENERIC_READ | GENERIC_WRITE;
    DWORD  m_no_create_flag   = OPEN_EXISTING;
    DWORD  m_read_mode_flag   = PIPE_READMODE_MESSAGE;
    DWORD  m_written_bytes    = 0;
    bool   m_have_to_close    = false;
    bool   m_pipe_initialized = false;

public:
    PipeManager() {}

    bool initialize()
    {
        m_h_pipe = CreateFileA(m_pipe_name.c_str(), m_access_flags, 0, nullptr, m_no_create_flag, 0, nullptr);

        if (m_h_pipe == INVALID_HANDLE_VALUE)
            return false;

        m_have_to_close = true;

        if (GetLastError() == ERROR_PIPE_BUSY)
            return false;

        if (!SetNamedPipeHandleState(m_h_pipe, &m_read_mode_flag, nullptr, nullptr))
            return false;

        m_pipe_initialized = true;
        return true;
    }

    ~PipeManager()
    {
        if (!m_have_to_close)
            return;

        CloseHandle(m_h_pipe);
    }

    void send_message(const MSG* msg)
    {
        if (!m_pipe_initialized)
            return;

        std::vector<char> message_buffer(m_msg_size, 0);
        memcpy(message_buffer.data(), msg, m_msg_size);
        if (!WriteFile(m_h_pipe, message_buffer.data(), static_cast<DWORD>(m_msg_size), &m_written_bytes, nullptr))
        {
            CloseHandle(m_h_pipe);
            m_pipe_initialized = false;
            m_have_to_close = false;
        }
    }
};

class InjectorManager {
    enum HookStatus : uint32_t
    {
        undefined = 0,
        success,
        hook_failed,
        pipe_failed,
    };

    bool                            m_hook_stop_thread = false;
    int                             m_socket_port = 0;
    HookStatus                      m_status = undefined;
    PipeManager                     m_pipe_manager;
    std::set<int>                   m_approved_messages_ids;
    std::unique_ptr<std::thread>    m_hook_thread;
    std::condition_variable         m_hook_cv;
    std::mutex                      m_hook_mutex;
    std::mutex                      m_send_mutex;

private:
    void cpu_save_infinite_wait() {
        std::unique_lock<std::mutex> lock(m_hook_mutex);
        while (!m_hook_stop_thread)
        {
            m_hook_cv.wait(lock);
        }
    }

    void stop_infinite_wait() {
        std::unique_lock<std::mutex> lock(m_hook_mutex);
        m_hook_stop_thread = true;
        m_hook_cv.notify_one();
        if (m_hook_thread && m_hook_thread->joinable())
            m_hook_thread->join();
    }

    InjectorManager(const InjectorManager&) = delete;
    InjectorManager& operator=(const InjectorManager &) = delete;
    InjectorManager(InjectorManager &&) = delete;
    InjectorManager & operator=(InjectorManager &&) = delete;

public:
    InjectorManager() {}

    void send_msg(const MSG* msg)
    {
        std::lock_guard<std::mutex> lg(m_send_mutex);
        if (m_approved_messages_ids.count(msg->message))
            m_pipe_manager.send_message(msg);
    }

    void send_cwp_msg(const CWPSTRUCT* data)
    {
        MSG msg = { data->hwnd, data->message, data->wParam, data->lParam };
        if (data->message == WM_NOTIFY)
        {
            LPNMHDR hdr = (LPNMHDR)(data->lParam);
            msg.lParam = hdr->code;
            msg.hwnd = hdr->hwndFrom;
        }

        send_msg(&msg);
    }
    
    int get_status() const
    {
        return static_cast<int>(m_status);
    }

    void parse_skip_list(int** list)
    {
        size_t size = static_cast<size_t>((*list)[0]);
        for (size_t i = 0; i < size; ++i)
            m_approved_messages_ids.insert((*list)[i + 1]);
    }

    void initialize()
    {
        m_hook_thread = std::make_unique<std::thread>([&] {
            if (!m_pipe_manager.initialize())
            {
                m_status = HookStatus::pipe_failed;
                return;
            }

            g_hook_handle_sys = SetWindowsHookEx(WH_GETMESSAGE, SysMsgProc, g_hDll, 0);
            g_hook_handle_wnd = SetWindowsHookEx(WH_CALLWNDPROC, CallWndProc, g_hDll, 0);

            if (g_hook_handle_sys && g_hook_handle_wnd)
            {
                m_status = HookStatus::success;
                cpu_save_infinite_wait();
            }
            else
            {
                m_status = HookStatus::hook_failed;
            }
        });
    }

    ~InjectorManager() {

        MSG msg = {};
        msg.message = 0xFFFFFFFF;
        m_pipe_manager.send_message(&msg);

        if (g_hook_handle_sys && UnhookWindowsHookEx(g_hook_handle_sys))
            g_hook_handle_sys = nullptr;

        if (g_hook_handle_wnd && UnhookWindowsHookEx(g_hook_handle_wnd))
            g_hook_handle_wnd = nullptr;

        if (m_status == HookStatus::success)
            stop_infinite_wait();
    }

    static auto& instance() {
        static InjectorManager manager;
        return manager;
    }
};

BOOL APIENTRY DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    if (fdwReason == DLL_PROCESS_ATTACH)
        g_hDll = hinstDLL;

    return TRUE;
}

BOOL Initialize()
{
    InjectorManager::instance().initialize();
    return TRUE;
}

BOOL SetApprovedList(int* list)
{
    InjectorManager::instance().parse_skip_list(&list);
    return TRUE;
}

LRESULT CALLBACK CallWndProc(int nCode, WPARAM wParam, LPARAM lParam)
{
    if (nCode >= 0 && lParam)
        InjectorManager::instance().send_cwp_msg((CWPSTRUCT*)lParam);

    return CallNextHookEx(g_hook_handle_wnd, nCode, wParam, lParam);
}

LRESULT CALLBACK SysMsgProc(int nCode, WPARAM wParam, LPARAM lParam)
{
    if (nCode >= 0 && lParam)
        InjectorManager::instance().send_msg((MSG*)lParam);

    return CallNextHookEx(g_hook_handle_sys, nCode, wParam, lParam);
}
