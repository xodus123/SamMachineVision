using System.Windows.Input;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MVXTester.Chat.ViewModels;

public partial class ChatMessageViewModel : ObservableObject
{
    [ObservableProperty] private string _content = "";
    public bool IsUser { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;

    // 첨부 이미지 (유저 메시지용)
    public BitmapImage? AttachedImage { get; init; }
    public bool HasAttachedImage => AttachedImage != null;

    // 예제 로딩 관련
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasExample))]
    private string? _exampleFileName;

    [ObservableProperty]
    private ICommand? _loadExampleCommand;

    public bool HasExample => ExampleFileName != null;

    // 도움말 열기 버튼
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowHelpButton))]
    private ICommand? _openHelpCommand;

    public bool ShowHelpButton => OpenHelpCommand != null;

    // 액션 링크 (URL 하이퍼링크)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActionUrl))]
    private string? _actionUrl;

    [ObservableProperty]
    private string? _actionUrlText;

    public bool HasActionUrl => ActionUrl != null;
}
