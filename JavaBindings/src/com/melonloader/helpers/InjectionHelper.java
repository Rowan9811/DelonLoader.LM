package com.melonloader.helpers;

import android.app.Activity;
import android.content.Context;

import com.melonloader.ApplicationState;
import com.melonloader.Bootstrap;
import com.melonloader.LogBridge;

public class InjectionHelper {
    public static void InjectBootstrap() throws Exception {
        LogBridge.msg("Bootstrapping...");

        try {
            System.loadLibrary("Bootstrap");
        } catch (UnsatisfiedLinkError e) {
            LogBridge.error("Failed to load \"libBootstrap.so\" - Perhaps its not in lib?");
            throw e;
        }

        ApplicationState.IsReady = true;

        LogBridge.msg("libBootstrap.so successfully loaded");
    }

    public static void StartEOS() throws Exception {
        LogBridge.msg("Initializing EOS...");

        try {
            System.loadLibrary("EOSSDK");
        } catch (UnsatisfiedLinkError e) {
            LogBridge.error("Failed to load \"libEOSSDK.so\" - Perhaps its not in lib?");
            throw e;
        }

        LogBridge.msg("libEOSSDK.so successfully loaded");
    }

    public static void Initialize(Activity context)
    {
        ApplicationState.UpdateActivity(context);
        ContextHelper.DefineContext(context);

        AssemblyHelper.InstallAssemblies();
        // AssetsTools can't read the data, unsure why
        //UnityInformationHelper.SaveGlobalGameManagersToFile();

        try {
            InjectBootstrap();
        } catch (Exception e) {
            LogBridge.error(e.getMessage());
        }
        try {
            StartEOS();
        } catch (Exception e) {
            LogBridge.error(e.getMessage());
        }

        Bootstrap.Initialize();
    }
}
