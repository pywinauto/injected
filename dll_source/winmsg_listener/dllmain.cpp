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
extern "C" __declspec(dllexport) BOOL SetSocketPort(int* port);
extern "C" __declspec(dllexport) BOOL SetSkipList(int* list);
extern "C" __declspec(dllexport) HINSTANCE getDllHinstance() {
    return g_hDll;
}

class SocketManager {
    const static size_t m_msg_size = sizeof(MSG);
    bool                m_socket_initialized = false;
    int                 m_socket_port = 0;
    struct sockaddr_in  m_sender_address;
    SOCKET              m_socket;

public:
    SocketManager() {}

    bool initialize(int port)
    {
        m_socket_port = port;
        WSADATA wsaData = {};
        WSAStartup(MAKEWORD(2, 2), &wsaData);
        m_socket = socket(AF_INET, SOCK_DGRAM, 0);
        if (m_socket == INVALID_SOCKET)
            return false;

        BOOL enabled = TRUE;
        if (setsockopt(m_socket, SOL_SOCKET, SO_BROADCAST, (char*)&enabled, sizeof(BOOL)) < 0)
            return false;

        m_sender_address.sin_family = AF_INET;
        m_sender_address.sin_port = htons(m_socket_port);
        m_sender_address.sin_addr.s_addr = inet_addr("localhost");
        m_socket_initialized = true;
        return true;
    }

    ~SocketManager()
    {
        if (!m_socket_initialized)
            return;

        closesocket(m_socket);
        WSACleanup();
    }

    void send_message(const MSG* msg)
    {
        if (!m_socket_initialized)
            return;

        std::vector<char> message_buffer(m_msg_size, 0);
        memcpy(message_buffer.data(), msg, m_msg_size);
        if (sendto(m_socket, message_buffer.data(), (int)m_msg_size, 0, (sockaddr*)&m_sender_address, sizeof(m_sender_address)) < 0)
        {
            closesocket(m_socket);
            m_socket_initialized = false;
        }
    }
};

class InjectorManager {
    enum HookStatus : uint32_t
    {
        undefined = 0,
        success,
        hook_failed,
        socket_failed,
    };

    bool                            m_hook_stop_thread = false;
    int                             m_socket_port = 0;
    HookStatus                      m_status = undefined;
    SocketManager                   m_socket_manager;
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

    void set_socket_port(int port) {
        m_socket_port = port;
    }

    void send_msg(const MSG* msg)
    {
        std::lock_guard<std::mutex> lg(m_send_mutex);
        if (m_approved_messages_ids.count(msg->message))
            m_socket_manager.send_message(msg);
    }

    void send_cwp_msg(const CWPSTRUCT* data)
    {
        MSG msg = { data->hwnd, data->message, data->lParam, data->wParam };
        if (data->message == WM_NOTIFY)
            msg.lParam = ((LPNMHDR)(data->lParam))->code;

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
            if (!m_socket_manager.initialize(m_socket_port))
            {
                m_status = HookStatus::socket_failed;
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

BOOL SetSocketPort(int* port) {
    InjectorManager::instance().set_socket_port(*port);
    return TRUE;
}

BOOL Initialize()
{
    InjectorManager::instance().initialize();
    return TRUE;
}

BOOL SetSkipList(int* list)
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
