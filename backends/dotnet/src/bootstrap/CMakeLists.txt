file(GLOB SOURCES
    "${CMAKE_CURRENT_SOURCE_DIR}/*.h"
    "${CMAKE_CURRENT_SOURCE_DIR}/*.hpp"
    "${CMAKE_CURRENT_SOURCE_DIR}/*.cpp"
  ) 
add_library(bootstrap SHARED ${SOURCES})

# install

# set architecture var (x86 or x64)
if(CMAKE_SIZEOF_VOID_P EQUAL 8)
	set(ARCH x64)
elseif(CMAKE_SIZEOF_VOID_P EQUAL 4)
	set(ARCH x86)
endif()

set(INSTALL_DIR ${BACKEND_BIN_DIR}/${ARCH})
install(TARGETS bootstrap 
		RUNTIME
		DESTINATION ${INSTALL_DIR})