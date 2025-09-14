using Demo.Admin.Models;
using Demo.Admin.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Demo.Admin.Components.Dialogs;

public partial class CompanyCreateDialog : ComponentBase
{
    [CascadingParameter]
    IDialogReference? MudDialog { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter]
    public EventCallback<bool> OnCreateSuccess { get; set; }

    [Parameter]
    public Action<bool>? OnCompanyCreated { get; set; }

    [Inject]
    private ICompanyService CompanyService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    private MudForm _form = default!;
    private bool _isFormValid;
    private bool _isCreating;
    private CompanyCreateModel _model = new();
    private CompanyCreateModelValidator _validator = new();

    protected override void OnInitialized()
    {
        // 스낵바 설정
        Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
        Snackbar.Configuration.ShowTransitionDuration = 100;
        Snackbar.Configuration.VisibleStateDuration = 3000;
        Snackbar.Configuration.HideTransitionDuration = 100;
        Snackbar.Configuration.ShowCloseIcon = true;
    }

    private async Task CreateCompany()
    {
        Console.WriteLine("CreateCompany 메서드 시작");

        if (!_isFormValid || _isCreating)
        {
            Console.WriteLine($"폼 유효성 검사 실패 - IsFormValid: {_isFormValid}, IsCreating: {_isCreating}");
            return;
        }

        // 폼 유효성 재검사
        await _form.Validate();
        if (!_form.IsValid)
        {
            Console.WriteLine("폼 유효성 검사 실패");
            Snackbar.Add("입력 정보를 확인해주세요.", MudBlazor.Severity.Warning);
            return;
        }

        _isCreating = true;
        StateHasChanged();
        Console.WriteLine("회사 생성 API 호출 시작");

        try
        {
            var (success, message) = await CompanyService.CreateCompanyAsync(_model);
            Console.WriteLine($"API 호출 결과 - Success: {success}, Message: {message}");

            if (success)
            {
                Snackbar.Add(message, MudBlazor.Severity.Success);
                Console.WriteLine("회사 생성 성공, 부모에게 알림");

                // 부모에게 성공 알림
                OnCompanyCreated?.Invoke(true);
                Console.WriteLine("OnCompanyCreated 호출 완료");
            }
            else
            {
                Console.WriteLine("회사 생성 실패");
                Snackbar.Add(message, MudBlazor.Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"예외 발생: {ex.Message}");
            Snackbar.Add($"예상치 못한 오류가 발생했습니다: {ex.Message}", MudBlazor.Severity.Error);
        }
        finally
        {
            _isCreating = false;
            StateHasChanged();
            Console.WriteLine("CreateCompany 메서드 종료");
        }
    }

    private async Task Cancel()
    {
        Console.WriteLine("Cancel 메서드가 호출되었습니다.");
        if (MudDialog != null)
        {
            MudDialog.Close(DialogResult.Cancel());
        }
        else
        {
            Console.WriteLine("MudDialog가 null입니다. OnCancel EventCallback을 사용합니다.");
            await OnCancel.InvokeAsync();
        }
    }
}

public class CompanyCreateModelValidator : AbstractValidator<CompanyCreateModel>
{
    public CompanyCreateModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("회사명은 필수입니다")
            .Length(2, 255).WithMessage("회사명은 2자 이상 255자 이하여야 합니다");
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        var result = await ValidateAsync(ValidationContext<CompanyCreateModel>.CreateWithOptions((CompanyCreateModel)model, x => x.IncludeProperties(propertyName)));
        if (result.IsValid)
            return Array.Empty<string>();
        return result.Errors.Select(e => e.ErrorMessage);
    };
}