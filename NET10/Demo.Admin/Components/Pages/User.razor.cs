using Demo.Infra.Repositories;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace Demo.Admin.Components.Pages;

public partial class User : ComponentBase
{
    private List<Domain.Entities.User> Users = new List<Demo.Domain.Entities.User>();
    private MudDataGrid<Domain.Entities.User> _dataGrid;
    private int _pageSize = 10;
    private string _searchTerm = string.Empty;
    private string _currentSearchTerm = string.Empty;

    [Inject] private IDbContextFactory<DemoDbContext> DbFactory { get; set; } = default!;
    
    [Inject] private Blazored.LocalStorage.ILocalStorageService LocalStorage { get; set; } 
    
    protected override async Task OnInitializedAsync()
    {
        var storedPageSize = await LocalStorage.GetItemAsync<int>("PageSize");
        if (storedPageSize != 0)
        {
            _pageSize = storedPageSize;
            if (_dataGrid != null)
                await _dataGrid.SetRowsPerPageAsync(_pageSize);
        }
        
        var storedSearchTerm = await LocalStorage.GetItemAsync<string>("SearchTerm");
        if (!string.IsNullOrEmpty(storedSearchTerm))
        {
            _searchTerm = storedSearchTerm;
            _currentSearchTerm = storedSearchTerm;
        }
        
        _dataGrid.PagerStateHasChangedEvent += async () => 
            await OnPageSizeChangedAsync(_dataGrid.RowsPerPage);
    }
    
    private async Task OnPageSizeChangedAsync(int newPageSize)
    {
        _pageSize = newPageSize;
        await LocalStorage.SetItemAsync("PageSize", newPageSize);
    }
    
    private async Task OnSearchAsync()
    {
        _currentSearchTerm = _searchTerm;
        await LocalStorage.SetItemAsync("SearchTerm", _searchTerm);
        await _dataGrid.ReloadServerData();
    }
    
    private async Task<GridData<Domain.Entities.User>> LoadGridData(GridState<Domain.Entities.User> state)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        
        var query = db.Users.AsNoTracking();
        
        if (!string.IsNullOrWhiteSpace(_currentSearchTerm))
        {
            query = query.Where(u => u.Name.Contains(_currentSearchTerm));
        }
        
        var result = await query
            .OrderBy(x => x.Id)
            .Skip(state.Page * state.PageSize)
            .Take(state.PageSize)
            .ToListAsync();
        
        var totalCount = await query.CountAsync();
        
        GridData<Domain.Entities.User> data = new()
        {
            Items = result,
            TotalItems = totalCount
        };
        return data;
    }
}
