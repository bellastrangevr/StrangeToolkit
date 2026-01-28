using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void ScanAudio()
        {
            var audioSources = FindObjectsOfType<AudioSource>();
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
                        if (!importer.forceToMono) reasons.Add("Stereo (2x RAM)");
                        if (settings.compressionFormat != AudioCompressionFormat.ADPCM && settings.compressionFormat != AudioCompressionFormat.Vorbis)
                            reasons.Add("Not Compressed (ADPCM/Vorbis)");
                    }
                    else // PC
                    {
                        if (src.clip.length > 10f && settings.loadType == AudioClipLoadType.DecompressOnLoad)
                            reasons.Add("Large file DecompressOnLoad");
                        if (settings.compressionFormat == AudioCompressionFormat.PCM && src.clip.length > 2f)
                            reasons.Add("Uncompressed PCM");
                    }

                    if (reasons.Count > 0)
                    {
                        // Estimate savings (Rough calc: PCM is ~2 bytes/sample, ADPCM is ~0.5 bytes/sample)
                        long estimatedBytes = 0;
                        if (settings.compressionFormat == AudioCompressionFormat.PCM)
                            estimatedBytes += (long)(src.clip.length * src.clip.frequency * src.clip.channels * 2);
                        if (!importer.forceToMono && src.clip.channels == 2)
                            estimatedBytes += (long)(src.clip.length * src.clip.frequency * 2); // Half size savings
                        
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
            _showAudio = EditorGUILayout.Foldout(_showAudio, "Audio Audit", true, _foldoutStyle);

            if (_showAudio)
            {
                if (_audioIssues.Count > 0)
                {
                    GUILayout.Space(10);
                    long totalSavings = _audioIssues.Sum(x => x.potentialSavings);
                    string savingsText = EditorUtility.FormatBytes(totalSavings);
                    
                    DrawTooltipHelpBox($"{_audioIssues.Count} Audio Candidates (Save ~{savingsText})", "Optimizing these clips could save significant RAM.", MessageType.Info);
                    _audioScroll = EditorGUILayout.BeginScrollView(_audioScroll, GUILayout.Height(Mathf.Min(150, _audioIssues.Count * 25 + 10)));
                    foreach (var issue in _audioIssues)
                    {
                        EditorGUILayout.BeginHorizontal(_listItemStyle);
                        issue.isSelected = EditorGUILayout.Toggle(issue.isSelected, GUILayout.Width(20));
                        GUILayout.Label(issue.clip.name, EditorStyles.boldLabel, GUILayout.Width(180));
                        GUILayout.Label(issue.reason, EditorStyles.miniLabel);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Sel", GUILayout.Width(40))) Selection.activeObject = issue.clip;
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();
                    
                    GUILayout.Space(5);
                    
                    // Optimization Tools Foldout
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Label("Optimization Tools", EditorStyles.boldLabel);
                    
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Optimize Checked"))
                    {
                        var checkedClips = _audioIssues.Where(x => x.isSelected).Select(x => x.clip).ToList();
                        if (checkedClips.Count > 0) OptimizeAudioClips(checkedClips);
                    }
                    
                    if (GUILayout.Button("Optimize All Listed"))
                    {
                        OptimizeAudioClips(_audioIssues.Select(x => x.clip).ToList());
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(5);
                    GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                    if (GUILayout.Button("Undo Last Audio Action")) Undo.PerformUndo();
                    GUI.backgroundColor = Color.white;
                    
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    GUILayout.Label($"Audio settings look good for {_auditProfile}.", _successStyle);
                }
            }
            EditorGUILayout.EndVertical();
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
                    settings.quality = 0.7f; // Good balance
                    
                    importer.defaultSampleSettings = settings;
                    importer.SaveAndReimport();
                }
            }
            
            RunExtendedScan(); // Use orchestrator to properly clear and rescan
            Debug.Log($"[StrangeToolkit] Optimized {clips.Count} audio clips.");
        }
    }
}
