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
private static bool IsItemCompleted(DebtItem x, Guid? pkhDeptId = null, Guid? pqlnDeptId = null)
    {
        if (!x.IsSupplemented) return false;

        var isPkhItem = x.SourceDepartment?.Code == "PKH"
            || x.SourceDepartmentId == null
            || (pkhDeptId.HasValue && x.SourceDepartmentId == pkhDeptId.Value);
        if (isPkhItem) return x.IsVerifiedByPqln;

        var isPqlnItem = x.SourceDepartment?.Code == "PQLN"
            || (pqlnDeptId.HasValue && x.SourceDepartmentId == pqlnDeptId.Value);
        if (isPqlnItem) return x.IsVerifiedByPdv;

        return true;
    }

    private static int SupplementedCount(DebtDocument d, Guid? pkhDeptId = null, Guid? pqlnDeptId = null)
        => d.DebtItems.Count(x => IsItemCompleted(x, pkhDeptId, pqlnDeptId));

    private static int SupplementedCount(DebtDocument d, Guid? pkhDeptId = null)
        => d.DebtItems.Count(x => IsItemCompleted(x, pkhDeptId));

    private static DebtDocumentDto Map(DebtDocument d, Guid? pkhDeptId = null) => new(
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
        d.DebtItems.Count, SupplementedCount(d, pkhDeptId),
        d.CreatedAt, d.UpdatedAt);

