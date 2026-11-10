[CmdletBinding()]
param([string]$BaseUrl = "http://localhost:5000/api", [switch]$Cleanup)
$ErrorActionPreference = "Stop"
$script:Results = [System.Collections.Generic.List[object]]::new()
$script:CreatedDocs = [System.Collections.Generic.List[string]]::new()
$runId = (Get-Date -Format "yyyyMMddHHmmss")

function Log-Test([string]$Name, [bool]$Ok, [string]$Details = "") {
    $statusStr = "FAIL"
    $color = "Red"
    if ($Ok) {
        $statusStr = "PASS"
        $color = "Green"
    }
    $script:Results.Add([pscustomobject]@{ Test = $Name; Status = $statusStr; Details = $Details })
    Write-Host "[$statusStr] $Name - $Details" -ForegroundColor $color
    if (-not $Ok) {
        throw "Test failed: $Name ($Details)"
    }
}

function Call-Api {
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][string]$Path,
        [hashtable]$Headers = @{},
        $Body = $null,
        [int]$Expected = 200
    )

    $uri = "$BaseUrl$Path"
    $h = @{}
    foreach ($k in $Headers.Keys) { $h[$k] = $Headers[$k] }
    $req = @{ Uri = $uri; Method = $Method; Headers = $h; ErrorAction = "Stop" }
    if ($null -ne $Body) {
        $req.ContentType = "application/json; charset=utf-8"
        $jsonStr = ($Body | ConvertTo-Json -Depth 10)
        $req.Body = [System.Text.Encoding]::UTF8.GetBytes($jsonStr)
    }

    try {
        $r = Invoke-WebRequest @req
        $code = [int]$r.StatusCode
        $data = if ($r.Content) { $r.Content | ConvertFrom-Json } else { $null }
    } catch {
        $resp = $_.Exception.Response
        if ($null -eq $resp) { throw }
        $code = [int]$resp.StatusCode
        $reader = [System.IO.StreamReader]::new($resp.GetResponseStream())
        $raw = $reader.ReadToEnd()
        $reader.Dispose()
        $data = if ($raw) { try { $raw | ConvertFrom-Json } catch { $raw } } else { $null }
    }

    if ($code -ne $Expected) {
        $err = "HTTP $code"
        if ($data -is [string]) { $err = $data }
        elseif ($null -ne $data.error) { $err = $data.error }
        elseif ($null -ne $data.title) { $err = $data.title }
        throw "$Method $Path expected $Expected but got $code. Server response: $err"
    }
    return $data
}

function Do-Login([string]$Email, [string]$Password) {
    $res = Call-Api -Method "POST" -Path "/auth/login" -Body @{ Email = $Email; Password = $Password } -Expected 200
    $ok = ($null -ne $res.Token -and $res.User.Email -eq $Email)
    Log-Test "Login $Email" $ok "Role=$($res.User.Role)"
    return @{ Token = $res.Token; User = $res.User; Headers = @{ Authorization = "Bearer $($res.Token)" } }
}

