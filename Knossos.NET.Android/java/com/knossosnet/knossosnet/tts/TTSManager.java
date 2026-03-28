package com.knossosnet.knossosnet.tts;

import android.content.Context;
import android.media.AudioAttributes;
import android.os.Bundle;
import android.speech.tts.TextToSpeech;
import android.speech.tts.UtteranceProgressListener;
import java.util.Locale;
import android.app.Activity;

public final class TTSManager implements TextToSpeech.OnInitListener {
    private static final String defaultLangTag = "en-US";
    private static final float defaultRate  = 1.0f;
    private static final float defaultPitch = 1.0f;
    private static volatile TextToSpeech tts;
    private static volatile boolean ready;
    private static volatile boolean speaking;

    private TTSManager() {}

    public static void init(Context context) {
        if (tts != null) return;
        tts = new TextToSpeech(context, new TTSManager());
    }

    public static void init(Activity activity) {
        init(activity.getApplicationContext());
    }

    @Override
    public void onInit(int status) {
        ready = (status == TextToSpeech.SUCCESS);
        if (!ready) return;

        tts.setAudioAttributes(new AudioAttributes.Builder()
                .setUsage(AudioAttributes.USAGE_MEDIA)
                .setContentType(AudioAttributes.CONTENT_TYPE_SPEECH)
                .build());

        setLanguageTag(defaultLangTag);
        tts.setSpeechRate(defaultRate);
        tts.setPitch(defaultPitch);

        tts.setOnUtteranceProgressListener(new UtteranceProgressListener() {
            @Override public void onStart(String utteranceId) { speaking = true; }
            @Override public void onDone(String utteranceId)  { speaking = false; }
            @Override public void onError(String utteranceId) { speaking = false; }
        });
    }

    public static boolean speak(String text) {
        TextToSpeech engine = tts;
        if (engine == null || !ready) return false;

        String id = String.valueOf(System.nanoTime());

        Bundle params = new Bundle();
        int res = engine.speak(text != null ? text : "", TextToSpeech.QUEUE_FLUSH, params, id);
        return res == TextToSpeech.SUCCESS;
    }

    public static boolean stop() {
        TextToSpeech engine = tts;
        if (engine == null) return false;
        return engine.stop() == TextToSpeech.SUCCESS;
    }

    public static boolean pause() { return stop(); }
    public static boolean resume() { return false; }
    public static boolean isSpeaking() { return speaking; }

    public static void setRate(float rate) { if (tts != null) tts.setSpeechRate(rate); }
    public static void setPitch(float pitch) { if (tts != null) tts.setPitch(pitch); }

    public static void setLanguageTag(String bcp47) {
        TextToSpeech engine = tts;
        if (engine == null || bcp47 == null || bcp47.isEmpty()) return;

        engine.setLanguage(Locale.forLanguageTag(bcp47));
    }

    public static void shutdown() {
        TextToSpeech engine = tts;
        if (engine != null) {
            engine.shutdown();
            tts = null;
            ready = false;
            speaking = false;
        }
    }
}