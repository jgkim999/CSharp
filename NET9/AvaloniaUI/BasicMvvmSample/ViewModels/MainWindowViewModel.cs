namespace BasicMvvmSample.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Welcome to Avalonia!";
    
    public SimpleViewModel SimpleViewModel { get; } = new SimpleViewModel();
}
