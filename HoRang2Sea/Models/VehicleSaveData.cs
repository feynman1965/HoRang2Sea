using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization;

namespace HoRang2Sea.Models
{
    public class VehicleSaveData
    {
        public string VehicleType { get; set; }
        public int DesignLayout { get; set; }
        public int ControlLayout { get; set; }
        public string DriveModePath { get; set; }
        public string DatabaseXml { get; set; }

        public void Save(string filePath)
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
        }

        public static VehicleSaveData Load(string filePath)
        {
            var json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            return JsonSerializer.Deserialize<VehicleSaveData>(json);
        }

        public static string SerializeDatabase(DatabaseDefinition db)
        {
            if (db == null) return null;
            using (var sw = new StringWriter())
            {
                var serializer = new XmlSerializer(typeof(DatabaseDefinition));
                serializer.Serialize(sw, db);
                return sw.ToString();
            }
        }

        public static DatabaseDefinition DeserializeDatabase(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return null;
            using (var sr = new StringReader(xml))
            {
                var serializer = new XmlSerializer(typeof(DatabaseDefinition));
                return (DatabaseDefinition)serializer.Deserialize(sr);
            }
        }

        // --- Path management ---
        private static readonly Dictionary<string, string> _lastUsedPaths = new();

        public static string GetDefaultDir(string vehicleType)
        {
            // If user previously used a custom path, return that
            if (_lastUsedPaths.TryGetValue(vehicleType, out var last) && Directory.Exists(last))
                return last;

            // Otherwise, create/return default: %LOCALAPPDATA%/HoRang2/Configs/VehicleType
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HoRang2", "Configs", vehicleType);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        public static void RememberDir(string vehicleType, string filePath)
        {
            _lastUsedPaths[vehicleType] = Path.GetDirectoryName(filePath);
        }

        // --- History (자동 저장: Run 시 현재 입력 스냅샷, 최근 N개 유지) ---
        public const int MaxHistory = 20;

        // %LOCALAPPDATA%/HoRang2/Configs/{type}/_history
        public static string GetHistoryDir(string vehicleType)
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HoRang2", "Configs", vehicleType, "_history");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        private static string[] HistoryFilesNewestFirst(string dir)
        {
            var files = Directory.GetFiles(dir, "*.hr2v");
            Array.Sort(files, (a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));
            return files;
        }

        // 현재 스냅샷을 history에 기록. 직전 항목과 내용이 같으면 건너뛰고, MaxHistory 초과분은 오래된 것부터 삭제.
        public void SaveToHistory(string vehicleType)
        {
            try
            {
                var dir = GetHistoryDir(vehicleType);
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                var existing = HistoryFilesNewestFirst(dir);
                if (existing.Length > 0)
                {
                    try { if (File.ReadAllText(existing[0], System.Text.Encoding.UTF8) == json) return; } catch { }
                }
                var name = "Run_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".hr2v";
                File.WriteAllText(Path.Combine(dir, name), json, System.Text.Encoding.UTF8);

                var all = HistoryFilesNewestFirst(dir);
                for (int i = MaxHistory; i < all.Length; i++)
                {
                    try { File.Delete(all[i]); } catch { }
                }
            }
            catch { }
        }
    }
}
