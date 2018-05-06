cmake_minimum_required( VERSION 3.4.0 )

project( winmsg_cather CXX )			

set( SOURCE_LIB dllmain.cpp )		

set ( CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON )

add_library( winmsg_catcher SHARED ${SOURCE_LIB} )
