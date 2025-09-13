using Demo.Application.DTO.User;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.Json;
using RestSharp;
using System.Diagnostics;
using System.Net;
using Demo.Application.Models;
using Demo.Admin.Components.Dialogs;
using Demo.Admin.Services;

namespace Demo.Admin.Components.Pages;

public partial class User : ComponentBase
{
    private const string USER_SEARCH_TERM_KEY = "UserSearchTerm";
    private const string PAGE_SIZE_KEY = "UserPageSize";
    
    private static readonly ActivitySource DemoAdminActivitySource = new("Demo.Admin");
    
    private MudDataGrid<UserDto> _dataGrid = null!;
    private string _searchTerm = string.Empty;
    
    // 중복 trace 방지를 위한 상태 추적
    private DateTime _lastTraceTime = DateTime.MinValue;
    private string _lastTraceKey = string.Empty;

    [Inject] private RestClient RestClient { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private Blazored.LocalStorage.ILocalStorageService LocalStorage { get; set; } = default!;
    [Inject] private ILogger<User> Logger { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation(nameof(User));
        
        // LocalStorage에서 검색어 복원
        var storedSearchTerm = await GetStoredSearchTermAsync();
        if (!string.IsNullOrEmpty(storedSearchTerm))
        {
            _searchTerm = storedSearchTerm;
        }
        
        Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
        Snackbar.Configuration.ShowTransitionDuration = 100;
        Snackbar.Configuration.VisibleStateDuration = 500;
        Snackbar.Configuration.HideTransitionDuration = 100;
        Snackbar.Configuration.ShowCloseIcon = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _dataGrid != null)
        {
            // 저장된 페이지 크기 복원
            var storedPageSize = await GetStoredPageSizeAsync();
            if (storedPageSize > 0)
            {
                await _dataGrid.SetRowsPerPageAsync(storedPageSize);
            }

            // 페이지 크기 변경 이벤트 등록 (중복 등록 방지)
            // LoadGridData에서 이미 페이지 크기를 저장하므로 여기서는 제거
        }
    }

    private async Task OnSearchAsync()
    {
        // 검색어 저장
        await SaveSearchTermAsync(_searchTerm);
        
        // 검색 실행 알림
        if (!string.IsNullOrWhiteSpace(_searchTerm))
        {
            Snackbar.Add($"'{_searchTerm}'로 검색 중...", Severity.Info);
        }
        else
        {
            Snackbar.Add("전체 사용자 목록을 불러오는 중...", Severity.Info);
        }
        
        // 데이터 그리드 새로고침
        if (_dataGrid != null)
        {
            await _dataGrid.ReloadServerData();
        }
    }

