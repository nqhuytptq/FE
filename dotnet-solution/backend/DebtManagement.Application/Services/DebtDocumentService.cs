using DebtManagement.Application.DTOs;
using DebtManagement.Application.Interfaces;
using DebtManagement.Domain.Entities;

namespace DebtManagement.Application.Services;

public class DebtDocumentService(
    IDebtDocumentRepository docs,
    IDepartmentRepository departments,
    IUserRepository users) : IDebtDocumentService
{
    // ===== Mapping =====
    private static int SupplementedCount(DebtDocument d) => d.DebtItems.Count(x => x.IsSupplemented);

    private static DebtDocumentDto Map(DebtDocument d) => new(
        d.Id, d.DocumentNumber, d.CustomerName, d.CustomerPhone,
        d.CustomerType?.ToString().ToLower(),
        d.IssueDate.ToString("yyyy-MM-dd"), d.DueDate.ToString("yyyy-MM-dd"),
        d.CurrentDepartmentId, d.CurrentDepartment?.Name, d.CurrentDepartment?.Code,
        d.Status.ToString(), d.Priority.ToString().ToLower(),
        d.Notes, d.HasDebtDocuments,
        d.CreatedByUserId, d.CreatedByUser?.Name,
        d.PqlnReceiverId, d.PqlnReceiver?.Name, d.PqlnReceivedAt,
        d.PdvReceiverId, d.PdvReceiver?.Name, d.PdvReceivedAt,
        d.EntryPostedAt, d.CompletedAt,
        d.DebtItems.Count, SupplementedCount(d),
        d.CreatedAt, d.UpdatedAt);

    private static DebtItemDto Item(DebtItem x) => new(
        x.Id, x.DebtDocumentId, x.ItemName, x.IsSupplemented,
        x.SupplementedAt, x.SupplementedByUserId, x.SupplementedByUser?.Name, x.Notes,
        x.SourceDepartmentId, x.SourceDepartment?.Name, x.SourceDepartment?.Code,
        x.CreatedByUserId, x.CreatedByUser?.Name, x.CreatedAt);

    private static CirculationHistoryDto Hist(CirculationHistory x) => new(
        x.Id, x.FromDepartmentId, x.FromDepartment?.Name,
        x.ToDepartmentId, x.ToDepartment?.Name,
        x.Action.ToString(), x.StatusAfter?.ToString(),
        x.PerformedByUserId, x.PerformedByUser?.Name,
        x.Notes, x.CreatedAt);

    private static EditLogDto Log(DocumentEditLog x) => new(
        x.Id, x.EditedByUserId, x.EditedByUser?.Name, x.ChangesJson, x.CreatedAt);

    // ===== Quyền truy cập =====
    private async Task<(AppUser User, Department? Dept)> GetActorAsync(Guid userId)
    {
        var user = await users.GetByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("Không tìm thấy người dùng");
        var dept = user.DepartmentId.HasValue
            ? await departments.GetByIdAsync(user.DepartmentId.Value)
            : null;
        return (user, dept);
    }

    /// <summary>Quyền xem hồ sơ: admin/manager xem toàn bộ; nhân viên chỉ xem hồ sơ của mình.</summary>
    private static bool CanView(AppUser user, Department? dept, DebtDocument d)
    {
        if (user.Role is "admin" or "manager") return true;
        var code = dept?.Code;
        if (code == "PKH") return d.CreatedByUserId == user.Id;
        if (code == "PQLN") return (d.CurrentDepartmentId == dept?.Id && d.Status == DocumentStatus.PqlnPendingReceive) || d.PqlnReceiverId == user.Id;
        if (code == "PDV") return (d.CurrentDepartmentId == dept?.Id && d.Status == DocumentStatus.PdvPendingReceive) || d.PdvReceiverId == user.Id;
        return false;
    }

    /// <summary>Quyền sửa thông tin hồ sơ: NV PKH sửa hồ sơ mình tạo; NV PQLN sửa hồ sơ mình tiếp nhận.</summary>
    private static bool CanEdit(AppUser user, Department? dept, DebtDocument d)
    {
        if (user.Role is "admin" or "manager") return false;
        var code = dept?.Code;
        if (code == "PKH") return d.CreatedByUserId == user.Id && d.CurrentDepartmentId == dept!.Id;
        if (code == "PQLN") return (d.CurrentDepartmentId == dept?.Id && d.Status == DocumentStatus.PqlnPendingReceive) || d.PqlnReceiverId == user.Id;
        return false;
    }

    // ===== Queries =====
    public async Task<IReadOnlyList<DebtDocumentDto>> GetAllAsync(
        string? status, Guid? deptId, string? search, Guid currentUserId)
    {
        var (user, dept) = await GetActorAsync(currentUserId);
        var all = await docs.GetAllAsync(status, deptId, search);
        var visible = all.Where(d => CanView(user, dept, d)).ToList();
        return visible.Select(Map).ToList();
    }

    public async Task<DebtDocumentDetailDto?> GetByIdAsync(Guid id, Guid currentUserId)
    {
        var (user, dept) = await GetActorAsync(currentUserId);
        var d = await docs.GetDetailByIdAsync(id);
        if (d == null) return null;
        if (!CanView(user, dept, d))
            throw new UnauthorizedAccessException("Bạn không có quyền xem hồ sơ này");

        return new(
            d.Id, d.DocumentNumber, d.CustomerName, d.CustomerPhone,
            d.CustomerType?.ToString().ToLower(),
            d.IssueDate.ToString("yyyy-MM-dd"), d.DueDate.ToString("yyyy-MM-dd"),
            d.CurrentDepartmentId, d.CurrentDepartment?.Name, d.CurrentDepartment?.Code,
            d.Status.ToString(), d.Priority.ToString().ToLower(),
            d.Notes, d.HasDebtDocuments,
            d.CreatedByUserId, d.CreatedByUser?.Name, d.CreatedAt,
            d.SupPkhApprovedAt, d.SupPkhApproverId, null,
            d.PqlnReceiverId, d.PqlnReceiver?.Name, d.PqlnReceivedAt,
            d.SupPqlnApprovedAt, d.SupPqlnApproverId, null,
            d.PdvReceiverId, d.PdvReceiver?.Name, d.PdvReceivedAt,
            d.EntryPostedAt, d.EntryPostedById, null,
            d.SupPdvApprovedEntryAt, d.SupPdvEntryApproverId, null,
            d.CompletedAt, d.CompletedByUserId, null,
            d.UpdatedAt,
            d.DebtItems.Select(Item).ToList(),
            (await docs.GetHistoryAsync(id)).Select(Hist).ToList(),
            (await docs.GetEditLogsAsync(id)).Select(Log).ToList());
    }

    // ===== Commands =====
    public async Task<DebtDocumentDto> CreateAsync(CreateDebtDocumentRequest r, Guid uid)
    {
        var pkh = await departments.GetByCodeAsync("PKH")
            ?? throw new InvalidOperationException("Không tìm thấy phòng ban PKH");
        var d = new DebtDocument
        {
            DocumentNumber = $"VB-{DateTime.UtcNow.AddHours(7):yyyy-MM-dd}-{Random.Shared.Next(100000, 1000000)}",
            CustomerName = r.CustomerName,
            CustomerPhone = r.CustomerPhone,
            CustomerType = string.IsNullOrEmpty(r.CustomerType) ? null : Enum.Parse<CustomerType>(r.CustomerType, true),
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)),
            DueDate = DateOnly.Parse(r.DueDate),
            CurrentDepartmentId = pkh.Id,
            Status = DocumentStatus.PkhPendingSup,
            Priority = Enum.Parse<DocumentPriority>(r.Priority, true),
            Notes = r.Notes,
            HasDebtDocuments = r.HasDebtDocuments,
            CreatedByUserId = uid
        };
        await docs.AddAsync(d);

        if (r.HasDebtDocuments)
            await docs.AddDebtItemsAsync((r.DebtItems ?? [])
                .Select(x => new DebtItem
                {
                    DebtDocumentId = d.Id,
                    ItemName = x.ItemName,
                    Notes = x.Notes,
                    SourceDepartmentId = pkh.Id,
                    CreatedByUserId = uid
                }));

        var itemSummary = d.DebtItems.Count == 0 ? "Không có chứng từ nợ" : $"Chứng từ nợ: {string.Join(", ", d.DebtItems.Select(x => x.ItemName))}";
        await AddHistory(d.Id, null, pkh.Id, CirculationAction.Created,
            DocumentStatus.PkhPendingSup, uid, $"Tạo hồ sơ {d.DocumentNumber} cho khách hàng '{d.CustomerName}'. Ngày phát sinh: {d.IssueDate:dd/MM/yyyy}. {itemSummary}. Hồ sơ được gửi SUP PKH phê duyệt.");

        return Map((await docs.GetByIdAsync(d.Id))!);
    }

    public async Task<DebtDocumentDto> UpdateAsync(Guid id, UpdateDebtDocumentRequest r, Guid uid)
    {
        var (user, dept) = await GetActorAsync(uid);
        var d = await docs.GetByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        if (!CanEdit(user, dept, d))
            throw new UnauthorizedAccessException("Bạn chỉ được sửa hồ sơ do chính mình khởi tạo hoặc tiếp nhận");

        d.CustomerName = r.CustomerName;
        d.CustomerPhone = r.CustomerPhone;
        d.CustomerType = string.IsNullOrEmpty(r.CustomerType) ? null : Enum.Parse<CustomerType>(r.CustomerType, true);
        d.DueDate = DateOnly.Parse(r.DueDate);
        d.Priority = Enum.Parse<DocumentPriority>(r.Priority, true);
        d.Notes = r.Notes;
        d.HasDebtDocuments = r.HasDebtDocuments;

        if (r.DebtItems != null)
        {
            await docs.RemoveDebtItemsAsync(id);
            if (r.HasDebtDocuments)
                await docs.AddDebtItemsAsync(r.DebtItems.Select(x => new DebtItem
                {
                    DebtDocumentId = id,
                    ItemName = x.ItemName,
                    Notes = x.Notes,
                    SourceDepartmentId = dept?.Id,
                    CreatedByUserId = uid
                }));
        }

        await docs.UpdateAsync(d);
        return Map((await docs.GetByIdAsync(id))!);
    }

    private async Task ChangeDept(Guid id, Guid uid, string code, DocumentStatus status, CirculationAction action, string note)
    {
        var d = await docs.GetByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        var dept = await departments.GetByCodeAsync(code) ?? throw new InvalidOperationException("Phòng ban không tồn tại");
        var from = d.CurrentDepartmentId;
        d.CurrentDepartmentId = dept.Id;
        d.Status = status;
        await docs.UpdateAsync(d);
        await AddHistory(id, from, dept.Id, action, status, uid, note);
    }

    private async Task AddHistory(Guid id, Guid? from, Guid? to, CirculationAction a, DocumentStatus st, Guid uid, string note)
        => await docs.AddCirculationEntryAsync(new CirculationHistory
        {
            DebtDocumentId = id,
            FromDepartmentId = from,
            ToDepartmentId = to,
            Action = a,
            StatusAfter = st,
            PerformedByUserId = uid,
            Notes = note
        });

    public Task PkhSupApproveAsync(Guid id, string? n, Guid u)
        => ChangeDept(id, u, "PQLN", DocumentStatus.PqlnPendingReceive, CirculationAction.PkhSupApproved, n ?? "SUP PKH đã duyệt và chuyển hồ sơ sang PQLN");

    public async Task PqlnTakeOwnershipAsync(Guid id, Guid u)
    {
        var d = await docs.GetByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        d.PqlnReceiverId = u;
        d.PqlnReceivedAt = DateTime.UtcNow;
        d.Status = DocumentStatus.PqlnReceived;
        await docs.UpdateAsync(d);
        await AddHistory(id, d.CurrentDepartmentId, d.CurrentDepartmentId, CirculationAction.PqlnReceived, d.Status, u, "Cán bộ PQLN đã tiếp nhận hồ sơ");
    }

    public Task PqlnSubmitSupAsync(Guid id, string? n, Guid u)
        => ChangeDept(id, u, "PQLN", DocumentStatus.PqlnPendingSup, CirculationAction.PqlnSubmittedSup, n ?? "Cán bộ PQLN đã kiểm tra và đẩy hồ sơ lên SUP PQLN duyệt");

    public Task PqlnSupApproveAsync(Guid id, string? n, Guid u)
        => ChangeDept(id, u, "PDV", DocumentStatus.PdvPendingReceive, CirculationAction.PqlnSupApproved, n ?? "SUP PQLN đã duyệt và chuyển hồ sơ sang PDV");

    public async Task PdvTakeOwnershipAsync(Guid id, Guid u)
    {
        var d = await docs.GetByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        d.PdvReceiverId = u;
        d.PdvReceivedAt = DateTime.UtcNow;
        d.Status = DocumentStatus.PdvReceived;
        await docs.UpdateAsync(d);
        await AddHistory(id, d.CurrentDepartmentId, d.CurrentDepartmentId, CirculationAction.PdvReceived, d.Status, u, "Cán bộ Giao dịch PDV đã tiếp nhận hồ sơ");
    }

    public async Task PdvPostEntryAsync(Guid id, string? n, Guid u)
    {
        var d = await docs.GetByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        d.EntryPostedAt = DateTime.UtcNow;
        d.EntryPostedById = u;
        d.Status = DocumentStatus.PdvEntryPosted;
        await docs.UpdateAsync(d);
        await AddHistory(id, d.CurrentDepartmentId, d.CurrentDepartmentId, CirculationAction.PdvEntryPosted, d.Status, u, n ?? "Cán bộ Giao dịch PDV đã thực hiện đi bút toán");
    }

    public async Task PdvSupApproveAsync(Guid id, string? n, Guid u)
    {
        var d = await docs.GetDetailByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        var all = !d.HasDebtDocuments || d.DebtItems.All(x => x.IsSupplemented);
        if (d.HasDebtDocuments && !all)
        {
            d.Status = DocumentStatus.PdvEntryApproved;
            await docs.UpdateAsync(d);
            await AddHistory(id, d.CurrentDepartmentId, d.CurrentDepartmentId, CirculationAction.PdvSupApprovedEntry, d.Status, u, n ?? "SUP PDV phê duyệt Lần 1: Đã hoàn tất đi bút toán, chờ bổ sung chứng từ nợ");
        }
        else
        {
            d.Status = DocumentStatus.Completed;
            d.CompletedAt = DateTime.UtcNow;
            d.CompletedByUserId = u;
            await docs.UpdateAsync(d);
            await AddHistory(id, d.CurrentDepartmentId, d.CurrentDepartmentId, CirculationAction.Completed, d.Status, u, n ?? "SUP PDV phê duyệt: Hồ sơ ĐÃ HOÀN TẤT");
        }
        d.SupPdvApprovedEntryAt = DateTime.UtcNow;
        await docs.UpdateAsync(d);
    }

    public async Task AddDebtItemsAsync(Guid id, AddDebtItemsRequest r, Guid u)
    {
        var d = await docs.GetDetailByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        var pqln = await departments.GetByCodeAsync("PQLN");
        if (pqln == null || d.CurrentDepartmentId != pqln.Id || d.PqlnReceiverId != u || d.Status != DocumentStatus.PqlnReceived)
            throw new InvalidOperationException("Chỉ cán bộ PQLN đã tiếp nhận hồ sơ mới được thêm chứng từ nợ");
        var items = (r.Items ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemName))
            .Select(x => new DebtItem
            {
                DebtDocumentId = id,
                ItemName = x.ItemName.Trim(),
                Notes = x.Notes,
                SourceDepartmentId = pqln.Id,
                CreatedByUserId = u
            }).ToList();
        if (items.Count == 0) throw new InvalidOperationException("Vui lòng nhập ít nhất một chứng từ");
        await docs.AddDebtItemsAsync(items);
        d.HasDebtDocuments = true;
        await docs.UpdateAsync(d);
        await AddHistory(id, d.CurrentDepartmentId, d.CurrentDepartmentId, CirculationAction.Supplemented, d.Status, u, $"Thêm chứng từ nợ tại PQLN: {string.Join(", ", items.Select(x => x.ItemName))}. Ghi chú yêu cầu: {r.Notes ?? "Không có"}.");
    }

    private async Task<DebtItem> GetEditableItemAsync(Guid documentId, Guid itemId, Guid userId)
    {
        var (user, dept) = await GetActorAsync(userId);
        var document = await docs.GetDetailByIdAsync(documentId) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        var item = document.DebtItems.FirstOrDefault(x => x.Id == itemId) ?? throw new InvalidOperationException("Không tìm thấy chứng từ nợ");
        var allowed = dept?.Code switch
        {
            "PKH" => document.Status == DocumentStatus.PkhPendingSup && item.SourceDepartmentId == dept.Id && item.CreatedByUserId == userId,
            "PQLN" => document.Status == DocumentStatus.PqlnPendingSup && item.SourceDepartmentId == dept.Id && item.CreatedByUserId == userId,
            _ => false
        };
        if (!allowed) throw new UnauthorizedAccessException("Chỉ được sửa/xóa chứng từ trước khi gửi SUP phòng mình phê duyệt");
        return item;
    }

    public async Task UpdateDebtItemAsync(Guid documentId, Guid itemId, UpdateDebtItemRequest request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.ItemName)) throw new InvalidOperationException("Tên chứng từ không được để trống");
        var item = await GetEditableItemAsync(documentId, itemId, userId);
        var oldName = item.ItemName; item.ItemName = request.ItemName.Trim(); item.Notes = request.Notes;
        await docs.UpdateDebtItemAsync(item);
        await AddHistory(documentId, null, null, CirculationAction.Edited, (await docs.GetByIdAsync(documentId))!.Status, userId, $"Sửa chứng từ nợ: ''{oldName}'' → ''{item.ItemName}''. Ghi chú: {item.Notes ?? "Không có"}.");
    }

    public async Task DeleteDebtItemAsync(Guid documentId, Guid itemId, Guid userId)
    {
        var item = await GetEditableItemAsync(documentId, itemId, userId);
        var document = await docs.GetByIdAsync(documentId) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        await docs.DeleteDebtItemAsync(itemId);
        document.HasDebtDocuments = (await docs.GetDebtItemsAsync(documentId)).Count > 0;
        await docs.UpdateAsync(document);
        await AddHistory(documentId, document.CurrentDepartmentId, document.CurrentDepartmentId, CirculationAction.Edited, document.Status, userId, $"Xóa chứng từ nợ: ''{item.ItemName}''. Cập nhật trạng thái hồ sơ còn chứng từ: {document.HasDebtDocuments}.");
    }
    public async Task SupplementItemsAsync(Guid id, SupplementItemsRequest r, Guid u)
    {
        var (user, dept) = await GetActorAsync(u);
        var d = await docs.GetByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        var allowed = dept?.Code switch
        {
            "PKH" => d.CreatedByUserId == u,
            "PQLN" => d.PqlnReceiverId == u,
            _ => false
        };
        if (!allowed)
            throw new UnauthorizedAccessException("Bạn chỉ được bổ sung chứng từ của hồ sơ do mình phụ trách");
        foreach (var x in await docs.GetDebtItemsAsync(id))
        {
            if (!r.SupplementedItemIds.Contains(x.Id)) continue;
            x.IsSupplemented = true;
            x.SupplementedAt = DateTime.UtcNow;
            x.SupplementedByUserId = u;
            await docs.UpdateDebtItemAsync(x);
        }
        await AddHistory(id, d.CurrentDepartmentId, d.CurrentDepartmentId, CirculationAction.Supplemented, d.Status, u, $"Bổ sung các chứng từ nợ: {string.Join(", ", (await docs.GetDebtItemsAsync(id)).Where(x => r.SupplementedItemIds.Contains(x.Id)).Select(x => x.ItemName))}. Ghi chú: {r.Notes ?? "Không có"}.");
    }

    public async Task ReturnAsync(Guid id, Guid to, string? n, Guid u)
    {
        var d = await docs.GetByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        var from = d.CurrentDepartmentId;
        d.CurrentDepartmentId = to;
        var target = await departments.GetByIdAsync(to);
        d.Status = target?.Code switch
        {
            "PKH" => DocumentStatus.PkhPendingSup,
            "PQLN" => DocumentStatus.PqlnReceived,
            _ => DocumentStatus.PdvReceived
        };
        await docs.UpdateAsync(d);
        await AddHistory(id, from, to, CirculationAction.Returned, d.Status, u, n ?? "Trả lại hồ sơ để chỉnh sửa/bổ sung");
    }

    public Task DeleteAsync(Guid id) => docs.DeleteAsync(id);
}



