# LUỒNG THÔNG TIN MÀN HÌNH CHECK-IN
## Input - Process - Output (IPO Flow)
**Hệ thống**: Robotia_AGMPro
**Màn hình**: SC-03 Bàn làm việc Check-in
**Phiên bản**: 1.1
**Ngày cập nhật**: 27/04/2026

> **Thay đổi so với v1.0:** Bổ sung nhóm dữ liệu và output mới cho 3 yêu cầu từ thực tế vận hành: (A) Quản lý phiếu tách theo nhóm ý kiến biểu quyết; (B) Thu thập và sử dụng số điện thoại người tham dự; (C) Theo dõi nhận quà tặng và hiển thị STT VSDC ký nhận.

---

## TỔNG QUAN

Màn hình Check-in là điểm giao thoa của nhiều luồng dữ liệu: dữ liệu nền từ VSDC, trạng thái ủy quyền đã ghi nhận trước đó, thao tác xác nhận tại quầy, và đầu ra là phiếu biểu quyết vật lý. Tài liệu này phân tách rõ từng nhóm thông tin theo vai trò: hiển thị tham chiếu (chỉ đọc), thông tin nghiệp vụ (cần xử lý) và kết quả đầu ra (phát sinh từ thao tác).

---

## PHẦN 1 - INPUT (Thông tin đầu vào)

### 1A. Dữ liệu nền - Tham chiếu (chỉ đọc, không thay đổi trong phiên)

| Thông tin | Nguồn gốc | Mục đích hiển thị |
|-----------|-----------|-------------------|
| Tên cuộc họp | Bảng `meetings` | Xác nhận đúng phiên làm việc |
| Ngày giờ họp | Bảng `meetings` | Tham chiếu |
| Tổng số cổ đông có quyền dự họp | File VSDC đã import | Mẫu số tỷ lệ tham dự |
| Tổng số cổ phần có quyền biểu quyết | File VSDC đã import | Mẫu số tỷ lệ CP tham dự |
| Cấu hình quà tặng | Bảng `meetings.gift_enabled` | Xác định có hiển thị bước tick quà không |
| Chế độ in mặc định | Bảng `meetings.default_print_mode` | IN-1 / IN-2 / IN-3 |
| Trạng thái phiên check-in | Bảng `meetings.status` | Xác định nghiệp vụ có được phép không |
| Mã quầy và thiết bị POS hiện tại | Cấu hình hệ thống | Ghi vào Audit Log |

### 1B. Dữ liệu tổng hợp - Real-time (cập nhật sau mỗi giao dịch)

Hiển thị trên Topbar, cập nhật tức thì sau mỗi check-in thành công.

**Dòng 1 - Cơ sở VSDC (cố định):**
- Tổng số cổ đông theo VSDC
- Tổng CP có quyền biểu quyết

**Dòng 2 - Thực tế check-in (động):**
- Số cổ đông đã check-in hợp lệ (đếm theo cổ đông VSDC, không phải người vật lý)
- Tổng CP đang được đại diện (= tổng CP của phiếu ACTIVE)
- Tỷ lệ tham dự (%) = CP đang đại diện / Tổng CP VSDC × 100
- Trạng thái điều kiện họp: đủ điều kiện (≥ 50%) hay chưa đủ

**Dòng 3 - Kiểm soát phiếu (động) *(MỚI v1.1):***
- Tổng người tham dự trực tiếp (đếm theo người vật lý, theo CMND)
- Tổng phiếu đã phát ra (có thể > tổng người vật lý khi có phiếu tách)
- Chênh lệch: nếu phiếu > người → hiển thị "(trong đó có phiếu tách)"

### 1C. Dữ liệu tra cứu theo cổ đông - Kích hoạt khi quét/nhập

**Thông tin cổ đông từ VSDC:**
- Họ tên
- Số CMND/CCCD/Hộ chiếu (Số ĐKSH)
- Ngày cấp
- Tổng số CP có quyền biểu quyết (Cột 16 VSDC)
- Số điện thoại (Cột 9 VSDC) *(MỚI v1.1 — tự động lấy và điền vào trường SĐT)*
- Quốc tịch (để xác định template in song ngữ hay không)
- STT trên danh sách VSDC (Cột 1) *(MỚI v1.1 — dùng cho ký nhận quà)*

