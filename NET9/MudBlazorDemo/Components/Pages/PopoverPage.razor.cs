using Microsoft.AspNetCore.Components;

namespace MudBlazorDemo.Components.Pages;

public partial class PopoverPage : ComponentBase
{
    public string TextValue { get; set; } = "Hello, World!";
    
    protected async Task OnInitializeAsync()
    {
        await base.OnInitializedAsync();
    }
}
