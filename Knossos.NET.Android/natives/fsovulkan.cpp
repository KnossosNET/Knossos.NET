#include <adrenotools/driver.h>
#include <android/log.h>
#include <dlfcn.h>
#include <jni.h>
#include <vulkan/vulkan.h>

#include <cstring>
#include <string>
#include <vector>

namespace {

constexpr char LogTag[] = "FSOVulkanDriver";
PFN_vkGetInstanceProcAddr driverGetProc = nullptr;

std::string getString(JNIEnv* env, jstring value)
{
    if (!value) {
        return {};
    }
    const auto* utf = env->GetStringUTFChars(value, nullptr);
    if (!utf) {
        return {};
    }
    std::string result(utf);
    env->ReleaseStringUTFChars(value, utf);
    return result;
}

bool supportsAndroidSurface(PFN_vkGetInstanceProcAddr getProc)
{
    const auto enumerate = reinterpret_cast<PFN_vkEnumerateInstanceExtensionProperties>(
        getProc(VK_NULL_HANDLE, "vkEnumerateInstanceExtensionProperties"));
    if (!enumerate || !getProc(VK_NULL_HANDLE, "vkCreateInstance")) {
        return false;
    }

    uint32_t count = 0;
    if (enumerate(nullptr, &count, nullptr) != VK_SUCCESS || count == 0) {
        return false;
    }
    std::vector<VkExtensionProperties> extensions(count);
    if (enumerate(nullptr, &count, extensions.data()) != VK_SUCCESS) {
        return false;
    }

    bool surface = false;
    bool androidSurface = false;
    for (uint32_t i = 0; i < count; ++i) {
        const auto* name = extensions[i].extensionName;
        surface |= std::strcmp(name, VK_KHR_SURFACE_EXTENSION_NAME) == 0;
        androidSurface |= std::strcmp(name, VK_KHR_ANDROID_SURFACE_EXTENSION_NAME) == 0;
    }
    return surface && androidSurface;
}

bool setVulkanHint(const std::string& sdlPath, const std::string& shimPath)
{
    // SDL comes with the selected FSO build and has already been loaded by GameActivity.
    auto* sdl = dlopen(sdlPath.c_str(), RTLD_NOW | RTLD_NOLOAD);
    if (!sdl) {
        return false;
    }
    const auto setHint = reinterpret_cast<bool (*)(const char*, const char*)>(dlsym(sdl, "SDL_SetHint"));
    const bool result = setHint && setHint("SDL_VULKAN_LIBRARY", shimPath.c_str());
    dlclose(sdl);
    return result;
}

} // namespace

// SDL resolves this symbol from the shim when it loads Vulkan.
extern "C" VKAPI_ATTR __attribute__((visibility("default"))) PFN_vkVoidFunction VKAPI_CALL
vkGetInstanceProcAddr(VkInstance instance, const char* name)
{
    return driverGetProc ? driverGetProc(instance, name) : nullptr;
}

extern "C" JNIEXPORT jboolean JNICALL
Java_com_knossosnet_knossosnet_VulkanDriverLoader_configure(
    JNIEnv* env, jclass, jstring nativeDir, jstring tempDir, jstring driverDir,
    jstring libraryName, jstring sdlLibrary)
{
    const auto hooks = getString(env, nativeDir);
    const auto temp = getString(env, tempDir);
    const auto directory = getString(env, driverDir);
    const auto library = getString(env, libraryName);
    const auto sdl = getString(env, sdlLibrary);
    if (hooks.empty() || directory.empty() || library.empty() || sdl.empty()) {
        return JNI_FALSE;
    }

    // PanVK implements its own Android WSI. Use the same ICD entry point as FSO Wrapper.
    const bool directPanvk = library == "libvulkan_panfrost.so";
    const auto driverPath = directory + "/";
    const auto tempPath = temp + "/";
    auto* loader = directPanvk
        ? dlopen((driverPath + library).c_str(), RTLD_NOW | RTLD_LOCAL)
        : adrenotools_open_libvulkan(RTLD_NOW | RTLD_LOCAL, ADRENOTOOLS_DRIVER_CUSTOM,
            temp.empty() ? nullptr : tempPath.c_str(), hooks.c_str(),
            driverPath.c_str(), library.c_str(), nullptr, nullptr);

    if (!loader) {
        const char* error = dlerror();
        __android_log_print(ANDROID_LOG_WARN, LogTag, "Could not open %s: %s",
            library.c_str(), error ? error : "unknown loader error");
        return JNI_FALSE;
    }

    auto getProc = directPanvk ? reinterpret_cast<PFN_vkGetInstanceProcAddr>(
        dlsym(loader, "vk_icdGetInstanceProcAddr")) : nullptr;
    if (!getProc) {
        getProc = reinterpret_cast<PFN_vkGetInstanceProcAddr>(dlsym(loader, "vkGetInstanceProcAddr"));
    }
    if (!getProc || !supportsAndroidSurface(getProc)) {
        __android_log_print(ANDROID_LOG_WARN, LogTag, "Loader for %s lacks Android Vulkan surface support",
            library.c_str());
        dlclose(loader);
        return JNI_FALSE;
    }

    driverGetProc = getProc;
    if (!setVulkanHint(sdl, hooks + "/libfsovulkan.so")) {
        __android_log_print(ANDROID_LOG_WARN, LogTag, "Could not set SDL_HINT_VULKAN_LIBRARY");
        driverGetProc = nullptr;
        dlclose(loader);
        return JNI_FALSE;
    }

    // Keep the loader and its dispatch functions alive until the :game process exits.
    __android_log_print(ANDROID_LOG_INFO, LogTag, "Configured %s with %s",
        library.c_str(), directPanvk ? "the PanVK ICD" : "libadrenotools");
    return JNI_TRUE;
}
