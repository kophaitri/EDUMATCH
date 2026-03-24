using System.Text.Json;
using EduMatch.DTOs.Admin;

namespace EduMatch.Services;

public class AdminAuditService : IAdminAuditService
{
    private readonly EduMatchDbContext _db;

    public AdminAuditService(EduMatchDbContext db)
    {
        _db = db;
    }

    public async Task<(List<AuditLogListDto> Items, int TotalCount)> GetAuditLogsAsync(
        string? action = null,
        string? entityType = null,
        string? userId = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _db.AuditLogs
            .Include(a => a.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action.Contains(action));

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(a => a.UserId == userId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogListDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User != null ? a.User.FullName : null,
                UserEmail = a.User != null ? a.User.Email : null,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                IpAddress = a.IpAddress,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<AuditLogDetailDto?> GetAuditLogDetailAsync(int id)
    {
        var log = await _db.AuditLogs
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (log == null) return null;

        var dto = new AuditLogDetailDto
        {
            Id = log.Id,
            UserId = log.UserId,
            UserName = log.User?.FullName,
            UserEmail = log.User?.Email,
            Action = log.Action,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            OldValues = log.OldValues,
            NewValues = log.NewValues,
            IpAddress = log.IpAddress,
            CreatedAt = log.CreatedAt,
            Changes = BuildChanges(log.OldValues, log.NewValues)
        };

        return dto;
    }

    private static List<AuditFieldChange> BuildChanges(string? oldJson, string? newJson)
    {
        var changes = new List<AuditFieldChange>();

        if (string.IsNullOrWhiteSpace(oldJson) && string.IsNullOrWhiteSpace(newJson))
            return changes;

        try
        {
            var oldDict = string.IsNullOrWhiteSpace(oldJson)
                ? new Dictionary<string, JsonElement>()
                : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(oldJson) ?? new();

            var newDict = string.IsNullOrWhiteSpace(newJson)
                ? new Dictionary<string, JsonElement>()
                : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(newJson) ?? new();

            var allKeys = oldDict.Keys.Union(newDict.Keys).Distinct();

            foreach (var key in allKeys)
            {
                var oldVal = oldDict.TryGetValue(key, out var ov) ? ov.ToString() : null;
                var newVal = newDict.TryGetValue(key, out var nv) ? nv.ToString() : null;

                if (oldVal != newVal)
                    changes.Add(new AuditFieldChange { Field = key, OldValue = oldVal, NewValue = newVal });
            }
        }
        catch
        {
            // JSON parse thất bại — trả về rỗng, view sẽ hiển thị raw JSON
        }

        return changes;
    }
}
