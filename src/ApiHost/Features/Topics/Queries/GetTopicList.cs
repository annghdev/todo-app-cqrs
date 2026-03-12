using Marten;
using Marten.Pagination;
using Wolverine.Http;

namespace ApiHost.Topics;

public class GetTopicList
{
    [WolverineGet("topics")]
    public static async Task<PagedResult<TopicView>> Get(
        IQuerySession session,
        int page = 1,
        int size = 10,
        string? name = null,
        string orderBy = "date",
        bool isDescending = true)
    {
        IQueryable<TopicView> query = session.Query<TopicView>();

        if (!string.IsNullOrEmpty(name))
        {
            //query = query.Where(x => x.Name.Contains(filter.Name));
            query = query.Where(x => x.Search(name));
        }
        if (!string.IsNullOrEmpty(orderBy) && orderBy.Equals("title", StringComparison.OrdinalIgnoreCase))
        {
            query = isDescending ? query.OrderByDescending(x => x.Title) : query.OrderBy(x => x.Title);
        }
        else
        {
            query = isDescending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt);
        }

        var data = await query.ToPagedListAsync(page, size);

        return new PagedResult<TopicView>(page, size, data.ToArray(), (int)data.TotalItemCount);
    }
}
