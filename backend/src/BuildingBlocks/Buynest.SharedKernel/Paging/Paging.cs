namespace Buynest.SharedKernel.Paging {
    /// <summary>
    /// Requisição paginada. Padrão do front: PageIndex = 1 e PageSize = 10.
    /// </summary>
    public record PagedRequest(int PageIndex = 1, int PageSize = 10, string? Search = null) {
        public const int MaxPageSize = 100;

        public int SafePageIndex => PageIndex < 1 ? 1 : PageIndex;
        public int SafePageSize => PageSize is < 1 or > MaxPageSize ? 10 : PageSize;
        public int Skip => (SafePageIndex - 1) * SafePageSize;
    }

    public record PagedResult<T>(IReadOnlyList<T> Items, int PageIndex, int PageSize, int TotalCount) {
        public int TotalPages => PageSize == 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasNextPage => PageIndex < TotalPages;

        public PagedResult<TOut> Map<TOut>(Func<T, TOut> map) =>
            new (Items.Select(map).ToList(), PageIndex, PageSize, TotalCount);
    }
}