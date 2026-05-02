# LUỒNG IPO - MÀN HÌNH CHECK-IN
**Hệ thống**: Robotia_AGMPro | **Màn hình**: SC-03 | **Phiên bản**: 2.2
**Ngày cập nhật**: 27/04/2026

> **Thay đổi so với v2.1:** Bổ sung (A) Tình huống SPLIT — tách phiếu theo nhóm ý kiến biểu quyết; (B) Bước thu thập số điện thoại trong mọi tình huống check-in; (C) Bước tick nhận quà tặng và hiển thị STT VSDC ký nhận; (D) Cập nhật Topbar bổ sung bộ đếm "Phiếu phát ra".

---

## PHẦN 1 - THÔNG TIN THAM CHIẾU CHUNG (Header / Topbar)

Hiển thị thường trực trên Topbar, không phụ thuộc tình huống, không yêu cầu thao tác. Cập nhật real-time sau mỗi giao dịch check-in thành công.

**Dòng 1 - Số cố định từ VSDC (không đổi trong phiên):**
- Tổng số cổ đông có quyền dự họp
- Tổng cổ phần có quyền biểu quyết

**Dòng 2 - Số động (cập nhật sau mỗi giao dịch):**
- Số cổ đông đã check-in hợp lệ + tỷ lệ % + mini progress bar
- Tổng CP đang được đại diện + tỷ lệ % + mini progress bar
- Badge điều kiện họp: đỏ (<50%) / cam (50–55%) / xanh (>55%)

**Thông tin phiên làm việc (góc phải):**
- Tên cuộc họp, ngày họp
- Mã quầy + tên nhân viên đang đăng nhập
- Trạng thái phiên check-in (Đang mở / Đã đóng)

**Thông tin tham chiếu ẩn vào nút (không chiếm bàn làm việc):**
- Nút "Đã check-in ([N] người · [M] phiếu)" → mở drawer Danh sách 2 *(CẬP NHẬT: hiển thị cả số người và số phiếu)*
- Nút "Reprint Queue ([N])" → mở panel hàng đợi in lại (cam khi N > 0)
- Nút "Ủy quyền hôm nay ([N])" → mở danh sách ủy quyền trong ngày

*Lý do hiển thị cả "người" và "phiếu" trên nút:* Khi có phiếu tách, số phiếu > số người tham dự. Hai con số này là cơ sở của Danh sách 2 và là số liệu then chốt để kiểm soát phiếu phát ra.

---

## PHẦN 2 - LUỒNG IPO THEO TỪNG TÌNH HUỐNG

---

### TÌNH HUỐNG 0 - TRA CỨU CỔ ĐÔNG (bước chung, xảy ra trước mọi tình huống)

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Nhân viên quét QR / Barcode trên thư mời, hoặc gõ CMND/CCCD/ĐKSH/Tên vào ô tìm kiếm | Truy vấn DB theo định danh. Nếu tìm thấy nhiều kết quả: hiển thị dropdown chọn | Thẻ cổ đông hiện ra: Tên (font lớn), CMND, ĐKSH, Ngày cấp, Tổng CP |
| 2 | Kết quả tra cứu từ bước 1 | Kiểm tra đồng thời 4 điều kiện và hiển thị kết quả từng điều kiện lên màn hình dưới dạng checklist 4 dòng | Panel "Trạng thái cổ đông" hiện ngay dưới thẻ cổ đông với 4 dòng trạng thái (xem chi tiết bên dưới) |
| 3 | Panel trạng thái 4 dòng đã hiện đủ | Hệ thống tổng hợp 4 điều kiện → xác định tình huống phù hợp nhất | Banner tình huống hiện ra (xanh/cam/đỏ/vàng) với hướng dẫn hành động tiếp theo |
| 4 | Tình huống đã xác định | Tải template phiếu tương ứng, điền sẵn thông tin cổ đông và mã tham dự dự kiến | Preview phiếu BQ + Phiếu bầu cử + Thẻ BQ hiện ở cột phải |

