# Kế hoạch triển khai: Ủy quyền — Check-in — Kiểm phiếu

**Căn cứ**: BRD v2.3 (28/04/2026, bản cập nhật Mục 2.3) + Phụ lục Kỹ thuật v1.3 + IPO Check-in v2.2 + IPO Screen v1.1 + IPO Proxy-Ballot v1.1

---

## Điểm mới từ BRD v2.3 so với các phụ lục

> [!IMPORTANT]
> BRD v2.3 là tài liệu chính thức mới nhất. Các phụ lục (v1.3, v2.2, v1.1) chưa cập nhật một số điểm dưới đây.

| # | Điểm mới/sửa đổi | Ảnh hưởng |
|---|---|---|
| 1 | **Phiếu bầu TVHĐQT (4a) và Phiếu bầu BKS (4b) là 2 phiếu ĐỘC LẬP**, in riêng, kiểm riêng | Entity `Ballot` cần `BallotType`; `MeetingCandidate` cần `CandidateBoard` (HDQT/BKS) |
| 2 | **7 loại template** rõ ràng: Thư mời(1), Thẻ BQ(2), Phiếu BQ(3), Bầu HĐQT(4a), Bầu BKS(4b), BB Thẩm tra(5), BB Kiểm phiếu(6) | Cập nhật `TemplateType` enum |
| 3 | **Gói in = 4 loại** trong 1 lệnh: Thẻ BQ + Phiếu BQ + Phiếu bầu HĐQT + Phiếu bầu BKS | PrintService multi-template |
| 4 | **QR/Barcode cấu hình riêng từng loại phiếu** tại thời điểm thiết lập đại hội | Meeting cần `MeetingTemplateConfig` |
| 5 | **MERGE tài khoản trùng ĐKSH giờ được phép tại quầy check-in** (trước đây chỉ trước check-in) | Thay đổi RB-07, thêm tình huống MERGE tại quầy |
| 6 | **Quy mô thiết kế**: ≤15.000 CĐ VSDC, ≤1.000 người trực tiếp, ≤20 POS | NFR benchmarks |
| 7 | **Render engine**: python-docx + LibreOffice (đã xác nhận dùng Mms.PrintAgent) | Tận dụng PrintAgent hiện có |

### Open Questions đã giải đáp

| Q | Câu hỏi | Trả lời từ BRD v2.3 |
|---|---------|---------------------|
| Q1 | MERGE cần 2 người phê duyệt — cơ chế nào? | **Xác nhận kép**: Trưởng quầy + 1 thành viên (tại quầy check-in) hoặc 2 người phê duyệt độc lập (trước ngày họp) |
| Q2 | Bầu cử nhân sự có trong phase 3? | **Có** — TVHĐQT và BKS tách riêng, Cumulative Voting, là phần core |
| Q3 | Blazor render mode? | **Interactive Server** — hệ thống on-premise, SignalR real-time |
| Q4 | Engine in phiếu? | **Mms.PrintAgent** + python-docx + LibreOffice |
| Q5 | BRD thiếu Mục 8/9/10? | **Không liên quan** — đó là mục kỹ thuật cho hệ thống khác |

---

## Hiện trạng codebase vs. yêu cầu

### Đã có
- Entities: `Meeting`, `Shareholder`, `Proxy`, `Ballot`, `AuditLog`, `MeetingResolution`, `MeetingCandidate`, `Template`
- Enums: `BallotStatus`, `MeetingStatus`, `ProxyScope`, `ProxyType`, `AuditCategory`, `TemplateType`
- UI: Meeting CRUD, Shareholder import wizard, Invitation Letters
- Infra: EF Core + PostgreSQL, Identity, Document builder, PrintAgent

### Cần bổ sung

