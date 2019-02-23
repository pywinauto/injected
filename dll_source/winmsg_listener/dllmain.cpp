#pragma once
#define _WINSOCKAPI_
#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#include <windows.h>
#include <fstream>
#include <comdef.h>
#include <string>
#include <sstream>
#include <thread>
#include <vector>
#include <mutex>   
#include <condition_variable>
#include "TlHelp32.h"
#pragma comment (lib, "Ws2_32.lib")

HHOOK g_hHook = nullptr;
HINSTANCE g_hDll = nullptr;

BOOL APIENTRY DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved);
extern "C" __declspec(dllexport) BOOL SetMsgHook(wchar_t* path);    // Exported
extern "C" __declspec(dllexport) BOOL UnsetMsgHook();
extern "C" __declspec(dllexport) BOOL OpenLogFile(wchar_t* path);
extern "C" __declspec(dllexport) BOOL InitSocket(int* port);
LRESULT CALLBACK SysMsgProc(int nCode, WPARAM wParam, LPARAM lParam);

class InjectorManager {
private:
	//

	void print_log(const std::wstring& message)
	{
		if (m_log_file.is_open()) {
			m_log_file << message << std::endl;
		}
	}

	void message_to_file(LPMSG aMsg) {
		if (m_log_file.is_open()) {
			std::wstringstream ss;
			ss << aMsg->time << ";";
			ss << aMsg->message << ";";
			ss << aMsg->hwnd << ";";
			ss << aMsg->lParam << ";";
			ss << aMsg->wParam << ";";
			ss << aMsg->pt.x << ";";
			ss << aMsg->pt.y << ";";
			print_log(ss.str());
		}
	}

	void cpu_save_infinite_wait() {
		
		std::unique_lock<std::mutex> lock(m_hook_mutex);
		while (!m_hook_stop_thread)
		{
			m_hook_cv.wait(lock);
		}

		std::vector<char> message_buffer(m_msg_size, 0);
		//initialize_socket(m_port);
		print_log(L"TRYYYYYYYYYYYYYY!");
		if (m_socket_initialized && sendto(m_socket, message_buffer.data(), (int)m_msg_size, 0, (sockaddr*)&m_sender_address, sizeof(m_sender_address)) < 0)
		{
			wchar_t *s = NULL;
			FormatMessageW(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
				NULL, WSAGetLastError(),
				MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
				(LPWSTR)&s, 0, NULL);
			std::wstring skdaksjk(s);
			LocalFree(s);
			print_log(skdaksjk);
			closesocket(m_socket);
			m_socket_initialized = false;
			print_log(L"FAIIIIIIIIIIL!");;
		}

	}

	std::wstring get_last_error()
	{
		HRESULT hr = HRESULT_FROM_WIN32(GetLastError());
		_com_error err(hr);
		LPCTSTR errMsg = err.ErrorMessage();
		return std::wstring();
	}

	SOCKET m_socket;
	struct sockaddr_in m_sender_address;
	bool m_socket_initialized = false;
	std::wofstream m_log_file;
	const static size_t m_msg_size = sizeof(MSG);
	bool							m_hook_stop_thread = false;
	std::unique_ptr<std::thread>	m_hook_thread;
	std::condition_variable			m_hook_cv;
	std::mutex						m_hook_mutex;
	bool							m_socket_stop_thread = false;
	std::unique_ptr<std::thread>	m_socket_thread;
	std::condition_variable			m_socket_cv;
	std::mutex						m_socket_mutex;
	bool m_wait;
	int m_port;
public:
	InjectorManager() {}
	InjectorManager(const InjectorManager&) = delete;
	InjectorManager& operator=(const InjectorManager &) = delete;
	InjectorManager(InjectorManager &&) = delete;
	InjectorManager & operator=(InjectorManager &&) = delete;


	BOOL open_log_file(wchar_t* path) {
		if (path)
			m_log_file.open(path);
		return m_log_file.is_open();
	}

	BOOL initialize_socket(int port) {
		m_port = port;
		//
		//m_socket_thread = std::make_unique<std::thread>([&] {
			WSADATA wsaData = {};
			WSAStartup(MAKEWORD(2, 2), &wsaData);
			m_socket = socket(AF_INET, SOCK_DGRAM, 0);
			if (m_socket == INVALID_SOCKET) {
				return FALSE;
			}
			BOOL enabled = TRUE;
			if (setsockopt(m_socket, SOL_SOCKET, SO_BROADCAST, (char*)&enabled, sizeof(BOOL)) < 0) {
				return FALSE;
			}
			m_sender_address.sin_family = AF_INET;
			m_sender_address.sin_port = htons(m_port);
			m_sender_address.sin_addr.s_addr = inet_addr("localhost");
			m_socket_initialized = true;

			{
				std::unique_lock<std::mutex> lock(m_socket_mutex);
				while (!m_socket_stop_thread)
				{
					m_socket_cv.wait(lock);
				}
			}



		//});
		return TRUE;
	}

