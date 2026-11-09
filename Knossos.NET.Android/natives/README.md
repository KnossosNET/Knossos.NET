# Native touch controls

Knossos.NET packages these prebuilt libraries; its Android project does not compile their C++ source.

The source is `Fso_Android_Wrapper/app/src/main/cpp/touch_controls.cpp` and `touch_text_input.h`.
This revision sends printable overlay button presses as SDL text input while text entry is active,
preserves gameplay key events, and excludes Ctrl/Alt/GUI shortcuts. Text dispatch runs on the SDL main thread.

Build: 2026-11-09, Android NDK 28.2.13676358, minimum API 28, C++17, static C++ runtime.
The libraries retain both existing NativeBridge JNI namespaces and all 14 entry points. ELF load alignment is 16 KB.

touch_controls.cpp SHA-256: `1F13F0A29E82D1366ED1448824C7D1CF099647094C9BA6F9742E08A8D9E80C66`
touch_text_input.h SHA-256: `80E9029F73E48A15903C380163BBD85B28FCACB180472720E25E42B30D76378C`

| ABI | libtouchcontrols.so SHA-256 |
| --- | --- |
| arm64-v8a | `54FBFD936E8FF00CDA24DDDE861B228B82E8011DD6C7F61B3CDC2DC3ED8674F3` |
| armeabi-v7a | `940C543CE6B95218AB9CC70BC560BE82E1ED97F137C7621AA2D8A583CF734463` |
| x86 | `35D7313BBB288F4577EB8495262A82A9EFCC1C6BC557AC0C70A649D68A34987D` |
| x86_64 | `0E744011D933B1FA453900B2F69F35ACA6D246E614BD6235BB8641D38E716FD6` |

To update, rebuild the native library in Fso_Android_Wrapper for all four ABIs and copy each
`libtouchcontrols.so` into its matching directory here. The Android csproj packages them through
its existing AndroidNativeLibrary items. Do not copy binaries across ABI directories.

The native SDL regression tests are in the wrapper C++ `tests` directory. SCPUI on Android also
needs the engine-side text conversion fix; updating this library alone does not replace that fix.
