using UnityEditor;
using UnityEngine;

public sealed class EnforcePcmAudioImport : AssetPostprocessor {
    void OnPreprocessAudio() {
        var importer = (AudioImporter)assetImporter;

        var settings = importer.defaultSampleSettings;
        bool changed = false;

        if (settings.compressionFormat != UnityEngine.AudioCompressionFormat.PCM) {
            settings.compressionFormat = UnityEngine.AudioCompressionFormat.PCM;
            changed = true;
        }

        if (settings.loadType != AudioClipLoadType.DecompressOnLoad) {
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            changed = true;
        }

        if (settings.sampleRateSetting != AudioSampleRateSetting.PreserveSampleRate) {
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            changed = true;
        }

        if (changed) {
            importer.defaultSampleSettings = settings;
        }
    }
}
