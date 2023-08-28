cmake_minimum_required(VERSION 3.21)
set(PROJECT_NAME Bootstrap)
#set(CMAKE_VERBOSE_MAKEFILE ON)
project(${PROJECT_NAME})

if(NOT USING_WRAPPER)
    message(WARNING "recommend to use ./tools/cmake_wrapper.sh instead")
endif()

set(CMAKE_CXX_STANDARD 17)

function(omit_path GLOB_VARIABLE EXCLUDED_DIR)
    file(GLOB_RECURSE EXCLUSION "${EXCLUDED_DIR}")
    list(REMOVE_ITEM ${GLOB_VARIABLE} ${EXCLUSION})
    set(${GLOB_VARIABLE} ${${GLOB_VARIABLE}} PARENT_SCOPE)
endfunction()

set(BOOTSTRAP_DEPS "")

file(GLOB_RECURSE SRC_FILES  "./Managers/*.cpp" "./Utils/*.cpp")
file(GLOB_RECURSE HEADERS_FILES "./Managers/*.h" "./Utils/*.h" ./Core.h)

list(APPEND SRC_FILES ./Core.cpp)
list(APPEND HEADERS_FILES ./Core.h)

if(PLATFORM STREQUAL "ANDROID")
    set(USE_CAPSTONE YES)
    set(USE_FUNCHOOK YES)
    set(USE_DOBBY YES)

    add_compile_definitions(__ANDROID__ PORT_DISABLE)

    list(APPEND SRC_FILES ./Base/main.cpp)
    list(APPEND BOOTSTRAP_DEPS capstone-shared funchook-shared dobby log android)
elseif(PLATFORM STREQUAL "WINDOWS")
    add_compile_definitions(_WIN32)

    list(APPEND SRC_FILES ./Base/DLLMain.cpp)
elseif(NOT DEFINED PLATFORM)
    message(FATAL_ERROR "PLATFORM is unset")
endif()

add_library(${PROJECT_NAME} SHARED ${SRC_FILES} ${HEADER_FILES} Utils/Console/Log.cpp Utils/Console/Log.h Utils/JNIManagedInterface.cpp Utils/JNIManagedInterface.h Utils/AssetHelper.cpp Utils/AssetHelper.h Utils/AssemblyLoader.cpp Utils/AssemblyLoader.h)

if(DEFINED USE_CAPSTONE)
    set(CAPSTONE_BUILD_STATIC OFF)
    set(CAPSTONE_BUILD_SHARED ON)
    set(CAPSTONE_BUILD_TESTS OFF)
    set(CAPSTONE_BUILD_CSTOOL OFF)
    set(CAPSTONE_ARCHITECTURE_DEFAULT OFF)

    set(DISABLED_ARCHITECTURES ARM M68K MIPS PPC SPARC SYSZ XCORE X86 TMS320C64X M680X EVM MOS65XX)
    set(ENABLED_ARCHITECTURES ARM64)

    # propagate architecture support variables to preprocessor
    foreach(ARCH ${DISABLED_ARCHITECTURES})
        set("CAPSTONE_${ARCH}_SUPPORT" OFF)
    endforeach(ARCH)
    unset(DISABLED_ARCHITECTURES)

    foreach(ARCH ${ENABLED_ARCHITECTURES})
        set("CAPSTONE_${ARCH}_SUPPORT" ON)
    endforeach(ARCH)

    add_subdirectory("./external/capstone" capstone)
    set_property(
        TARGET capstone-shared
        PROPERTY INTERFACE_INCLUDE_DIRECTORIES "${PROJECT_SOURCE_DIR}/external/capstone/include"
    )
endif()

if(DEFINED USE_FUNCHOOK)
    if(PLATFORM STREQUAL "ANDROID")
        set(FUNCHOOK_CPU arm64)
    else()
        message(FATAL_ERROR "Platform not setup for funchook")
    endif()

    set(FUNCHOOK_BUILD_SHARED ON)
    set(FUNCHOOK_BUILD_STATIC OFF)
    set(FUNCHOOK_BUILD_TESTS OFF)

    add_subdirectory("./external/funchook" funchook)

    get_property(FC_LINK_LIBRARIES TARGET funchook-shared PROPERTY LINK_LIBRARIES)
    list(REMOVE_ITEM LINK_LIBRARIES capstone)
    set_property(TARGET funchook-shared PROPERTY LINK_LIBRARIES dl capstone-shared)
endif()

if(DEFINED USE_DOBBY)
    set(DOBBY_GENERATE_SHARED ON)
    set(FullFloatingPointRegisterPack ON)
    set(Plugin.ImportTableReplace ON)
    set(DOBBY_DEBUG ON)
    set(NearBranch ON)

    add_compile_definitions("ANDROID_LOG_STDOUT")
    add_subdirectory("./external/Dobby" Dobby)

    get_property(FC_LINK_LIBRARIES TARGET dobby PROPERTY LINK_LIBRARIES)
    set_property(TARGET dobby PROPERTY LINK_LIBRARIES dl log android)
endif()

#if (CMAKE_BUILD_TYPE EQUAL "Debug")
add_compile_definitions(DEBUG)
#endif()

target_link_libraries(${PROJECT_NAME} ${BOOTSTRAP_DEPS})
