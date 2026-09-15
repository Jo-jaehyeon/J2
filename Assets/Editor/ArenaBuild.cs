using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class ArenaBuild
{
    public static void BuildSmoke()
    {
        DigitalArenaValidation.Run();

        const string scenePath = "Assets/Scenes/DragonEyeLake.unity";

        if (!File.Exists(scenePath))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(scenePath, true)
        };
        Directory.CreateDirectory("Builds/DigitalArena");

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { scenePath }, locationPathName = "Builds/DigitalArena/DigitalArena.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new Exception("3D player build failed: " + report.summary.result);
        }

        Debug.Log("ARENA 3D BUILD PASSED");
    }
}
