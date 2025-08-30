using Demo.Infra.Repositories;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Demo.Admin.Components.Pages;

public partial class User : ComponentBase
{
    private List<Demo.Domain.Entities.User> Users = new List<Demo.Domain.Entities.User>();
   
    [Inject]
    private IDbContextFactory<DemoDbContext> DbFactory { get; set; } = default!;
    
    protected override async Task OnInitializedAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        Users = await db.Users.AsNoTracking().Take(5).ToListAsync();
    }
}
