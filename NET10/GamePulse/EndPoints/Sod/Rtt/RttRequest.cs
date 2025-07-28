using FastEndpoints;
using FluentValidation;

namespace GamePulse.EndPoints.Sod.Rtt;

/// <summary>
/// Rtt 저장 요청
/// </summary>
public class RttRequest
{
    /// <summary>
    /// client or server
    /// </summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>
    /// mirror에서 측정된 rtt (단위 msec)
    /// </summary>
    public int Rtt { get; set; }
    /// <summary>
    /// 접속 품질
    /// 0 = EXCELLENT, ideal experience for high level competitors
    /// 1 = GOOD,      very playable for everyone but high level competitors
    /// 2 = FAIR,       very noticeable latency, not very enjoyable anymore
    /// 3 = POOR,       unplayable
    /// 4 = ESTIMATING, still estimating
    /// </summary>
    public int Quality { get; set; }
}

/// <summary>
/// RttRequest Validator
/// </summary>
public class RttRequestValidator : Validator<RttRequest>
{
    private static readonly List<string> _allowTypes = ["client", "server"];
    
    /// <summary>
    /// 
    /// </summary>
    public RttRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotNull()
            .WithMessage("Type 은 null 이면 안됩니다")
            .NotEmpty()
            .WithMessage("Type 은 비어 있으면 안됩니다")
            .Must(item => _allowTypes.Contains(item))
            .WithMessage("허용되지 않은 type 문자열 입니다");

        RuleFor(x => x.Rtt)
            .NotNull()
            .WithMessage("rtt는 null 이면 안됩니다")
            .GreaterThan(0)
            .WithMessage("rtt는 0보다 작으면 안됩니다");

        RuleFor(x => x.Quality)
            .NotNull()
            .WithMessage("quality는 null 이면 안됩니다")
            .GreaterThan(0)
            .WithMessage("quality는 0보다 작으면 안됩니다")
            .LessThan(5)
            .WithMessage("quality는 4보다 크면 안됩니다");
    }
}