**Trạng thái hiện tại của cổ đông:**
- Đã check-in chưa (nếu rồi: lúc mấy giờ, quầy nào)
- Đã ủy quyền chưa (nếu rồi: bao nhiêu CP, cho ai, trạng thái)
- Có nhận ủy quyền từ người khác không (nếu có: từ ai, bao nhiêu CP)
- Có phiếu ACTIVE không (nếu có: mã phiếu, số phiếu, có phiếu tách không)
- Cờ trùng ĐKSH: có bản ghi LINK hay không

**Dữ liệu ủy quyền liên quan:**
- Danh sách bản ghi ủy quyền PENDING/CONFIRMED (cả chiều đi và chiều nhận)
- CP khả dụng = Tổng CP − CP đã ủy quyền

**Dữ liệu tra cứu bổ sung cho người được ủy quyền không có trong VSDC *(MỚI v1.1):***
- Kiểm tra bảng `proxy_recipients` xem đã có số điện thoại từ lần trước chưa
- Nếu có: điền sẵn vào trường SĐT với nguồn "proxy_recipients"
- Nếu không: trường SĐT trống, cho phép nhập tay

---

## PHẦN 2 - PROCESS (Xử lý nghiệp vụ)

### 2A. Luồng quyết định chính

```
[Tra cứu cổ đông / người được UQ]
         │
         ▼
[Phân loại tình huống: F1 / F2 / F3 / F4 / CK-4 / LINK]
         │
         ▼
[Nếu F2/F4 với ≥ 2 cổ đông UQ]
    → Hỏi: Tách nhóm ý kiến không?
    → Nếu Có: Tình huống SPLIT (xem 2B)
    → Nếu Không: Chế độ in chuẩn (Gộp / Tách nguồn)
         │
         ▼
[Thu thập SĐT (2C)]
         │
         ▼
[Preview phiếu → Xác nhận → In phiếu]
         │
         ▼
[Nếu đại hội có quà tặng: Bước tick quà (2D)]
```

### 2B. Luồng cấu hình tách phiếu theo nhóm ý kiến *(MỚI v1.1)*

```
INPUT: Danh sách cổ đông UQ đã xác nhận (≥ 2 cổ đông)

PROCESS:
1. Mở panel cấu hình nhóm inline (không chuyển màn hình)
2. Hiển thị từng cổ đông UQ dưới dạng card kéo-thả
3. Nhân viên tạo nhóm theo lời khai của người tham dự
4. Hệ thống validate real-time:
   - Tổng CP phân bổ phải = Tổng CP người đó đại diện (SPLIT-RULE-01)
   - Mỗi cổ đông chỉ xuất hiện 1 nhóm (SPLIT-RULE-02)
   - Số dư CP chưa phân bổ phải = 0 trước khi xác nhận
5. Nhân viên xác nhận → lưu vào bảng ballot_groups

OUTPUT:
- N nhóm, mỗi nhóm = 1 phiếu sẽ in ra
- Mã phiếu: [Mã tham dự]-[Số thứ tự]: AST-2026-00089-1, -2, -3
- Audit Log ghi flag split_ballot = true
```

### 2C. Luồng thu thập số điện thoại *(MỚI v1.1)*

```
INPUT: Kết quả tra cứu cổ đông / người được UQ

PROCESS:
1. Kiểm tra nguồn SĐT theo thứ tự ưu tiên:
   Ưu tiên 1: Cột 9 file VSDC (nếu người đó là cổ đông trong VSDC)
   Ưu tiên 2: Bảng proxy_recipients (nếu đã lưu trước đó)
   Ưu tiên 3: Trống — nhân viên nhập tay (không bắt buộc)

2. Hiển thị trường SĐT trong form confirm trước khi in:
   - SĐT tự động: "SĐT (VSDC): 09xx xxx xxx" (xám, có thể sửa)
   - SĐT từ proxy_recipients: "SĐT (đã lưu): 09xx xxx xxx" (xám, có thể sửa)
   - SĐT trống: placeholder "Nhập SĐT (không bắt buộc)"

3. Khi commit transaction check-in:
   - Lưu SĐT vào attendance_records.phone_number
   - Ghi phone_source: 'VSDC' / 'PROXY_RECIPIENT' / 'MANUAL'
   - Nếu người này không có trong VSDC (người được UQ bên ngoài):
     đồng thời cập nhật proxy_recipients.phone_number

OUTPUT:
- SĐT lưu trong attendance_records (dùng trong Module Kiểm phiếu)
- SĐT lưu trong proxy_recipients nếu áp dụng (dùng cho tương lai)
- phone_source ghi lại để audit
```

