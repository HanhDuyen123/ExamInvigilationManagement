using ExamInvigilationManagement.Application.DTOs.Notification;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Common;
using ExamInvigilationManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _db;

        public NotificationRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task CreateAsync(
            NotificationWriteDto dto,
            CancellationToken cancellationToken = default)
        {
            _db.Notifications.Add(new Data.Entities.Notification
            {
                UserId = dto.UserId,
                Title = dto.Title,
                Content = dto.Content,
                Type = dto.Type,
                IsRead = dto.IsRead ?? false,
                CreatedAt = dto.CreatedAt ?? DateTime.Now,
                RelatedId = dto.RelatedId,
                CreatedBy = dto.CreatedBy
            });

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task UpsertAsync(
            NotificationWriteDto dto,
            CancellationToken cancellationToken = default)
        {
            var existing = await _db.Notifications
                .FirstOrDefaultAsync(x =>
                    x.UserId == dto.UserId &&
                    x.Type == dto.Type &&
                    x.RelatedId == dto.RelatedId,
                    cancellationToken);

            if (existing == null)
            {
                _db.Notifications.Add(new Data.Entities.Notification
                {
                    UserId = dto.UserId,
                    Title = dto.Title,
                    Content = dto.Content,
                    Type = dto.Type,
                    IsRead = dto.IsRead ?? false,
                    CreatedAt = dto.CreatedAt ?? DateTime.Now,
                    RelatedId = dto.RelatedId,
                    CreatedBy = dto.CreatedBy
                });
            }
            else
            {
                existing.Title = dto.Title;
                existing.Content = dto.Content;
                existing.Type = dto.Type;
                existing.IsRead = false;
                existing.CreatedAt = dto.CreatedAt ?? DateTime.Now;
                existing.RelatedId = dto.RelatedId;
                existing.CreatedBy = dto.CreatedBy;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<PagedResult<NotificationListItemDto>> GetPagedAsync(
            int userId,
            bool canViewAll,
            NotificationSearchDto search,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _db.Notifications
                .AsNoTracking()
                .AsQueryable();

            if (!canViewAll)
                query = query.Where(x => x.UserId == userId);

            if (canViewAll && search.RecipientId.HasValue)
                query = query.Where(x => x.UserId == search.RecipientId.Value);

            if (search.SenderId.HasValue)
                query = query.Where(x => x.CreatedBy == search.SenderId.Value);

            if (!string.IsNullOrWhiteSpace(search.Keyword))
            {
                var key = search.Keyword.Trim();

                query = query.Where(x =>
                    (x.Title ?? "").Contains(key) ||
                    (x.Content ?? "").Contains(key) ||
                    (x.Type ?? "").Contains(key));
            }

            if (!string.IsNullOrWhiteSpace(search.Type) &&
                !string.Equals(search.Type, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.Type == search.Type);
            }

            if (search.IsRead.HasValue)
                query = query.Where(x => x.IsRead == search.IsRead.Value);

            if (search.FromDate.HasValue)
                query = query.Where(x => x.CreatedAt >= search.FromDate.Value);

            if (search.ToDate.HasValue)
            {
                var end = search.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(x => x.CreatedAt <= end);
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.NotificationId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new NotificationListItemDto
                {
                    Id = x.NotificationId,
                    UserId = x.UserId,
                    Title = x.Title,
                    Content = x.Content,
                    Type = x.Type,

                    IsRead = x.IsRead ?? false,
                    CreatedAt = x.CreatedAt ?? DateTime.MinValue,

                    RelatedId = x.RelatedId,
                    CreatedBy = x.CreatedBy,
                    CreatedByName = x.CreatedByNavigation == null
                        ? null
                        : ((x.CreatedByNavigation.Information.LastName ?? "") + " " + (x.CreatedByNavigation.Information.FirstName ?? "")).Trim(),
                    RecipientName = ((x.User.Information.LastName ?? "") + " " + (x.User.Information.FirstName ?? "")).Trim()
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<NotificationListItemDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<NotificationListItemDto>> GetRecentAsync(
            int userId,
            int take = 5,
            CancellationToken cancellationToken = default)
        {
            return await _db.Notifications
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.NotificationId)
                .Take(take)
                .Select(x => new NotificationListItemDto
                {
                    Id = x.NotificationId,
                    UserId = x.UserId,
                    Title = x.Title,
                    Content = x.Content,
                    Type = x.Type,

                    IsRead = x.IsRead ?? false,
                    CreatedAt = x.CreatedAt ?? DateTime.MinValue,

                    RelatedId = x.RelatedId,
                    CreatedBy = x.CreatedBy,
                    CreatedByName = x.CreatedByNavigation == null
                        ? null
                        : ((x.CreatedByNavigation.Information.LastName ?? "") + " " + (x.CreatedByNavigation.Information.FirstName ?? "")).Trim(),
                    RecipientName = ((x.User.Information.LastName ?? "") + " " + (x.User.Information.FirstName ?? "")).Trim()
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetUnreadCountAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await _db.Notifications
                .AsNoTracking()
                .CountAsync(x =>
                    x.UserId == userId &&
                    !(x.IsRead ?? false),
                    cancellationToken);
        }

        public async Task<NotificationDetailDto?> GetByIdAsync(
            int id,
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await _db.Notifications
                .AsNoTracking()
                .Where(x => x.NotificationId == id && x.UserId == userId)
                .Select(x => new NotificationDetailDto
                {
                    Id = x.NotificationId,
                    UserId = x.UserId,
                    Title = x.Title,
                    Content = x.Content,
                    Type = x.Type,

                    IsRead = x.IsRead ?? false,
                    CreatedAt = x.CreatedAt ?? DateTime.MinValue,

                    RelatedId = x.RelatedId,
                    CreatedBy = x.CreatedBy,
                    CreatedByName = x.CreatedByNavigation == null
                        ? null
                        : ((x.CreatedByNavigation.Information.LastName ?? "") + " " + (x.CreatedByNavigation.Information.FirstName ?? "")).Trim(),
                    RecipientName = ((x.User.Information.LastName ?? "") + " " + (x.User.Information.FirstName ?? "")).Trim()
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> MarkAsReadAsync(
            int id,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var entity = await _db.Notifications
                .FirstOrDefaultAsync(x =>
                    x.NotificationId == id &&
                    x.UserId == userId,
                    cancellationToken);

            if (entity == null)
                return false;

            if (!(entity.IsRead ?? false))
            {
                entity.IsRead = true;
                await _db.SaveChangesAsync(cancellationToken);
            }

            return true;
        }

        public async Task<int> MarkAllAsReadAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            var items = await _db.Notifications
                .Where(x =>
                    x.UserId == userId &&
                    !(x.IsRead ?? false))
                .ToListAsync(cancellationToken);

            if (items.Count == 0)
                return 0;

            foreach (var item in items)
                item.IsRead = true;

            await _db.SaveChangesAsync(cancellationToken);

            return items.Count;
        }
    }
}