	void set_msg_hook(wchar_t* path = nullptr)
	{
		MessageBoxA(0, "Attach now", "InitSocketCall", 0);
		//m_hook_thread = std::make_unique<std::thread>([&] {
			OpenLogFile(path);
			g_hHook = SetWindowsHookEx(WH_GETMESSAGE, SysMsgProc, g_hDll, 0);
			if (g_hHook != nullptr)
			{
				print_log(L"Hook successfully set!");
				print_log(L"time;hwnd;lParam;wParam;message;pt.x;pt.y");
			}
			else
			{
				print_log(L"Error when hook set: " + get_last_error());
				//return FALSE;
			}
			cpu_save_infinite_wait();


			std::vector<char> message_buffer(m_msg_size, 0);
			//initialize_socket(m_port);
			print_log(L"TRYYYYYYYYYYYYYY!");
			if (m_socket_initialized && sendto(m_socket, message_buffer.data(), (int)m_msg_size, 0, (sockaddr*)&m_sender_address, sizeof(m_sender_address)) < 0)
			{
				wchar_t *s = NULL;
				FormatMessageW(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
					NULL, WSAGetLastError(),
					MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
					(LPWSTR)&s, 0, NULL);
				std::wstring skdaksjk(s);
				LocalFree(s);
				print_log(skdaksjk);
				closesocket(m_socket);
				m_socket_initialized = false;
				print_log(L"FAIIIIIIIIIIL!");;
			}

		//});
	}

	void send_msg(const MSG* msg)
	{
		std::vector<char> message_buffer(m_msg_size, 0);
		//message_to_file(*msg);
		memcpy(message_buffer.data(), msg, m_msg_size);
		if (m_socket_initialized && sendto(m_socket, message_buffer.data(), (int)m_msg_size, 0, (sockaddr*)&m_sender_address, sizeof(m_sender_address)) < 0)
		{
			closesocket(m_socket);
			m_socket_initialized = false;
			print_log(L"Socket closed!");
		}
	}

	void unload() {

		std::vector<char> message_buffer(m_msg_size, 0);
		//initialize_socket(m_port);
		print_log(L"TRYYYYYYYYYYYYYY!");
		if (m_socket_initialized && sendto(m_socket, message_buffer.data(), (int)m_msg_size, 0, (sockaddr*)&m_sender_address, sizeof(m_sender_address)) < 0)
		{
			wchar_t *s = NULL;
			FormatMessageW(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
				NULL, WSAGetLastError(),
				MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
				(LPWSTR)&s, 0, NULL);
			std::wstring skdaksjk(s);
			LocalFree(s);
			print_log(skdaksjk);
			closesocket(m_socket);
			m_socket_initialized = false;
			print_log(L"FAIIIIIIIIIIL!");;
		}

		{
			std::unique_lock<std::mutex> lock(m_hook_mutex);
			m_hook_stop_thread = true;
			m_hook_cv.notify_one();

		}
		{
			std::unique_lock<std::mutex> lock(m_socket_mutex);
			m_socket_stop_thread = true;
			m_socket_cv.notify_one();
		}

		if (g_hHook == nullptr)
			return;

		BOOL isUnhooked = UnhookWindowsHookEx(g_hHook);
		if (isUnhooked)
		{
			print_log(L"Unhook");
			g_hHook = nullptr;
		}

		if (m_log_file.is_open())
			m_log_file.close();
		m_wait = false;
	}

	static auto& instance() {
		static InjectorManager test;
		return test;
	}
};

extern "C" __declspec(dllexport) HINSTANCE getDllHinstance() {
    return g_hDll;
}

BOOL APIENTRY DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    switch (fdwReason)
    {
    case DLL_PROCESS_ATTACH:
        g_hDll = hinstDLL;
        return true;
    case DLL_PROCESS_DETACH:
		InjectorManager::instance().unload();
        return true;
    }
}

BOOL OpenLogFile(wchar_t* path) {
    return InjectorManager::instance().open_log_file(path);
}

BOOL InitSocket(int* port) {
	int _port = *port;
	return InjectorManager::instance().initialize_socket(_port);
}

BOOL SetMsgHook(wchar_t* path = nullptr)
{
	InjectorManager::instance().set_msg_hook(path);
    return TRUE;
}


LRESULT CALLBACK SysMsgProc(int nCode, WPARAM wParam, LPARAM lParam)
{
    if (nCode < 0)
        return CallNextHookEx(g_hHook, nCode, wParam, lParam);

    LPMSG aMsg = (LPMSG)lParam;
    if (aMsg) {
		InjectorManager::instance().send_msg((MSG*)aMsg);
    }
    return CallNextHookEx(g_hHook, nCode, wParam, lParam);
}