### 2D. Luồng xử lý quà tặng *(MỚI v1.1)*

```
INPUT: Attendance Record vừa tạo (sau khi check-in thành công)
       Cấu hình meetings.gift_enabled = true

PROCESS:
1. Nhân viên tick "Đã nhận quà" trên màn hình

2. Hệ thống tra danh sách cổ đông gốc mà người này đại diện:
   - Nếu tự đến (F1): 1 cổ đông gốc = chính họ
   - Nếu nhận UQ (F2/F4): N cổ đông gốc = danh sách ủy quyền
   - Nếu phiếu tách: N cổ đông gốc theo nhóm ballot_groups

3. Với mỗi cổ đông gốc, lấy STT VSDC (Cột 1 file VSDC)

4. Hiển thị panel STT VSDC để hướng dẫn ký nhận:
   Ví dụ: STT 124 (Quỹ A) · STT 215 (Quỹ B) · STT 389 (Quỹ C)...

5. Ghi vào attendance_records:
   gift_received = TRUE
   gift_received_at = now()
   gift_received_by = mã nhân viên

OUTPUT:
- attendance_records cập nhật gift_received = TRUE
- Panel STT VSDC hiển thị để hướng dẫn ký nhận trên danh sách giấy
- Nút "In thông tin này" in tờ hướng dẫn ký nhận nhỏ
- Danh sách 2 cập nhật cột trạng thái quà: ✅ Đã nhận
```

### 2E. Validation chuỗi xử lý

Trước khi commit transaction check-in, hệ thống kiểm tra tuần tự:

| # | Validation | Cơ chế | Kết quả nếu fail |
|---|-----------|--------|-----------------|
| V1 | Cổ đông chưa check-in (RB-04) | Query Attendance Record ACTIVE | Chặn, hiển thị thông tin lần check-in trước |
| V2 | Bảo toàn cổ phần (RB-01) | Tính tổng CP phiếu sẽ tạo = tổng CP người đó đại diện | Chặn, hiển thị số lệch |
| V3 | Không trùng cổ đông nguồn trong nhóm phiếu tách (RB-11) | Unique check trên ballot_groups | Chặn, highlight cổ đông bị trùng |
| V4 | Tổng CP phiếu tách = Tổng CP đại diện (RB-10) | Tính CP còn lại chưa phân bổ | Chặn, hiển thị số CP còn lại |
| V5 | Phiếu cũ đã INVALIDATED trước khi tạo mới (RB-05) | Kiểm tra trạng thái phiếu cũ | Chặn, yêu cầu hủy phiếu cũ trước |
| V6 | Ràng buộc 1 tầng ủy quyền (RB-02) | Kiểm tra chuỗi ủy quyền | Chặn với giải thích rõ vi phạm |

---

## PHẦN 3 - OUTPUT (Kết quả đầu ra)

### 3A. Phiếu vật lý

**Phiếu thông thường (1 cổ đông / 1 lô ủy quyền):**
- Tên người tham dự (font lớn)
- Ghi chú đại diện (nếu là ủy quyền): "Đại diện ủy quyền: [Tên cổ đông gốc]"
- Mã tham dự: `AST-2026-00089` (font mono, lớn)
- Số CP biểu quyết (số rất lớn)
- QR code + Barcode
- Tên công ty, tên cuộc họp, ngày họp

