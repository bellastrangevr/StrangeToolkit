using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void ScanAudio(ScanContext ctx)
        {
            var audioSources = ctx.audioSources;
            HashSet<AudioClip> checkedClips = new HashSet<AudioClip>();
            foreach (var src in audioSources)
            {
                if (src.clip == null || checkedClips.Contains(src.clip)) continue;
                checkedClips.Add(src.clip);

                string path = AssetDatabase.GetAssetPath(src.clip);
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer != null)
                {
                    List<string> reasons = new List<string>();

                    var settings = importer.defaultSampleSettings;

                    if (_auditProfile == AuditProfile.Quest)
                    {
                        if (!importer.forceToMono) reasons.Add("Stereo");
                        if (settings.compressionFormat != AudioCompressionFormat.ADPCM && settings.compressionFormat != AudioCompressionFormat.Vorbis)
                            reasons.Add("Uncompressed");
                    }
                    else // PC
                    {
                        if (src.clip.length > 10f && settings.loadType == AudioClipLoadType.DecompressOnLoad)
                            reasons.Add("Large DecompressOnLoad");
                        if (settings.compressionFormat == AudioCompressionFormat.PCM && src.clip.length > 2f)
                            reasons.Add("Uncompressed PCM");
                    }

                    if (reasons.Count > 0)
                    {
                        // Estimate savings
                        long estimatedBytes = 0;
                        if (settings.compressionFormat == AudioCompressionFormat.PCM)
                            estimatedBytes += (long)(src.clip.length * src.clip.frequency * src.clip.channels * 2);
                        if (!importer.forceToMono && src.clip.channels == 2)
                            estimatedBytes += (long)(src.clip.length * src.clip.frequency * 2);

                        _audioIssues.Add(new AudioIssue
                        {
                            clip = src.clip,
                            reason = string.Join(", ", reasons),
                            potentialSavings = estimatedBytes
                        });
                    }
                }
            }
        }

        private void DrawAudioAuditor()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Compact header with status
            EditorGUILayout.BeginHorizontal();
            _showAudio = EditorGUILayout.Foldout(_showAudio, "Audio", true, _foldoutStyle);

            GUILayout.FlexibleSpace();

            if (_audioIssues.Count > 0)
            {
                long totalSavings = _audioIssues.Sum(x => x.potentialSavings);
                GUI.color = new Color(1f, 0.8f, 0.4f);
                GUILayout.Label($"{_audioIssues.Count} issues (~{EditorUtility.FormatBytes(totalSavings)} savings)", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(0.4f, 0.8f, 0.4f);
                GUILayout.Label("OK", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            if (_showAudio)
            {
                GUILayout.Space(3);

                if (_audioIssues.Count > 0)
                {
                    _audioScroll = EditorGUILayout.BeginScrollView(_audioScroll, GUILayout.Height(Mathf.Min(150, _audioIssues.Count * 24 + 10)));

                    foreach (var issue in _audioIssues)
                    {
                        DrawAudioIssueRow(issue);
                    }

                    EditorGUILayout.EndScrollView();

                    // Action buttons at bottom
                    GUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("Optimize Checked", EditorStyles.miniButton))
                    {
                        var checkedClips = _audioIssues.Where(x => x.isSelected).Select(x => x.clip).ToList();
                        if (checkedClips.Count > 0) OptimizeAudioClips(checkedClips);
                    }

                    if (GUILayout.Button("Optimize All", EditorStyles.miniButton))
                    {
                        OptimizeAudioClips(_audioIssues.Select(x => x.clip).ToList());
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    GUI.color = new Color(0.4f, 0.8f, 0.4f);
                    GUILayout.Label($"Audio settings look good for {_auditProfile}.", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawAudioIssueRow(AudioIssue issue)
        {
            EditorGUILayout.BeginHorizontal(_listItemStyle);

            issue.isSelected = EditorGUILayout.Toggle(issue.isSelected, GUILayout.Width(18));
            GUILayout.Space(3);

            // Clip name (truncated)
            string clipName = issue.clip.name;
            if (clipName.Length > 20) clipName = clipName.Substring(0, 17) + "...";
            GUILayout.Label(clipName, EditorStyles.miniLabel, GUILayout.Width(140));

            // Reason
            GUI.color = new Color(1f, 0.8f, 0.4f);
            GUILayout.Label(issue.reason, EditorStyles.miniLabel);
            GUI.color = Color.white;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Sel", EditorStyles.miniButton, GUILayout.Width(30)))
            {
                Selection.activeObject = issue.clip;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void OptimizeAudioClips(List<AudioClip> clips)
        {
            if (clips == null || clips.Count == 0) return;

            foreach (var clip in clips)
            {
                string path = AssetDatabase.GetAssetPath(clip);
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer != null)
                {
                    Undo.RecordObject(importer, "Optimize Audio");

                    importer.forceToMono = true;

                    AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                    settings.loadType = clip.length > 10f ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                    settings.compressionFormat = clip.length > 5f ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
                    settings.quality = 0.7f;

                    importer.defaultSampleSettings = settings;
                    importer.SaveAndReimport();
                }
            }

            RunExtendedScan();
            StrangeToolkitLogger.LogSuccess($"Optimized {clips.Count} audio clips.");
        }
    }
}
