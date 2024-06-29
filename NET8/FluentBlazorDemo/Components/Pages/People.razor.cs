using Microsoft.AspNetCore.Components;

namespace FluentBlazorDemo.Components.Pages;

public partial class People : ComponentBase
{
    private void BtnClick()
    {
        Logger.LogInformation("Click me");
    }
}
