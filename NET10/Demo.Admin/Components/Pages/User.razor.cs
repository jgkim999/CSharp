using Demo.Infra.Repositories;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Demo.Admin.Components.Pages;

public partial class User : ComponentBase
{
    private List<Demo.Domain.Entities.User> Users = new List<Demo.Domain.Entities.User>();
   
    [Inject]
    private DemoDbContext DbContext { get; set; } = default!;
    
    protected override async Task OnInitializedAsync()
    {
        Users = await DbContext.Users.AsNoTracking().Take(5).ToListAsync();
    }
}