| Thành phần | Loại | Phase |
|-----------|------|-------|
| `ProxyRecipient` | NEW entity | 1 |
| `ProxyStatus` enum | NEW | 1 |
| `Proxy` mở rộng (Status, GranteeShareholderId, GranteeRecipientId) | MODIFY | 1 |
| `AttendanceRecord` | NEW entity | 2 |
| `BallotGroup` | NEW entity | 2 |
| `AttendanceType` enum (F1/F2/F3/F4) | NEW | 2 |
| `BallotType` enum (VotingCard/VotingBallot/ElectionHDQT/ElectionBKS) | NEW | 2 |
| `BallotStatus` mở rộng (+PendingPrint, +Counted, +NotReturned) | MODIFY | 2 |
| `Ballot` mở rộng (+AttendanceRecordId, +BallotType, +SplitSequence) | MODIFY | 2 |
| `Meeting` mở rộng (+GiftEnabled, +DefaultPrintMode, +QuorumThreshold) | MODIFY | 1 |
| `MeetingTemplateConfig` (gán template + QR/Barcode per loại phiếu) | NEW entity | 1 |
| `MeetingCandidate` mở rộng (+CandidateBoard: HDQT/BKS) | MODIFY | 1 |
| `MeetingResolution` mở rộng (+ResolutionType, +ApprovalThreshold) | MODIFY | 1 |
| `TemplateType` cập nhật (7 loại) | MODIFY | 1 |
| `VoteResult` | NEW entity | 3 |
| `ElectionVote` | NEW entity | 3 |
| `TallySnapshot` | NEW entity | 3 |
| `AttendanceSnapshot` | NEW entity | 2 |
| SignalR `CheckinHub` | NEW | 2 |

---

## PHASE 1 — ỦY QUYỀN (Proxy Management)

### 1.1 Data Model

#### [NEW] Entities
- **`ProxyRecipient`**: Id, FullName, IdNumber, Organization?, Position?, PhoneNumber?, PhoneUpdatedAt
- **`MeetingTemplateConfig`**: Id, MeetingId, TemplateType, TemplateId, CodeType(QR/Barcode)

#### [NEW] Enums
- **`ProxyStatus`**: Pending, Confirmed, Cancelled, Superseded
- **`CandidateBoard`**: HDQT, BKS
- **`PrintMode`**: Consolidated, SplitBySource, Hybrid
- **`CodeType`**: QR, Barcode

#### [MODIFY] Existing entities
- **`Proxy`**: +Status(ProxyStatus), +GranteeShareholderId?, +GranteeRecipientId?, +SupersededById?, +CancellationReason
- **`Meeting`**: +GiftEnabled, +DefaultPrintMode, +QuorumThreshold
- **`MeetingResolution`**: +ResolutionType(Normal/Important), +ApprovalThreshold
- **`MeetingCandidate`**: +CandidateBoard(HDQT/BKS), +NumberOfSeats
- **`TemplateType`** enum: InvitationLetter, VotingCard, VotingBallot, ElectionHDQT, ElectionBKS, QuorumReport, TallyReport

### 1.2 Service Layer

#### [NEW] `ProxyService`
| Method | Ràng buộc |
|--------|-----------|
| `CreateProxy()` — tạo UQ, validate CP khả dụng | UQ-01, RB-02 |
| `CancelProxy()` — hủy UQ, hoàn trả CP | UQ-04, trigger Ballot Lifecycle nếu cần |
| `GetAvailableShares()` — CP khả dụng = Tổng − Σ UQ | UQ-06 |
| `ValidateProxyChain()` — kiểm tra 1 tầng | RB-02 |
| `LookupOrCreateRecipient()` — tra/tạo proxy_recipients | UQ-03 |

### 1.3 UI — SC-01 Quản lý Ủy quyền

**Layout**: Topbar 2 dòng + Workspace 2 cột + Drawer

| Khu vực | Nội dung |
|---------|---------|
| Topbar dòng 1 | Tổng CĐ VSDC · Tổng CP BQ (cố định) |
| Topbar dòng 2 | Số CĐ đã UQ · Tổng CP UQ · Số PENDING (động) |
| Cột trái | Tra cứu → Panel 3 dòng trạng thái → Form UQ (slider CP, người nhận, file đính kèm) |
| Cột phải | Danh sách UQ của CĐ đang chọn + nút Hủy |

**5 tình huống**: UQ-1 (toàn bộ), UQ-2 (một phần), UQ-3 (nhiều người), UQ-4 (hủy), UQ-5 (người ngoài VSDC)

---

