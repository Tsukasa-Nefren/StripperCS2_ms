cmake_minimum_required(VERSION 3.20)
project(StripperCS2_ms_adapter CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

include(FetchContent)

# ---------- spdlog (vendor or FetchContent) ----------
set(SPDLOG_TARGET spdlog::spdlog)
if (EXISTS "${CMAKE_SOURCE_DIR}/vendor/spdlog/CMakeLists.txt")
  add_subdirectory(vendor/spdlog EXCLUDE_FROM_ALL)
else()
  message(STATUS "vendor/spdlog not found, fetching spdlog...")
  FetchContent_Declare(spdlog
    GIT_REPOSITORY https://github.com/gabime/spdlog.git
    GIT_TAG v1.13.0
  )
  FetchContent_MakeAvailable(spdlog)
endif()

# ---------- PCRE2 (optional; vendor or FetchContent) ----------
set(PCRE2_AVAILABLE OFF)
if (EXISTS "${CMAKE_SOURCE_DIR}/vendor/pcre2/CMakeLists.txt")
  add_subdirectory(vendor/pcre2 EXCLUDE_FROM_ALL)
  include_directories(vendor/pcre2/src)
  add_definitions(-DPCRE2_CODE_UNIT_WIDTH=8)
  set(PCRE2_AVAILABLE ON)
else()
  message(STATUS "vendor/pcre2 not found, attempting to fetch PCRE2...")
  FetchContent_Declare(pcre2
    GIT_REPOSITORY https://github.com/PCRE2Project/pcre2.git
    GIT_TAG pcre2-10.43
  )
  FetchContent_MakeAvailable(pcre2)
  # Try the exported target name. If it fails on your fork, keep header-only include as fallback.
  if (TARGET PCRE2::pcre2-8)
    set(PCRE2_AVAILABLE ON)
  else()
    include_directories(${pcre2_SOURCE_DIR}/src)
    add_definitions(-DPCRE2_CODE_UNIT_WIDTH=8)
  endif()
endif()

# ---------- Sources ----------
include_directories(src)
file(GLOB_RECURSE SRC_ACTIONS   src/actions/*.cpp)
file(GLOB_RECURSE SRC_PROVIDERS src/providers/*.cpp)
file(GLOB_RECURSE SRC_UTILS     src/utils/*.cpp)

# exclude metamod/funchook entry points
list(FILTER SRC_UTILS EXCLUDE REGEX "extension\\.cpp$")
list(FILTER SRC_UTILS EXCLUDE REGEX "hook\\.cpp$")

set(SRC_BRIDGE src/bridge/bridge_api.cpp)

add_library(StripperCS2_ms_adapter SHARED
  ${SRC_ACTIONS}
  ${SRC_PROVIDERS}
  ${SRC_UTILS}
  ${SRC_BRIDGE}
)

target_link_libraries(StripperCS2_ms_adapter PRIVATE ${SPDLOG_TARGET})
if (PCRE2_AVAILABLE)
  target_link_libraries(StripperCS2_ms_adapter PRIVATE PCRE2::pcre2-8)
endif()

target_compile_definitions(StripperCS2_ms_adapter PRIVATE _CRT_SECURE_NO_WARNINGS)

if (MSVC)
  add_definitions(/bigobj)
  target_compile_definitions(StripperCS2_ms_adapter PRIVATE _WIN32 WIN32_LEAN_AND_MEAN)
endif()
