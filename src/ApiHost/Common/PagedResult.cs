namespace ApiHost;

public class PagedResult<T>(int page, int size, IEnumerable<T> items, int totalCount)
{
    public int Page { get; set; } = page;
    public int Size { get; set; } = size;
    public IEnumerable<T> Items { get; set; } = items;
    public int TotalCount { get; set; } = totalCount;
    public int PageCount => (int)Math.Ceiling((double)TotalCount / Size);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < PageCount;
}