## PHASE 2 — CHECK-IN & IN PHIẾU

### 2.1 Data Model

#### [NEW] Entities
- **`AttendanceRecord`**: Id, MeetingId, ShareholderId, PhysicalAttendeeIdNumber, PhysicalAttendeeName, AttendanceType, AttendCode, PhoneNumber, PhoneSource, GiftReceived, GiftReceivedAt, GiftReceivedBy, CheckedInAt, PosTerminal, OperatorUserId, Status, CancelReason, Xmin
- **`BallotGroup`**: Id, AttendanceRecordId, BallotId, SourceShareholderId, GroupNumber, CreatedBy, CreatedAt — UNIQUE(AttendanceRecordId, SourceShareholderId) [RB-11]
- **`AttendanceSnapshot`**: Id, MeetingId, SnapshotType(Opening/PreVote), TotalAttendingShareholders, TotalAttendingShares, PercentageQuorum, SnapshotAt, ConfirmedBy

#### [NEW] Enums
- **`AttendanceType`**: F1_Direct, F2_FullProxy, F3_Combined, F4_ProxyOnly
- **`BallotType`**: VotingCard, VotingBallot, ElectionHDQT, ElectionBKS

#### [MODIFY] Existing
- **`BallotStatus`**: PendingPrint, Active, Invalidated, Counted, NotReturned
- **`Ballot`**: +AttendanceRecordId, +BallotType, +SplitSequence?, +IsSplitBallot, +BulkApproved, +ProxyRepresentationNote

### 2.2 Service Layer

#### [NEW] `CheckinService`
| Method | Mô tả |
|--------|-------|
| `IdentifySituation()` | Phân loại CK-1→CK-5 từ trạng thái CĐ |
| `CheckIn()` | Transaction nguyên tử: AttendanceRecord + Ballot(s) + confirm Proxy |
| `ConfigureSplitBallot()` | Phân nhóm phiếu tách, validate SPLIT-RULE-01→04 |
| `GenerateAttendCode()` | Sinh `[Ticker]-[Year]-[00001]` |
| `CollectPhoneNumber()` | 3 nguồn ưu tiên: VSDC → proxy_recipients → manual |
| `ToggleGiftReceived()` | Tick quà + lấy STT VSDC cổ đông gốc |

#### [NEW] `BallotLifecycleService`
| Method | Tình huống |
|--------|-----------|
| `HandleProxyCancellation()` | L1, L2, L6 |
| `HandleOnSiteProxy()` | L3, L4, L7 |
| `HandleWithdrawal()` | L5 |
| `HandleSplitBallotInvalidation()` | L8 |
| `ReconciliationCheck()` | Kiểm tra RB-01 sau mỗi event |

#### [NEW] `PrintService`
| Method | Mô tả |
|--------|-------|
| `PrintBallotPackage()` | In gói 4 loại (Thẻ BQ + Phiếu BQ + Bầu HĐQT + Bầu BKS) trong 1 lệnh |
| `GetReprintQueue()` | Danh sách phiếu cần in lại |
| `ProcessReprint()` | In lại từ đầu queue |

> [!IMPORTANT]
> **Gói in**: Khi check-in, hệ thống in TẤT CẢ loại phiếu áp dụng trong 1 lệnh duy nhất. Template và QR/Barcode lấy từ `MeetingTemplateConfig`. Phiếu bầu HĐQT chỉ in khi Meeting có ứng viên HĐQT; tương tự BKS.

#### [NEW] `CheckinHub` (SignalR)
- `BroadcastTopbarUpdate()` — real-time Topbar 3 dòng
- `BroadcastBallotLifecycleAlert()` — phiếu bị hủy/cần in lại
- `BroadcastSplitReconfigRequired()` — L8
- `BroadcastQuorumReached()` — đủ điều kiện họp

### 2.3 UI — SC-03 Bàn làm việc Check-in

**Layout**: Topbar 3 dòng + Workspace 2 cột + Drawer/Panel

