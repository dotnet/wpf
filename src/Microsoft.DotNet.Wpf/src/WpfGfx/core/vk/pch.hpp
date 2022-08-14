// pch.h: 这是预编译标头文件。
// 下方列出的文件仅编译一次，提高了将来生成的生成性能。
// 这还将影响 IntelliSense 性能，包括代码完成和许多代码浏览功能。
// 但是，如果此处列出的文件中的任何一个在生成之间有更新，它们全部都将被重新编译。
// 请勿在此处添加要频繁更新的文件，这将使得性能优势无效。

#ifndef PCH_H
#define PCH_H

// 添加要在此处预编译的标头
#define VULKAN_HPP_NO_EXCEPTIONS
#define VULKAN_HPP_TYPESAFE_CONVERSION
#include <vulkan/vulkan.hpp>

#include "common/common.h"
#include "scanop/scanop.h"

// needed for geometry
#include "geometry/geometry.h"

#include "api/api_include.h"

#include "targets/targets.h"

// Needed for Video
#include "av/av.h"

// Needed for CBrushRealizer
#include "meta/meta.h"

#include "effects/effectlist.h"
#include "uce/uce.h"
#include "resources/resources.h"

#include "fxjit/public/effectparams.h"
#include "fxjit/public/pshader.h"


#endif //PCH_H
