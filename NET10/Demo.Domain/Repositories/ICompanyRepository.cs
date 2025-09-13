using Demo.Domain.Entities;
using FluentResults;

namespace Demo.Domain.Repositories;

public interface ICompanyRepository
{
    /// <summary>
    /// 회사 목록을 페이징과 선택적 검색 기능으로 비동기적으로 조회합니다.
    /// </summary>
    /// <param name="searchTerm">회사명으로 필터링할 선택적 검색어.</param>
    /// <param name="page">페이지 번호 (0부터 시작).</param>
    /// <param name="pageSize">페이지당 항목 수.</param>
    /// <param name="ct">취소 토큰.</param>
    /// <returns>회사 목록과 전체 개수를 포함하는 결과를 반환하는 작업.</returns>
    Task<Result<(IEnumerable<CompanyEntity> Companies, int TotalCount)>> GetPagedAsync(string? searchTerm, int page, int pageSize, CancellationToken ct = default);
}