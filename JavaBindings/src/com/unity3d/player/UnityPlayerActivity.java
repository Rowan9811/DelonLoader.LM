package com.unity3d.player;

import android.app.Activity;
import android.os.Bundle;
import android.util.Log;
import com.melonloader.Bootstrap;
import com.melonloader.LogBridge;
import com.melonloader.helpers.InjectionHelper;
import lanchon.dexpatcher.annotation.DexEdit;
import lanchon.dexpatcher.annotation.DexIgnore;
import lanchon.dexpatcher.annotation.DexPrepend;

@DexEdit
public class UnityPlayerActivity extends Activity {
    @DexPrepend
    @Override protected void onCreate(Bundle bundle) { InjectionHelper.Initialize(this); }

    //@DexPrepend
    //@Override protected void onPause() { LogBridge.msg("onPause!"); Bootstrap.OnExit(); }

    //@DexPrepend
    //@Override protected void onStop() { LogBridge.msg("onStop!"); Bootstrap.OnExit(); }
}