**Nội dung Panel "Trạng thái cổ đông" — 4 dòng:**

| Điều kiện | Bình thường | Có vấn đề |
|-----------|-------------|-----------|
| (a) Trạng thái check-in | ✓ Chưa check-in (xanh) | ⚠ Đã check-in lúc 08:32 tại Quầy 2 (cam) |
| (b) Ủy quyền đi | - Không có ủy quyền đi (xám) | ⚠ Đã ủy quyền 200.000 CP cho Lê Văn Tuấn — CONFIRMED (cam) |
| (c) Nhận ủy quyền | - Không nhận ủy quyền từ ai (xám) | ℹ Đang nhận ủy quyền từ 3 cổ đông — tổng 1.500.000 CP (xanh dương) |
| (d) Trùng ĐKSH | - Không có cờ trùng ĐKSH (xám) | ⚠ Phát hiện tài khoản LINK cùng CMND — ngày cấp khác (vàng) |

---

### TÌNH HUỐNG PREVIEW - XEM TRƯỚC PHIẾU TRƯỚC KHI IN

*Áp dụng cho mọi tình huống F1/F2/F3/F4 sau khi hệ thống đã xác định đủ thông tin.*

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Hệ thống đã xác định tình huống và tính được số CP, chế độ in, số phiếu (1 hay nhiều nếu tách) | Tự động render preview. Điền sẵn: Tên người biểu quyết, CMND, Số CP, ghi chú đại diện cho ai (nếu UQ), Mã tham dự dự kiến, QR code mẫu | Preview hiện ở cột phải. Nếu nhiều phiếu: tab chuyển giữa Phiếu 1 / Phiếu 2 / Phiếu 3... Tab chung: [Thẻ biểu quyết] [Phiếu biểu quyết] [Phiếu bầu cử] |
| 2 | Nhân viên muốn ẩn preview | Bấm "▼ Ẩn preview". Toggle không ảnh hưởng dữ liệu | Cột phải thu gọn. Trạng thái lưu cho phiên làm việc |
| 3 | Nhân viên phát hiện sai thông tin | Điều chỉnh ở cột trái (chọn lại chế độ in, xác nhận lại ủy quyền, cấu hình lại nhóm phiếu tách) | Preview tự cập nhật real-time. Không cần reload |
| 4 | Nhân viên bấm "Check-in & In phiếu" | Hệ thống dùng đúng template và dữ liệu đang preview để tạo file in chính thức. Mã tham dự được sinh chính thức | Phiếu vật lý in ra khớp 100% với preview |

**Nội dung bắt buộc trên từng tab preview:**

| Tab | Thông tin bắt buộc hiển thị |
|-----|---------------------------|
| Thẻ biểu quyết | Tên người tham dự · Ghi chú đại diện cho ai (nếu UQ) · Mã tham dự (font lớn, mono, kèm hậu tố -1/-2 nếu tách) · Số CP biểu quyết · QR code · Tên công ty · Tên cuộc họp |
| Phiếu biểu quyết | Tên người biểu quyết · Đại diện cho: [tên cổ đông gốc, cách nhau dấu phẩy] · Mã tham dự · Số CP · Nội dung tờ trình với 3 ô · QR code · Thời gian in · Mã quầy |
| Phiếu bầu cử | Tên người biểu quyết · Tổng điểm bầu (= Số CP × Số ứng viên) · Danh sách ứng viên · QR code |

**Lưu ý ngôn ngữ:** Preview hiển thị đúng ngôn ngữ sẽ in (tiếng Việt hoặc song ngữ Việt-Anh) dựa trên quốc tịch cổ đông từ Cột 10 file VSDC.

---

### TÌNH HUỐNG F1 - CỔ ĐÔNG TRỰC TIẾP TOÀN PHẦN

