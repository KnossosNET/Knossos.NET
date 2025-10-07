//#define ENABLE_LOG
#include <jni.h>
#include <SDL.h>
#include <atomic>
#include <cmath>
#include <thread>

#ifdef ENABLE_LOG
#include <android/log.h>
#define LOGI(...) __android_log_print(ANDROID_LOG_INFO, "NB", __VA_ARGS__)
#define LOGW(...) __android_log_print(ANDROID_LOG_WARN,  "NB", __VA_ARGS__)
#endif

static std::atomic<Uint32> sdl_window_id{0};
static std::atomic<bool>  g_mouse_active{false};
static std::atomic<float> g_vx{0}, g_vy{0};
static Uint32 g_last_ms = 0;
// dpad/mouse settings
const float dead = 0.12f; // deadzone
const float baseSpeed = 1100.f; // sensitivity bigger = faster
const float accelExp  = 1.6f; // acceleration

static Uint32 detect_window_id_once() {
    SDL_Window* w = SDL_GetKeyboardFocus();
    if (!w) w = SDL_GetMouseFocus();
    if (!w) w = SDL_GetGrabbedWindow();
    return w ? SDL_GetWindowID(w) : 0;
}

static Uint32 ensure_window_id() {
    Uint32 wid = sdl_window_id.load(std::memory_order_relaxed);
    if (wid && SDL_GetWindowFromID(wid)) return wid;
    wid = detect_window_id_once();
    sdl_window_id.store(wid, std::memory_order_relaxed);
    return wid;
}

static void push_key(jboolean down, SDL_Scancode sc, SDL_Keycode kc) {
    SDL_Event e{};
    e.type = down ? SDL_KEYDOWN : SDL_KEYUP;
    e.key.state = down ? SDL_PRESSED : SDL_RELEASED;
    e.key.repeat = 0;
    e.key.keysym.scancode = sc;
    e.key.keysym.sym = kc;
    e.key.keysym.mod = SDL_GetModState();
    e.key.windowID = ensure_window_id();

    int ret = SDL_PushEvent(&e);
    #ifdef ENABLE_LOG
    if (ret != 1) {
        LOGW("SDL_PushEvent %s %s ret=%d windowID=%u err=%s",
             down ? "KEYDOWN" : "KEYUP",
             SDL_GetKeyName(kc),
             ret, e.key.windowID, SDL_GetError());
    } else {
        LOGI("SDL_PushEvent %s %s ret=%d windowID=%u",
             down ? "KEYDOWN" : "KEYUP",
             SDL_GetKeyName(kc), ret, e.key.windowID);
    }
    #endif
}

static void push_alt_combo(jboolean down, SDL_Scancode sc, SDL_Keycode kc) {
    if (down) {
        push_key(true,  SDL_SCANCODE_LALT, SDLK_LALT);
        push_key(true,  sc, kc);
    } else {
        push_key(false, sc, kc);
        push_key(false, SDL_SCANCODE_LALT, SDLK_LALT);
    }
}

