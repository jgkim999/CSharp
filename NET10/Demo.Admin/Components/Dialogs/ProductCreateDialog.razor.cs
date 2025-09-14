using Demo.Admin.Models;
using Demo.Admin.Services;
using Demo.Application.DTO.Company;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Demo.Admin.Components.Dialogs;

public partial class ProductCreateDialog : ComponentBase
{
    private readonly ProductCreateModel _model = new();
    private bool _isLoading = false;
    private string _errorMessage = string.Empty;
    private CompanyDto? _selectedCompany = null;

    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public Action<bool> OnProductCreated { get; set; } = null!;
    [Parameter] public List<CompanyDto> Companies { get; set; } = new();

    [Inject] private IProductService ProductService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<ProductCreateDialog> Logger { get; set; } = default!;

    private async Task Cancel()
    {
        Console.WriteLine("ProductCreateDialog Cancel 호출");
        await OnCancel.InvokeAsync();
    }

    private async Task Submit()
    {
        await OnValidSubmit();
    }

    private async Task OnValidSubmit()
    {
        Console.WriteLine($"ProductCreateDialog OnValidSubmit 시작 - CompanyId: {_model.CompanyId}, Name: {_model.Name}, Price: {_model.Price}");

        if (_isLoading)
        {
            Console.WriteLine("이미 로딩 중이므로 요청 무시");
            return;
        }

        _isLoading = true;
        _errorMessage = string.Empty;
        StateHasChanged();

        try
        {
            Console.WriteLine("ProductService.CreateProductAsync 호출");
            var success = await ProductService.CreateProductAsync(_model);
            Console.WriteLine($"ProductService.CreateProductAsync 결과: {success}");

            if (success)
            {
                Snackbar.Add("상품이 성공적으로 생성되었습니다.", Severity.Success);
                Console.WriteLine("OnProductCreated Action 호출 준비 - Success: true");
                OnProductCreated?.Invoke(true);
            }
            else
            {
                _errorMessage = "상품 생성에 실패했습니다. 다시 시도해주세요.";
                Snackbar.Add(_errorMessage, Severity.Error);
                Console.WriteLine($"상품 생성 실패: {_errorMessage}");
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"오류가 발생했습니다: {ex.Message}";
            Snackbar.Add(_errorMessage, Severity.Error);
            Logger.LogError(ex, "상품 생성 중 예외 발생");
            Console.WriteLine($"상품 생성 중 예외: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
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
            return Task.FromResult(Companies.Take(20).AsEnumerable());
        }

        // 회사명에서 검색어가 포함된 회사들을 찾아서 반환
        var result = Companies
            .Where(company => company.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
            .Take(50);

        return Task.FromResult(result.AsEnumerable());
    }

    /// <summary>
    /// Autocomplete에서 회사 선택 시 호출되는 이벤트 핸들러
    /// </summary>
    private void OnCompanyChanged(CompanyDto? selectedCompany)
    {
        _selectedCompany = selectedCompany;
        _model.CompanyId = selectedCompany?.Id ?? 0;
        StateHasChanged();
    }
}