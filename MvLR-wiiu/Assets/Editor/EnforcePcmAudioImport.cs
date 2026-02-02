using UnityEditor;
using UnityEngine;

public sealed class EnforcePcmAudioImport : AssetPostprocessor {
    void OnPreprocessAudio() {
        var importer = (AudioImporter)assetImporter;

        var settings = importer.defaultSampleSettings;
        if (settings.compressionFormat != UnityEngine.AudioCompressionFormat.PCM) {
            settings.compressionFormat = UnityEngine.AudioCompressionFormat.PCM;
            importer.defaultSampleSettings = settings;
        }
    }
}