*Cổ đông đến trực tiếp, không có ủy quyền đi, không nhận ủy quyền từ ai.*

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Kết quả tra cứu: cổ đông chưa check-in, không có ủy quyền liên quan | Xác nhận điều kiện đơn giản nhất | Banner xanh lá "Tham dự trực tiếp toàn phần · [Số CP] CP". Nút "Check-in & In phiếu" active ngay |
| 2 | *(Bước bổ sung v2.2)* Hệ thống kiểm tra Cột 9 VSDC | Nếu Cột 9 có số điện thoại: điền sẵn vào trường SĐT và hiển thị "SĐT (từ VSDC): 09xx xxx xxx". Nếu không có: trường SĐT trống, placeholder "Nhập SĐT (không bắt buộc)" | Trường SĐT hiển thị trong form confirm trước khi in. Nhân viên có thể sửa hoặc bổ sung |
| 3 | Nhân viên bấm "Check-in & In phiếu" (hoặc Enter) | Transaction nguyên tử: (1) Tạo Attendance Record hình thức F1, (2) Lưu SĐT vào attendance_records.phone_number + ghi phone_source, (3) Sinh mã tham dự, (4) Tạo phiếu ACTIVE, (5) Ghi Audit Log, (6) Reconciliation Check RB-01 | Giao dịch commit. Lệnh in gửi máy in. Mã tham dự hiển thị to trên màn hình xác nhận |
| 4 | Phiếu in xong | Cập nhật trạng thái phiếu PENDING_PRINT → ACTIVE. *(Nếu đại hội có quà tặng: hiển thị bước 5)* | Topbar cập nhật: +1 CĐ, +1 phiếu, +[Số CP] CP. Màn hình sẵn sàng cổ đông tiếp theo |
| 5 *(nếu có quà tặng)* | Nhân viên tick "Đã nhận quà" | Ghi gift_received = TRUE + timestamp + mã nhân viên | Panel STT VSDC hiện ra: "Ký nhận tại STT [N] trên danh sách VSDC in sẵn". Nút "In thông tin này" để in tờ hướng dẫn ký nhận |

---

### TÌNH HUỐNG F2 - ỦY QUYỀN TOÀN PHẦN (người nhận ủy quyền đến thay)

*Cổ đông A không đến. Người B đến với tư cách đại diện nhận ủy quyền toàn bộ CP của A (hoặc nhiều cổ đông).*

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Tra cứu theo CMND của người B | Tìm kiếm bản ghi ủy quyền PENDING/CONFIRMED có người nhận = B. Có thể tìm thấy nhiều ủy quyền từ nhiều cổ đông | Hiển thị danh sách ủy quyền đang chờ B: mỗi dòng gồm Tên CĐ ủy quyền + Số CP + Trạng thái |
| 2 | Nhân viên xác nhận từng ủy quyền (tick hoặc bỏ tick) | Tính tổng CP B sẽ đại diện. Kiểm tra RB-02. Xác định chế độ in mặc định theo cấu hình cuộc họp | Tổng hợp "B sẽ đại diện [Tổng CP] CP cho [N] cổ đông". Banner xanh dương |
| 3 *(MỚI v2.2 — nếu B nhận UQ từ ≥ 2 cổ đông)* | Hệ thống hỏi: "Người tham dự có muốn tách phiếu theo nhóm ý kiến biểu quyết không?" | Nếu "Không": áp dụng chế độ in mặc định (Gộp hoặc Tách theo cổ đông nguồn). Nếu "Có": chuyển sang Tình huống SPLIT (xem dưới) | Câu hỏi hiển thị rõ ràng với 2 nút: [Không — gộp / tách chuẩn] và [Có — cấu hình nhóm ý kiến] |
| 4 | *(Bước bổ sung v2.2)* Kiểm tra SĐT | Nếu B có trong VSDC: lấy Cột 9. Nếu B không có trong VSDC (người được UQ bên ngoài): kiểm tra bảng proxy_recipients xem đã có SĐT chưa, nếu có thì điền sẵn, nếu không thì trống cho nhập | Trường SĐT với nguồn tương ứng (VSDC / proxy_recipients / trống) |
| 5 | Nhân viên bấm "Check-in & In phiếu" sau khi đã cấu hình xong | Transaction: (1) Tạo Attendance Record F4 cho B, (2) Lưu SĐT vào attendance_records + proxy_recipients nếu B không có trong VSDC, (3) Sinh mã tham dự, (4) Tạo phiếu ACTIVE theo cấu hình (gộp / tách / nhóm ý kiến), (5) Xác nhận ủy quyền → CONFIRMED, (6) Ghi Audit Log | Phiếu in ra. Topbar: +[N CĐ], +[N phiếu], +[Tổng CP] |
| 6 *(nếu có quà tặng)* | Nhân viên tick "Đã nhận quà" | Ghi gift_received + timestamp | Panel STT VSDC: hiển thị STT của TẤT CẢ cổ đông gốc B đại diện để ký nhận trên danh sách giấy |

