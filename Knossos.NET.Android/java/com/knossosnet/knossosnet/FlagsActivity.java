package com.knossosnet.knossosnet;
import android.content.Intent;
import android.os.Binder;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;
import java.lang.ref.WeakReference;
import android.os.ResultReceiver;
import android.app.Activity;

public class FlagsActivity extends org.libsdl.app.SDLActivity {

    private static WeakReference<FlagsActivity> _self = null;
    private static String _workingFolder = "";
    private static volatile boolean _delivered = false;
    private ResultReceiver _receiver = null;
    private String _resultPath;
    private final Binder _queryProcess = new Binder();
    private final Handler _handler = new Handler(Looper.getMainLooper());
    private final Runnable _timeout = () -> finish();

    /* FSO API */
    public static String getWorkingFolder() { return _workingFolder; }

    public static void setFlagsJson(String json) {
        FlagsActivity act = (_self != null) ? _self.get() : null;
        if (act != null) act.deliver(json);
    }

    public static void setOverlayOpacity(float opacity) {
    }

    public static void enableOverlay() {
    }

    public static void disableOverlay() {
    }

    /* ******* */

    @Override
    protected String[] getArguments() {
        Intent i = getIntent();
        ArrayList<String> args = (i != null) ? i.getStringArrayListExtra("fsoArgs") : null;
        if (args == null || args.isEmpty()) {
            return new String[] { "-get_flags", "json_v2" };
        }
        return args.toArray(new String[0]);
    }

    @Override
    protected String[] getLibraries() { return new String[] { }; }

    @Override
    protected String getMainSharedObject() {
        String path = getIntent().getStringExtra("engineLibName");
        return (path == null || path.isEmpty()) ? null : path;
    }

    @Override
    protected String getMainFunction() { return "android_main"; }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        _self = new WeakReference<>(this);
        _delivered = false;

        Intent i = getIntent();
        if (i != null) {
            _workingFolder = i.getStringExtra("workingFolder");
            if (_workingFolder == null) _workingFolder = "";
            _receiver = i.getParcelableExtra("flagsReceiver");
            _resultPath = i.getStringExtra("resultPath");
        }

        loadNatives();

        setResult(RESULT_CANCELED);

        super.onCreate(savedInstanceState);
        _handler.postDelayed(_timeout, 25000);
    }

    @Override
    protected void onDestroy() {
        _handler.removeCallbacks(_timeout);
        sendResult(Activity.RESULT_CANCELED);
        super.onDestroy();
        _self = null;
        try {
            String proc = android.app.Application.getProcessName();
            if (proc != null && proc.endsWith(":flags")) {
                android.os.Process.killProcess(android.os.Process.myPid());
            }
        } catch (Throwable ignored) {}
    }

    private static final String[] PREFERRED_ORDER = new String[] {
        "libSDL3.so", "libshaderc_shared.so", "libopenal.so", "libavutil.so",
        "libswresample.so", "libswscale.so", "libavcodec.so",
        "libavformat.so", "libavfilter.so"
    };

    private void loadNatives() {
        String engine = getMainSharedObject();
        if (engine == null) return;
        File dir = new File(engine).getParentFile();
        List<File> loadList = orderForLoad(dir);
        List<File> failed = new ArrayList<>();
        for (File so : loadList) if (!tryLoad(so)) failed.add(so);
        for (File so : failed) tryLoad(so);
    }

    private static List<File> orderForLoad(File dir) {
        ArrayList<File> ordered = new ArrayList<>();
        if (dir == null || !dir.isDirectory()) return ordered;
        for (String name : PREFERRED_ORDER) {
            File f = new File(dir, name);
            if (f.isFile()) ordered.add(f);
        }
        File[] arr = dir.listFiles((d, name) -> name != null && name.endsWith(".so"));
        if (arr != null) {
            for (File f : arr) {
                if (!containsName(ordered, f.getName()) && !isEngineName(f.getName())) {
                    ordered.add(f);
                }
            }
        }
        return ordered;
    }

    private boolean tryLoad(File so) {
        try {
            if (so != null && so.isFile()) { System.load(so.getAbsolutePath()); return true; }
        } catch (UnsatisfiedLinkError e) { e.printStackTrace(); }
        return false;
    }

    private static boolean containsName(List<File> list, String name) {
        for (File f : list) if (f.getName().equals(name)) return true;
        return false;
    }

    private static boolean isEngineName(String name) {
        if (name == null) return false;
        return name.startsWith("libfso") || name.contains("libfs2");
    }

    private void deliver(final String json) {
        if (_delivered) return;
        _delivered = true;
        runOnUiThread(() -> {
            int result = Activity.RESULT_CANCELED;
            if (json != null && _resultPath != null) {
                // Keep the full JSON out of Binder transactions. Both processes share this cache.
                try (FileOutputStream output = new FileOutputStream(_resultPath)) {
                    output.write(json.getBytes(StandardCharsets.UTF_8));
                    result = Activity.RESULT_OK;
                } catch (IOException e) {
                    new File(_resultPath).delete();
                    android.util.Log.e("FlagsActivity", "Unable to write FSO query result", e);
                }
            }
            sendResult(result);
            finish();
        });
    }

    private void sendResult(int result) {
        if (_receiver == null) return;
        Bundle data = new Bundle();
        data.putBinder("queryProcess", _queryProcess);
        _receiver.send(result, data);
        _receiver = null;
    }
}
