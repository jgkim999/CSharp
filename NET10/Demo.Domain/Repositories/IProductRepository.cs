using Demo.Domain.Entities;
using FluentResults;

namespace Demo.Domain.Repositories;

public interface IProductRepository
{
    /// <summary>
    /// 지정된 정보로 새 상품을 비동기적으로 생성합니다.
    /// </summary>
    /// <param name="companyId">회사 ID.</param>
    /// <param name="name">상품 이름.</param>
    /// <param name="price">상품 가격.</param>
    /// <param name="ct">취소 토큰.</param>
    /// <returns>성공을 나타내는 결과 또는 작업이 실패한 경우 오류 메시지가 포함된 실패를 나타내는 작업.</returns>
    Task<Result> CreateAsync(long companyId, string name, int price, CancellationToken ct = default);

    /// <summary>
    /// 상품 목록을 페이징과 선택적 검색 기능으로 비동기적으로 조회합니다.
    /// </summary>
    /// <param name="searchTerm">상품명으로 필터링할 선택적 검색어.</param>
    /// <param name="companyId">회사 ID로 필터링할 선택적 값.</param>
    /// <param name="page">페이지 번호 (0부터 시작).</param>
    /// <param name="pageSize">페이지당 항목 수.</param>
    /// <param name="ct">취소 토큰.</param>
    /// <returns>상품 목록과 전체 개수를 포함하는 결과를 반환하는 작업.</returns>
    Task<Result<(IEnumerable<ProductEntity> Products, int TotalCount)>> GetPagedAsync(string? searchTerm, long? companyId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// 회사 정보와 함께 상품 목록을 페이징과 선택적 검색 기능으로 비동기적으로 조회합니다.
    /// </summary>
    /// <param name="searchTerm">상품명으로 필터링할 선택적 검색어.</param>
    /// <param name="companyId">회사 ID로 필터링할 선택적 값.</param>
    /// <param name="page">페이지 번호 (0부터 시작).</param>
    /// <param name="pageSize">페이지당 항목 수.</param>
    /// <param name="ct">취소 토큰.</param>
    /// <returns>상품과 회사 정보가 결합된 목록과 전체 개수를 포함하는 결과를 반환하는 작업.</returns>
    Task<Result<(IEnumerable<(ProductEntity Product, CompanyEntity Company)> Products, int TotalCount)>> GetPagedWithCompanyAsync(string? searchTerm, long? companyId, int page, int pageSize, CancellationToken ct = default);
}