---

### TÌNH HUỐNG F3 - KẾT HỢP (cổ đông giữ một phần, ủy quyền một phần)

*Cổ đông A đến tự tham dự một phần cổ phần, đã ủy quyền phần còn lại cho B.*

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Tra cứu A: chưa check-in, đang có ủy quyền PENDING/CONFIRMED một phần cho B | Phát hiện tình huống kết hợp: tính CP giữ lại = Tổng CP − CP đã ủy quyền | Banner cam "Tham dự kết hợp · [CP giữ lại] CP trực tiếp + [CP ủy quyền] CP đã ủy quyền cho B" |
| 2 | Nhân viên xác nhận thông tin | Validate CP giữ lại > 0. Tạo phiếu chỉ cho phần CP giữ lại của A | Preview phiếu của A với CP giữ lại. Ghi chú "Phần ủy quyền [Y] CP sẽ được in khi B check-in" |
| 3 | *(Bước bổ sung v2.2)* Kiểm tra SĐT của A từ VSDC Cột 9 | Điền sẵn nếu có, trống nếu không | Trường SĐT hiển thị để xác nhận/bổ sung |
| 4 | Nhân viên bấm "Check-in & In phiếu" | Transaction: (1) Tạo Attendance Record F3 cho A, (2) Lưu SĐT, (3) Sinh mã tham dự của A, (4) Tạo phiếu A với CP giữ lại, (5) Ghi Audit Log | Phiếu A in ra với CP giữ lại. Topbar: +1 CĐ (A), +1 phiếu, +CP giữ lại |
| 5 *(nếu có quà tặng)* | Nhân viên tick "Đã nhận quà" cho A | Ghi gift_received | Panel STT VSDC: STT của A trên danh sách VSDC |

---

### TÌNH HUỐNG F4 - NGƯỜI NHẬN ỦY QUYỀN VÀ CỔ ĐÔNG TRỰC TIẾP

*Người C vừa là cổ đông (có CP riêng) vừa nhận ủy quyền từ D.*

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Tra cứu C: có CP riêng trong VSDC + có ủy quyền PENDING từ D | Phân biệt 2 phần: (a) CP riêng của C, (b) CP nhận ủy quyền từ D | Banner xanh dương "Cổ đông trực tiếp [X] CP + Nhận ủy quyền từ D [Y] CP · Tổng [X+Y] CP" |
| 2 | Nhân viên xác nhận cả 2 phần | Validate tổng CP = X + Y | Hệ thống hỏi chế độ in: [Gộp: 1 phiếu [X+Y] CP] hoặc [Tách: 2 phiếu riêng] |
| 3 *(MỚI v2.2)* | *(Nếu D là 1 trong nhiều người UQ và C nhận UQ từ ≥ 2 người)* Hệ thống hỏi có muốn tách nhóm ý kiến không | Nếu "Có": chuyển sang Tình huống SPLIT cho phần nhận UQ | Câu hỏi tách nhóm ý kiến chỉ áp dụng cho phần nhận UQ, không áp dụng cho phần CP riêng của C |
| 4 | *(Bước bổ sung v2.2)* Kiểm tra SĐT của C từ VSDC Cột 9 | Điền sẵn nếu có | Trường SĐT hiển thị |
| 5 | Nhân viên bấm "Check-in & In phiếu" | Transaction: Tạo Attendance Record F1 cho C (phần riêng) + F4 (phần UQ), sinh mã tham dự, tạo phiếu theo chế độ in, xác nhận UQ từ D → CONFIRMED, lưu SĐT, ghi Audit Log | Phiếu in ra. Topbar: +C (1 CĐ cho CP riêng) + D (1 CĐ cho CP UQ) |
| 6 *(nếu có quà tặng)* | Nhân viên tick "Đã nhận quà" | Ghi gift_received | Panel STT VSDC: STT của C (CP riêng) + STT của D (CP ủy quyền) |

