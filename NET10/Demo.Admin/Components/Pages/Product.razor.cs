using Demo.Application.DTO.Product;
using Demo.Application.DTO.Company;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.Json;
using RestSharp;
using System.Diagnostics;
using Demo.Application.Models;
using Demo.Admin.Components.Dialogs;

namespace Demo.Admin.Components.Pages;

public partial class Product : ComponentBase
{
    private const string PRODUCT_SEARCH_TERM_KEY = "ProductSearchTerm";
    private const string PRODUCT_COMPANY_FILTER_KEY = "ProductCompanyFilter";
    private const string PRODUCT_PAGE_SIZE_KEY = "ProductPageSize";

    private static readonly ActivitySource DemoAdminActivitySource = new("Demo.Admin");

    private MudDataGrid<ProductDto> _dataGrid = null!;
    private string _searchTerm = string.Empty;
    private long? _selectedCompanyId = null;
    private CompanyDto? _selectedCompany = null;
    private List<CompanyDto> _companies = new();

    // 중복 trace 방지를 위한 상태 추적
    private DateTime _lastTraceTime = DateTime.MinValue;
    private string _lastTraceKey = string.Empty;

    [Inject] private RestClient RestClient { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private Blazored.LocalStorage.ILocalStorageService LocalStorage { get; set; } = default!;
    [Inject] private ILogger<Product> Logger { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation(nameof(Product));

        // 회사 목록 로드
        await LoadCompaniesAsync();

        // LocalStorage에서 검색어와 필터 복원
        var storedSearchTerm = await GetStoredSearchTermAsync();
        if (!string.IsNullOrEmpty(storedSearchTerm))
        {
            _searchTerm = storedSearchTerm;
        }

        var storedCompanyId = await GetStoredCompanyFilterAsync();
        if (storedCompanyId.HasValue)
        {
            _selectedCompanyId = storedCompanyId;
            _selectedCompany = _companies.FirstOrDefault(c => c.Id == storedCompanyId.Value);
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
        }
    }

    private async Task LoadCompaniesAsync()
    {
        try
        {
            Logger.LogInformation("회사 목록 로드 시작");

            var allCompanies = new List<CompanyDto>();
            var page = 0;
            const int pageSize = 100;
            bool hasMoreData = true;

            while (hasMoreData)
            {
                var request = new RestRequest("api/company/list", Method.Post);
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Content-Type", "application/json");

                var requestObject = new
                {
                    SearchTerm = "",
                    Page = page,
                    PageSize = pageSize
                };

                request.AddJsonBody(requestObject);
                Logger.LogInformation("회사 목록 API 요청 전송 - Page: {Page}", page);

                var response = await RestClient.ExecuteAsync(request);
                Logger.LogInformation("회사 목록 API 응답 수신 - StatusCode: {StatusCode}, IsSuccessful: {IsSuccessful}",
                    response.StatusCode, response.IsSuccessful);

                if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var apiResponse = JsonSerializer.Deserialize<CompanyListResponse>(response.Content, options);
                    if (apiResponse != null)
                    {
                        allCompanies.AddRange(apiResponse.Items);
                        Logger.LogInformation("페이지 {Page} 로드 완료 - {Count}개 회사 추가, 총 {TotalItems}개 중 {LoadedCount}개 로드됨",
                            page, apiResponse.Items.Count, apiResponse.TotalItems, allCompanies.Count);

                        // 더 가져올 데이터가 있는지 확인
                        hasMoreData = allCompanies.Count < apiResponse.TotalItems && apiResponse.Items.Count == pageSize;
                        page++;

                        // 안전장치: 최대 10페이지까지만 (1000개 회사)
                        if (page >= 10)
                        {
                            Logger.LogWarning("회사 수가 너무 많아 최대 1000개까지만 로드합니다");
                            hasMoreData = false;
                        }
                    }
                    else
                    {
                        Logger.LogWarning("회사 목록 API 응답이 null입니다");
                        hasMoreData = false;
                    }
                }
                else
                {
                    var errorMessage = $"HTTP {response.StatusCode}: {response.ErrorMessage ?? "알 수 없는 오류"}";
                    Logger.LogError("회사 목록 API 호출 실패: {Error}, Content: {Content}", errorMessage, response.Content);
                    Snackbar.Add($"회사 목록을 불러오는데 실패했습니다: {errorMessage}", Severity.Error);
                    hasMoreData = false;
                }
            }

            _companies = allCompanies;
            Logger.LogInformation("회사 목록 로드 완료 - 총 {Count}개 회사", _companies.Count);

            // UI 업데이트 강제
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "회사 목록 로드 중 예외 발생");
            Snackbar.Add($"회사 목록을 불러오는데 실패했습니다: {ex.Message}", Severity.Error);
        }
    }

    private async Task OnSearchAsync()
    {
        // 검색어와 필터 저장
        await SaveSearchTermAsync(_searchTerm);
        await SaveCompanyFilterAsync(_selectedCompanyId);

        // 검색 실행 알림
        var searchInfo = new List<string>();
        if (!string.IsNullOrWhiteSpace(_searchTerm))
        {
            searchInfo.Add($"'{_searchTerm}'");
        }
        if (_selectedCompanyId.HasValue)
        {
            var companyName = _companies.FirstOrDefault(c => c.Id == _selectedCompanyId.Value)?.Name ?? "선택된 회사";
            searchInfo.Add($"회사: {companyName}");
        }

        if (searchInfo.Any())
        {
            Snackbar.Add($"{string.Join(", ", searchInfo)}로 검색 중...", Severity.Info);
        }
        else
        {
            Snackbar.Add("전체 상품 목록을 불러오는 중...", Severity.Info);
        }

        // 데이터 그리드 새로고침
        if (_dataGrid != null)
        {
            await _dataGrid.ReloadServerData();
        }
    }

    private async Task<GridData<ProductDto>> LoadGridData(GridState<ProductDto> state)
    {
        // 마지막 페이지 호출인지 확인 (페이지네이션 중복 호출 방지)
        var shouldTrace = ShouldCreateTrace(state);

        // OpenTelemetry Activity 생성 (조건부)
        using var activity = shouldTrace ? DemoAdminActivitySource.StartActivity("Product List API Call") : null;
        activity?.SetTag("blazor.component", "Product");
        activity?.SetTag("operation.name", "load_product_list");

        try
        {
            // 현재 검색어와 필터로 필터링
            var currentSearchTerm = await GetStoredSearchTermAsync();
            var currentCompanyId = await GetStoredCompanyFilterAsync();

            // UI의 검색 필드도 동기화
            if (_searchTerm != currentSearchTerm || _selectedCompanyId != currentCompanyId)
            {
                _searchTerm = currentSearchTerm;
                _selectedCompanyId = currentCompanyId;
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

            // RestSharp를 사용한 Demo.Web product list API 호출
            var request = new RestRequest("api/product/list", Method.Post);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");

            // POST 요청을 위한 요청 객체 구성
            var requestObject = new
            {
                SearchTerm = currentSearchTerm,
                CompanyId = currentCompanyId,
                Page = state.Page,
                PageSize = actualPageSize
            };

            request.AddJsonBody(requestObject);

            activity?.SetTag("api.request.search_term", currentSearchTerm ?? "null");
            activity?.SetTag("api.request.company_id", currentCompanyId?.ToString() ?? "null");
            activity?.SetTag("api.request.page", state.Page);
            activity?.SetTag("api.request.page_size", actualPageSize);

            var response = await RestClient.ExecuteAsync(request);

            if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var apiResponse = JsonSerializer.Deserialize<ProductListResponse>(response.Content, options);

                if (apiResponse == null)
                {
                    throw new InvalidOperationException("API 응답이 null입니다.");
                }

                activity?.SetTag("api.response.total_items", apiResponse.TotalItems);
                activity?.SetTag("api.response.returned_items", apiResponse.Items.Count);

                // 검색 결과에 따른 적절한 피드백 제공
                if (!string.IsNullOrEmpty(currentSearchTerm) || currentCompanyId.HasValue)
                {
                    var filterInfo = new List<string>();
                    if (!string.IsNullOrEmpty(currentSearchTerm))
                        filterInfo.Add($"'{currentSearchTerm}'");
                    if (currentCompanyId.HasValue)
                    {
                        var companyName = _companies.FirstOrDefault(c => c.Id == currentCompanyId.Value)?.Name ?? "선택된 회사";
                        filterInfo.Add($"회사: {companyName}");
                    }
                    Snackbar.Add($"{string.Join(", ", filterInfo)} 검색 완료: {apiResponse.TotalItems}건 발견", Severity.Success);
                }
                else if (state.Page == 0)
                {
                    Snackbar.Add($"상품 목록을 성공적으로 불러왔습니다. (총 {apiResponse.TotalItems}건)", Severity.Success);
                }

                return new GridData<ProductDto>
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

                Snackbar.Add("상품 목록을 불러오는데 실패했습니다.", Severity.Error);

                return new GridData<ProductDto>
                {
                    Items = new List<ProductDto>(),
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

            return new GridData<ProductDto>
            {
                Items = new List<ProductDto>(),
                TotalItems = 0
            };
        }
    }

    private async Task<string> GetStoredSearchTermAsync()
    {
        try
        {
            return await LocalStorage.GetItemAsync<string>(PRODUCT_SEARCH_TERM_KEY) ?? string.Empty;
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
                await LocalStorage.RemoveItemAsync(PRODUCT_SEARCH_TERM_KEY);
            }
            else
            {
                await LocalStorage.SetItemAsync(PRODUCT_SEARCH_TERM_KEY, searchTerm);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"검색어 저장 오류: {ex.Message}");
        }
    }

    private async Task<long?> GetStoredCompanyFilterAsync()
    {
        try
        {
            return await LocalStorage.GetItemAsync<long?>(PRODUCT_COMPANY_FILTER_KEY);
        }
        catch
        {
            return null;
        }
    }

    private async Task SaveCompanyFilterAsync(long? companyId)
    {
        try
        {
            if (companyId.HasValue)
            {
                await LocalStorage.SetItemAsync(PRODUCT_COMPANY_FILTER_KEY, companyId.Value);
            }
            else
            {
                await LocalStorage.RemoveItemAsync(PRODUCT_COMPANY_FILTER_KEY);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"회사 필터 저장 오류: {ex.Message}");
        }
    }

    private async Task<int> GetStoredPageSizeAsync()
    {
        try
        {
            return await LocalStorage.GetItemAsync<int>(PRODUCT_PAGE_SIZE_KEY);
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
            await LocalStorage.SetItemAsync(PRODUCT_PAGE_SIZE_KEY, pageSize);
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
    private bool ShouldCreateTrace(GridState<ProductDto> state)
    {
        var now = DateTime.UtcNow;
        var currentTraceKey = $"{state.Page}_{state.PageSize}_{_searchTerm}_{_selectedCompanyId}";

        // 1초 이내의 동일한 요청은 trace 생성 스킵
        var timeSinceLastTrace = now - _lastTraceTime;
        if (timeSinceLastTrace.TotalMilliseconds < 1000 && _lastTraceKey == currentTraceKey)
        {
            return false;
        }

        // 초기 로드, 검색, 페이지 크기 변경 시에만 trace 생성
        var isInitialLoad = state.Page == 0 && string.IsNullOrEmpty(_searchTerm) && !_selectedCompanyId.HasValue;
        var isSearch = !string.IsNullOrEmpty(_searchTerm) || _selectedCompanyId.HasValue;
        var isPageSizeChange = _lastTraceKey != currentTraceKey && state.Page == 0;

        var shouldTrace = isInitialLoad || isSearch || isPageSizeChange;

        if (shouldTrace)
        {
            _lastTraceTime = now;
            _lastTraceKey = currentTraceKey;
        }

        return shouldTrace;
    }

    private async Task OnCreateProductAsync()
    {
        Console.WriteLine("OnCreateProductAsync 시작");
        IDialogReference? dialogRef = null;

        var parameters = new DialogParameters
        {
            ["OnCancel"] = EventCallback.Factory.Create(this, () => dialogRef?.Close(DialogResult.Cancel())),
            ["OnProductCreated"] = new Action<bool>((success) =>
            {
                Console.WriteLine($"OnProductCreated Action 호출됨 - Success: {success}");
                if (success)
                {
                    // 즉시 다이얼로그 닫기
                    dialogRef?.Close(DialogResult.Ok(true));
                    Console.WriteLine("Action에서 다이얼로그 닫기 완료");

                    // 데이터 그리드 새로고침을 비동기로 실행
                    InvokeAsync(async () =>
                    {
                        if (_dataGrid != null)
                        {
                            await _dataGrid.ReloadServerData();
                            Console.WriteLine("Action에서 데이터 그리드 새로고침 완료");
                        }
                    });
                }
            }),
            ["Companies"] = _companies
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        Console.WriteLine("다이얼로그 열기");
        dialogRef = await DialogService.ShowAsync<ProductCreateDialog>("새 상품 생성", parameters, options);
        Console.WriteLine("다이얼로그 결과 대기 중");

        // 다이얼로그 결과를 안전하게 기다리기
        try
        {
            var result = await dialogRef.Result;
            Console.WriteLine($"다이얼로그 결과 - Canceled: {result?.Canceled}, Data: {result?.Data}");

            if (result != null && !result.Canceled)
            {
                Console.WriteLine("상품 생성 성공, 데이터 그리드 새로고침 시작");
                // 상품이 성공적으로 생성되면 데이터 그리드 새로고침
                if (_dataGrid != null)
                {
                    await _dataGrid.ReloadServerData();
                    Console.WriteLine("데이터 그리드 새로고침 완료");
                }
                else
                {
                    Console.WriteLine("데이터 그리드가 null입니다");
                }
            }
            else
            {
                Console.WriteLine("다이얼로그가 취소되었거나 결과가 null입니다");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"다이얼로그 결과 처리 중 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// MudAutocomplete에서 회사를 검색하는 함수
    /// </summary>
    private Task<IEnumerable<CompanyDto>> SearchCompanies(string value, CancellationToken cancellationToken)
    {
        // 검색어가 비어있으면 상위 20개 회사 반환
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult(_companies.Take(20).AsEnumerable());
        }

        // 회사명에서 검색어가 포함된 회사들을 찾아서 반환
        var result = _companies
            .Where(company => company.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
            .Take(50);

        return Task.FromResult(result.AsEnumerable());
    }

    /// <summary>
    /// Autocomplete에서 회사 선택 시 호출되는 이벤트 핸들러
    /// </summary>
    private async Task OnCompanySelectionChanged(CompanyDto? selectedCompany)
    {
        _selectedCompany = selectedCompany;
        _selectedCompanyId = selectedCompany?.Id;

        // 선택된 회사 저장
        await SaveCompanyFilterAsync(_selectedCompanyId);

        // 자동 검색 실행
        await OnSearchAsync();
    }
}