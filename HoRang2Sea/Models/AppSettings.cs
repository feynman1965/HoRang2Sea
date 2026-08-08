using System;
using System.IO;
using System.Text.Json;

namespace HoRang2Sea.Models
{
    /// <summary>
    /// 앱 전역 설정. %LOCALAPPDATA%/HoRang2/settings.json 에 보관한다.
    /// (모델별 입력값은 VehicleSaveData 쪽 .hr2v 를 쓰고, 여기는 프로그램 동작 설정만 담는다.)
    /// </summary>
    public class AppSettings
    {
        /// <summary>결과 기록 간격(시뮬 step). DLL step = 1 ms 이므로 100 = 0.1초.</summary>
        public int RecordStepInterval { get; set; } = 100;

        private static readonly object _lock = new();
        private static AppSettings _current;

        public static AppSettings Current
        {
            get
            {
                lock (_lock)
                {
                    if (_current == null) _current = Load();
                    return _current;
                }
            }
        }

        private static string FilePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "HoRang2", "settings.json");

        public static AppSettings Load()
        {
            try
            {
                var p = FilePath;
                if (File.Exists(p))
                {
                    var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(p));
                    if (s != null)
                    {
                        s.Normalize();
                        GenericPortDllModel.RecordStepInterval = s.RecordStepInterval;
                        return s;
                    }
                }
            }
            catch { /* 설정 파일이 깨졌으면 기본값으로 진행 */ }

            var def = new AppSettings();
            GenericPortDllModel.RecordStepInterval = def.RecordStepInterval;
            return def;
        }

        public void Save()
        {
            Normalize();
            try
            {
                var p = FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(p));
                File.WriteAllText(p, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { /* 저장 실패해도 실행에는 지장 없도록 무시 */ }
            GenericPortDllModel.RecordStepInterval = RecordStepInterval;
        }

        private void Normalize()
        {
            if (RecordStepInterval < 1) RecordStepInterval = 1;
            if (RecordStepInterval > 100000) RecordStepInterval = 100000;
        }
    }
}