**Phiếu tách (Split Ballot) *(MỚI v1.1):***
- Tên người tham dự (font lớn): "HSBC Securities (Vietnam) Company Limited"
- Ghi chú đại diện: "(đại diện ủy quyền: Quỹ A Vietnam Equity Fund, Quỹ B Dragon Capital II)"
- Mã tham dự: `AST-2026-00089-1` (font mono, lớn — hậu tố phân biệt rõ)
- Số CP biểu quyết của nhóm này: 3.000.000
- QR code mã hóa mã phiếu tách (không phải mã gốc)
- Góc phiếu có badge nhỏ: "Phiếu 1/3" để nhận diện trực quan

### 3B. Cập nhật dữ liệu hệ thống

| Bảng | Thay đổi |
|------|---------|
| `attendance_records` | Tạo mới 1 bản ghi; ghi phone_number, phone_source, gift_received |
| `ballots` | Tạo 1 hoặc nhiều bản ghi (trạng thái PENDING_PRINT → ACTIVE sau khi in xong) |
| `ballot_groups` | Tạo bản ghi phân nhóm nếu có phiếu tách |
| `proxy_records` | Cập nhật trạng thái ủy quyền: PENDING → CONFIRMED |
| `proxy_recipients` | Cập nhật phone_number nếu người được UQ không có trong VSDC |
| `audit_log` | Ghi đầy đủ: tình huống, người thực hiện, timestamp, quầy, phiếu tạo ra |

### 3C. Cập nhật Topbar real-time

Sau mỗi giao dịch check-in thành công, Topbar cập nhật tức thì:
- +[N] cổ đông (tính theo cổ đông VSDC đã check-in)
- +[M] phiếu (M có thể > N nếu có phiếu tách)
- +[X] CP (tổng CP đại diện thêm)
- Tỷ lệ % tính lại
- Badge điều kiện họp cập nhật nếu vượt ngưỡng

### 3D. Alert và thông báo

**Trường hợp Ballot Lifecycle kích hoạt:**
SignalR broadcast đến tất cả thiết bị: "Phiếu [Mã] của [Tên] đã bị hủy. Cần in phiếu mới."

**Trường hợp phiếu tách bị hủy do Ballot Lifecycle (L8):**
SignalR broadcast: "⚡ Cần cấu hình lại phiếu tách — [Tên người tham dự] — [Tổng CP]. Đề nghị ra quầy."

**Trường hợp đủ điều kiện họp (vượt ngưỡng 50%):**
Thông báo tự động đến màn hình SC-06 và SC-07.

**Trường hợp Reconciliation Check phát hiện lệch:**
Alert đỏ trên màn hình Supervisor.

### 3E. Dữ liệu cho các màn hình khác (downstream)

| Màn hình | Dữ liệu nhận |
|---------|-------------|
| SC-07 Thẩm tra tư cách | Tổng hợp 3 Danh sách real-time: CĐ dự họp / Người vật lý + phiếu / CĐ biểu quyết |
| SC-08 Kiểm phiếu | Danh sách phiếu ACTIVE (bao gồm phiếu tách) + SĐT từ attendance_records |
| Báo cáo cuối | Danh sách tham dự trực tiếp đầy đủ, tổng phiếu phát ra, trạng thái quà tặng, audit trail |

---

## PHẦN 4 - THÔNG TIN CHỈ ĐỌC TRÊN BÀN LÀM VIỆC

### Nhóm A - Topbar (3 dòng) *(CẬP NHẬT v1.1)*

Dòng 1 (xám, cố định): Tổng CĐ VSDC · Tổng CP BQ

Dòng 2 (lớn, màu, động): Số CĐ đã check-in và tỷ lệ % · Tổng CP đại diện và tỷ lệ % · Badge điều kiện họp

Dòng 3 (nhỏ, động, MỚI): Số người vật lý · Tổng phiếu đã phát *(để phát hiện ngay khi số phiếu > số người)*

### Nhóm B - Khu vực bàn làm việc (sau khi tra cứu)

**Thẻ cổ đông (sau tra cứu):**
- Tên đầy đủ (font lớn)
- CMND/CCCD + ĐKSH + Ngày cấp
- Tổng CP, CP tự tham dự, CP đã ủy quyền
- SĐT (từ nguồn nào: VSDC / đã lưu / trống) *(MỚI v1.1)*
- Badge trạng thái hiện tại