| Khu vực | Nội dung |
|---------|---------|
| Topbar dòng 1 | VSDC cố định: Tổng CĐ · Tổng CP BQ |
| Topbar dòng 2 | Số CĐ check-in · Tổng CP · % · Badge điều kiện họp |
| Topbar dòng 3 | Số người vật lý · Tổng phiếu phát ra |
| Cột trái | QR/Barcode/text tra cứu → Thẻ CĐ → Panel 4 dòng trạng thái → Banner tình huống → Form (SĐT, chế độ in, panel tách phiếu, tick quà) |
| Cột phải | Preview phiếu (4 tab: Thẻ BQ / Phiếu BQ / Bầu HĐQT / Bầu BKS). Nếu tách: N×4 tab |
| Drawer | Danh sách 2 |
| Panel | Reprint Queue |
| Overlay | Ủy quyền tại chỗ (slide phải ~55%) |

**11 tình huống**: F1, F2, F3, F4, SPLIT, CK-4, L-ONSITE, L-CANCEL, LINK, MERGE-AT-COUNTER, RÚT LUI

> [!WARNING]
> **MERGE tại quầy (mới từ BRD v2.3)**: Khi nhân viên quét CCCD/QR phát hiện 2 tài khoản trùng ĐKSH, hệ thống hiển thị cảnh báo vàng với 2 tài khoản cạnh nhau. Nhân viên chọn [Có — MERGE ngay] hoặc [Không — check-in độc lập]. MERGE tại quầy yêu cầu xác nhận kép (Trưởng quầy + 1 thành viên), ghi Audit Log + ảnh CCCD nếu có. Không thể hoàn tác — nếu sai phải Trưởng BTC can thiệp.

### 2.4 UI — SC-07 Thẩm tra tư cách

3 tab Danh sách (DS1/DS2/DS3) + Panel số liệu 3 tầng + Nút chốt lần 1/2 + Snapshot

---

## PHASE 3 — KIỂM PHIẾU & KẾT QUẢ

### 3.1 Data Model

#### [NEW] Entities
- **`VoteResult`**: Id, BallotId, MeetingResolutionId, VoteChoice(Approve/Reject/Abstain/Invalid), VotingShares, BulkApproved, EnteredBy, EnteredAt
- **`ElectionVote`**: Id, BallotId, MeetingCandidateId, Points, EnteredBy, EnteredAt
- **`TallySnapshot`**: Id, MeetingId, SnapshotType, TotalIssued, TotalCounted, TotalNotReturned, TotalInvalid, DenominatorShares, ConfirmedBy1, ConfirmedBy2, LockedAt

### 3.2 Service Layer

#### [NEW] `TallyService`
| Method | Mô tả | Ràng buộc |
|--------|-------|-----------|
| `RecordVote()` | Ghi kết quả 1 phiếu BQ (mặc định Tán thành) | — |
| `RecordElectionVote()` | Ghi điểm bầu cử Cumulative Voting | Tổng điểm ≤ CP × Số ứng viên |
| `MarkBallotCounted()` | Phiếu → COUNTED | — |
| `MarkBallotNotReturned()` | Phiếu → NOT_RETURNED, loại khỏi mẫu số | — |
| `MarkBallotInvalid()` | Không hợp lệ (vẫn vào mẫu số, không vào tử số) | — |
| `GetDenominator()` | Mẫu số = Σ CP phiếu COUNTED (kể cả KHL) | RB-06 |
| `CalculateResult()` | Tỷ lệ % = CP tán thành / Mẫu số × 100 | — |
| `BulkApprove()` | Duyệt nhanh N phiếu tán thành 100% | BA-RULE-01→04, RB-09 |
| `FinalizeTally()` | Chốt + lock — yêu cầu 2 PIN | KP-LOCK |
| `ExportMinutes()` | Xuất biên bản .docx (Template 6) | — |

> [!IMPORTANT]
> **Phiếu bầu HĐQT và BKS kiểm phiếu riêng biệt**: Mỗi loại có grid kết quả riêng, bảng điểm ứng viên riêng. Bulk Approve KHÔNG áp dụng cho phiếu bầu cử (BA-RULE-04).

### 3.3 UI — SC-08 Kiểm phiếu

