using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StrangeToolkit.Repair
{
    // ==================================================================================
    // REPAIR ON LOAD
    // ==================================================================================
    // Once per editor session, after scripts load, checks the toolkit's files and asks to fix them when something is
    // wrong, so a broken install or upgrade fixes itself with one click. Runs a single time (no update loop), not in
    // batch mode or play mode, and "Not now" stays quiet until Unity is restarted.
    [InitializeOnLoad]
    internal static class StrangeRepairStartup
    {
        private const string CheckedKey = "StrangeToolkit.Repair.CheckedThisSession";

        static StrangeRepairStartup()
        {
            if (Application.isBatchMode || SessionState.GetBool(CheckedKey, false)) return;
            EditorApplication.delayCall += Check;
        }

        private static void Check()
        {
            // Wait until Unity has finished importing and compiling; delayCall can land mid-refresh
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += Check;
                return;
            }
            // A dialog shown before the editor window has focus answers itself, so wait for focus (an event, not a loop)
            if (!UnityEditorInternal.InternalEditorUtility.isApplicationActive)
            {
                EditorApplication.focusChanged -= OnFocus;
                EditorApplication.focusChanged += OnFocus;
                return;
            }
            SessionState.SetBool(CheckedKey, true);

            try
            {
                var files = ToolkitFiles.Load();
                if (files == null || !InstallRepair.NeedsFix(files, out var problems)) return;

                var fixable = problems.Where(p => p.Fixable).ToList();
                string list = string.Join("\n", fixable.GroupBy(p => p.Kind).Select(g => $"• {Summary(g.Key)}: {g.Count()}"));
                int choice = EditorUtility.DisplayDialogComplex("Strange Toolkit",
                    "Strange Toolkit found problems with its files, usually left over from an older install:\n\n" + list +
                    "\n\nFix them now? Only toolkit files are changed.", "Fix Now", "Not Now", "Show Details");

                if (choice == 0)
                {
                    var failures = InstallRepair.Fix(files, problems);
                    if (failures.Count > 0) Debug.LogWarning("[Strange Toolkit] File repair:\n" + string.Join("\n", failures));
                    else Debug.Log($"[Strange Toolkit] Fixed {fixable.Count} file problem(s).");
                }
                else if (choice == 2)
                {
                    StrangeRepairWindow.Open();
                }
            }
            catch (Exception e)
            {
                // Never get in the way of opening a project
                Debug.LogWarning("[Strange Toolkit] Startup file check skipped: " + e.Message);
            }
        }

        private static void OnFocus(bool focused)
        {
            if (!focused) return;
            EditorApplication.focusChanged -= OnFocus;
            EditorApplication.delayCall += Check;
        }

        private static string Summary(FileProblemKind kind)
        {
            switch (kind)
            {
                case FileProblemKind.Duplicate: return "Duplicate copies";
                case FileProblemKind.Leftover: return "Old files from an earlier version";
                case FileProblemKind.Misplaced: return "Files moved out of the toolkit";
                case FileProblemKind.WrongId: return "Files whose ID Unity changed";
                case FileProblemKind.Unlinked: return "Udon programs missing their script";
                default: return kind.ToString();
            }
        }
    }
}