**Banner tình huống (xác định tự động):**
- Loại tình huống (F1/F2/F3/F4)
- Số CP theo từng phần
- Cảnh báo đặc biệt nếu có

**Panel cấu hình tách phiếu *(MỚI v1.1 — chỉ hiện khi F2/F4 với ≥ 2 cổ đông UQ):***
- Câu hỏi: "Tách phiếu theo nhóm ý kiến biểu quyết?"
- Nếu "Có": Panel kéo-thả cổ đông vào nhóm
- Counter CP real-time: "[X] CP đã phân bổ / [Y] CP tổng / [Z] CP chưa phân bổ"

**Panel quà tặng *(MỚI v1.1 — chỉ hiện khi gift_enabled = true):***
- Ô tick "Đã nhận quà"
- Sau khi tick: Panel STT VSDC hiện ra với danh sách cổ đông gốc và STT tương ứng

### Nhóm C - Nút/phím tắt

- "Đã check-in ([N] người · [M] phiếu)" → mở drawer Danh sách 2 *(CẬP NHẬT v1.1)*
- "Reprint Queue ([N])" → mở panel hàng đợi in lại
- "Ủy quyền hôm nay ([N])" → mở danh sách ủy quyền trong ngày

---

## PHẦN 5 - SƠ ĐỒ LUỒNG TÓM TẮT *(CẬP NHẬT v1.1)*

```
INPUT                          PROCESS                        OUTPUT
─────────────────────────────────────────────────────────────────────────

[VSDC Data]                    Topbar 3 dòng tính             Topbar cập nhật:
Tổng CĐ, Tổng CP,  ──────────► real-time (CĐ / CP /     ────► % tham dự + badge
Cột 9 SĐT, Cột 1 STT          người vật lý / phiếu)          + số phiếu vs số người

[QR / CMND input]              Tra cứu DB:                    Thẻ cổ đông + Banner
Nhân viên quét     ──────────► - Phân loại tình huống    ────► + SĐT tự động
                               - Lấy SĐT từ VSDC/proxy        + STT VSDC
                               - Kiểm tra cờ LINK             + Preview phiếu

[Xác nhận UQ + chế độ in]      Nếu ≥ 2 UQ: hỏi tách          Panel cấu hình nhóm
Nhân viên chọn     ──────────► nhóm ý kiến               ────► hiển thị (nếu áp dụng)
                               Nếu tách: cấu hình nhóm        Preview N tab phiếu

[Xác nhận check-in]            Transaction nguyên tử:         N phiếu vật lý in ra
Nhân viên bấm      ──────────► - Tạo Attendance Record   ────► (1 phiếu thông thường
Enter/F5                       - Lưu SĐT (phone_source)        hoặc ≥ 2 phiếu tách
                               - Tạo ballot(s) ACTIVE          mã AST-xxxx-1/-2/-3)
                               - Tạo ballot_groups nếu tách    Audit Log ghi vào DB
                               - Xác nhận UQ → CONFIRMED       Topbar cập nhật 3 dòng
                               - Ghi Audit Log
                               - Reconciliation Check

[Tick quà *(MỚI)*]             Lấy STT VSDC của               Panel STT ký nhận:
Nhân viên tick     ──────────► tất cả cổ đông gốc        ────► "Ký tại STT 124, 215..."
"Đã nhận quà"                  Ghi gift_received + ts          Nút "In hướng dẫn"

[Sự kiện thay đổi]             Ballot Lifecycle Cascade:      Reprint Queue cập nhật
Hủy UQ / UQ mới   ──────────► Invalidate phiếu cũ        ────► Alert SignalR
Rút lui                        Nếu phiếu tách: L8             Nếu L8: yêu cầu
                               Broadcast alert                  cấu hình lại nhóm
                               Tạo phiếu mới PENDING_PRINT
```

---

*Tài liệu này mô tả luồng IPO của màn hình SC-03. Đọc kết hợp với BRD kỹ thuật (brd-uy-quyen-checkin-kiemphieu.md v1.3) và IPO Check-in v2.2 để có đầy đủ ngữ cảnh nghiệp vụ.*
*Phiên bản 1.1 — Cập nhật 27/04/2026.*