private static DebtItemDto Item(DebtItem x, string? pqlnVerifierName = null, string? pdvVerifierName = null) => new(
        x.Id, x.DebtDocumentId, x.ItemName, x.IsSupplemented,
        x.SupplementedAt, x.SupplementedByUserId, x.SupplementedByUser?.Name, x.Notes,
        x.SourceDepartmentId, x.SourceDepartment?.Name, x.SourceDepartment?.Code,
        x.CreatedByUserId, x.CreatedByUser?.Name, x.CreatedAt,
        x.IsVerifiedByPqln, x.PqlnVerifiedAt, x.PqlnVerifiedByUserId, pqlnVerifierName,
        x.IsVerifiedByPdv, x.PdvVerifiedAt, x.PdvVerifiedByUserId, pdvVerifierName);

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
        var pkh = await departments.GetByCodeAsync("PKH");
        var all = await docs.GetAllAsync(status, deptId, search);
        var visible = all.Where(d => CanView(user, dept, d)).ToList();
        return visible.Select(d => Map(d, pkh?.Id)).ToList();
    }

    public async Task<DebtDocumentDetailDto?> GetByIdAsync(Guid id, Guid currentUserId)
    {
        var (user, dept) = await GetActorAsync(currentUserId);
        var d = await docs.GetDetailByIdAsync(id);
        if (d == null) return null;
        if (!CanView(user, dept, d))
            throw new UnauthorizedAccessException("Bạn không có quyền xem hồ sơ này");

        var userNameCache = new Dictionary<Guid, string?>();
        foreach (var verifierId in d.DebtItems
            .Where(x => x.PqlnVerifiedByUserId.HasValue)
            .Select(x => x.PqlnVerifiedByUserId!.Value)
            .Distinct())
        {
            var verifier = await users.GetByIdAsync(verifierId);
            userNameCache[verifierId] = verifier?.Name;
        }

        var mappedItems = d.DebtItems.Select(x =>
        {
            string? verifierName = x.PqlnVerifiedByUserId.HasValue && userNameCache.TryGetValue(x.PqlnVerifiedByUserId.Value, out var name)
                ? name
                : null;
            return Item(x, verifierName);
        }).ToList();

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
            mappedItems,
            (await docs.GetHistoryAsync(id)).Select(Hist).ToList(),
            (await docs.GetEditLogsAsync(id)).Select(Log).ToList());
    }

    // ===== Commands =====
    public async Task<DebtDocumentDto> CreateAsync(CreateDebtDocumentRequest r, Guid uid)
    {
        var pkh = await departments.GetByCodeAsync("PKH")
            ?? throw new InvalidOperationException("Không tìm thấy phòng ban PKH");
        var nowVn = DateTime.UtcNow.AddHours(7);

        var d = new DebtDocument
        {
            DocumentNumber = $"VB-{nowVn:yyyy-MM-dd}-{Random.Shared.Next(100000, 1000000)}",
            CustomerName = r.CustomerName,
            CustomerPhone = r.CustomerPhone,
            CustomerType = string.IsNullOrEmpty(r.CustomerType) ? null : Enum.Parse<CustomerType>(r.CustomerType, true),
            IssueDate = DateOnly.FromDateTime(nowVn),
            DueDate = DateOnly.Parse(r.DueDate),
            CurrentDepartmentId = pkh.Id,
            Status = DocumentStatus.PkhPendingSup,
            Priority = Enum.Parse<DocumentPriority>(r.Priority, true),
            Notes = r.Notes,
            CreatedByUserId = uid
        };

        var debtItems = r.HasDebtDocuments
            ? (r.DebtItems ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x.ItemName))
                .Select(x => new DebtItem
                {
                    DebtDocumentId = d.Id,
                    ItemName = x.ItemName.Trim(),
                    Notes = x.Notes,
                    SourceDepartmentId = pkh.Id,
                    CreatedByUserId = uid
                }).ToList()
            : [];

        d.HasDebtDocuments = debtItems.Count > 0;
        await docs.AddAsync(d);

        if (debtItems.Count > 0)
            await docs.AddDebtItemsAsync(debtItems);

        var createNote = debtItems.Count > 0
            ? $"Khởi tạo hồ sơ tại phòng Khách hàng (Chứng từ nợ: {string.Join(", ", debtItems.Select(x => x.ItemName))}) và gửi SUP PKH"
            : "Khởi tạo hồ sơ tại phòng Khách hàng và gửi SUP PKH";

        await AddHistory(d.Id, null, pkh.Id, CirculationAction.Created,
            DocumentStatus.PkhPendingSup, uid, createNote);

        return Map((await docs.GetByIdAsync(d.Id))!, pkh.Id);
    }

    public async Task<DebtDocumentDto> UpdateAsync(Guid id, UpdateDebtDocumentRequest r, Guid uid)
    {
        var (user, dept) = await GetActorAsync(uid);
        var pkh = await departments.GetByCodeAsync("PKH");
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
        return Map((await docs.GetByIdAsync(id))!, pkh?.Id);
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

    public Task PqlnSubmitSupAsync(Guid id, string? n, Guid u)
        => ChangeDept(id, u, "PQLN", DocumentStatus.PqlnPendingSup, CirculationAction.PqlnSubmittedSup, n ?? "Cán bộ PQLN đã kiểm tra và đẩy hồ sơ lên SUP PQLN duyệt");

    public Task PqlnSupApproveAsync(Guid id, string? n, Guid u)
        => ChangeDept(id, u, "PDV", DocumentStatus.PdvPendingReceive, CirculationAction.PqlnSupApproved, n ?? "SUP PQLN đã duyệt và chuyển hồ sơ sang PDV");

    public async Task PdvTakeOwnershipAsync(Guid id, Guid u)
    {
        var (user, dept) = await GetActorAsync(u);
        if (dept?.Code != "PDV" || user.Role != "staff")
            throw new UnauthorizedAccessException("Chỉ nhân viên phòng PDV mới có quyền tiếp nhận hồ sơ này.");

        var d = await docs.GetDetailByIdAsync(id)
            ?? throw new InvalidOperationException("Hồ sơ không tồn tại.");

        if (d.CurrentDepartmentId != dept.Id)
            throw new InvalidOperationException("Hồ sơ hiện không nằm tại phòng PDV.");

        if (d.Status != DocumentStatus.PdvPendingReceive || d.PdvReceiverId != null)
        {
            var receiverName = d.PdvReceiver?.Name
                ?? (d.PdvReceiverId.HasValue ? (await users.GetByIdAsync(d.PdvReceiverId.Value))?.Name : null)
                ?? "cán bộ khác";
            throw new InvalidOperationException($"Không thể tiếp nhận: Hồ sơ này đã được {receiverName} tiếp nhận trước đó.");
        }

        var claimed = await docs.TryTakePdvOwnershipAsync(id, u, DateTime.UtcNow);
        if (!claimed)
        {
            var latest = await docs.GetDetailByIdAsync(id);
            var receiverName = latest?.PdvReceiver?.Name
                ?? (latest?.PdvReceiverId.HasValue == true ? (await users.GetByIdAsync(latest.PdvReceiverId.Value))?.Name : null)
                ?? "cán bộ khác";
            throw new InvalidOperationException($"Không thể tiếp nhận: Hồ sơ này vừa được {receiverName} tiếp nhận trước đó.");
        }

        await AddHistory(id, dept.Id, dept.Id, CirculationAction.PdvReceived, DocumentStatus.PdvReceived, u, "Cán bộ Giao dịch PDV đã tiếp nhận hồ sơ");
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
        var (user, dept) = await GetActorAsync(u);
        if (dept?.Code != "PDV" || user.Role != "manager")
            throw new UnauthorizedAccessException("Chỉ SUP phòng PDV mới có quyền phê duyệt bước này.");

        var d = await docs.GetDetailByIdAsync(id)
            ?? throw new InvalidOperationException("Hồ sơ không tồn tại.");

        if (d.CurrentDepartmentId != dept.Id)
            throw new InvalidOperationException("Hồ sơ hiện không nằm tại phòng PDV.");

        if (d.Status is not (DocumentStatus.PdvEntryPosted or DocumentStatus.PdvEntryApproved))
            throw new InvalidOperationException("Hồ sơ không ở trạng thái chờ SUP PDV phê duyệt.");

        var pkh = await departments.GetByCodeAsync("PKH");
        var unsupItems = d.DebtItems.Where(x => !IsItemCompleted(x, pkh?.Id)).ToList();
        var allSupplemented = unsupItems.Count == 0;
        var isDefaultUiNote = string.IsNullOrWhiteSpace(n)
            || n.Trim().Equals("SUP PDV kiểm tra và duyệt hoàn tất", StringComparison.OrdinalIgnoreCase);

        if (d.Status == DocumentStatus.PdvEntryPosted)
        {
            // === DUYỆT LẦN 1: Duyệt bút toán ===
            d.SupPdvApprovedEntryAt = DateTime.UtcNow;
            d.SupPdvEntryApproverId = u;

            if (!allSupplemented)
            {
                d.Status = DocumentStatus.PdvEntryApproved;
                await docs.UpdateAsync(d);

                var note1 = isDefaultUiNote
                    ? $"SUP PDV phê duyệt Lần 1: Đã hoàn tất đi bút toán, chờ bổ sung/xác nhận {unsupItems.Count} chứng từ nợ"
                    : n!.Trim();

                await AddHistory(id, d.CurrentDepartmentId, d.CurrentDepartmentId,
                    CirculationAction.PdvSupApprovedEntry, d.Status, u, note1);
            }
            else
            {
                d.Status = DocumentStatus.Completed;
                d.CompletedAt = DateTime.UtcNow;
                d.CompletedByUserId = u;
                await docs.UpdateAsync(d);

                var noteDone = isDefaultUiNote
                    ? "SUP PDV phê duyệt: Đã đi bút toán và đủ chứng từ nợ — Hồ sơ ĐÃ HOÀN TẤT"
                    : n!.Trim();

                await AddHistory(id, d.CurrentDepartmentId, d.CurrentDepartmentId,
                    CirculationAction.Completed, d.Status, u, noteDone);
            }
        }
        else // d.Status == DocumentStatus.PdvEntryApproved
        {
            // === DUYỆT LẦN 2: Duyệt hoàn tất cuối cùng ===
            if (!allSupplemented)
                throw new InvalidOperationException(
                    $"Không thể duyệt hoàn tất (Lần 2): Hồ sơ vẫn còn {unsupItems.Count} chứng từ nợ chưa bổ sung hoặc chưa được PQLN xác nhận.");

            d.Status = DocumentStatus.Completed;
            d.CompletedAt = DateTime.UtcNow;
            d.CompletedByUserId = u;
            await docs.UpdateAsync(d);

            var note2 = isDefaultUiNote
                ? "SUP PDV phê duyệt Lần 2: Đã bổ sung và xác nhận đủ chứng từ nợ — Hồ sơ ĐÃ HOÀN TẤT"
                : n!.Trim();

            await AddHistory(id, d.CurrentDepartmentId, d.CurrentDepartmentId,
                CirculationAction.Completed, d.Status, u, note2);
        }
    }

    public async Task AddDebtItemsAsync(Guid id, AddDebtItemsRequest r, Guid u)
    {
        var (user, dept) = await GetActorAsync(u);
        var d = await docs.GetDetailByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");

        var isStaff = user.Role == "staff";
        var canAdd = dept?.Code switch
        {
            "PKH" => isStaff
                     && d.Status == DocumentStatus.PkhPendingSup
                     && d.CreatedByUserId == u,

            "PQLN" => isStaff
                      && d.Status is DocumentStatus.PqlnReceived or DocumentStatus.PqlnPendingSup
                      && d.PqlnReceiverId == u,

            _ => false
        };

        if (!canAdd || dept == null)
            throw new UnauthorizedAccessException("Bạn không có quyền thêm chứng từ nợ ở trạng thái hiện tại của hồ sơ");

        var items = (r.Items ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemName))
            .Select(x => new DebtItem
            {
                DebtDocumentId = id,
                ItemName = x.ItemName.Trim(),
                Notes = x.Notes,
                SourceDepartmentId = dept.Id,
                CreatedByUserId = u
            }).ToList();

        if (items.Count == 0) throw new InvalidOperationException("Vui lòng nhập ít nhất một chứng từ");

        await docs.AddDebtItemsAsync(items);
        d.HasDebtDocuments = true;
        await docs.UpdateAsync(d);

        var itemsText = string.Join(", ", items.Select(x => x.ItemName));
        var isGenericNote = string.IsNullOrWhiteSpace(r.Notes)
            || r.Notes.Trim().StartsWith("Thêm chứng từ nợ tại", StringComparison.OrdinalIgnoreCase);

        var note = isGenericNote
            ? $"NV {dept.Code} thêm chứng từ nợ: {itemsText}"
            : $"{r.Notes!.Trim()} ({itemsText})";

        await AddHistory(id, d.CurrentDepartmentId, d.CurrentDepartmentId, CirculationAction.Supplemented, d.Status, u, note);
    }

    // ===== Thêm / Sửa / Xóa chứng từ nợ theo đúng trạng thái từng phòng =====
    private async Task<(DebtDocument Document, DebtItem Item)> GetEditableItemAsync(Guid documentId, Guid itemId, Guid userId)
    {
        var (user, dept) = await GetActorAsync(userId);
        var document = await docs.GetDetailByIdAsync(documentId)
            ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        var item = document.DebtItems.FirstOrDefault(x => x.Id == itemId)
            ?? throw new InvalidOperationException("Không tìm thấy chứng từ nợ");

        if (item.IsSupplemented)
            throw new InvalidOperationException("Không thể sửa hoặc xóa chứng từ nợ đã được bổ sung");

        var isStaff = user.Role == "staff";
        var allowed = dept?.Code switch
        {
            "PKH" => isStaff
                     && document.Status == DocumentStatus.PkhPendingSup
                     && document.CreatedByUserId == userId
                     && item.SourceDepartmentId == dept.Id,

            "PQLN" => isStaff
                      && document.Status is DocumentStatus.PqlnReceived or DocumentStatus.PqlnPendingSup
                      && document.PqlnReceiverId == userId
                      && item.SourceDepartmentId == dept.Id,

            _ => false
        };

        if (!allowed)
            throw new UnauthorizedAccessException("Bạn không có quyền sửa/xóa chứng từ nợ ở trạng thái hiện tại của hồ sơ");

        return (document, item);
    }

    public async Task UpdateDebtItemAsync(Guid documentId, Guid itemId, UpdateDebtItemRequest request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.ItemName))
            throw new InvalidOperationException("Tên chứng từ không được để trống");

        var (document, item) = await GetEditableItemAsync(documentId, itemId, userId);
        var oldName = item.ItemName;
        item.ItemName = request.ItemName.Trim();
        item.Notes = request.Notes;
        await docs.UpdateDebtItemAsync(item);

        await AddHistory(document.Id, document.CurrentDepartmentId, document.CurrentDepartmentId,
            CirculationAction.Supplemented, document.Status, userId, $"Sửa tên chứng từ nợ: '{oldName}' → '{item.ItemName}'");
    }

    public async Task DeleteDebtItemAsync(Guid documentId, Guid itemId, Guid userId)
    {
        var (document, item) = await GetEditableItemAsync(documentId, itemId, userId);
        await docs.DeleteDebtItemAsync(itemId);

        if (!document.DebtItems.Any(x => x.Id != itemId))
        {
            document.HasDebtDocuments = false;
            await docs.UpdateAsync(document);
        }

        await AddHistory(document.Id, document.CurrentDepartmentId, document.CurrentDepartmentId,
            CirculationAction.Supplemented, document.Status, userId, $"Xóa chứng từ nợ: {item.ItemName}");
    }

    public async Task SupplementItemsAsync(Guid id, SupplementItemsRequest r, Guid u)
    {
        var (user, dept) = await GetActorAsync(u);
        var d = await docs.GetByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");

        var isStaff = user.Role == "staff";
        var allowed = dept?.Code switch
        {
            "PKH" => isStaff && d.CreatedByUserId == u,
            "PQLN" => isStaff && d.PqlnReceiverId == u,
            _ => false
        };
        if (!allowed || dept == null)
            throw new UnauthorizedAccessException("Bạn chỉ được bổ sung chứng từ của hồ sơ do mình phụ trách");

        var requestedIds = (r.SupplementedItemIds ?? []).Distinct().ToList();
        if (requestedIds.Count == 0)
            throw new InvalidOperationException("Vui lòng chọn ít nhất một chứng từ để bổ sung");

        var allItems = await docs.GetDebtItemsAsync(id);
        var targetItems = allItems.Where(x => requestedIds.Contains(x.Id)).ToList();

        bool BelongsToMyDept(DebtItem item) =>
            item.SourceDepartmentId == dept.Id
            || (dept.Code == "PKH" && item.SourceDepartmentId == null);

        if (targetItems.Any(x => !BelongsToMyDept(x)))
            throw new UnauthorizedAccessException($"Nhân viên {dept.Code} không được phép bổ sung chứng từ nợ thuộc phòng ban khác");

        var supplementedNames = new List<string>();
        foreach (var x in targetItems)
        {
            if (x.IsSupplemented) continue;
            x.IsSupplemented = true;
            x.SupplementedAt = DateTime.UtcNow;
            x.SupplementedByUserId = u;
            await docs.UpdateDebtItemAsync(x);
            supplementedNames.Add(x.ItemName);
        }

        if (supplementedNames.Count == 0)
            throw new InvalidOperationException("Vui lòng chọn ít nhất một chứng từ chưa bổ sung");

        var itemsText = string.Join(", ", supplementedNames);
        var note = string.IsNullOrWhiteSpace(r.Notes)
                   || r.Notes.Trim().Equals("Bổ sung chứng từ nợ", StringComparison.OrdinalIgnoreCase)
            ? $"NV {dept.Code} bổ sung chứng từ nợ: {itemsText}"
            : $"{r.Notes.Trim()} ({itemsText})";

        await AddHistory(id, d.CurrentDepartmentId, d.CurrentDepartmentId, CirculationAction.Supplemented, d.Status, u, note);
    }

    public async Task VerifySupplementItemsAsync(Guid id, VerifySupplementItemsRequest r, Guid u)
    {
        var (user, dept) = await GetActorAsync(u);
        if (dept?.Code != "PQLN" || user.Role != "staff")
            throw new UnauthorizedAccessException("Chỉ nhân viên phòng PQLN mới có quyền xác nhận chứng từ bổ sung từ PKH.");

        var d = await docs.GetDetailByIdAsync(id)
            ?? throw new InvalidOperationException("Hồ sơ không tồn tại.");

        if (d.PqlnReceiverId != u)
            throw new UnauthorizedAccessException("Chỉ cán bộ PQLN đang thụ lý hồ sơ này mới được xác nhận.");

        var pkh = await departments.GetByCodeAsync("PKH")
            ?? throw new InvalidOperationException("Không tìm thấy phòng ban PKH.");

        var requestedIds = (r.ItemIds ?? []).Distinct().ToList();
        if (requestedIds.Count == 0)
            throw new InvalidOperationException("Vui lòng chọn ít nhất một chứng từ để xác nhận.");

        var targetItems = d.DebtItems.Where(x => requestedIds.Contains(x.Id)).ToList();

        if (targetItems.Any(x => x.SourceDepartmentId != null && x.SourceDepartmentId != pkh.Id))
            throw new InvalidOperationException("Chỉ thực hiện xác nhận đối với chứng từ nợ thuộc phòng PKH.");

        if (targetItems.Any(x => !x.IsSupplemented))
            throw new InvalidOperationException("Không thể xác nhận chứng từ mà nhân viên PKH chưa tích bổ sung.");

        var verifiedNames = new List<string>();
        var now = DateTime.UtcNow;
        foreach (var x in targetItems)
        {
            if (x.IsVerifiedByPqln) continue;
            x.IsVerifiedByPqln = true;
            x.PqlnVerifiedAt = now;
            x.PqlnVerifiedByUserId = u;
            await docs.UpdateDebtItemAsync(x);
            verifiedNames.Add(x.ItemName);
        }

        if (verifiedNames.Count == 0)
            throw new InvalidOperationException("Các chứng từ đã chọn đều đã được xác nhận trước đó.");

        var itemsText = string.Join(", ", verifiedNames);
        var note = string.IsNullOrWhiteSpace(r.Notes)
            ? $"NV PQLN xác nhận đã nhận chứng từ bổ sung từ PKH: {itemsText}"
            : $"{r.Notes.Trim()} ({itemsText})";

        await AddHistory(id, d.CurrentDepartmentId, d.CurrentDepartmentId, CirculationAction.Supplemented, d.Status, u, note);
    }
    public async Task VerifyPqlnSupplementItemsAsync(Guid id, VerifyPqlnSupplementItemsRequest r, Guid u)
    {
        var (user, dept) = await GetActorAsync(u);
        if (dept?.Code != "PDV" || user.Role != "staff")
            throw new UnauthorizedAccessException("Chỉ nhân viên phòng PDV mới có quyền xác nhận chứng từ bổ sung từ PQLN.");

        var d = await docs.GetDetailByIdAsync(id)
            ?? throw new InvalidOperationException("Hồ sơ không tồn tại.");

        if (d.PdvReceiverId != u)
            throw new UnauthorizedAccessException("Chỉ cán bộ PDV đang thụ lý hồ sơ này mới được xác nhận.");

        var pqln = await departments.GetByCodeAsync("PQLN")
            ?? throw new InvalidOperationException("Không tìm thấy phòng ban PQLN.");

        var requestedIds = (r.ItemIds ?? []).Distinct().ToList();
        if (requestedIds.Count == 0)
            throw new InvalidOperationException("Vui lòng chọn ít nhất một chứng từ để xác nhận.");

        var targetItems = d.DebtItems.Where(x => requestedIds.Contains(x.Id)).ToList();

        if (targetItems.Any(x => x.SourceDepartmentId != pqln.Id))
            throw new InvalidOperationException("Chỉ thực hiện xác nhận đối với chứng từ nợ thuộc phòng PQLN.");

        if (targetItems.Any(x => !x.IsSupplemented))
            throw new InvalidOperationException("Không thể xác nhận chứng từ mà nhân viên PQLN chưa tích bổ sung.");

        var verifiedNames = new List<string>();
        var now = DateTime.UtcNow;
        foreach (var x in targetItems)
        {
            if (x.IsVerifiedByPdv) continue;
            x.IsVerifiedByPdv = true;
            x.PdvVerifiedAt = now;
            x.PdvVerifiedByUserId = u;
            await docs.UpdateDebtItemAsync(x);
            verifiedNames.Add(x.ItemName);
        }

        if (verifiedNames.Count == 0)
            throw new InvalidOperationException("Các chứng từ đã chọn đều đã được xác nhận trước đó.");

        var itemsText = string.Join(", ", verifiedNames);
        var note = string.IsNullOrWhiteSpace(r.Notes)
            ? $"NV PDV xác nhận đã nhận chứng từ bổ sung từ PQLN: {itemsText}"
            : $"{r.Notes.Trim()} ({itemsText})";

        await AddHistory(id, d.CurrentDepartmentId, d.CurrentDepartmentId, CirculationAction.Supplemented, d.Status, u, note);
    }
    public async Task PqlnTakeOwnershipAsync(Guid id, Guid u)
    {
        var (user, dept) = await GetActorAsync(u);
        if (dept?.Code != "PQLN" || user.Role != "staff")
            throw new UnauthorizedAccessException("Chỉ nhân viên phòng PQLN mới có quyền tiếp nhận hồ sơ này.");

        var d = await docs.GetDetailByIdAsync(id)
            ?? throw new InvalidOperationException("Hồ sơ không tồn tại.");

        if (d.CurrentDepartmentId != dept.Id)
            throw new InvalidOperationException("Hồ sơ hiện không nằm tại phòng PQLN.");

        if (d.Status != DocumentStatus.PqlnPendingReceive || d.PqlnReceiverId != null)
        {
            var receiverName = d.PqlnReceiver?.Name
                ?? (d.PqlnReceiverId.HasValue ? (await users.GetByIdAsync(d.PqlnReceiverId.Value))?.Name : null)
                ?? "cán bộ khác";
            throw new InvalidOperationException($"Không thể tiếp nhận: Hồ sơ này đã được {receiverName} tiếp nhận trước đó.");
        }

        var claimed = await docs.TryTakePqlnOwnershipAsync(id, u, DateTime.UtcNow);
        if (!claimed)
        {
            var latest = await docs.GetDetailByIdAsync(id);
            var receiverName = latest?.PqlnReceiver?.Name
                ?? (latest?.PqlnReceiverId.HasValue == true ? (await users.GetByIdAsync(latest.PqlnReceiverId.Value))?.Name : null)
                ?? "cán bộ khác";
            throw new InvalidOperationException($"Không thể tiếp nhận: Hồ sơ này vừa được {receiverName} tiếp nhận trước đó.");
        }

        await AddHistory(id, dept.Id, dept.Id, CirculationAction.PqlnReceived, DocumentStatus.PqlnReceived, u, "Cán bộ PQLN đã tiếp nhận hồ sơ");
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