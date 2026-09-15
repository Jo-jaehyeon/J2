using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ArenaMobileBuild
{
    [MenuItem("Digital Arena/Mobile/Apply Mobile Settings")]
    public static void ApplySettings()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.optimizedFramePacing = true;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
        AssetDatabase.SaveAssets();
        Debug.Log("Mobile settings applied: landscape, Android ARM64/IL2CPP, iOS IL2CPP, frame pacing.");
    }

    [MenuItem("Digital Arena/Mobile/Build Android Development APK")]
    public static void BuildAndroid() => Build(BuildTarget.Android, "Builds/Android/DigiTactics.apk");

    [MenuItem("Digital Arena/Mobile/Export iOS Development Project")]
    public static void BuildIos() => Build(BuildTarget.iOS, "Builds/iOS");

    static void Build(BuildTarget target, string path)
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target))
        {
            throw new InvalidOperationException("Install Unity " + target + " Build Support in Unity Hub first.");
        }

        ApplySettings();

        const string scene = "Assets/Scenes/DragonEyeLake.unity";

        if (!File.Exists(scene))
        {
            throw new FileNotFoundException("Game scene missing", scene);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path));

        bool previousBundle = EditorUserBuildSettings.buildAppBundle;

        try
        {
            if (target == BuildTarget.Android)
            {
                EditorUserBuildSettings.buildAppBundle = false;
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { scene }, target = target, locationPathName = path, options = BuildOptions.Development });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("Mobile build failed: " + report.summary.result);
            }
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = previousBundle;
        }
    }
}