---

### TÌNH HUỐNG SPLIT - TÁCH PHIẾU THEO NHÓM Ý KIẾN BIỂU QUYẾT *(MỚI - v2.2)*

*Người tham dự nhận ủy quyền từ ≥ 2 cổ đông và có dự định biểu quyết khác nhau theo từng nhóm cổ đông.*

**Điều kiện kích hoạt:** Người tham dự chọn "Có — cấu hình nhóm ý kiến" từ câu hỏi tách phiếu trong Tình huống F2, F4, hoặc CK-2.

**Lưu ý thiết kế:** Tình huống này xử lý qua **bước cấu hình nhóm** chèn vào giữa bước xác nhận ủy quyền và bước in phiếu. Không chuyển sang màn hình khác — thao tác xảy ra inline trên cùng màn hình SC-03.

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Người tham dự chọn "Có — cấu hình nhóm ý kiến" | Mở panel cấu hình nhóm phiếu ngay trong màn hình. Panel hiển thị danh sách các cổ đông ủy quyền cho người này, mỗi cổ đông là 1 dòng có thể kéo-thả vào nhóm | Panel cấu hình mở với danh sách cổ đông UQ: Quỹ A (1.200.000 CP) · Quỹ B (800.000 CP) · Quỹ C (1.000.000 CP) · Quỹ D (700.000 CP) · Quỹ E (800.000 CP). Bên phải: "Nhóm 1 (trống) · Nhóm 2 (trống)..." |
| 2 | Nhân viên kéo-thả hoặc tick từng cổ đông vào nhóm tương ứng (theo lời khai của người tham dự) | Mỗi lần phân bổ: hệ thống tính lại CP của từng nhóm + số CP chưa phân bổ (phải = 0 trước khi xác nhận). Nút "Thêm nhóm" nếu cần nhiều hơn 2 nhóm | Cột phải hiển thị live: Nhóm 1: Quỹ A + B + C = 3.000.000 CP · Nhóm 2: Quỹ D = 700.000 CP · Nhóm 3: Quỹ E = 800.000 CP · Chưa phân bổ: 0 CP ✓ |
| 3 | Nhân viên xác nhận phân nhóm | Validate SPLIT-RULE-01 (tổng CP khớp) và SPLIT-RULE-02 (không trùng lặp cổ đông). Hệ thống tính số phiếu sẽ tạo | Bảng xác nhận: "Sẽ in [3] phiếu:" · Phiếu 1: HSBC (đại diện Quỹ A, Quỹ B, Quỹ C) — 3.000.000 CP · Phiếu 2: HSBC (đại diện Quỹ D) — 700.000 CP · Phiếu 3: HSBC (đại diện Quỹ E) — 800.000 CP · Tổng: 4.500.000 CP ✓ |
| 4 | Nhân viên bấm "Xác nhận phân nhóm" | Lưu cấu hình nhóm vào bảng ballot_groups. Tạo preview cho từng phiếu. Cập nhật Preview cột phải: hiện tab Phiếu 1 / Phiếu 2 / Phiếu 3 | Màn hình quay về luồng chính. Preview cột phải cập nhật hiển thị 3 tab phiếu |
| 5 | Nhân viên bấm "Check-in & In phiếu" | Transaction: (1) Tạo Attendance Record, (2) Lưu SĐT, (3) Sinh mã tham dự gốc (AST-2026-00089), (4) Tạo 3 phiếu ACTIVE với mã AST-2026-00089-1, -2, -3, (5) Ghi Audit Log với flag "split_ballot = true", (6) Reconciliation Check RB-01 | 3 phiếu in ra lần lượt. Topbar: +5 CĐ (số cổ đông gốc), +3 phiếu, +4.500.000 CP |
| 6 *(nếu có quà tặng)* | Nhân viên tick "Đã nhận quà" | Ghi gift_received | Panel STT VSDC: hiển thị STT của tất cả 5 cổ đông gốc (Quỹ A: STT 124, Quỹ B: STT 215...) |