| Khu vực | Nội dung |
|---------|---------|
| Summary Bar 5 ô | Phiếu phát · Thu về · Chưa thu (cam) · KHL (đỏ) · Mẫu số NQ |
| Workspace | Quét QR → Thẻ phiếu → Grid nội dung BQ (mặc định Tán thành) |
| Tab riêng | Phiếu BQ / Phiếu bầu HĐQT / Phiếu bầu BKS — kiểm riêng |
| Cột phải | Bảng kết quả: % Tán thành / Không TT / Ý kiến khác / KHL + Badge |
| Panel slide | "Phiếu chưa thu về" + cột SĐT + nút sao chép |
| Bulk Approve | Panel riêng — chỉ cho Phiếu BQ, không cho bầu cử |
| Dialog cuối | Tóm tắt + 2 PIN xác nhận → Lock |

**7 tình huống**: KP-1 (tán thành), KP-2 (ý kiến khác), KP-3 (KHL), KP-4 (không thu về), KP-5 (Bulk Approve), KP-6 (bầu cử — tách HĐQT/BKS), KP-7 (hoàn tất)

---

## Ràng buộc hệ thống (RB-01 → RB-12)

| Mã | Mô tả | Cơ chế | Phase |
|----|--------|--------|-------|
| RB-01 | Bảo toàn CP: phiếu ACTIVE = CP check-in | DB constraint + ReconciliationCheck | 2 |
| RB-02 | 1 tầng ủy quyền | Validation ProxyService | 1 |
| RB-03 | Khóa sau khai BQ | MeetingStatus guard | 1,2,3 |
| RB-04 | 1 AttendanceRecord/CĐ/meeting | UNIQUE constraint | 2 |
| RB-05 | Invalidate trước tạo mới | Workflow lock | 2 |
| RB-06 | Mẫu số = CP thu về (kể cả KHL) | TallyService tính động | 3 |
| RB-07 | MERGE được phép **cả trước và tại quầy check-in** — xác nhận kép bắt buộc (Trưởng quầy + 1 thành viên hoặc 2 người phê duyệt độc lập) | Dual confirmation + Audit Log | 1,2 |
| RB-08 | MERGE không hoàn tác — nếu sai phải Trưởng BTC can thiệp thủ công | Immutable after confirm + escape hatch via Trưởng BTC | 1,2 |
| RB-09 | Cấm Bulk Approve phiếu chưa thu | Hard filter | 3 |
| RB-10 | Tổng CP tách = Tổng CP đại diện | Validation | 2 |
| RB-11 | CĐ nguồn unique trong ballot_groups | UNIQUE constraint | 2 |
| RB-12 | SĐT bảo mật theo role | Row-level security | 2 |

---

## Phân quyền

| Nghiệp vụ | NV Check-in | Trưởng quầy | Trưởng BTC | Ban KP |
|-----------|:-----------:|:-----------:|:----------:|:------:|
| Tạo/sửa UQ | ✓ | ✓ | ✓ | ✗ |
| Check-in + in phiếu | ✓ | ✓ | ✓ | ✗ |
| Hủy + in lại phiếu | ✗ | ✓ | ✓ | ✗ |
| Xem SĐT panel phiếu | ✗ | ✓ | ✓ | ✓ |
| Nhập kiểm phiếu | ✗ | ✗ | ✗ | ✓ |
| Chốt thẩm tra | ✗ | ✗ | ✓ | ✓ |
| Tick quà sau check-in | ✗ | ✗ | ✓ | ✗ |

---

## Verification Plan

### Automated Tests
- Unit tests: mỗi validation rule (RB-01→12), BallotLifecycleService (L1→L8), TallyService (mẫu số, ngưỡng)
- Integration: E2E từ UQ → check-in → kiểm phiếu → chốt
- Gói in: verify 4 loại phiếu đúng template + QR/Barcode config

### Manual Verification
- Dev server `dotnet run`, test từng màn hình SC-01/03/07/08
- SignalR real-time giữa 2 tab
- Split Ballot workflow trên UI
- Xuất biên bản .docx với dữ liệu mẫu
- Performance: 1.000 CĐ, 20 POS đồng thời

---

*Tất cả open questions đã được giải đáp. Plan sẵn sàng để triển khai sau khi được phê duyệt.*
