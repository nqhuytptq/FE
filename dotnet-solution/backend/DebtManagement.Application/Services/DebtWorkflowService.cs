using DebtManagement.Application.Interfaces;
using DebtManagement.Domain.Entities;
using DebtManagement.Shared.Constants;
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
        var (user, dept) = await GetActorAsync(users, departments, u);
        if (dept == null || !WorkflowConstants.IsSalesDept(dept.Code) || user.Role != WorkflowConstants.RoleManager)
            throw new UnauthorizedAccessException("Chỉ Lãnh đạo (SUP) phòng bán hàng mới có quyền duyệt bước này.");

        var d = await docs.GetDetailByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        if (d.Status != DocumentStatus.PkhPendingSup)
            throw new InvalidOperationException("Hồ sơ không ở trạng thái chờ SUP duyệt.");

        var originCode = GetOriginDepartmentCode(d);
        if (!string.Equals(originCode, dept.Code, StringComparison.OrdinalIgnoreCase) && d.CurrentDepartmentId != dept.Id)
            throw new UnauthorizedAccessException($"Hồ sơ thuộc phòng {originCode}, bạn không có quyền phê duyệt.");

        var isDirectPdv = NormalizeWorkflowType(d.WorkflowType) == WorkflowDirectPdv;

        d.SupPkhApprovedAt = DateTime.UtcNow;
        d.SupPkhApproverId = u;
        ClearReturnInfo(d);
        await docs.UpdateAsync(d);

        if (isDirectPdv)
        {
            var note = string.IsNullOrWhiteSpace(n) || n.Trim().StartsWith("SUP ", StringComparison.OrdinalIgnoreCase)
                ? $"SUP {dept.Code} đã duyệt và chuyển thẳng hồ sơ sang PDV"
                : n.Trim();
            await ChangeDeptAsync(id, u, "PDV", DocumentStatus.PdvPendingReceive, CirculationAction.PkhSupApproved, note);
        }
        else
        {
            var note = string.IsNullOrWhiteSpace(n)
                ? $"SUP {dept.Code} đã duyệt và chuyển hồ sơ sang PQLN"
                : n.Trim();
            await ChangeDeptAsync(id, u, "PQLN", DocumentStatus.PqlnPendingReceive, CirculationAction.PkhSupApproved, note);
        }
    }

    public async Task ResubmitPkhAsync(Guid id, string? n, Guid u)
    {
        var (user, dept) = await GetActorAsync(users, departments, u);
        if (dept == null || !WorkflowConstants.IsSalesDept(dept.Code) || user.Role != WorkflowConstants.RoleStaff)
            throw new UnauthorizedAccessException("Chỉ nhân viên phòng bán hàng mới có quyền gửi lại hồ sơ này.");

        var d = await docs.GetByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại.");
        if (d.CreatedByUserId != u)
            throw new UnauthorizedAccessException("Chỉ cán bộ khởi tạo hồ sơ mới được phép gửi lại.");
        if (d.Status != DocumentStatus.ReturnedToPkh)
            throw new InvalidOperationException("Hồ sơ không ở trạng thái bị trả về.");

        d.CurrentDepartmentId = dept.Id;
        d.Status = DocumentStatus.PkhPendingSup;
        ClearReturnInfo(d);
        await docs.UpdateAsync(d);

        var note = string.IsNullOrWhiteSpace(n)
            ? $"NV {dept.Code} đã chỉnh sửa và gửi lại SUP {dept.Code} phê duyệt"
            : $"NV {dept.Code} đã chỉnh sửa và gửi lại SUP {dept.Code}: {n.Trim()}";

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

        var d = await docs.GetDetailByIdAsync(id)
            ?? throw new InvalidOperationException("Hồ sơ không tồn tại.");

        var requestedCode = toDepartmentCode.Trim().ToUpperInvariant();
        var targetIsSales = WorkflowConstants.IsSalesDept(requestedCode);
        var actorIsSales = WorkflowConstants.IsSalesDept(actorDept.Code);

        Department targetDept;
        if (targetIsSales)
        {
            // Tự động ánh xạ về đúng phòng bán hàng đã khởi tạo hồ sơ (KHDN, KHBL, PGD_LLQ, PGD_CD hoặc PKH cũ)
            var originCode = GetOriginDepartmentCode(d);
            if (requestedCode != WorkflowConstants.DeptPkh
                && !string.Equals(requestedCode, originCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Hồ sơ này thuộc phòng {originCode}, không thể trả về {requestedCode}.");
            }

            targetDept = GetOriginDepartment(d)
                ?? await departments.GetByCodeAsync(originCode)
                ?? throw new InvalidOperationException($"Không tìm thấy phòng ban đích {originCode}.");
        }
        else
        {
            targetDept = await departments.GetByCodeAsync(requestedCode)
                ?? throw new InvalidOperationException($"Không tìm thấy phòng ban đích {requestedCode}.");
        }

        var isDirectPdv = NormalizeWorkflowType(d.WorkflowType) == WorkflowDirectPdv;
        if (isDirectPdv && targetDept.Code == "PQLN")
            throw new InvalidOperationException("Hồ sơ luồng chuyển thẳng PDV không thể trả về PQLN.");

        var isAllowed = (actorIsSales, actorDept.Code, user.Role, d.Status, targetIsSales, targetDept.Code) switch
        {
            (true, _, "manager", DocumentStatus.PkhPendingSup, true, _)
                when string.Equals(actorDept.Code, targetDept.Code, StringComparison.OrdinalIgnoreCase) => true,

            (false, "PQLN", "staff", DocumentStatus.PqlnPendingReceive, true, _) when !isDirectPdv => true,
            (false, "PQLN", "staff", DocumentStatus.PqlnReceived or DocumentStatus.ReturnedToPqln, true, _)
                when !isDirectPdv && d.PqlnReceiverId == u => true,

            (false, "PQLN", "manager", DocumentStatus.PqlnPendingSup, false, "PQLN") when !isDirectPdv => true,
            (false, "PQLN", "manager", DocumentStatus.PqlnPendingSup, true, _) when !isDirectPdv => true,

            (false, "PDV", "staff", DocumentStatus.PdvPendingReceive, true, _) => true,
            (false, "PDV", "staff", DocumentStatus.PdvPendingReceive, false, "PQLN") when !isDirectPdv => true,
            (false, "PDV", "staff", DocumentStatus.PdvReceived, true, _) when d.PdvReceiverId == u => true,
            (false, "PDV", "staff", DocumentStatus.PdvReceived, false, "PQLN") when !isDirectPdv && d.PdvReceiverId == u => true,

            (false, "PDV", "manager", DocumentStatus.PdvEntryPosted, true, _) => true,
            (false, "PDV", "manager", DocumentStatus.PdvEntryPosted, false, "PQLN") when !isDirectPdv => true,

            _ => false
        };

        if (!isAllowed)
            throw new UnauthorizedAccessException("Bạn không có quyền trả lại hồ sơ tới phòng ban này ở trạng thái hiện tại.");

        var fromDeptId = d.CurrentDepartmentId;
        d.CurrentDepartmentId = targetDept.Id;

        if (targetIsSales)
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
        else if (targetDept.Code == "PQLN")
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
        var historyNote = $"[{actorLabel} → {targetDept.Code}] Trả lại hồ sơ: {reason}";
        await audit.AddHistoryAsync(id, fromDeptId, targetDept.Id, CirculationAction.Returned, d.Status, u, historyNote);
    }
}