using System.ComponentModel;

namespace BulbPicker.App.Services
{
    // TODO: OLD LOGIC. DELETE

    // 바슬러 카메라에서 온 이미지가 몇 번째 이미지인지, 함께 합칠 때 사용할 이미지의 차례용 index 관리 클래스
    // 200ms 간격으로 바슬러 카메라에 사진 찍기 요청을 보낸다는 점에 주의하여 알고리즘 설계
    // outside, inside  이미지가 동시에 찍혔다면 둘 다 인덱스 n을 가져야 한다
    public sealed class GrabbedImageIndexManager : INotifyPropertyChanged
    {
        private static readonly GrabbedImageIndexManager _instance = new GrabbedImageIndexManager();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public  static GrabbedImageIndexManager Instance => _instance;
        private GrabbedImageIndexManager() { }

        private int _managedImageIndex = 0;
        public int ManagedImageIndex
        {
            get => _managedImageIndex;
            private set
            {
                _managedImageIndex = value;
                OnPropertyChanged(nameof(ManagedImageIndex));
            }
        }

        public void Increment() => ManagedImageIndex++;
    }
}
