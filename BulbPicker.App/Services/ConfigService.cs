using BulbPicker.App.Infrastructures;
using BulbPicker.App.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

namespace BulbPicker.App.Services
{
    // 급하니까 일단 GPT 코드 씀
    file sealed class OffsetDto { public int X { get; set; } public int Y { get; set; } public int Z { get; set; } }

    // TODO later: textbox 값은 수정했는데 save 버튼 안 눌렀을 경우 'Changes Not Saved' 등 문구 떠 있게 하는 기능 만들기 (bool)
    class ConfigService : ObservableObject
    {
        private static readonly ConfigService _instance = new ConfigService();
        public static ConfigService Instance => _instance;

        private readonly Dispatcher _dispatcher;

        /// <summary>
        /// AppConfigUserControl에서 사용되는 유저가 조작할 수 있는 Offsets
        /// </summary>
        private ObservableCollection<RobotArmOffsets> _displayedOffsets = new ObservableCollection<RobotArmOffsets>();
        public ObservableCollection<RobotArmOffsets> DisplayedOffsets
        {
            get => _displayedOffsets;
            set
            {
                _displayedOffsets = value;
                OnPropertyChanged(nameof(DisplayedOffsets));
            }
        }

        private ConfigService( )
        {
            _dispatcher = Application.Current.Dispatcher;
            ApplyConfigSettings();
        }

        // TODO later:
        // 원래는 config 파일에서 로봇팔 정보를 가져와 로봇팔을 initialize하고 add 한 뒤 그렇게 하며 displayedOffset (user interactable offsets)도 set 하는 게 이상적이나
        // 지금은 시간이 없으므로 robotarm을 코드에서 수동으로 만들면 해당 로봇팔의 ip에 해당 되는 offset을 config에서 찾아 로봇팔과 displayedOffset을 setting해야 함
        // read from config json and set up robot arms
        // set up offsets as well
        private void ApplyConfigSettings() { }
        private void SetUpRobotArmDefaultSettings() { }
        private void SetUpRobotArmDisplayedOffset() { }

        public string GetRobotArmConfigFilePath()
        {
            string filePath  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Config", "robotarm-config.json");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("NO CONFIG FILE for Robot arms");
                return string.Empty;
            }
            return filePath;
        }

        // return도 하고 (robot arm을 위해)
        // display offsets도 설정하고
        // TODO later: 정돈하기
        private RobotArmOffsets FetchOffsetsFromConfig(string ip)
        {
            string filePath = GetRobotArmConfigFilePath();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            Dictionary<string, OffsetDto>? map;

            using (var fs = File.OpenRead(filePath))
                map = JsonSerializer.Deserialize<Dictionary<string, OffsetDto>>(fs, options);

            if (map == null || !map.TryGetValue(ip, out var dto))
                throw new KeyNotFoundException($"Offsets for IP '{ip}' not found.");

            return new RobotArmOffsets(ip, dto.X, dto.Y, dto.Z);
        }

        // 일단 이렇게 코딩함..
        public RobotArmOffsets InitializeOffsetSetUps(string ip)
        {
            var presetOffsets = FetchOffsetsFromConfig(ip);
            DisplayedOffsets.Add(presetOffsets);
            return presetOffsets;
        }

        async public Task UpdateRobotArmOffsetsAsync()
        {
            string filePath = GetRobotArmConfigFilePath();
            
            // update 된 config file에서 값 가져와서 쓰게 하는 게 이상적이지만 일단 유저가 바꾼 값을 바로 사용하여 수정하는 걸로 구현한다
            SetUpRobotArmOffsets();

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };

            // 기존 파일 로드(없으면 빈 맵)
            Dictionary<string, OffsetDto> map = new();
            var text = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            map = JsonSerializer.Deserialize<Dictionary<string, OffsetDto>>(text, jsonOptions) ?? new();

            // UI 컬렉션 스냅샷
            var snapshot = (_dispatcher != null && !_dispatcher.CheckAccess())
                ? await _dispatcher.InvokeAsync(() => DisplayedOffsets.ToList())
                : DisplayedOffsets.ToList();

            // IP를 키로 병합/갱신
            foreach (var o in snapshot)
                map[o.IP] = new OffsetDto { X = o.X, Y = o.Y, Z = o.Z };

            // 저장
            var output = JsonSerializer.Serialize(map, jsonOptions);
            await File.WriteAllTextAsync(filePath, output, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            LogService.Instance.AddLog(new Log("New offsets saved to the config file.", LogType.SettingFileUpdated));
        }

        // called when...
        // (1) initializing robot arms when the app starts
        // -> (2) user modifies offsets & save them
        private void SetUpRobotArmOffsets()
        {
            // -> RobotService에 가서 수정하는 거
            foreach (var robotArm in RobotArmService.Instance.RobotArms)
            {
                var newOffset = DisplayedOffsets.Where(x => x.IP == robotArm.IP).FirstOrDefault();

                if (newOffset == null)
                {
                    throw new Exception("Unexpected Error when setting new offset for robot arms");
                }

                robotArm.UpdateOffsets(new RobotArmOffsets(robotArm.IP, newOffset.X, newOffset.Y, newOffset.Z));
            }
        }

    }
}
