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
    }
}
