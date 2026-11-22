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
    private async Task ChangeDeptAsync(Guid id, Guid uid, string code, DocumentStatus status, CirculationAction action, string note)
    {
        var d = await docs.GetByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        var dept = await departments.GetByCodeAsync(code) ?? throw new InvalidOperationException("Phòng ban không tồn tại");
        var from = d.CurrentDepartmentId;
        d.CurrentDepartmentId = dept.Id;
        d.Status = status;
        await docs.UpdateAsync(d);
        await audit.AddHistoryAsync(id, from, dept.Id, action, status, uid, note);
    }

    public async Task PkhSupApproveAsync(Guid id, string? n, Guid u)
    {
        var d = await docs.GetByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        var isDirectPdv = NormalizeWorkflowType(d.WorkflowType) == WorkflowDirectPdv;

        d.SupPkhApprovedAt = DateTime.UtcNow;
        d.SupPkhApproverId = u;
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

        var claimed = await docs.TryTakePqlnOwnershipAsync(id, u, DateTime.UtcNow);
        if (!claimed)
        {
            var latest = await docs.GetDetailByIdAsync(id);
            var receiverName = latest?.PqlnReceiver?.Name
                ?? (latest?.PqlnReceiverId.HasValue == true ? (await users.GetByIdAsync(latest.PqlnReceiverId.Value))?.Name : null)
                ?? "cán bộ khác";
            throw new InvalidOperationException($"Không thể tiếp nhận: Hồ sơ này vừa được {receiverName} tiếp nhận trước đó.");
        }

        await audit.AddHistoryAsync(id, dept.Id, dept.Id, CirculationAction.PqlnReceived, DocumentStatus.PqlnReceived, u, "Cán bộ PQLN đã tiếp nhận hồ sơ");
    }

    public async Task PqlnSubmitSupAsync(Guid id, string? n, Guid u)
    {
        var d = await docs.GetByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        if (NormalizeWorkflowType(d.WorkflowType) == WorkflowDirectPdv)
            throw new InvalidOperationException("Hồ sơ luồng chuyển thẳng PDV không đi qua PQLN.");

        await ChangeDeptAsync(id, u, "PQLN", DocumentStatus.PqlnPendingSup, CirculationAction.PqlnSubmittedSup, n ?? "Cán bộ PQLN đã kiểm tra và đẩy hồ sơ lên SUP PQLN duyệt");
    }

    public async Task PqlnSupApproveAsync(Guid id, string? n, Guid u)
    {
        var d = await docs.GetByIdAsync(id) ?? throw new InvalidOperationException("Hồ sơ không tồn tại");
        if (NormalizeWorkflowType(d.WorkflowType) == WorkflowDirectPdv)
            throw new InvalidOperationException("Hồ sơ luồng chuyển thẳng PDV không đi qua PQLN.");

        d.SupPqlnApprovedAt = DateTime.UtcNow;
        d.SupPqlnApproverId = u;
        await docs.UpdateAsync(d);

        await ChangeDeptAsync(id, u, "PDV", DocumentStatus.PdvPendingReceive, CirculationAction.PqlnSupApproved, n ?? "SUP PQLN đã duyệt và chuyển hồ sơ sang PDV");
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

        var claimed = await docs.TryTakePdvOwnershipAsync(id, u, DateTime.UtcNow);
        if (!claimed)
        {
            var latest = await docs.GetDetailByIdAsync(id);
            var receiverName = latest?.PdvReceiver?.Name
                ?? (latest?.PdvReceiverId.HasValue == true ? (await users.GetByIdAsync(latest.PdvReceiverId.Value))?.Name : null)
                ?? "cán bộ khác";
            throw new InvalidOperationException($"Không thể tiếp nhận: Hồ sơ này vừa được {receiverName} tiếp nhận trước đó.");
        }

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
            throw new UnauthorizedAccessException("Chỉ SUP phòng PDV mới có quyền phê duyệt bước này.");

        var d = await docs.GetDetailByIdAsync(id)
            ?? throw new InvalidOperationException("Hồ sơ không tồn tại.");

        if (d.CurrentDepartmentId != dept.Id)
            throw new InvalidOperationException("Hồ sơ hiện không nằm tại phòng PDV.");

        if (d.Status is not (DocumentStatus.PdvEntryPosted or DocumentStatus.PdvEntryApproved))
            throw new InvalidOperationException("Hồ sơ không ở trạng thái chờ SUP PDV phê duyệt.");

        var pkh = await departments.GetByCodeAsync("PKH");
        var pqln = await departments.GetByCodeAsync("PQLN");
        var unsupItems = d.DebtItems.Where(x => !IsItemCompleted(x, d.WorkflowType, pkh?.Id, pqln?.Id)).ToList();
        var allSupplemented = unsupItems.Count == 0;
        var isDefaultUiNote = string.IsNullOrWhiteSpace(n)
            || n.Trim().Equals("SUP PDV kiểm tra và duyệt hoàn tất", StringComparison.OrdinalIgnoreCase);

        if (d.Status == DocumentStatus.PdvEntryPosted)
        {
            d.SupPdvApprovedEntryAt = DateTime.UtcNow;
            d.SupPdvEntryApproverId = u;

            if (!allSupplemented)
            {
                d.Status = DocumentStatus.PdvEntryApproved;
                await docs.UpdateAsync(d);

                var note1 = isDefaultUiNote
                    ? $"SUP PDV phê duyệt Lần 1: Đã hoàn tất đi bút toán, chờ bổ sung/xác nhận {unsupItems.Count} chứng từ nợ"
                    : n!.Trim();

                await audit.AddHistoryAsync(id, d.CurrentDepartmentId, d.CurrentDepartmentId,
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

                await audit.AddHistoryAsync(id, d.CurrentDepartmentId, d.CurrentDepartmentId,
                    CirculationAction.Completed, d.Status, u, noteDone);
            }
        }
        else
        {
            if (!allSupplemented)
                throw new InvalidOperationException(
                    $"Không thể duyệt hoàn tất (Lần 2): Hồ sơ vẫn còn {unsupItems.Count} chứng từ nợ chưa bổ sung hoặc chưa được xác nhận đủ.");

            d.Status = DocumentStatus.Completed;
            d.CompletedAt = DateTime.UtcNow;
            d.CompletedByUserId = u;
            await docs.UpdateAsync(d);

            var note2 = isDefaultUiNote
                ? "SUP PDV phê duyệt Lần 2: Đã bổ sung và xác nhận đủ chứng từ nợ — Hồ sơ ĐÃ HOÀN TẤT"
                : n!.Trim();

            await audit.AddHistoryAsync(id, d.CurrentDepartmentId, d.CurrentDepartmentId,
                CirculationAction.Completed, d.Status, u, note2);
        }
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
        await audit.AddHistoryAsync(id, from, to, CirculationAction.Returned, d.Status, u, n ?? "Trả lại hồ sơ để chỉnh sửa/bổ sung");
    }
}