extern "C" JNIEXPORT void JNICALL
Java_com_knossosnet_knossosnet_overlay_NativeBridge_onButton(JNIEnv*, jclass, jint code, jboolean down) {
    auto send = [&](SDL_Scancode sc, SDL_Keycode kc){ push_key(down, sc, kc); };
    #ifdef ENABLE_LOG
    LOGI("native onButton code=%d down=%d", (int)code, (int)down);
    #endif
    switch (code) {

        //COMMON AREA
        case 1: // ESC
            push_key(down, SDL_SCANCODE_ESCAPE, SDLK_ESCAPE);
            break;
        case 2: // F3
            push_key(down, SDL_SCANCODE_F3, SDLK_F3);
            break;
        case 3: // ALT+M
            push_alt_combo(down, SDL_SCANCODE_M, SDLK_m);
            break;
        case 4: // ALT+H
            push_alt_combo(down, SDL_SCANCODE_H, SDLK_h);
            break;
        case 5: // ALT+J
            push_alt_combo(down, SDL_SCANCODE_J, SDLK_j);
            break;
        case 6: // ALT+A
            push_alt_combo(down, SDL_SCANCODE_A, SDLK_a);
            break;

        //Weapons Area
        case 20: // Space
            push_key(down, SDL_SCANCODE_SPACE, SDLK_SPACE);
            break;
        case 21: // Ctrl
            push_key(down, SDL_SCANCODE_LCTRL, SDLK_LCTRL);
            break;
        case 22: // Cycle P
            push_key(down, SDL_SCANCODE_PERIOD, SDLK_PERIOD);
            break;
        case 23: // Cycle S
            push_key(down, SDL_SCANCODE_SLASH, SDLK_SLASH);
            break;

        // General
        case 40: // Tab
            push_key(down, SDL_SCANCODE_TAB, SDLK_TAB);
            break;
        case 41: // +
            push_key(down, SDL_SCANCODE_EQUALS, SDLK_EQUALS);
            break;
        case 42: // -
            push_key(down, SDL_SCANCODE_MINUS, SDLK_MINUS);
            break;
        case 43: // Q
            push_key(down, SDL_SCANCODE_Q, SDLK_q);
            break;
        case 44: // X
            push_key(down, SDL_SCANCODE_X, SDLK_x);
            break;
        case 45: // M
            push_key(down, SDL_SCANCODE_M, SDLK_m);
            break;
        case 46: // A
            push_key(down, SDL_SCANCODE_A, SDLK_a);
            break;
        case 47: // Z
            push_key(down, SDL_SCANCODE_Z, SDLK_z);
            break;
        case 48: // backslash
            push_key(down, SDL_SCANCODE_BACKSLASH, SDLK_BACKSLASH);
            break;
        case 49: // backspace
            push_key(down, SDL_SCANCODE_BACKSPACE, SDLK_BACKSPACE);
            break;

        //Targeting Area
        case 30: // Y
            push_key(down, SDL_SCANCODE_Y, SDLK_y);
            break;
        case 31: // H
            push_key(down, SDL_SCANCODE_H, SDLK_h);
            break;
        case 32: // B
            push_key(down, SDL_SCANCODE_B, SDLK_b);
            break;
        case 33: // E
            push_key(down, SDL_SCANCODE_E, SDLK_e);
            break;
        case 34: // F
            push_key(down, SDL_SCANCODE_F, SDLK_f);
            break;
        case 35: // T
            push_key(down, SDL_SCANCODE_T, SDLK_t);
            break;
        case 36: // T
            push_key(down, SDL_SCANCODE_S, SDLK_s);
            break;

        default:
            #ifdef ENABLE_LOG
            LOGI("Error: Unmapped key code=%d, (int)code);
            #endif
            break;
    }
}

namespace macro {
    enum Kind { DOWN, UP, TAP, WAIT };
    struct Step {
        Kind kind;
        SDL_Scancode sc;
        SDL_Keycode  kc;
        Uint32 ms;
        static Step Down(SDL_Scancode s, SDL_Keycode k){ return {DOWN,s,k,0}; }
        static Step Up  (SDL_Scancode s, SDL_Keycode k){ return {UP,  s,k,0}; }
        static Step Tap (SDL_Scancode s, SDL_Keycode k, Uint32 durMs=40){ return {TAP,s,k,durMs}; }
        static Step Wait(Uint32 durMs){ return {WAIT, SDL_SCANCODE_UNKNOWN, SDLK_UNKNOWN, durMs}; }
    };

    static std::atomic<bool> g_cancel{false};
    static std::atomic<bool> g_running{false};

    static void exec(const std::vector<Step>& steps) {
        g_running.store(true);
        g_cancel.store(false);
        for (const auto& st : steps) {
            if (g_cancel.load()) break;
            switch (st.kind) {
                case DOWN: push_key(true,  st.sc, st.kc); break;
                case UP:   push_key(false, st.sc, st.kc); break;
                case TAP:
                    push_key(true,  st.sc, st.kc);
                    SDL_Delay(st.ms);
                    push_key(false, st.sc, st.kc);
                    break;
                case WAIT:
                    SDL_Delay(st.ms);
                    break;
            }
        }
        g_running.store(false);
    }

    static std::vector<Step> make(int id) {
        using S = Step;
        const Uint32 TAP = 45;
        const Uint32 GAP = 350; // key wait

        switch (id) {
            case 1: // C + 3 + 1
                return {
                        S::Tap (SDL_SCANCODE_C, SDLK_c, TAP),
                        S::Wait(GAP),
                        S::Tap (SDL_SCANCODE_3, SDLK_3, TAP),
                        S::Wait(GAP),
                        S::Tap (SDL_SCANCODE_1, SDLK_1, TAP),
                };

            case 2: // C + 3 + 6
                return {
                        S::Tap (SDL_SCANCODE_C, SDLK_c, TAP),
                        S::Wait(GAP),
                        S::Tap (SDL_SCANCODE_3, SDLK_3, TAP),
                        S::Wait(GAP),
                        S::Tap (SDL_SCANCODE_6, SDLK_6, TAP),
                };

            case 3: // C + 3 + 9
                return {
                        S::Tap (SDL_SCANCODE_C, SDLK_c, TAP),
                        S::Wait(GAP),
                        S::Tap (SDL_SCANCODE_3, SDLK_3, TAP),
                        S::Wait(GAP),
                        S::Tap (SDL_SCANCODE_9, SDLK_9, TAP),
                };

            case 4: // C + 5
                return {
                        S::Tap (SDL_SCANCODE_C, SDLK_c, TAP),
                        S::Wait(GAP),
                        S::Tap (SDL_SCANCODE_5, SDLK_5, TAP),
                };

            case 5: // C + 3 + 5
                return {
                        S::Tap (SDL_SCANCODE_C, SDLK_c, TAP),
                        S::Wait(GAP),
                        S::Tap (SDL_SCANCODE_3, SDLK_3, TAP),
                        S::Wait(GAP),
                        S::Tap (SDL_SCANCODE_5, SDLK_5, TAP),
                };

            default:
                return {};
        }
    }

    static void start_async(int id) {
        if (g_running.load()) return;
        auto steps = make(id);
        if (steps.empty()) return;
        std::thread([s = std::move(steps)]() { exec(s); }).detach();
    }

    static void cancel() {
        g_cancel.store(true);
    }
}

extern "C" JNIEXPORT void JNICALL
Java_com_knossosnet_knossosnet_overlay_NativeBridge_runMacro(JNIEnv*, jclass, jint id) {
    macro::start_async((int)id);
}
extern "C" JNIEXPORT void JNICALL
Java_com_knossosnet_knossosnet_overlay_NativeBridge_cancelMacros(JNIEnv*, jclass) {
    macro::cancel();
}

extern "C" JNIEXPORT void JNICALL
Java_com_knossosnet_knossosnet_overlay_NativeBridge_mouseStart(JNIEnv*, jclass) {
    SDL_SetRelativeMouseMode(SDL_TRUE);
    g_mouse_active.store(true);
    g_last_ms = SDL_GetTicks();
    #ifdef ENABLE_LOG
    LOGI("mouseStart");
    #endif
}

extern "C" JNIEXPORT void JNICALL
Java_com_knossosnet_knossosnet_overlay_NativeBridge_mouseStop(JNIEnv*, jclass) {
    g_mouse_active.store(false);
    SDL_SetRelativeMouseMode(SDL_FALSE);
    g_vx.store(0); g_vy.store(0);
    #ifdef ENABLE_LOG
    LOGI("mouseStop");
    #endif
}

extern "C" JNIEXPORT void JNICALL
Java_com_knossosnet_knossosnet_overlay_NativeBridge_mouseTick(JNIEnv*, jclass, jfloat nx, jfloat ny) {
    if (!g_mouse_active.load()) return;


    float x = nx, y = ny;
    float mag = std::sqrt(x*x + y*y);
    if (mag < dead) { x = y = 0.f; mag = 0.f; }
    if (mag > 0.f) { x /= mag; y /= mag; }

    float speed = baseSpeed * std::pow(mag, accelExp);

    Uint32 now = SDL_GetTicks();
    float dt = (now - g_last_ms) / 1000.0f;
    g_last_ms = now;

    int dx = (int)std::lround(x * speed * dt);
    int dy = (int)std::lround(-y * speed * dt);

    if (dx == 0 && dy == 0) return;

    SDL_Event e{};
    e.type = SDL_MOUSEMOTION;
    e.motion.windowID = ensure_window_id();
    e.motion.state = 0; // no buttons
    e.motion.x = 0; // using relative
    e.motion.y = 0;
    e.motion.xrel = dx;
    e.motion.yrel = dy;

    SDL_PushEvent(&e);
}