**Nội dung hiển thị trên phiếu tách:**

```
PHIẾU BIỂU QUYẾT
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Người biểu quyết:  HSBC Securities (Vietnam) Company Limited
Đại diện UQ:       Quỹ A Vietnam Equity Fund
                   Quỹ B Dragon Capital II
Mã phiếu:         AST-2026-00089-1
Số cổ phần:       3.000.000
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**Xử lý khi có Ballot Lifecycle ảnh hưởng đến người đang có phiếu tách (L8):**

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Sự kiện Ballot Lifecycle L1–L7 xảy ra ảnh hưởng đến HSBC | Hệ thống hủy TOÀN BỘ phiếu tách của HSBC (AST-2026-00089-1, -2, -3 đều → INVALIDATED) | Alert SignalR đến tất cả quầy: "Cần cấu hình lại phiếu tách — HSBC — 4.500.000 CP" |
| 2 | HSBC ra quầy | Nhân viên tra cứu HSBC: hệ thống hiển thị cảnh báo "Phiếu tách đã bị hủy do thay đổi ủy quyền. Cần cấu hình lại." | Reprint Queue hiển thị: "⚡ Cấu hình lại phiếu tách cần thiết — [Tên] — [CP]" |
| 3 | Nhân viên bắt đầu cấu hình lại | Hệ thống tải lại danh sách cổ đông UQ hiện tại (sau khi đã xử lý sự kiện Lifecycle) | Quay lại Bước 1 của Tình huống SPLIT với danh sách cổ đông UQ đã cập nhật |

---

### TÌNH HUỐNG CK-4 - CỔ ĐÔNG ĐẾN NHƯNG ĐÃ ỦY QUYỀN TOÀN BỘ

*Cổ đông A đã ủy quyền 100% CP cho B trước đó. A đổi ý, đến quầy muốn tự tham dự.*

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Tra cứu A: đã có ủy quyền CONFIRMED toàn bộ CP cho B | Phát hiện ủy quyền toàn bộ đang CONFIRMED → không cho check-in trực tiếp ngay | Cảnh báo đỏ nhạt "Cổ đông đã ủy quyền toàn bộ [Số CP] CP cho [Tên B]. Muốn hủy ủy quyền để tự tham dự?" Nút [Hủy ủy quyền & Tự tham dự] và [Giữ nguyên ủy quyền] |
| 2a (giữ nguyên) | Nhân viên bấm "Giữ nguyên" | Không thay đổi gì. Ghi log "A đến quầy nhưng giữ nguyên ủy quyền cho B" | Form reset |
| 2b (hủy UQ) | Nhân viên bấm "Hủy ủy quyền & Tự tham dự" | Kiểm tra B đã check-in chưa → Ballot Lifecycle L1 (B có phiếu) hoặc L2 (B chưa check-in) | Hiện tóm tắt hành động sẽ thực hiện |
| 3 | Nhân viên xác nhận lần cuối | Transaction Ballot Lifecycle L1/L2: (1) Hủy UQ A→B, (2) Nếu B có phiếu ACTIVE: INVALIDATED + Reprint Queue + Broadcast alert, (3) Tạo Attendance Record và phiếu cho A | Alert SignalR nếu B đã có phiếu |
| 4 *(Bước bổ sung v2.2)* | Kiểm tra SĐT của A từ VSDC Cột 9 và lưu vào Attendance Record | Tự động điền nếu có, trống nếu không | SĐT lưu vào attendance_records của A |
| 5 *(nếu có quà tặng)* | Nhân viên tick "Đã nhận quà" | Ghi gift_received | Panel STT VSDC của A |

---

### TÌNH HUỐNG L-ONSITE - ỦY QUYỀN TẠI CHỖ (cổ đông đã check-in rồi muốn ủy quyền)

*Cổ đông A đã check-in và có phiếu ACTIVE. A ra quầy yêu cầu ủy quyền.*

**Lưu ý thiết kế:** Xử lý bằng **overlay panel trượt từ phải** ngay trên màn hình SC-03, KHÔNG chuyển sang màn hình ủy quyền SC-01.

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Từ màn hình check-in đang hiển thị thông tin CĐ A (dòng (a) = "Đã check-in"), nhân viên bấm "+ Ủy quyền tại chỗ" | Mở overlay panel từ phải (~55% màn hình). Panel prefill sẵn thông tin A | Overlay panel mở với badge "⚡ Sẽ kích hoạt Ballot Lifecycle", phạm vi mặc định Toàn bộ CP |
| 2 | Nhân viên chọn phạm vi và tìm người nhận B | Tính CP giữ lại = Tổng CP − CP ủy quyền. Kiểm tra RB-02 | Xác nhận 2 cột: [A giữ lại X CP] → [B nhận Y CP]. Danh sách hành động hệ thống hiện bên dưới |
| 3 | Nhân viên bấm "Xác nhận & Ghi nhận" | Transaction Ballot Lifecycle L3/L4: (1) Tạo bản ghi UQ CONFIRMED, (2) Phiếu cũ A → INVALIDATED, (3) Phiếu mới A (CP giữ lại), (4) Reprint Queue, (5) Broadcast alert, (6) Audit Log | Overlay đóng. Reprint Queue badge tăng. Alert đến tất cả quầy |

---

### TÌNH HUỐNG L-CANCEL - HỦY ỦY QUYỀN DO NGƯỜI ĐƯỢC UQ YÊU CẦU RÚT

*B đã nhận ủy quyền từ A và đã check-in. B yêu cầu trả lại ủy quyền.*

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Tra cứu B: có phiếu ACTIVE bao gồm CP nhận từ A | Xác định CP riêng của B vs CP nhận từ A | Hiển thị phân tách rõ: "CP riêng của B: [X] · CP nhận từ A: [Y]. Xác nhận hủy phần UQ từ A?" |
| 2 | Nhân viên xác nhận hủy | Kiểm tra A đã check-in trực tiếp chưa. Tính phiếu mới của B chỉ còn CP riêng | Tóm tắt: "Hủy phiếu cũ của B. Tạo phiếu mới với [X] CP." |
| 3 | Nhân viên bấm xác nhận | Transaction L6: (1) Hủy UQ A→B, (2) Phiếu cũ B → INVALIDATED, (3) Phiếu mới B với CP riêng (nếu B = 0 CP riêng: B không còn tư cách dự họp), (4) Audit Log | Phiếu mới của B in ra. A cần đến quầy check-in lại nếu muốn tham dự |

---

### TÌNH HUỐNG LINK - PHÁT HIỆN TRÙNG ĐKSH

*Hệ thống phát hiện cổ đông tra cứu có bản ghi LINK (cùng CMND, khác ngày cấp).*

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Tra cứu theo CMND: tìm thấy 2 bản ghi đã LINK | Lấy thông tin cả 2 bản ghi | Cảnh báo vàng "Phát hiện 2 tài khoản cùng chủ sở hữu". 2 thẻ cổ đông cạnh nhau |
| 2 | Nhân viên chọn: [Check-in cả hai] / [Check-in Tài khoản 1] / [Check-in Tài khoản 2] | Tùy chọn quyết định số Attendance Record và phiếu tạo ra | Preview cập nhật theo lựa chọn |
| 3 | Nhân viên bấm xác nhận | Transaction tạo 1 hoặc 2 Attendance Record + phiếu. *(Bước bổ sung v2.2)* Lấy SĐT từ Cột 9 VSDC cho từng tài khoản được check-in. Ghi Audit Log | Phiếu in ra. Topbar cập nhật. SĐT lưu vào attendance_records |

---

### TÌNH HUỐNG RÚT LUI - CỔ ĐÔNG ĐÃ CHECK-IN MUỐN RỜI HOÀN TOÀN

*Cổ đông A đã check-in, đang có phiếu ACTIVE, muốn về và không tham dự nữa.*

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Tra cứu A: đang có phiếu ACTIVE | Kiểm tra điều kiện: phiên chưa chuyển sang Kiểm phiếu (RB-03) | Cảnh báo rõ "Hủy tham dự sẽ xóa phiếu của A. CP của A sẽ không tính vào mẫu số. Xác nhận?" |
| 2 | Nhân viên (Trưởng quầy trở lên) xác nhận | Yêu cầu nhập lý do (bắt buộc). Ghi Audit Log | Tóm tắt: "Phiếu [Mã] của A sẽ bị hủy · [Số CP] CP ra khỏi tổng dự họp" |
| 3 | Nhân viên bấm xác nhận lần cuối | Transaction L5: (1) Phiếu A → INVALIDATED (bao gồm mọi phiếu tách nếu có), (2) Attendance Record A → CANCELLED, (3) Broadcast alert, (4) Audit Log L5 với lý do | Topbar: −1 CĐ, −[N phiếu], −[Số CP]. Alert đến tất cả quầy. Phiếu vật lý cần thu hồi |

---

## PHẦN 3 - RÀNG BUỘC CHUNG ÁP DỤNG CHO MỌI TÌNH HUỐNG

| Ràng buộc | Nội dung | Hậu quả nếu vi phạm |
|-----------|---------|---------------------|
| RB-01 | Tổng CP trên phiếu ACTIVE = Tổng CP đã check-in của cổ đông đó | Chặn giao dịch + alert Supervisor |
| RB-02 | Người nhận UQ không được ủy quyền tiếp phần CP nhận từ người khác | Chặn tạo ủy quyền mới |
| RB-03 | Không cho phép thay đổi sau khi phiên chuyển sang Kiểm phiếu | Ẩn tất cả nút thao tác, chỉ hiển thị đọc |
| RB-04 | Mỗi cổ đông VSDC chỉ có 1 Attendance Record ACTIVE tại 1 thời điểm | Chặn check-in trùng lặp |
| RB-05 | Phiếu mới chỉ được tạo sau khi phiếu cũ đã INVALIDATED | Workflow lock trong transaction |
| RB-10 *(MỚI)* | Tổng CP phiếu tách = Tổng CP người đó được đại diện | Không cho phép xác nhận khi còn CP chưa phân bổ |
| RB-11 *(MỚI)* | Mỗi cổ đông nguồn chỉ xuất hiện trong 1 nhóm phiếu tách | Database unique constraint trong ballot_groups |
| RB-12 *(MỚI)* | SĐT chỉ hiển thị cho người có quyền xem (Trưởng quầy, Ban KP, Trưởng BTC) | Row-level security trên cột phone_number |

---

*Tài liệu này là phụ lục của UI Specification và BRD kỹ thuật (brd-uy-quyen-checkin-kiemphieu.md v1.3).*
*Phiên bản 2.2 — Cập nhật 27/04/2026 — Thêm Tình huống SPLIT, bổ sung bước SĐT và quà tặng vào mọi tình huống.*
