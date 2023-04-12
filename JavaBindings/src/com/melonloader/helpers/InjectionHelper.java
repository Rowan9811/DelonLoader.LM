package com.melonloader.helpers;

import android.app.Activity;

import com.melonloader.ApplicationState;
import com.melonloader.Bootstrap;
import com.melonloader.LogBridge;

import java.io.File;
import java.nio.file.Paths;

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

    public static void Initialize(Activity context)
    {
        ApplicationState.UpdateActivity(context);
        ContextHelper.DefineContext(context);

        AssemblyHelper.InstallAssemblies();
        // AssetsTools can't read the data, unsure why
        //UnityInformationHelper.SaveGlobalGameManagersToFile();

        // Funchook Cleanup
        File logPath = Paths.get(ApplicationState.BaseDirectory, "funchook.log").toFile();
        if (logPath.exists())
            logPath.delete();

        try {
            InjectBootstrap();
        } catch (Exception e) {
            LogBridge.error(e.getMessage());
        }

        Bootstrap.Initialize();
    }
}
