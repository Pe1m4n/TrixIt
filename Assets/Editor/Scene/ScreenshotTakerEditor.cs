using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScreenshotTaker))]
public class ScreenshotTakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Screenshot"))
        {
            var target = (ScreenshotTaker)serializedObject.targetObject;
            if (!Directory.Exists(target.screenshotDirectory)) Directory.CreateDirectory(target.screenshotDirectory);
            int num = 1;
            while (File.Exists(Path.Combine(target.screenshotDirectory, $"screenshot{num}.png"))) num++;
            ScreenCapture.CaptureScreenshot(Path.Combine(target.screenshotDirectory, $"screenshot{num}.png"), target.superSize);
            Debug.Log($"Screenshot saved at {(new FileInfo(Path.Combine(target.screenshotDirectory, $"screenshot{num}.png"))).FullName}");
        }
        base.OnInspectorGUI();
    }
}