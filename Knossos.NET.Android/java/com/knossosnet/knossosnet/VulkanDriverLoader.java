package com.knossosnet.knossosnet;

import android.app.Activity;
import android.content.Intent;
import android.os.Build;
import android.util.Log;
import android.widget.Toast;

import java.io.File;

final class VulkanDriverLoader {
    private static final String TAG = "FSOVulkanDriver";

    private VulkanDriverLoader() {}

    static void prepare(Activity activity, Intent intent, File sdlLibrary) {
        String directory = intent == null ? null : intent.getStringExtra("vulkanDriverDirectory");
        if (directory == null) return;

        try {
            System.loadLibrary("fsovulkan");
            String nativeDir = activity.getApplicationInfo().nativeLibraryDir;
            // Android 9 needs a writable location for the private copy of libvulkan.
            String tempDir = Build.VERSION.SDK_INT == 28 ? activity.getCacheDir().getAbsolutePath() : null;
            if (configure(nativeDir, tempDir, directory, intent.getStringExtra("vulkanDriverLibrary"),
                    sdlLibrary.getAbsolutePath())) {
                return;
            }
            Log.w(TAG, "Custom Vulkan loader could not be initialized");
        } catch (LinkageError | RuntimeException e) {
            Log.w(TAG, "Custom Vulkan loader unavailable", e);
        }
        activity.runOnUiThread(() -> Toast.makeText(activity,
                "Could not load the custom Vulkan driver. Using the system driver.",
                Toast.LENGTH_LONG).show());
    }

    private static native boolean configure(
            String nativeDir, String tempDir, String driverDir, String libraryName, String sdlLibrary);
}