try {
    Write-Host "=== BẮT ĐẦU CHẠY KỊCH BẢN TEST API ===" -ForegroundColor Yellow
    $admin = Do-Login "admin@vcb.com" "Admin@123456"
    $nvPkh = Do-Login "nv.pkh@vcb.com" "Staff@123456"
    $supPkh = Do-Login "sup.pkh@vcb.com" "Sup@123456"
    $nvPqln = Do-Login "nv.pqln@vcb.com" "Staff@123456"
    $supPqln = Do-Login "sup.pqln@vcb.com" "Sup@123456"
    $nvPdv = Do-Login "nv.pdv@vcb.com" "Staff@123456"
    $supPdv = Do-Login "sup.pdv@vcb.com" "Sup@123456"

    # Test reject login
    Call-Api -Method "POST" -Path "/auth/login" -Body @{ Email = "admin@vcb.com"; Password = "Wrong" } -Expected 401 | Out-Null
    Log-Test "Chặn đăng nhập sai mật khẩu" $true "HTTP 401"

    # Test departments
    $depts = @(Call-Api -Method "GET" -Path "/departments" -Headers $admin.Headers -Expected 200)
    Log-Test "Lấy danh mục phòng ban" ($depts.Count -eq 3) "Đủ 3 phòng: $($depts.Code -join ',')"

    # Test users
    $users = @(Call-Api -Method "GET" -Path "/users" -Headers $admin.Headers -Expected 200)
    Log-Test "Danh sách người dùng" ($users.Count -ge 7) "Tổng user: $($users.Count)"
    $newUser = Call-Api -Method "POST" -Path "/users" -Headers $admin.Headers -Body @{
        Name = "Test User $runId"
        Email = "user.$runId@vcb.com"
        Password = "Test@123456"
        DepartmentId = $depts[0].id
        Role = "staff"
    } -Expected 201
    Log-Test "Admin tạo người dùng mới" ($newUser.Email -eq "user.$runId@vcb.com") "Id=$($newUser.Id)"
    $upUser = Call-Api -Method "PUT" -Path "/users/$($newUser.Id)" -Headers $admin.Headers -Body @{
        Name = "Test User Updated $runId"
        DepartmentId = $depts[0].id
        Role = "staff"
        IsActive = $true
        Password = $null
    } -Expected 200
    Log-Test "Admin cập nhật người dùng" ($upUser.Name -like "*Updated*") "Tên mới=$($upUser.Name)"

    # --- LUỒNG 2: HỒ SƠ KHÔNG CÓ CHỨNG TỪ NỢ ---
    Write-Host "`n--- TEST LUỒNG 2: KHÔNG CÓ CHỨNG TỪ NỢ ---" -ForegroundColor Cyan
    $doc1 = Call-Api -Method "POST" -Path "/documents" -Headers $nvPkh.Headers -Body @{
        CustomerName = "Cty CP Không Nợ CT $runId"
        CustomerPhone = "0911223344"
        CustomerType = "company"
        IssueDate = (Get-Date).ToString("yyyy-MM-dd")
        DueDate = (Get-Date).AddDays(45).ToString("yyyy-MM-dd")
        Priority = "normal"
        Notes = "Hồ sơ không nợ chứng từ"
        HasDebtDocuments = $false
        DebtItems = @()
    } -Expected 201
    $script:CreatedDocs.Add($doc1.Id)
    $okStatus = ($doc1.Status -eq "PkhPendingSup" -and -not $doc1.HasDebtDocuments)
    Log-Test "NV PKH tạo hồ sơ không nợ CT" $okStatus "Số VB=$($doc1.DocumentNumber)"

    Call-Api -Method "POST" -Path "/documents/$($doc1.Id)/pkh-sup-approve" -Headers $supPkh.Headers -Body @{ Notes = "SUP PKH duyệt" } -Expected 204 | Out-Null
    $doc1 = Call-Api -Method "GET" -Path "/documents/$($doc1.Id)" -Headers $nvPqln.Headers -Expected 200
    Log-Test "SUP PKH duyệt chuyển PQLN" ($doc1.Status -eq "PqlnPendingReceive") "Status=$($doc1.Status)"

    Call-Api -Method "POST" -Path "/documents/$($doc1.Id)/pqln-receive" -Headers $nvPqln.Headers -Expected 204 | Out-Null
    $doc1 = Call-Api -Method "GET" -Path "/documents/$($doc1.Id)" -Headers $nvPqln.Headers -Expected 200
    $recvOk = ($doc1.Status -eq "PqlnReceived" -and $doc1.PqlnReceiverId -eq $nvPqln.User.Id)
    Log-Test "NV PQLN tiếp nhận hồ sơ" $recvOk "Gán cán bộ PQLN=$($doc1.PqlnReceiverName)"

    Call-Api -Method "POST" -Path "/documents/$($doc1.Id)/pqln-submit-sup" -Headers $nvPqln.Headers -Body @{ Notes = "Đẩy SUP PQLN" } -Expected 204 | Out-Null
    $doc1 = Call-Api -Method "GET" -Path "/documents/$($doc1.Id)" -Headers $supPqln.Headers -Expected 200
    Log-Test "NV PQLN đẩy SUP PQLN duyệt" ($doc1.Status -eq "PqlnPendingSup") "Status=$($doc1.Status)"

    Call-Api -Method "POST" -Path "/documents/$($doc1.Id)/pqln-sup-approve" -Headers $supPqln.Headers -Body @{ Notes = "SUP PQLN duyệt chuyển PDV" } -Expected 204 | Out-Null
    $doc1 = Call-Api -Method "GET" -Path "/documents/$($doc1.Id)" -Headers $nvPdv.Headers -Expected 200
    Log-Test "SUP PQLN duyệt chuyển PDV" ($doc1.Status -eq "PdvPendingReceive") "Status=$($doc1.Status)"

    Call-Api -Method "POST" -Path "/documents/$($doc1.Id)/pdv-receive" -Headers $nvPdv.Headers -Expected 204 | Out-Null
    $doc1 = Call-Api -Method "GET" -Path "/documents/$($doc1.Id)" -Headers $nvPdv.Headers -Expected 200
    $pdvRecvOk = ($doc1.Status -eq "PdvReceived" -and $doc1.PdvReceiverId -eq $nvPdv.User.Id)
    Log-Test "NV PDV tiếp nhận hồ sơ" $pdvRecvOk "Gán cán bộ PDV=$($doc1.PdvReceiverName)"

    Call-Api -Method "POST" -Path "/documents/$($doc1.Id)/pdv-post-entry" -Headers $nvPdv.Headers -Body @{ Notes = "Đi bút toán xong" } -Expected 204 | Out-Null
    $doc1 = Call-Api -Method "GET" -Path "/documents/$($doc1.Id)" -Headers $nvPdv.Headers -Expected 200
    $entryOk = ($doc1.Status -eq "PdvEntryPosted" -and $null -ne $doc1.EntryPostedAt)
    Log-Test "NV PDV đi bút toán" $entryOk "Thời gian đi bút toán=$($doc1.EntryPostedAt)"

    Call-Api -Method "POST" -Path "/documents/$($doc1.Id)/pdv-sup-approve" -Headers $supPdv.Headers -Body @{ Notes = "Duyệt hoàn tất không nợ CT" } -Expected 204 | Out-Null
    $doc1 = Call-Api -Method "GET" -Path "/documents/$($doc1.Id)" -Headers $supPdv.Headers -Expected 200
    $doneOk = ($doc1.Status -eq "Completed" -and $null -ne $doc1.CompletedAt)
    Log-Test "SUP PDV duyệt 1 lần -> ĐÃ HOÀN TẤT" $doneOk "Hoàn thành lúc=$($doc1.CompletedAt)"

    # --- LUỒNG 1: HỒ SƠ CÓ CHỨNG TỪ NỢ (DUYỆT 2 LẦN) ---
    Write-Host "`n--- TEST LUỒNG 1: CÓ CHỨNG TỪ NỢ (DUYỆT 2 LẦN) ---" -ForegroundColor Cyan
    $doc2 = Call-Api -Method "POST" -Path "/documents" -Headers $nvPkh.Headers -Body @{
        CustomerName = "Cty TNHH Có Nợ Chứng Từ $runId"
        CustomerPhone = "0988776655"
        CustomerType = "company"
        IssueDate = (Get-Date).ToString("yyyy-MM-dd")
        DueDate = (Get-Date).AddDays(60).ToString("yyyy-MM-dd")
        Priority = "urgent"
        Notes = "Hồ sơ nợ hợp đồng thế chấp và hóa đơn GTGT"
        HasDebtDocuments = $true
        DebtItems = @(
            @{ ItemName = "Hợp đồng thế chấp công chứng"; Notes = "Chưa công chứng" }
            @{ ItemName = "Hóa đơn VAT bản gốc"; Notes = "Bên bán chưa xuất" }
        )
    } -Expected 201
    $script:CreatedDocs.Add($doc2.Id)
    $hasDebtOk = ($doc2.Status -eq "PkhPendingSup" -and $doc2.HasDebtDocuments)
    Log-Test "NV PKH tạo hồ sơ CÓ nợ CT" $hasDebtOk "Số VB=$($doc2.DocumentNumber)"

    # Kiểm tra danh sách CT nợ
    $doc2Detail = Call-Api -Method "GET" -Path "/documents/$($doc2.Id)" -Headers $nvPkh.Headers -Expected 200
    $twoItems = ($doc2Detail.DebtItems.Count -eq 2 -and ($doc2Detail.DebtItems | Where-Object { -not $_.IsSupplemented }).Count -eq 2)
    Log-Test "Hệ thống ghi nhận 2 chứng từ nợ" $twoItems "Số CT chưa bổ sung=2"

    Call-Api -Method "POST" -Path "/documents/$($doc2.Id)/pkh-sup-approve" -Headers $supPkh.Headers -Body @{ Notes = "SUP PKH duyệt" } -Expected 204 | Out-Null
    Call-Api -Method "POST" -Path "/documents/$($doc2.Id)/pqln-receive" -Headers $nvPqln.Headers -Expected 204 | Out-Null
    Call-Api -Method "POST" -Path "/documents/$($doc2.Id)/pqln-submit-sup" -Headers $nvPqln.Headers -Body @{ Notes = "PQLN thẩm định CT nợ hợp lệ" } -Expected 204 | Out-Null
    Call-Api -Method "POST" -Path "/documents/$($doc2.Id)/pqln-sup-approve" -Headers $supPqln.Headers -Body @{ Notes = "SUP PQLN duyệt sang PDV" } -Expected 204 | Out-Null
    Call-Api -Method "POST" -Path "/documents/$($doc2.Id)/pdv-receive" -Headers $nvPdv.Headers -Expected 204 | Out-Null
    Call-Api -Method "POST" -Path "/documents/$($doc2.Id)/pdv-post-entry" -Headers $nvPdv.Headers -Body @{ Notes = "PDV đi bút toán xong" } -Expected 204 | Out-Null

    # NV PQLN thêm chứng từ nợ mới
    Call-Api -Method "POST" -Path "/documents/$($doc2.Id)/debt-items" -Headers $nvPqln.Headers -Body @{
        Items = @(
            @{ ItemName = "Chứng từ nợ do PQLN bổ sung"; Notes = "Thẩm định yêu cầu thêm" }
        )
        Notes = "PQLN yêu cầu bổ sung thêm hợp đồng bảo hiểm"
    } -Expected 204 | Out-Null
    $doc2Detail = Call-Api -Method "GET" -Path "/documents/$($doc2.Id)" -Headers $nvPqln.Headers -Expected 200
    Log-Test "NV PQLN thêm chứng từ nợ mới" ($doc2Detail.DebtItems.Count -eq 3) "Tổng số CT nợ hiện tại=$($doc2Detail.DebtItems.Count)"
    # SUP PDV Duyệt lần 1: Hoàn tất bút toán (vì còn nợ CT)
    Call-Api -Method "POST" -Path "/documents/$($doc2.Id)/pdv-sup-approve" -Headers $supPdv.Headers -Body @{ Notes = "Duyệt lần 1: Đã đi bút toán" } -Expected 204 | Out-Null
    $doc2 = Call-Api -Method "GET" -Path "/documents/$($doc2.Id)" -Headers $supPdv.Headers -Expected 200
    $app1Ok = ($doc2.Status -eq "PdvEntryApproved" -and $null -ne $doc2.SupPdvApprovedEntryAt)
    Log-Test "SUP PDV duyệt Lần 1 (Bút toán)" $app1Ok "Status=PdvEntryApproved (Chờ bổ sung đủ CT nợ)"

    # NV PKH Bổ sung toàn bộ chứng từ nợ
    $itemIds = @($doc2Detail.DebtItems | Select-Object -ExpandProperty Id)
    Call-Api -Method "POST" -Path "/documents/$($doc2.Id)/supplement" -Headers $nvPkh.Headers -Body @{
        SupplementedItemIds = $itemIds
        Notes = "Khách hàng đã nộp đủ hợp đồng công chứng và hóa đơn gốc"
    } -Expected 204 | Out-Null
    $doc2Detail = Call-Api -Method "GET" -Path "/documents/$($doc2.Id)" -Headers $nvPkh.Headers -Expected 200
    $unsupplemented = ($doc2Detail.DebtItems | Where-Object { -not $_.IsSupplemented }).Count
    Log-Test "NV PKH bổ sung toàn bộ chứng từ nợ" ($unsupplemented -eq 0) "Số CT chưa bổ sung=$unsupplemented"

    # SUP PDV Duyệt lần 2: Đã đủ CT -> ĐÃ HOÀN TẤT
    Call-Api -Method "POST" -Path "/documents/$($doc2.Id)/pdv-sup-approve" -Headers $supPdv.Headers -Body @{ Notes = "Duyệt lần 2: Đã đủ CT -> Hoàn tất hồ sơ" } -Expected 204 | Out-Null
    $doc2 = Call-Api -Method "GET" -Path "/documents/$($doc2.Id)" -Headers $supPdv.Headers -Expected 200
    $app2Ok = ($doc2.Status -eq "Completed" -and $null -ne $doc2.CompletedAt)
    Log-Test "SUP PDV duyệt Lần 2 -> ĐÃ HOÀN TẤT" $app2Ok "Hoàn tất lúc=$($doc2.CompletedAt)"

    # Kiểm tra Audit Trail
    Log-Test "Lưu vết toàn diện Luồng theo dõi hồ sơ" ($doc2.CirculationHistory.Count -ge 8) "Tổng số bước lưu vết=$($doc2.CirculationHistory.Count)"

    # --- NHÁNH TỪ CHỐI & TRẢ VỀ ---
    Write-Host "`n--- TEST NHÁNH TỪ CHỐI VÀ TRẢ VỀ ---" -ForegroundColor Cyan
    $docReject = Call-Api -Method "POST" -Path "/documents" -Headers $nvPkh.Headers -Body @{
        CustomerName = "Hồ sơ Test Từ Chối $runId"
        CustomerPhone = "0900000000"
        CustomerType = "individual"
        IssueDate = (Get-Date).ToString("yyyy-MM-dd")
        DueDate = (Get-Date).AddDays(15).ToString("yyyy-MM-dd")
        Priority = "low"
        Notes = "Test reject"
        HasDebtDocuments = $false
        DebtItems = @()
    } -Expected 201
    $script:CreatedDocs.Add($docReject.Id)
    Call-Api -Method "POST" -Path "/documents/$($docReject.Id)/reject" -Headers $supPkh.Headers -Body @{ Notes = "Không đủ điều kiện pháp lý" } -Expected 204 | Out-Null
    $docReject = Call-Api -Method "GET" -Path "/documents/$($docReject.Id)" -Headers $supPkh.Headers -Expected 200
    Log-Test "Từ chối hồ sơ thành công" ($docReject.Status -eq "Rejected") "Status=Rejected"

    $docReturn = Call-Api -Method "POST" -Path "/documents" -Headers $nvPkh.Headers -Body @{
        CustomerName = "Hồ sơ Test Trả Về $runId"
        CustomerPhone = "0900000000"
        CustomerType = "company"
        IssueDate = (Get-Date).ToString("yyyy-MM-dd")
        DueDate = (Get-Date).AddDays(20).ToString("yyyy-MM-dd")
        Priority = "normal"
        Notes = "Test return"
        HasDebtDocuments = $false
        DebtItems = @()
    } -Expected 201
    $script:CreatedDocs.Add($docReturn.Id)
    Call-Api -Method "POST" -Path "/documents/$($docReturn.Id)/pkh-sup-approve" -Headers $supPkh.Headers -Body @{ Notes = "Duyệt sang PQLN" } -Expected 204 | Out-Null
    Call-Api -Method "POST" -Path "/documents/$($docReturn.Id)/return" -Headers $nvPqln.Headers -Body @{ ToDepartmentId = $depts[0].id; Notes = "Thiếu chữ ký khách hàng, trả lại PKH" } -Expected 204 | Out-Null
    $docReturn = Call-Api -Method "GET" -Path "/documents/$($docReturn.Id)" -Headers $nvPqln.Headers -Expected 200
    Log-Test "Trả về hồ sơ thành công" ($docReturn.Status -eq "PkhPendingSup") "Quay lại PKH, Status=PkhPendingSup"

    Write-Host "`n===== TỔNG KẾT BÁO CÁO TEST API =====" -ForegroundColor Yellow
    $script:Results | Format-Table -AutoSize
    $passCount = @($script:Results | Where-Object { $_.Status -eq "PASS" }).Count
    $failCount = @($script:Results | Where-Object { $_.Status -eq "FAIL" }).Count
    $summaryColor = "Green"
    if ($failCount -gt 0) { $summaryColor = "Red" }
    Write-Host ("KẾT QUẢ: {0} PASSED, {1} FAILED" -f $passCount, $failCount) -ForegroundColor $summaryColor
    if ($failCount -gt 0) { exit 1 }
} catch {
    Log-Test "Lỗi ngoại lệ khi chạy test" $false $_.Exception.Message
    Write-Host "`n===== TỔNG KẾT BÁO CÁO TEST API =====" -ForegroundColor Yellow
    $script:Results | Format-Table -AutoSize
    exit 1
} finally {
    if ($Cleanup -and $script:CreatedDocs.Count -gt 0) {
        Write-Host "Dọn dẹp các hồ sơ test..." -ForegroundColor Yellow
        foreach ($id in $script:CreatedDocs) {
            try { Call-Api -Method "DELETE" -Path "/documents/$id" -Headers $admin.Headers -Expected 204 | Out-Null } catch { }
        }
    }
}
