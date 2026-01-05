using CherryPickTool.App.ViewModels;

namespace CherryPickTool.App;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
