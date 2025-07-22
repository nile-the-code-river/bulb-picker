# bulb-picker

## 프로젝트 폴더 구조 설계
App.xaml
Main.xaml
appsettings.json

Services/
- CameraService.cs
- LogService.cs
- ScaraService.cs
- ConfigService.cs

Views/
- ViewerControl.xaml
- AppConfigControl.xaml
- CamerasControl.xaml
- RobotArmsControl.xaml
- LogsControl.xaml

ViewModels/
- ViewerViewModel.cs
- AppConfigViewModel.cs
- CamerasViewModel.cs
- RobotArmsViewModel.cs
- LogsViewModel.cs

Models/
- CameraFrame.cs
- ScaraState.cs (Enum)
- LogMessage.cs
- SystemConfig.cs

Infrastructures/
- ViewModelBase.cs
- RelayCommand.cs
- AsyncCommand.cs
- DispatcherHelper.cs