    private async Task<GridData<UserDto>> LoadGridData(GridState<UserDto> state)
    {
        // 마지막 페이지 호출인지 확인 (페이지네이션 중복 호출 방지)
        var shouldTrace = ShouldCreateTrace(state);
        
        // OpenTelemetry Activity 생성 (조건부)
        using var activity = shouldTrace ? DemoAdminActivitySource.StartActivity("User List API Call") : null;
        activity?.SetTag("blazor.component", "User");
        activity?.SetTag("operation.name", "load_user_list");
        
        try
        {
            // 현재 검색어로 필터링 (페이지 이동 후 돌아왔을 때도 LocalStorage에서 자동으로 가져옴)
            var currentSearchTerm = await GetStoredSearchTermAsync();
            
            // UI의 검색 필드도 동기화
            if (_searchTerm != currentSearchTerm)
            {
                _searchTerm = currentSearchTerm;
                StateHasChanged();
            }

            // 현재 상태의 페이지 크기를 우선 사용하고, 변경된 경우 저장
            var actualPageSize = state.PageSize;
            
            // 페이지 크기가 변경되었으면 저장
            var storedPageSize = await GetStoredPageSizeAsync();
            if (storedPageSize != actualPageSize)
            {
                await SavePageSizeAsync(actualPageSize);
            }

            // RestSharp를 사용한 Demo.Web user list API 호출
            var request = new RestRequest("api/user/list", Method.Post);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");

            // POST 요청을 위한 요청 객체 구성
            var requestObject = new
            {
                SearchTerm = currentSearchTerm,
                Page = state.Page,
                PageSize = actualPageSize
            };

            request.AddJsonBody(requestObject);

            activity?.SetTag("api.request.search_term", currentSearchTerm ?? "null");
            activity?.SetTag("api.request.page", state.Page);
            activity?.SetTag("api.request.page_size", actualPageSize);

            var response = await RestClient.ExecuteAsync(request);
            
            if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var apiResponse = JsonSerializer.Deserialize<UserListResponse>(response.Content, options);

                if (apiResponse == null)
                {
                    throw new InvalidOperationException("API 응답이 null입니다.");
                }

                activity?.SetTag("api.response.total_items", apiResponse.TotalItems);
                activity?.SetTag("api.response.returned_items", apiResponse.Items.Count);

                // 검색 결과에 따른 적절한 피드백 제공
                if (!string.IsNullOrEmpty(currentSearchTerm))
                {
                    Snackbar.Add($"'{currentSearchTerm}' 검색 완료: {apiResponse.TotalItems}건 발견", Severity.Success);
                }
                else if (state.Page == 0)
                {
                    Snackbar.Add($"사용자 목록을 성공적으로 불러왔습니다. (총 {apiResponse.TotalItems}건)", Severity.Success);
                }

                return new GridData<UserDto>
                {
                    Items = apiResponse.Items,
                    TotalItems = apiResponse.TotalItems
                };
            }
            else
            {
                var errorMessage = $"HTTP {(int)response.StatusCode} {response.StatusCode}: {response.ErrorMessage ?? "알 수 없는 오류"}";
                
                activity?.SetTag("operation.result", "http_error");
                activity?.SetTag("error.message", errorMessage);
                activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
                
                Snackbar.Add("사용자 목록을 불러오는데 실패했습니다.", Severity.Error);
                
                return new GridData<UserDto>
                {
                    Items = new List<UserDto>(),
                    TotalItems = 0
                };
            }
        }
        catch (Exception ex)
        {
            activity?.SetTag("operation.result", "exception");
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            
            Snackbar.Add("네트워크 오류가 발생했습니다.", Severity.Error);
            Console.WriteLine($"데이터 로드 오류: {ex.Message}");
            
            return new GridData<UserDto>
            {
                Items = new List<UserDto>(),
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

    private async Task<int> GetStoredPageSizeAsync()
    {
        try
        {
            return await LocalStorage.GetItemAsync<int>(PAGE_SIZE_KEY);
        }
        catch
        {
            return 0;
        }
    }

    private async Task SavePageSizeAsync(int pageSize)
    {
        try
        {
            await LocalStorage.SetItemAsync(PAGE_SIZE_KEY, pageSize);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"페이지 크기 저장 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// OpenTelemetry trace 생성 여부를 결정합니다.
    /// 중복 호출이나 빠른 연속 호출 시 trace 생성을 스킵합니다.
    /// </summary>
    /// <param name="state">현재 그리드 상태</param>
    /// <returns>trace를 생성할지 여부</returns>
    private bool ShouldCreateTrace(GridState<UserDto> state)
    {
        var now = DateTime.UtcNow;
        var currentTraceKey = $"{state.Page}_{state.PageSize}_{_searchTerm}";
        
        // 1초 이내의 동일한 요청은 trace 생성 스킵
        var timeSinceLastTrace = now - _lastTraceTime;
        if (timeSinceLastTrace.TotalMilliseconds < 1000 && _lastTraceKey == currentTraceKey)
        {
            return false;
        }
        
        // 초기 로드, 검색, 페이지 크기 변경 시에만 trace 생성
        var isInitialLoad = state.Page == 0 && string.IsNullOrEmpty(_searchTerm);
        var isSearch = !string.IsNullOrEmpty(_searchTerm);
        var isPageSizeChange = _lastTraceKey != currentTraceKey && state.Page == 0;
        
        var shouldTrace = isInitialLoad || isSearch || isPageSizeChange;
        
        if (shouldTrace)
        {
            _lastTraceTime = now;
            _lastTraceKey = currentTraceKey;
        }
        
        return shouldTrace;
    }

    private async Task OnCreateUserAsync()
    {
        IDialogReference? dialogRef = null;
        
        var parameters = new DialogParameters
        {
            ["OnCancel"] = EventCallback.Factory.Create(this, () => dialogRef?.Close(DialogResult.Cancel()))
        };
        
        var options = new DialogOptions 
        { 
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        dialogRef = await DialogService.ShowAsync<UserCreateDialog>("새 사용자 생성", parameters, options);
        var result = await dialogRef.Result;

        if (result != null && !result.Canceled)
        {
            // 사용자가 성공적으로 생성되면 데이터 그리드 새로고침
            if (_dataGrid != null)
            {
                await _dataGrid.ReloadServerData();
            }
        }
    }
}