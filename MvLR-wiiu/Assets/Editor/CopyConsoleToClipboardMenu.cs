using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CopyConsoleToClipboardMenu {
    [MenuItem("NSMB/Debug/Copy Console To Clipboard")]
    private static void CopyConsoleToClipboard() {
        try {
            // Unity 2017.x: UnityEditorInternal types live in the UnityEditor assembly, but Type.GetType
            // can fail depending on the exact assembly-qualified name. Resolve via typeof(Editor).Assembly.
            Type logEntriesType = ResolveType(
                "UnityEditorInternal.LogEntries",
                "UnityEditor.LogEntries"
            );
            Type logEntryType = ResolveType(
                "UnityEditorInternal.LogEntry",
                "UnityEditor.LogEntry"
            );
            if (logEntriesType == null) {
                logEntriesType = FindTypeBySimpleName("LogEntries");
            }
            if (logEntryType == null) {
                logEntryType = FindTypeBySimpleName("LogEntry");
            }
            if (logEntriesType == null || logEntryType == null) {
                string editorLogPath;
                string editorLog = TryReadEditorLog(out editorLogPath);
                if (!string.IsNullOrEmpty(editorLog)) {
                    EditorGUIUtility.systemCopyBuffer = editorLog;
                    EditorUtility.DisplayDialog("NSMB", "Console API not available; copied Editor.log to clipboard.", "OK");
                    return;
                }

                StringBuilder msg = new StringBuilder();
                msg.AppendLine("[NSMB] UnityEditorInternal.LogEntries/LogEntry not found in this Unity version.");
                msg.AppendLine(string.Format("- LogEntries type: {0}", logEntriesType != null ? logEntriesType.FullName : "(not found)"));
                msg.AppendLine(string.Format("- LogEntry type: {0}", logEntryType != null ? logEntryType.FullName : "(not found)"));
                if (!string.IsNullOrEmpty(editorLogPath)) {
                    msg.AppendLine(string.Format("- Editor.log path: {0}", editorLogPath));
                }
                msg.AppendLine();
                msg.AppendLine("Hint: if you can, update Unity or reimport; otherwise we may need an alternate Console export approach for this editor build.");
                EditorGUIUtility.systemCopyBuffer = msg.ToString();
                EditorUtility.DisplayDialog("NSMB", "Could not access Console internals or Editor.log.\nDiagnostics copied to clipboard.", "OK");
                return;
            }

            MethodInfo getCount = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo startGettingEntries = logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo endGettingEntries = logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo getEntryInternal = FindGetEntryMethod(logEntriesType, logEntryType);

            if (getCount == null || getEntryInternal == null) {
                EditorGUIUtility.systemCopyBuffer = "[NSMB] Console reflection failed (missing methods).";
                EditorUtility.DisplayDialog("NSMB", "Console export failed (missing methods).\nCopied message to clipboard.", "OK");
                return;
            }

            int count = (int)getCount.Invoke(null, null);

            StringBuilder sb = new StringBuilder(Math.Max(1024, count * 80));
            sb.AppendLine(string.Format("[NSMB] Console export ({0} entries)", count));
            sb.AppendLine("------------------------------------------------------------");

            if (startGettingEntries != null) {
                startGettingEntries.Invoke(null, null);
            }

            for (int i = 0; i < count; i++) {
                object entry = Activator.CreateInstance(logEntryType);
                object[] args = new object[] { i, entry };
                getEntryInternal.Invoke(null, args);
                // Some Unity versions use a by-ref LogEntry parameter.
                if (args.Length > 1 && args[1] != null) {
                    entry = args[1];
                }

                string condition = GetStringField(entry, logEntryType, "condition");
                if (string.IsNullOrEmpty(condition)) {
                    condition = GetStringField(entry, logEntryType, "message");
                }

                string stackTrace = GetStringField(entry, logEntryType, "stackTrace");
                int mode = GetIntField(entry, logEntryType, "mode");
                int entryCount = GetIntField(entry, logEntryType, "count");

                sb.AppendLine(string.Format("[{0}] mode=0x{1:X} count={2}", i, mode, (entryCount > 0 ? entryCount : 1)));
                sb.AppendLine(condition ?? string.Empty);
                if (!string.IsNullOrEmpty(stackTrace)) {
                    sb.AppendLine(stackTrace);
                }
                sb.AppendLine("------------------------------------------------------------");
            }

            if (endGettingEntries != null) {
                endGettingEntries.Invoke(null, null);
            }

            EditorGUIUtility.systemCopyBuffer = sb.ToString();
            EditorUtility.DisplayDialog("NSMB", string.Format("Copied full Console output to clipboard ({0} entries).", count), "OK");
        } catch (Exception ex) {
            EditorGUIUtility.systemCopyBuffer = "[NSMB] Console export failed:\n" + ex;
            EditorUtility.DisplayDialog("NSMB", "Console export failed.\nException copied to clipboard.", "OK");
        }
    }

    private static Type ResolveType(params string[] fullNames) {
        if (fullNames == null || fullNames.Length == 0) {
            return null;
        }

        // 1) Direct Type.GetType lookup
        for (int i = 0; i < fullNames.Length; i++) {
            string n = fullNames[i];
            if (string.IsNullOrEmpty(n)) continue;
            Type t = Type.GetType(n);
            if (t != null) return t;
            t = Type.GetType(n + ", UnityEditor");
            if (t != null) return t;
            t = Type.GetType(n + ", UnityEditor.dll");
            if (t != null) return t;
        }

        // 2) Scan loaded assemblies
        Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
        for (int a = 0; a < asms.Length; a++) {
            Assembly asm = asms[a];
            if (asm == null) continue;
            for (int i = 0; i < fullNames.Length; i++) {
                string n = fullNames[i];
                if (string.IsNullOrEmpty(n)) continue;
                try {
                    Type t = asm.GetType(n);
                    if (t != null) return t;
                } catch {
                    // ignore
                }
            }
        }

        return null;
    }

    private static Type FindTypeBySimpleName(string simpleName) {
        if (string.IsNullOrEmpty(simpleName)) {
            return null;
        }

        Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
        for (int a = 0; a < asms.Length; a++) {
            Assembly asm = asms[a];
            if (asm == null) continue;
            Type[] types = null;
            try {
                types = asm.GetTypes();
            } catch (ReflectionTypeLoadException ex) {
                types = ex.Types;
            } catch {
                types = null;
            }

            if (types == null) continue;

            for (int i = 0; i < types.Length; i++) {
                Type t = types[i];
                if (t == null) continue;
                if (string.Equals(t.Name, simpleName, StringComparison.InvariantCulture)) {
                    return t;
                }
            }
        }
        return null;
    }

    private static string TryReadEditorLog(out string path) {
        path = null;
        try {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrEmpty(localAppData)) {
                // .NET 3.5 doesn't have Path.Combine(string,string,string,string)
                string p = Path.Combine(Path.Combine(localAppData, "Unity"), Path.Combine("Editor", "Editor.log"));
                if (File.Exists(p)) {
                    path = p;
                    return ReadAllTextShared(p, 5 * 1024 * 1024);
                }
                path = p;
            }
        } catch {
            // ignore
        }

        try {
            string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!string.IsNullOrEmpty(roaming)) {
                // .NET 3.5 doesn't have Path.Combine(string,string,string,string)
                string p = Path.Combine(Path.Combine(roaming, "Unity"), Path.Combine("Editor", "Editor.log"));
                if (File.Exists(p)) {
                    path = p;
                    return ReadAllTextShared(p, 5 * 1024 * 1024);
                }
                path = p;
            }
        } catch {
            // ignore
        }

        return null;
    }

    private static string ReadAllTextShared(string path, int maxBytes) {
        if (string.IsNullOrEmpty(path)) {
            return null;
        }

        FileInfo fi = new FileInfo(path);
        if (!fi.Exists) {
            return null;
        }

        long len = fi.Length;
        if (len <= 0) {
            return null;
        }

        long start = 0;
        if (maxBytes > 0 && len > maxBytes) {
            start = len - maxBytes;
        }

        using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
            if (start > 0) {
                fs.Seek(start, SeekOrigin.Begin);
            }
            using (StreamReader sr = new StreamReader(fs, Encoding.UTF8, true)) {
                string text = sr.ReadToEnd();
                if (start > 0) {
                    text = "[NSMB] (tail) " + path + "\n" + text;
                } else {
                    text = "[NSMB] " + path + "\n" + text;
                }
                return text;
            }
        }
    }

    private static MethodInfo FindGetEntryMethod(Type logEntriesType, Type logEntryType) {
        BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        // Prefer the internal method if present.
        MethodInfo m = TryFindGetEntryWithName(logEntriesType, logEntryType, "GetEntryInternal", flags);
        if (m != null) return m;

        // Fallback seen in some editor versions.
        m = TryFindGetEntryWithName(logEntriesType, logEntryType, "GetEntry", flags);
        if (m != null) return m;

        return null;
    }

    private static MethodInfo TryFindGetEntryWithName(Type logEntriesType, Type logEntryType, string name, BindingFlags flags) {
        MethodInfo[] methods = logEntriesType.GetMethods(flags);
        for (int i = 0; i < methods.Length; i++) {
            MethodInfo mi = methods[i];
            if (mi == null) continue;
            if (!string.Equals(mi.Name, name, StringComparison.InvariantCulture)) continue;

            ParameterInfo[] ps = mi.GetParameters();
            if (ps == null || ps.Length != 2) continue;
            if (ps[0].ParameterType != typeof(int)) continue;

            Type p1 = ps[1].ParameterType;
            if (p1 == logEntryType) {
                return mi;
            }
            if (p1.IsByRef && p1.GetElementType() == logEntryType) {
                return mi;
            }
        }
        return null;
    }

    private static string GetStringField(object obj, Type t, string name) {
        try {
            FieldInfo f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null) return null;
            object v = f.GetValue(obj);
            return v as string;
        } catch {
            return null;
        }
    }

    private static int GetIntField(object obj, Type t, string name) {
        try {
            FieldInfo f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null) return 0;
            object v = f.GetValue(obj);
            if (v is int) return (int)v;
            if (v is long) return (int)(long)v;
            return Convert.ToInt32(v);
        } catch {
            return 0;
        }
    }
}
