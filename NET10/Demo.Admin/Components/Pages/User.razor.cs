using Demo.Infra.Repositories;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace Demo.Admin.Components.Pages;

public partial class User : ComponentBase
{
    private const string USER_SEARCH_TERM_KEY = "UserSearchTerm";
    
    private MudDataGrid<Domain.Entities.User> _dataGrid = null!;
    private string _searchTerm = string.Empty;

    [Inject] private IDbContextFactory<DemoDbContext> DbFactory { get; set; } = default!;
    [Inject] private Blazored.LocalStorage.ILocalStorageService LocalStorage { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        // LocalStorage에서 검색어 복원
        var storedSearchTerm = await GetStoredSearchTermAsync();
        if (!string.IsNullOrEmpty(storedSearchTerm))
        {
            _searchTerm = storedSearchTerm;
        }
    }

    private async Task OnSearchAsync()
    {
        // 검색어 저장
        await SaveSearchTermAsync(_searchTerm);
        
        // 데이터 그리드 새로고침
        if (_dataGrid != null)
        {
            await _dataGrid.ReloadServerData();
        }
    }

    private async Task<GridData<Domain.Entities.User>> LoadGridData(GridState<Domain.Entities.User> state)
    {
        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            var query = db.Users.AsNoTracking();

            // 현재 검색어로 필터링 (페이지 이동 후 돌아왔을 때도 LocalStorage에서 자동으로 가져옴)
            var currentSearchTerm = await GetStoredSearchTermAsync();
            if (!string.IsNullOrWhiteSpace(currentSearchTerm))
            {
                query = query.Where(u => u.Name.Contains(currentSearchTerm));
                
                // UI의 검색 필드도 동기화
                if (_searchTerm != currentSearchTerm)
                {
                    _searchTerm = currentSearchTerm;
                    StateHasChanged();
                }
            }

            // 페이징 적용
            var result = await query
                .OrderBy(x => x.Id)
                .Skip(state.Page * state.PageSize)
                .Take(state.PageSize)
                .ToListAsync();

            var totalCount = await query.CountAsync();

            return new GridData<Domain.Entities.User>
            {
                Items = result,
                TotalItems = totalCount
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"데이터 로드 오류: {ex.Message}");
            return new GridData<Domain.Entities.User>
            {
                Items = new List<Domain.Entities.User>(),
                TotalItems = 0
            };
        }
    }

    private async Task<string> GetStoredSearchTermAsync()
    {
        try
        {
            return await LocalStorage.GetItemAsync<string>(USER_SEARCH_TERM_KEY) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private async Task SaveSearchTermAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                await LocalStorage.RemoveItemAsync(USER_SEARCH_TERM_KEY);
            }
            else
            {
                await LocalStorage.SetItemAsync(USER_SEARCH_TERM_KEY, searchTerm);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"검색어 저장 오류: {ex.Message}");
        }
    }
}