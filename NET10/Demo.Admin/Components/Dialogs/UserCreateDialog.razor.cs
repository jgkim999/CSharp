using Demo.Admin.Models;
using Demo.Admin.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Demo.Admin.Components.Dialogs;

public partial class UserCreateDialog : ComponentBase
{
    [CascadingParameter] 
    IDialogReference? MudDialog { get; set; }
    
    [Parameter]
    public EventCallback OnCancel { get; set; }
    
    [Parameter]
    public EventCallback<bool> OnCreateSuccess { get; set; }
    
    [Inject] 
    private IUserService UserService { get; set; } = default!;
    
    [Inject] 
    private ISnackbar Snackbar { get; set; } = default!;
    
    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    private MudForm _form = default!;
    private bool _isFormValid;
    private bool _isCreating;
    private UserCreateModel _model = new();
    private UserCreateModelValidator _validator = new();

    protected override void OnInitialized()
    {
        // 스낵바 설정
        Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
        Snackbar.Configuration.ShowTransitionDuration = 100;
        Snackbar.Configuration.VisibleStateDuration = 3000;
        Snackbar.Configuration.HideTransitionDuration = 100;
        Snackbar.Configuration.ShowCloseIcon = true;
    }

    private async Task CreateUser()
    {
        if (!_isFormValid || _isCreating)
            return;

        // 폼 유효성 재검사
        await _form.Validate();
        if (!_form.IsValid)
        {
            Snackbar.Add("입력 정보를 확인해주세요.", MudBlazor.Severity.Warning);
            return;
        }

        _isCreating = true;
        StateHasChanged();

        try
        {
            var (success, message) = await UserService.CreateUserAsync(_model);

            if (success)
            {
                Snackbar.Add(message, MudBlazor.Severity.Success);
                Console.WriteLine("사용자 생성 성공, 다이얼로그 강제 닫기");

                // 강제로 다이얼로그 닫기
                StateHasChanged();
                await Task.Delay(200); // 조금 더 긴 지연

                if (MudDialog != null)
                {
                    MudDialog.Close(DialogResult.Ok(true));
                    Console.WriteLine("사용자 MudDialog.Close 호출 완료");
                }
                else
                {
                    Console.WriteLine("사용자 MudDialog가 null입니다");
                }
            }
            else
            {
                Snackbar.Add(message, MudBlazor.Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"예상치 못한 오류가 발생했습니다: {ex.Message}", MudBlazor.Severity.Error);
        }
        finally
        {
            _isCreating = false;
            StateHasChanged();
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

public class UserCreateModelValidator : AbstractValidator<UserCreateModel>
{
    public UserCreateModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("이름은 필수입니다")
            .Length(3, 255).WithMessage("이름은 3자 이상 255자 이하여야 합니다");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("이메일은 필수입니다")
            .EmailAddress().WithMessage("유효한 이메일 주소를 입력해주세요")
            .Length(3, 255).WithMessage("이메일은 3자 이상 255자 이하여야 합니다");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("비밀번호는 필수입니다")
            .Length(8, 64).WithMessage("비밀번호는 8자 이상 64자 이하여야 합니다");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("비밀번호 확인은 필수입니다")
            .Equal(x => x.Password).WithMessage("비밀번호가 일치하지 않습니다");
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        var result = await ValidateAsync(ValidationContext<UserCreateModel>.CreateWithOptions((UserCreateModel)model, x => x.IncludeProperties(propertyName)));
        if (result.IsValid)
            return Array.Empty<string>();
        return result.Errors.Select(e => e.ErrorMessage);
    };
}