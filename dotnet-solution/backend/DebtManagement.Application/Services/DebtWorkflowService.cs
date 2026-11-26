using DebtManagement.Application.Interfaces;
using DebtManagement.Domain.Entities;
using static DebtManagement.Application.Services.DebtDocumentRules;

namespace DebtManagement.Application.Services;

public class DebtWorkflowService(
    IDebtDocumentRepository docs,
    IDepartmentRepository departments,
    IUserRepository users,
    IDocumentAuditService audit) : IDebtWorkflowService
{
    private static void ClearReturnInfo(DebtDocument d)
    {
        d.ReturnReason = null;
        d.ReturnedAt = null;
        d.ReturnedByUserId = null;
        d.ReturnedByUserName = null;
        d.ReturnedFromDepartmentCode = null;
    }

    private async Task ChangeDeptAsync(Guid id, Guid uid, string code, DocumentStatus status, CirculationAction action, string note)
    {
        var d = await docs.GetByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        var dept = await departments.GetByCodeAsync(code) ?? throw new InvalidOperationException("Phòng ban không tồn tại");
        var from = d.CurrentDepartmentId;
        d.CurrentDepartmentId = dept.Id;
        d.Status = status;
        ClearReturnInfo(d);
        await docs.UpdateAsync(d);
        await audit.AddHistoryAsync(id, from, dept.Id, action, status, uid, note);
    }

    public async Task PkhSupApproveAsync(Guid id, string? n, Guid u)
    {
        var d = await docs.GetByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        if (d.Status != DocumentStatus.PkhPendingSup)
            throw new InvalidOperationException("Hồ sơ không ở trạng thái chờ SUP PKH duyệt.");

        var isDirectPdv = NormalizeWorkflowType(d.WorkflowType) == WorkflowDirectPdv;

        d.SupPkhApprovedAt = DateTime.UtcNow;
        d.SupPkhApproverId = u;
        ClearReturnInfo(d);
        await docs.UpdateAsync(d);

        if (isDirectPdv)
        {
            var note = string.IsNullOrWhiteSpace(n) || n.Trim().Equals("SUP duyệt hồ sơ", StringComparison.OrdinalIgnoreCase)
                ? "SUP PKH đã duyệt và chuyển thẳng hồ sơ sang PDV"
                : n.Trim();
            await ChangeDeptAsync(id, u, "PDV", DocumentStatus.PdvPendingReceive, CirculationAction.PkhSupApproved, note);
        }
        else
        {
            var note = string.IsNullOrWhiteSpace(n)
                ? "SUP PKH đã duyệt và chuyển hồ sơ sang PQLN"
                : n.Trim();
            await ChangeDeptAsync(id, u, "PQLN", DocumentStatus.PqlnPendingReceive, CirculationAction.PkhSupApproved, note);
        }
    }

    public async Task ResubmitPkhAsync(Guid id, string? n, Guid u)
    {
        var (user, dept) = await GetActorAsync(users, departments, u);
        if (dept?.Code != "PKH" || user.Role != "staff")
            throw new UnauthorizedAccessException("Chỉ nhân viên PKH mới có quyền gửi lại hồ sơ này.");

        var d = await docs.GetByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại.");
        if (d.CreatedByUserId != u)
            throw new UnauthorizedAccessException("Chỉ cán bộ khởi tạo hồ sơ mới được phép gửi lại.");
        if (d.Status != DocumentStatus.ReturnedToPkh)
            throw new InvalidOperationException("Hồ sơ không ở trạng thái bị trả về PKH.");

        d.Status = DocumentStatus.PkhPendingSup;
        ClearReturnInfo(d);
        await docs.UpdateAsync(d);

        var note = string.IsNullOrWhiteSpace(n)
            ? "NV PKH đã chỉnh sửa và gửi lại SUP PKH phê duyệt"
            : $"NV PKH đã chỉnh sửa và gửi lại SUP PKH: {n.Trim()}";

        await audit.AddHistoryAsync(id, dept.Id, dept.Id, CirculationAction.Supplemented, d.Status, u, note);
    }

    public async Task PqlnTakeOwnershipAsync(Guid id, Guid u)
    {
        var (user, dept) = await GetActorAsync(users, departments, u);
        if (dept?.Code != "PQLN" || user.Role != "staff")
            throw new UnauthorizedAccessException("Chỉ nhân viên phòng PQLN mới có quyền tiếp nhận hồ sơ này.");

        var d = await docs.GetDetailByIdAsync(id)
            ?? throw new InvalidOperationException("Hồ sơ không tồn tại.");

        if (NormalizeWorkflowType(d.WorkflowType) == WorkflowDirectPdv)
            throw new InvalidOperationException("Hồ sơ luồng chuyển thẳng PDV không đi qua PQLN.");

        if (d.CurrentDepartmentId != dept.Id)
            throw new InvalidOperationException("Hồ sơ hiện không nằm tại phòng PQLN.");

        if (d.Status != DocumentStatus.PqlnPendingReceive || d.PqlnReceiverId != null)
        {
            var receiverName = d.PqlnReceiver?.Name
                ?? (d.PqlnReceiverId.HasValue ? (await users.GetByIdAsync(d.PqlnReceiverId.Value))?.Name : null)
                ?? "cán bộ khác";
            throw new InvalidOperationException($"Không thể tiếp nhận: Hồ sơ này đã được {receiverName} tiếp nhận trước đó.");
        }

        var now = DateTime.UtcNow;
        var claimed = await docs.TryTakePqlnOwnershipAsync(id, u, now);
        if (!claimed)
        {
            var latest = await docs.GetDetailByIdAsync(id);
            var receiverName = latest?.PqlnReceiver?.Name
                ?? (latest?.PqlnReceiverId.HasValue == true ? (await users.GetByIdAsync(latest.PqlnReceiverId.Value))?.Name : null)
                ?? "cán bộ khác";
            throw new InvalidOperationException($"Không thể tiếp nhận: Hồ sơ này vừa được {receiverName} tiếp nhận trước đó.");
        }

        d.PqlnReceiverId = u;
        d.PqlnReceiver = user;
        d.PqlnReceivedAt = now;
        d.Status = DocumentStatus.PqlnReceived;
        ClearReturnInfo(d);

        await audit.AddHistoryAsync(id, dept.Id, dept.Id, CirculationAction.PqlnReceived, DocumentStatus.PqlnReceived, u, "Cán bộ PQLN đã tiếp nhận hồ sơ");
    }

    public async Task PqlnSubmitSupAsync(Guid id, string? n, Guid u)
    {
        var (user, dept) = await GetActorAsync(users, departments, u);
        if (dept?.Code != "PQLN" || user.Role != "staff")
            throw new UnauthorizedAccessException("Chỉ nhân viên phòng PQLN mới có quyền trình SUP PQLN.");

        var d = await docs.GetByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        if (NormalizeWorkflowType(d.WorkflowType) == WorkflowDirectPdv)
            throw new InvalidOperationException("Hồ sơ luồng chuyển thẳng PDV không đi qua PQLN.");

        if (d.Status is not (DocumentStatus.PqlnReceived or DocumentStatus.ReturnedToPqln))
            throw new InvalidOperationException("Hồ sơ không ở trạng thái được phép trình SUP PQLN.");
        if (d.PqlnReceiverId != u)
            throw new UnauthorizedAccessException("Chỉ cán bộ PQLN đang thụ lý mới có quyền trình SUP PQLN.");

        var defaultMsg = d.Status == DocumentStatus.ReturnedToPqln
            ? "NV PQLN đã xử lý hồ sơ bị trả lại và trình lại SUP PQLN duyệt"
            : "Cán bộ PQLN đã kiểm tra và đẩy hồ sơ lên SUP PQLN duyệt";

        var note = string.IsNullOrWhiteSpace(n) ? defaultMsg : n.Trim();
        await ChangeDeptAsync(id, u, "PQLN", DocumentStatus.PqlnPendingSup, CirculationAction.PqlnSubmittedSup, note);
    }

    public async Task PqlnSupApproveAsync(Guid id, string? n, Guid u)
    {
        var d = await docs.GetByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        if (NormalizeWorkflowType(d.WorkflowType) == WorkflowDirectPdv)
            throw new InvalidOperationException("Hồ sơ luồng chuyển thẳng PDV không đi qua PQLN.");
        if (d.Status != DocumentStatus.PqlnPendingSup)
            throw new InvalidOperationException("Hồ sơ không ở trạng thái chờ SUP PQLN duyệt.");

        d.SupPqlnApprovedAt = DateTime.UtcNow;
        d.SupPqlnApproverId = u;
        ClearReturnInfo(d);
        await docs.UpdateAsync(d);

        await ChangeDeptAsync(id, u, "PDV", DocumentStatus.PdvPendingReceive, CirculationAction.PqlnSupApproved, string.IsNullOrWhiteSpace(n) ? "SUP PQLN đã duyệt và chuyển hồ sơ sang PDV" : n.Trim());
    }

    public async Task PdvTakeOwnershipAsync(Guid id, Guid u)
    {
        var (user, dept) = await GetActorAsync(users, departments, u);
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

        var now = DateTime.UtcNow;
        var claimed = await docs.TryTakePdvOwnershipAsync(id, u, now);
        if (!claimed)
        {
            var latest = await docs.GetDetailByIdAsync(id);
            var receiverName = latest?.PdvReceiver?.Name
                ?? (latest?.PdvReceiverId.HasValue == true ? (await users.GetByIdAsync(latest.PdvReceiverId.Value))?.Name : null)
                ?? "cán bộ khác";
            throw new InvalidOperationException($"Không thể tiếp nhận: Hồ sơ này vừa được {receiverName} tiếp nhận trước đó.");
        }

        d.PdvReceiverId = u;
        d.PdvReceiver = user;
        d.PdvReceivedAt = now;
        d.Status = DocumentStatus.PdvReceived;
        ClearReturnInfo(d);

        await audit.AddHistoryAsync(id, dept.Id, dept.Id, CirculationAction.PdvReceived, DocumentStatus.PdvReceived, u, "Cán bộ Giao dịch PDV đã tiếp nhận hồ sơ");
    }

    public async Task PdvPostEntryAsync(Guid id, string? n, Guid u)
    {
        var (user, dept) = await GetActorAsync(users, departments, u);
        if (dept?.Code != "PDV" || user.Role != "staff")
            throw new UnauthorizedAccessException("Chỉ nhân viên phòng PDV mới có quyền đi bút toán.");

        var d = await docs.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Hồ sơ không tồn tại.");

        if (d.CurrentDepartmentId != dept.Id)
            throw new InvalidOperationException("Hồ sơ hiện không nằm tại phòng PDV.");

        if (d.Status != DocumentStatus.PdvReceived || d.PdvReceiverId != u)
            throw new InvalidOperationException("Hồ sơ phải được chính bạn tiếp nhận trước khi thực hiện đi bút toán.");

        d.EntryPostedAt = DateTime.UtcNow;
        d.EntryPostedById = u;
        d.Status = DocumentStatus.PdvEntryPosted;
        ClearReturnInfo(d);
        await docs.UpdateAsync(d);

        var note = string.IsNullOrWhiteSpace(n)
            ? "Cán bộ Giao dịch PDV đã thực hiện đi bút toán"
            : n.Trim();

        await audit.AddHistoryAsync(id, d.CurrentDepartmentId, d.CurrentDepartmentId, CirculationAction.PdvEntryPosted, d.Status, u, note);
    }

    public async Task PdvSupApproveAsync(Guid id, string? n, Guid u)
    {
        var (user, dept) = await GetActorAsync(users, departments, u);
        if (dept?.Code != "PDV" || user.Role != "manager")
            throw new UnauthorizedAccessException("Chỉ SUP phòng PDV mới có quyền phê duyệt bút toán.");

        var d = await docs.GetDetailByIdAsync(id)
            ?? throw new InvalidOperationException("Hồ sơ không tồn tại.");

        if (d.CurrentDepartmentId != dept.Id)
            throw new InvalidOperationException("Hồ sơ hiện không nằm tại phòng PDV.");

        if (d.Status == DocumentStatus.PdvEntryApproved)
            throw new InvalidOperationException("Hồ sơ đã được SUP PDV duyệt bút toán. Hệ thống sẽ tự động hoàn tất ngay khi toàn bộ chứng từ nợ được bổ sung và xác nhận đủ.");

        if (d.Status != DocumentStatus.PdvEntryPosted)
            throw new InvalidOperationException("Hồ sơ không ở trạng thái chờ SUP PDV duyệt bút toán.");

        var pkh = await departments.GetByCodeAsync("PKH");
        var pqln = await departments.GetByCodeAsync("PQLN");
        var pdv = await departments.GetByCodeAsync("PDV");
        var unsupItems = d.DebtItems.Where(x => !IsItemCompleted(x, d.WorkflowType, pkh?.Id, pqln?.Id, pdv?.Id)).ToList();
        var allSupplemented = unsupItems.Count == 0;
        var isDefaultUiNote = string.IsNullOrWhiteSpace(n)
            || n.Trim().Equals("SUP PDV kiểm tra và duyệt hoàn tất", StringComparison.OrdinalIgnoreCase)
            || n.Trim().Equals("SUP PDV kiểm tra và duyệt bút toán", StringComparison.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        ClearReturnInfo(d);
        d.SupPdvApprovedEntryAt = now;
        d.SupPdvEntryApproverId = u;

        if (!allSupplemented)
        {
            d.Status = DocumentStatus.PdvEntryApproved;
            await docs.UpdateAsync(d);

            var notePending = isDefaultUiNote
                ? $"SUP PDV duyệt bút toán: Đã hoàn tất hạch toán, chờ bổ sung/xác nhận {unsupItems.Count} chứng từ nợ (hồ sơ sẽ tự động hoàn tất khi đủ)"
                : n!.Trim();

            await audit.AddHistoryAsync(id, d.CurrentDepartmentId, d.CurrentDepartmentId,
                CirculationAction.PdvSupApprovedEntry, d.Status, u, notePending);
        }
        else
        {
            d.Status = DocumentStatus.Completed;
            d.CompletedAt = now;
            d.CompletedByUserId = u;
            await docs.UpdateAsync(d);

            var noteDone = isDefaultUiNote
                ? "SUP PDV duyệt bút toán: Đã đủ chứng từ nợ — Hồ sơ ĐÃ HOÀN TẤT"
                : n!.Trim();

            await audit.AddHistoryAsync(id, d.CurrentDepartmentId, d.CurrentDepartmentId,
                CirculationAction.Completed, d.Status, u, noteDone);
        }
    }

    public async Task ReturnAsync(Guid id, Guid to, string? n, Guid u)
    {
        var targetDept = await departments.GetByIdAsync(to)
            ?? throw new InvalidOperationException("Phòng ban đích không tồn tại.");
        await ReturnByCodeAsync(id, targetDept.Code, n, u);
    }

    public async Task ReturnByCodeAsync(Guid id, string toDepartmentCode, string? n, Guid u)
    {
        if (string.IsNullOrWhiteSpace(n))
            throw new InvalidOperationException("Vui lòng nhập lý do trả lại hồ sơ.");

        var reason = n.Trim();
        var (user, actorDept) = await GetActorAsync(users, departments, u);
        if (actorDept == null)
            throw new UnauthorizedAccessException("Tài khoản chưa được gán phòng ban.");

        var d = await docs.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Hồ sơ không tồn tại.");

        var targetCode = toDepartmentCode.Trim().ToUpperInvariant();
        var targetDept = await departments.GetByCodeAsync(targetCode)
            ?? throw new InvalidOperationException($"Không tìm thấy phòng ban đích {targetCode}.");

        var isDirectPdv = NormalizeWorkflowType(d.WorkflowType) == WorkflowDirectPdv;
        if (isDirectPdv && targetCode == "PQLN")
            throw new InvalidOperationException("Hồ sơ luồng chuyển thẳng PDV không thể trả về PQLN.");

        var isAllowed = (actorDept.Code, user.Role, d.Status, targetCode) switch
        {
            ("PKH", "manager", DocumentStatus.PkhPendingSup, "PKH") => true,

            ("PQLN", "staff", DocumentStatus.PqlnPendingReceive, "PKH") when !isDirectPdv => true,
            ("PQLN", "staff", DocumentStatus.PqlnReceived or DocumentStatus.ReturnedToPqln, "PKH")
                when !isDirectPdv && d.PqlnReceiverId == u => true,

            ("PQLN", "manager", DocumentStatus.PqlnPendingSup, "PQLN" or "PKH") when !isDirectPdv => true,

            ("PDV", "staff", DocumentStatus.PdvPendingReceive, "PKH") => true,
            ("PDV", "staff", DocumentStatus.PdvPendingReceive, "PQLN") when !isDirectPdv => true,
            ("PDV", "staff", DocumentStatus.PdvReceived, "PKH") when d.PdvReceiverId == u => true,
            ("PDV", "staff", DocumentStatus.PdvReceived, "PQLN") when !isDirectPdv && d.PdvReceiverId == u => true,

            ("PDV", "manager", DocumentStatus.PdvEntryPosted, "PKH") => true,
            ("PDV", "manager", DocumentStatus.PdvEntryPosted, "PQLN") when !isDirectPdv => true,

            _ => false
        };

        if (!isAllowed)
            throw new UnauthorizedAccessException("Bạn không có quyền trả lại hồ sơ tới phòng ban này ở trạng thái hiện tại.");

        var fromDeptId = d.CurrentDepartmentId;
        d.CurrentDepartmentId = targetDept.Id;

        if (targetCode == "PKH")
        {
            d.Status = DocumentStatus.ReturnedToPkh;
            d.SupPkhApprovedAt = null;
            d.SupPkhApproverId = null;
            d.PqlnReceiverId = null;
            d.PqlnReceivedAt = null;
            d.SupPqlnApprovedAt = null;
            d.SupPqlnApproverId = null;
            d.PdvReceiverId = null;
            d.PdvReceivedAt = null;
            d.EntryPostedAt = null;
            d.EntryPostedById = null;
            d.SupPdvApprovedEntryAt = null;
            d.SupPdvEntryApproverId = null;
        }
        else if (targetCode == "PQLN")
        {
            d.Status = DocumentStatus.ReturnedToPqln;
            d.SupPqlnApprovedAt = null;
            d.SupPqlnApproverId = null;
            d.PdvReceiverId = null;
            d.PdvReceivedAt = null;
            d.EntryPostedAt = null;
            d.EntryPostedById = null;
            d.SupPdvApprovedEntryAt = null;
            d.SupPdvEntryApproverId = null;
        }

        d.ReturnReason = reason;
        d.ReturnedAt = DateTime.UtcNow;
        d.ReturnedByUserId = u;
        d.ReturnedByUserName = user.Name;
        d.ReturnedFromDepartmentCode = actorDept.Code;

        await docs.UpdateAsync(d);

        var actorLabel = user.Role == "manager" ? $"SUP {actorDept.Code}" : $"NV {actorDept.Code}";
        var historyNote = $"[{actorLabel} → {targetCode}] Trả lại hồ sơ: {reason}";
        await audit.AddHistoryAsync(id, fromDeptId, targetDept.Id, CirculationAction.Returned, d.Status, u, historyNote);
    }
}