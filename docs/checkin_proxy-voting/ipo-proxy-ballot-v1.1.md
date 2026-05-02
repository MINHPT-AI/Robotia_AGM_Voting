# LUỒNG IPO - MÀN HÌNH ỦY QUYỀN & KIỂM PHIẾU
**Hệ thống**: Robotia_AGMPro | **Phiên bản**: 1.1
**Ngày cập nhật**: 27/04/2026

> **Thay đổi so với v1.0:** (A) SC-01 — Bổ sung trường số điện thoại khi nhập người nhận ủy quyền không có trong VSDC (UQ-5); cập nhật bảng ràng buộc. (B) SC-08 — Cập nhật Summary Bar Ô 1 và Ô 3 để phản ánh phiếu tách; bổ sung cột SĐT trong Panel phiếu chưa thu về (Tình huống KP-4, KP-5); bổ sung xử lý phiếu tách khi quét mã (Tình huống 0); cập nhật biên bản kiểm phiếu (KP-7); cập nhật bảng ràng buộc.

---

# MÀN HÌNH SC-01: ỦY QUYỀN TRƯỚC NGÀY HỌP

---

## PHẦN 1 - THÔNG TIN THAM CHIẾU CHUNG (Header / Topbar)

Hiển thị thường trực, không phụ thuộc tình huống.

**Dòng 1 - Số cố định từ VSDC:**
- Tổng số cổ đông có quyền dự họp
- Tổng cổ phần có quyền biểu quyết

**Dòng 2 - Số động (cập nhật sau mỗi giao dịch ủy quyền):**
- Số cổ đông đã ủy quyền + tỷ lệ % / mini bar
- Tổng CP đã ủy quyền + tỷ lệ % / mini bar
- Số ủy quyền đang chờ xác nhận (PENDING)

**Thông tin phiên làm việc:**
- Tên cuộc họp, ngày họp
- Tên nhân viên đang đăng nhập
- Trạng thái giai đoạn (Đang nhận ủy quyền / Đã đóng)

**Thông tin tham chiếu ẩn vào nút:**
- Nút "Danh sách ủy quyền ([N])" → mở drawer toàn bộ ủy quyền đã ghi nhận, lọc theo trạng thái PENDING/CONFIRMED/CANCELLED
- Nút "Cổ đông chưa ủy quyền ([N])" → mở danh sách cổ đông chưa có động thái gì

---

## PHẦN 2 - LUỒNG IPO THEO TỪNG TÌNH HUỐNG

---

### TÌNH HUỐNG 0 - TRA CỨU CỔ ĐÔNG (bước chung, xảy ra trước mọi tình huống)

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Nhân viên quét QR / gõ CMND/CCCD/ĐKSH/Tên cổ đông ủy quyền vào ô tìm kiếm | Truy vấn DB. Nếu nhiều kết quả: dropdown chọn | Thẻ cổ đông hiện ra: Tên, CMND, ĐKSH, Tổng CP |
| 2 | Kết quả tra cứu | Kiểm tra và hiển thị 3 trạng thái lên panel "Trạng thái cổ đông": (a) đã có ủy quyền đi chưa và bao nhiêu CP, (b) CP khả dụng còn lại, (c) có đang nhận ủy quyền từ người khác không | Panel trạng thái 3 dòng hiện rõ. Ô nhập CP ủy quyền tự điền sẵn = CP khả dụng (mặc định toàn bộ) |

**Nội dung Panel "Trạng thái cổ đông" — 3 dòng:**

| Điều kiện | Hiển thị bình thường | Hiển thị khi có vấn đề |
|-----------|----------------------|------------------------|
| (a) Ủy quyền đã có | - Chưa ủy quyền lần nào (xám) | ⚠ Đã ủy quyền 200.000 CP cho Lê Văn Tuấn — còn 100.000 CP khả dụng (cam) |
| (b) CP khả dụng | ✓ Toàn bộ 300.000 CP khả dụng (xanh) | ⚠ Chỉ còn 100.000 CP khả dụng / 300.000 CP tổng (cam) |
| (c) Nhận ủy quyền | - Không nhận ủy quyền từ ai (xám) | ℹ Đang nhận ủy quyền từ 2 cổ đông khác — RB-02 áp dụng (xanh dương) |

---

### TÌNH HUỐNG UQ-1 - ỦY QUYỀN TOÀN BỘ CHO 1 NGƯỜI

*Cổ đông A ủy quyền 100% CP cho người B. Đây là tình huống phổ biến nhất.*

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Kết quả tra cứu A: CP khả dụng = Tổng CP | Mặc định phạm vi = Toàn bộ | Banner xanh "Ủy quyền toàn bộ [Số CP] CP". Cursor chuyển ngay sang ô tìm người nhận B |
| 2 | Nhân viên quét QR hoặc gõ CMND của B | Tra cứu B trong DB. Kiểm tra RB-02 | Thẻ B hiện ra: Tên, CMND, vai trò (Cổ đông / Bên ngoài). Xác nhận 2 cột: [A giữ lại 0 CP] → [B nhận toàn bộ CP] |
| 3 | Nhân viên đính kèm file scan hồ sơ ủy quyền (PDF/JPG) — không bắt buộc nhưng khuyến nghị | Lưu file vào storage, liên kết với bản ghi ủy quyền | Tên file đính kèm hiện trên form |
| 4 | Nhân viên bấm "Ghi nhận ủy quyền" | Tạo bản ghi ủy quyền PENDING. Ghi Audit Log | Thông báo thành công. Topbar cập nhật: +1 ủy quyền, +[Số CP] CP đã UQ |

---

### TÌNH HUỐNG UQ-2 - ỦY QUYỀN MỘT PHẦN CHO 1 NGƯỜI

*Cổ đông A ủy quyền một phần CP cho B, giữ lại phần còn lại tự tham dự.*

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Kết quả tra cứu A: CP khả dụng = Tổng CP | Mặc định phạm vi = Toàn bộ. Nhân viên chuyển sang Một phần | Thanh slider + ô nhập số CP hiện ra. Cập nhật ngay: "Ủy quyền [X] CP · Giữ lại [Y] CP" |
| 2 | Nhân viên kéo slider hoặc gõ số CP ủy quyền | Tính CP giữ lại = Tổng CP − CP ủy quyền. Validate: CP ủy quyền > 0 và ≤ CP khả dụng | Xác nhận 2 cột cập nhật real-time: [A giữ lại Y CP] → [B nhận X CP]. Tỷ lệ % hiện dưới mỗi số |
| 3 | Nhân viên tìm và chọn người nhận B | Tra cứu B, kiểm tra RB-02 | Thẻ B hiện. Xác nhận 2 cột hoàn chỉnh |
| 4 | Nhân viên bấm "Ghi nhận ủy quyền" | Tạo bản ghi ủy quyền PENDING với số CP một phần. Ghi Audit Log | Thành công. Topbar cập nhật. Cổ đông A còn [Y] CP khả dụng |

---

### TÌNH HUỐNG UQ-3 - ỦY QUYỀN CHO NHIỀU NGƯỜI

*Cổ đông A ủy quyền cho B một phần, sau đó ủy quyền tiếp cho C (hoặc nhiều người khác).*

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Tra cứu lại A sau khi đã ghi nhận ủy quyền lần trước | Đọc CP khả dụng = Tổng CP − Tổng CP đã ủy quyền PENDING/CONFIRMED | Panel trạng thái dòng (a)(b) cập nhật: ủy quyền đã có + CP còn lại. Ô nhập CP tự điền = CP khả dụng còn lại |
| 2 | Nhân viên chọn phạm vi, tìm người nhận C | Validate: CP ủy quyền mới ≤ CP khả dụng còn lại | Xác nhận 2 cột với số CP còn lại |
| 3 | Nhân viên bấm "Ghi nhận ủy quyền" | Tạo bản ghi ủy quyền thứ 2 cho A. Ghi Audit Log | Danh sách ủy quyền bên phải hiện 2 dòng cho A: dòng 1 (B) + dòng 2 (C) |

---

### TÌNH HUỐNG UQ-4 - HỦY ỦY QUYỀN ĐÃ GHI NHẬN

*Cổ đông A yêu cầu hủy ủy quyền đã ghi nhận trước đó.*

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Tra cứu A: đang có ủy quyền PENDING hoặc CONFIRMED cho B | Lấy danh sách ủy quyền của A | Danh sách ủy quyền của A trong panel: tên người nhận, số CP, trạng thái, ngày ghi nhận, nút [Hủy] |
| 2 | Nhân viên bấm [Hủy] trên dòng muốn hủy | Kiểm tra trạng thái: PENDING → hủy ngay; CONFIRMED (người nhận đã check-in) → cảnh báo "Hủy sẽ kích hoạt Ballot Lifecycle tại màn hình check-in" | Dialog xác nhận: "Hủy ủy quyền [X] CP cho [Tên B]. CP [X] sẽ hoàn trả về khả dụng của A" |
| 3 | Nhân viên xác nhận hủy + nhập lý do (bắt buộc) | Cập nhật ủy quyền → CANCELLED. Hoàn trả CP về khả dụng của A. Nếu B đã check-in: gửi signal đến SC-03 kích hoạt Ballot Lifecycle. Ghi Audit Log | Thành công. Danh sách ủy quyền cập nhật dòng hủy → xám. Topbar cập nhật CP ủy quyền giảm |

---

### TÌNH HUỐNG UQ-5 - NGƯỜI NHẬN ỦY QUYỀN KHÔNG PHẢI CỔ ĐÔNG *(CẬP NHẬT v1.1)*

*Người B không có trong danh sách VSDC (nhân viên, luật sư, người thân... được ủy quyền đại diện).*

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Tra cứu B theo CMND: không tìm thấy trong DB | Kiểm tra thêm bảng `proxy_recipients` xem B đã từng được nhập trước đó chưa (cùng CMND) | Nếu đã có trong `proxy_recipients`: hiện thẻ B với thông tin đã lưu (tên, CMND, đơn vị, SĐT nếu có) và hỏi "Sử dụng người nhận này?" — bỏ qua bước 2. Nếu chưa có: thông báo "Không tìm thấy. Nhập thông tin người nhận mới?" + nút [Thêm mới] |
| 2 | Nhân viên bấm [Thêm mới] | Form nhập hiện ngay inline | Form nhập gồm các trường: Họ tên *(bắt buộc)*, CMND/CCCD/Hộ chiếu *(bắt buộc)*, Đơn vị/Tổ chức *(không bắt buộc)*, Chức vụ *(không bắt buộc)*, **Số điện thoại** *(không bắt buộc — khuyến nghị điền để phục vụ liên hệ thu hồi phiếu trong ngày họp)* |
| 3 | Nhân viên điền thông tin và bấm "Ghi nhận ủy quyền" | Validate: Họ tên và CMND bắt buộc. Kiểm tra CMND chưa tồn tại trong DB. Lưu người nhận mới vào bảng `proxy_recipients` kèm SĐT nếu có. Tạo bản ghi ủy quyền PENDING. Ghi Audit Log | Thành công. Người nhận mới được lưu và có thể tái sử dụng cho các ủy quyền khác trong cùng cuộc họp hoặc cuộc họp sau. SĐT (nếu có) sẽ tự động điền vào Attendance Record khi B check-in |

*Ghi chú về trường Số điện thoại:* Mặc dù không bắt buộc, số điện thoại đặc biệt quan trọng với người nhận ủy quyền bên ngoài VSDC vì họ không có thông tin trong file VSDC. Nếu thiếu SĐT, Ban kiểm phiếu sẽ không có cách liên hệ trực tiếp khi cần thu hồi phiếu chưa nộp trong ngày họp.

---

## PHẦN 3 - RÀNG BUỘC CHUNG MÀN HÌNH ỦY QUYỀN *(CẬP NHẬT v1.1)*

| Ràng buộc | Nội dung | Xử lý trên UI |
|-----------|---------|---------------|
| RB-01 | Tổng CP ủy quyền của A không được vượt Tổng CP VSDC của A | Chặn nhập + hiện lỗi đỏ inline trên ô số CP |
| RB-02 | Người nhận B không được ủy quyền lại CP nhận từ người khác | Cảnh báo vàng trong panel trạng thái dòng (c), không chặn nhận thêm |
| RB-07 | MERGE cổ đông trùng ĐKSH chỉ được phép trước khi giai đoạn Check-in bắt đầu | Ẩn tính năng MERGE khi trạng thái cuộc họp ≥ Check-in |
| RB-08 | MERGE không thể hoàn tác sau khi xác nhận | Nút xác nhận MERGE yêu cầu 2 người phê duyệt; hiển thị cảnh báo rõ "Không thể hoàn tác" trước khi xác nhận |

---
---

# MÀN HÌNH SC-08: KIỂM PHIẾU

---

## PHẦN 1 - THÔNG TIN THAM CHIẾU CHUNG (Summary Bar / Topbar) *(CẬP NHẬT v1.1)*

Hiển thị thường trực trên Summary Bar 5 ô, click vào mỗi ô để mở drilldown panel.

**5 ô Summary Bar (click được):**

| Ô | Tên | Nội dung hiển thị | Tooltip / Ghi chú |
|---|-----|-------------------|-------------------|
| 1 | Tổng phiếu phát ra | Số tờ + tổng CP | *"Số tờ có thể lớn hơn số cổ đông dự họp khi có phiếu tách theo nhóm ý kiến biểu quyết"* *(MỚI v1.1)* |
| 2 | Phiếu thu về | Số tờ + tổng CP | Click → danh sách phiếu COUNTED |
| 3 | Phiếu chưa thu | Số tờ + tổng CP (cam, nổi bật) | Click → Panel chi tiết — xem Mục 2 Tình huống KP-4 *(CẬP NHẬT v1.1: bổ sung cột SĐT)* |
| 4 | Phiếu không hợp lệ | Số tờ + tổng CP (đỏ nếu > 0) | Click → danh sách + lý do từng phiếu |
| 5 | Mẫu số nghị quyết | Tổng CP thu về (tự động) | Tooltip: *"= CP phát ra − CP của phiếu chưa thu về. Chú ý: CP phát ra bao gồm phiếu tách"* *(MỚI v1.1)* |

**Thông tin phiên làm việc:**
- Tên cuộc họp · Ngày họp
- Tên Ban kiểm phiếu đang đăng nhập
- Trạng thái phiên (Đang kiểm phiếu / Đã hoàn tất)

---

## PHẦN 2 - LUỒNG IPO THEO TỪNG TÌNH HUỐNG

---

### TÌNH HUỐNG 0 - QUÉT/NHẬP MÃ PHIẾU (bước chung trước mọi tình huống) *(CẬP NHẬT v1.1)*

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Ban kiểm phiếu quét QR/Barcode trên tờ phiếu, hoặc gõ mã tham dự / CMND vào ô tìm kiếm | Tra cứu DB theo mã phiếu hoặc mã tham dự. Nhận diện mã có hậu tố tách (-1, -2, -3) hay không. Kiểm tra trạng thái ACTIVE | Thẻ phiếu hiện ra: Mã phiếu (font mono) · Tên người biểu quyết · Số CP · Loại phiếu (Trực tiếp / Ủy quyền) · *Nếu phiếu tách: badge "Phiếu [N]/[Tổng]" màu xanh dương* *(MỚI v1.1)* |
| 2 | Kết quả tra cứu | Kiểm tra và hiển thị trạng thái phiếu: (a) ACTIVE/INVALIDATED, (b) đã nhập kết quả chưa | Dòng (a): ✓ Phiếu hợp lệ — ACTIVE (xanh) hoặc ⚠ Phiếu đã bị hủy — INVALIDATED (đỏ). Dòng (b): - Chưa nhập (xám) hoặc ⚠ Đã nhập lúc [giờ] (cam — cần xác nhận ghi đè) |
| 3 | *(Nếu phiếu tách)* Thẻ phiếu hiện thêm thông tin: "Phiếu 2/3 — HSBC (đại diện Quỹ D) — 700.000 CP. Phiếu còn lại: Phiếu 1/3 (đã nhập), Phiếu 3/3 (chưa nhập)" | Hệ thống hiển thị ngữ cảnh đầy đủ của nhóm phiếu tách để kiểm phiếu viên biết đang xử lý phiếu nào trong bộ | Panel nhỏ bên phải thẻ phiếu: danh sách phiếu cùng nhóm tách, trạng thái từng phiếu (ACTIVE/COUNTED). Giúp kiểm phiếu viên không bỏ sót phiếu nào trong bộ *(MỚI v1.1)* |
| 4 | Trạng thái đã xác nhận hợp lệ | Tải grid nội dung biểu quyết với mặc định Tán thành tất cả | Grid nội dung tờ trình hiện toàn bộ — tất cả đã tick Tán thành. Kiểm phiếu viên chỉ thay đổi khi có ý kiến khác |

---

### TÌNH HUỐNG KP-1 - NHẬP PHIẾU BIỂU QUYẾT TÁN THÀNH TOÀN BỘ

*Tờ phiếu tán thành 100% tất cả nội dung — tình huống phổ biến nhất (80–90% phiếu thực tế).*

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Quét mã phiếu. Grid hiện ra với toàn bộ nội dung đã tick Tán thành (mặc định) | Không cần thao tác thêm — mặc định đã đúng | Grid hiện đủ tất cả nội dung, tất cả highlight xanh lá. Kiểm phiếu viên quan sát phiếu vật lý và xác nhận khớp |
| 2 | Nhân viên bấm "Ghi nhận phiếu (Enter)" | Lưu kết quả: tất cả nội dung = Tán thành với số CP của phiếu. Cập nhật trạng thái phiếu → COUNTED. Ghi Audit Log. Cập nhật mẫu số và tử số ngay | Summary Bar cập nhật: Phiếu thu về +1, CP thu về tăng, Mẫu số tăng. Form reset, cursor về ô quét mã |

---

### TÌNH HUỐNG KP-2 - NHẬP PHIẾU CÓ Ý KIẾN KHÁC

*Tờ phiếu có ít nhất 1 nội dung không tán thành hoặc có ý kiến khác.*

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Quét mã phiếu. Grid hiện ra mặc định tất cả Tán thành | Kiểm phiếu viên đọc phiếu vật lý, phát hiện nội dung X có đánh dấu khác | Grid hiện toàn bộ nội dung |
| 2 | Nhân viên click vào ô "Không tán thành" hoặc "Ý kiến khác" cho nội dung X | Cập nhật lựa chọn nội dung X. Phím tắt số 1/2/3/4 để chọn nhanh | Ô được chọn highlight màu tương ứng: Tán thành xanh lá / Không tán thành đỏ / Ý kiến khác cam / Không HL xám |
| 3 | Nhân viên bấm "Ghi nhận phiếu (Enter)" | Lưu kết quả với đúng lựa chọn từng nội dung. Ghi Audit Log chi tiết. Cập nhật Summary Bar và kết quả bên phải | Summary Bar và kết quả cập nhật. Form reset |

---

### TÌNH HUỐNG KP-3 - ĐÁNH DẤU PHIẾU KHÔNG HỢP LỆ

*Tờ phiếu bị tẩy xóa, thiếu chữ ký, hỏng không đọc được hoặc vi phạm quy chế.*

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Quét mã phiếu. Kiểm phiếu viên xác định phiếu vật lý không hợp lệ | Bấm nút "Không hợp lệ" thay vì xử lý từng nội dung | Dialog xác nhận: "Đánh dấu phiếu này Không hợp lệ? Phiếu vẫn tính vào mẫu số nhưng không vào tử số." + Ô nhập lý do *(bắt buộc)* |
| 2 | Nhân viên nhập lý do và xác nhận | Lưu trạng thái phiếu = Không hợp lệ. Toàn bộ nội dung = 0 tử số. CP vẫn cộng vào mẫu số. Ghi Audit Log | Summary Bar: Phiếu thu về +1, Phiếu không hợp lệ +1. Kết quả các nội dung bên phải: mẫu số tăng nhưng tử số không đổi → tỷ lệ % giảm nhẹ |

---

### TÌNH HUỐNG KP-4 - PHIẾU KHÔNG THU VỀ (cổ đông về sớm) *(CẬP NHẬT v1.1)*

*Kết thúc kiểm phiếu còn phiếu chưa được nộp — cổ đông đã rời hội trường.*

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Summary Bar Ô 3 "Phiếu chưa thu" còn số > 0 sau khi đã nhập hết phiếu có trong tay | Ban kiểm phiếu click vào Ô 3 | Drilldown panel hiện danh sách phiếu chưa thu: *(CẬP NHẬT v1.1 — bổ sung cột SĐT)* |

**Nội dung drilldown panel "Phiếu chưa thu về" *(CẬP NHẬT v1.1):***

```
PHIẾU CHƯA THU VỀ  (4 tờ - 1.950.000 CP)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Mã phiếu           Người dự họp              CP         SĐT              Check-in lúc
─────────────────────────────────────────────────────────────
AST-2026-00045     Nguyễn Văn A              500.000    0912 345 678     08:32
AST-2026-00089-1   HSBC (Quỹ A, B, C)      1.200.000   0243 823 1234    09:15
AST-2026-00089-2   HSBC (Quỹ D)              150.000   0243 823 1234    09:15
AST-2026-00134     Trần Thị B                100.000   —                10:02
─────────────────────────────────────────────────────────────
[📞 Sao chép số]   [In danh sách này]   [Xuất Excel]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

*Ghi chú thiết kế:*
- Phiếu tách (AST-2026-00089-1 và -2) hiển thị riêng từng dòng vì mỗi phiếu cần thu về độc lập, nhưng cùng SĐT vì cùng 1 người vật lý (HSBC) — chỉ cần gọi 1 lần.
- Trần Thị B không có SĐT — hiển thị "—" màu cam để cảnh báo.
- Nút "📞 Sao chép số" xuất hiện khi hover vào dòng, copy vào clipboard.
- SĐT lấy từ `attendance_records.phone_number` — đã được thu thập lúc check-in.

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 2 | Ban kiểm phiếu xác nhận đã tìm hết và chấp nhận phiếu không thu về | Nhân viên chọn từng phiếu trong danh sách và bấm "Đánh dấu Không thu về" | Dialog xác nhận: "Phiếu [Mã] sẽ được đánh dấu Không thu về. CP [X] sẽ KHÔNG tính vào mẫu số nghị quyết." |
| 3 | Nhân viên xác nhận từng phiếu không thu về | Cập nhật trạng thái phiếu → NOT_RETURNED. CP bị loại khỏi mẫu số. Ghi Audit Log | Summary Bar: Phiếu chưa thu giảm, Mẫu số giảm theo. Kết quả bên phải: tỷ lệ % thay đổi tùy cấu trúc phiếu chưa thu |

---

### TÌNH HUỐNG KP-5 - KIỂM PHIẾU NHANH (Bulk Approve) *(CẬP NHẬT v1.1)*

*Sau khi đã nhập hết xấp phiếu có ý kiến khác, duyệt nhanh toàn bộ xấp tán thành 100% còn lại.*

**Lưu ý về phiếu tách *(MỚI v1.1):*** Phiếu tách (AST-2026-00089-1, -2, -3) được xử lý **độc lập** trong Bulk Approve — mỗi phiếu là 1 tờ riêng biệt. Nếu HSBC tách 3 phiếu và cả 3 đều tán thành 100%, cả 3 đều có thể đưa vào xấp tán thành và duyệt nhanh bình thường. Nếu chỉ 1 trong 3 có ý kiến khác, 2 phiếu kia vẫn được duyệt nhanh; chỉ phiếu có ý kiến khác phải nhập tay.

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Ban kiểm phiếu đã nhập ít nhất 1 phiếu có ý kiến khác. Bấm nút "Kiểm phiếu nhanh — Tán thành 100%" | Phân loại phiếu chưa xử lý thành 2 nhóm: (a) đã quét QR xác nhận có trong tay; (b) chưa thu về — nhóm (b) bị chặn cứng | Panel Bulk Approve mở ra hiện 2 nhóm: Nhóm xanh "Có thể duyệt nhanh: [N] phiếu" + Nhóm đỏ "KHÔNG thể duyệt nhanh: [M] phiếu chưa thu về" |
| 2 | Ban kiểm phiếu quét QR lần lượt từng tờ phiếu trong xấp tán thành (Ctrl+Q để bật chế độ quét hàng loạt) | Ghi nhận từng mã phiếu vào danh sách chờ. Không nhập kết quả ở bước này — chỉ xác nhận vật lý | Số phiếu "Có thể duyệt nhanh" tăng dần. Phiếu chưa thu về giữ nguyên |
| 3 | Bấm "Chọn tất cả [N] phiếu có thể duyệt" + "Xác nhận duyệt nhanh" | Hiện dialog xác nhận cuối: tổng số phiếu *(kèm chú thích số phiếu tách trong tổng nếu có)*, tổng CP, số nội dung. Cảnh báo "Không thể hoàn tác" | Dialog xác nhận cuối đầy đủ |
| 4 | Ban kiểm phiếu bấm "XÁC NHẬN" lần cuối | Ghi hàng loạt: tất cả phiếu trong danh sách = Tán thành 100%. Ghi Audit Log với flag `bulk_approved = true`, timestamp, mã nhân viên, số lượng *(bao gồm flag `has_split_ballots` nếu có phiếu tách trong lô)* *(MỚI v1.1)*. Cập nhật Summary Bar | Summary Bar: Phiếu thu về tăng mạnh. Kết quả bên phải cập nhật tỷ lệ. Form reset |

**Danh sách phiếu chưa thu về trong Bulk Approve *(CẬP NHẬT v1.1 — bổ sung SĐT):***

```
⛔ KHÔNG thể duyệt nhanh  (15 phiếu chưa thu về)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  □  AST-2026-00045    Nguyễn Văn A          500.000 CP   📞 0912 345 678
  □  AST-2026-00089-1  HSBC (Quỹ A, B, C)  1.200.000 CP   📞 0243 823 1234
  □  AST-2026-00089-2  HSBC (Quỹ D)          150.000 CP   📞 0243 823 1234
  □  AST-2026-00134    Trần Thị B            100.000 CP   ⚠ Không có SĐT
  ... (11 dòng nữa)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

### TÌNH HUỐNG KP-6 - PHIẾU BẦU CỬ NHÂN SỰ (Cumulative Voting)

*Nhập kết quả phiếu bầu HĐQT/BKS với cơ chế bầu dồn phiếu.*

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Quét mã phiếu bầu cử. Hệ thống nhận diện loại phiếu = Phiếu bầu cử | Chuyển sang giao diện nhập bầu cử. Tính tổng điểm bầu = Số CP × Số ứng viên. Chia đều mặc định cho tất cả ứng viên | Grid ứng viên hiện ra: mỗi dòng gồm Tên ứng viên + ô nhập điểm đã điền sẵn mặc định. Tổng điểm đã dùng / Tổng điểm bầu hiện real-time |
| 2 | Kiểm phiếu viên điều chỉnh điểm cho từng ứng viên khác với mặc định | Validate real-time: tổng điểm không được vượt tổng điểm bầu. Nếu vượt: ô chuyển đỏ + hiện chênh lệch | Tổng điểm còn lại = Tổng điểm bầu − Tổng đã phân bổ |
| 3 | Nhân viên bấm "Ghi nhận phiếu (Enter)" | Validate cuối: tổng điểm ≤ tổng điểm bầu. Lưu kết quả. Ghi Audit Log. Cập nhật bảng kết quả bầu cử | Bảng kết quả cập nhật: Tổng điểm từng ứng viên + Xếp hạng tạm thời. Form reset |

---

### TÌNH HUỐNG KP-7 - HOÀN TẤT KIỂM PHIẾU VÀ CHỐT KẾT QUẢ *(CẬP NHẬT v1.1)*

*Ban kiểm phiếu xác nhận đã xử lý hết toàn bộ phiếu và chốt kết quả chính thức.*

| Bước | Input | Process | Output |
|------|-------|---------|--------|
| 1 | Summary Bar Ô 3 "Phiếu chưa thu" = 0 (hoặc đã đánh dấu NOT_RETURNED hết). Ban kiểm phiếu đánh giá hoàn tất | Kiểm tra: tất cả phiếu ACTIVE đã có trạng thái COUNTED hoặc NOT_RETURNED. Nếu còn phiếu ACTIVE chưa xử lý: cảnh báo và liệt kê danh sách | Nút "Hoàn tất kiểm phiếu & Chốt kết quả" active |
| 2 | Ban kiểm phiếu bấm "Hoàn tất kiểm phiếu & Chốt kết quả" | Dialog tóm tắt cuối *(xem nội dung bên dưới)*. Yêu cầu ít nhất 2 thành viên Ban kiểm phiếu xác nhận (xác nhận kép) | Dialog xác nhận kép: 2 ô nhập mã PIN hoặc ký tên số |
| 3 | 2 thành viên Ban kiểm phiếu xác nhận | Lock toàn bộ dữ liệu — không thể chỉnh sửa sau bước này. Tạo snapshot kết quả chính thức. Cập nhật trạng thái cuộc họp → Hoàn tất. Ghi Audit Log với 2 chữ ký | Màn hình chuyển chế độ chỉ đọc. Nút "Xuất Biên bản kiểm phiếu (.docx)" active |
| 4 | Ban kiểm phiếu bấm "Xuất Biên bản kiểm phiếu" | Render file .docx từ template, điền tự động toàn bộ số liệu đã chốt *(xem nội dung biên bản bên dưới)* | File .docx tải về |

**Nội dung dialog tóm tắt cuối (Bước 2):**

```
TÓM TẮT TRƯỚC KHI CHỐT
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Tổng phiếu phát ra:         247 tờ
  Trong đó phiếu thông thường: 241 tờ
  Trong đó phiếu tách:           6 tờ ← (MỚI v1.1)
Phiếu thu về:               235 tờ
Phiếu không thu về:          12 tờ
Phiếu không hợp lệ:           3 tờ

Mẫu số nghị quyết:   18.450.000 CP
  (= CP thu về, không tính 12 phiếu không thu về)

Kết quả từng nội dung:
  Nội dung 1: THÔNG QUA  (87,3%)
  Nội dung 2: THÔNG QUA  (72,1%)
  Nội dung 3: THÔNG QUA  (91,5%)

Hành động này sẽ KHÓA toàn bộ dữ liệu.
Không thể chỉnh sửa sau khi xác nhận.
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**Nội dung biên bản kiểm phiếu xuất ra .docx *(CẬP NHẬT v1.1):***

Biên bản bao gồm các phần:

| Phần | Nội dung | Ghi chú |
|------|---------|---------|
| I. Tổng hợp số liệu | Tổng phiếu phát ra *(phân loại: thông thường / tách)*, phiếu thu về, phiếu không thu về, phiếu không hợp lệ, mẫu số chính thức | *(MỚI v1.1: tách riêng phiếu thông thường và phiếu tách)* |
| II. Kết quả từng nội dung tờ trình | Tổng CP tán thành / không tán thành / ý kiến khác / không hợp lệ · Tỷ lệ % · THÔNG QUA / KHÔNG THÔNG QUA | |
| III. Kết quả bầu cử nhân sự (nếu có) | Tổng điểm từng ứng viên theo thứ tự xếp hạng | |
| IV. Danh sách phiếu không hợp lệ | Mã phiếu · Tên người · Số CP · Lý do không hợp lệ | |
| V. Danh sách phiếu không thu về *(CẬP NHẬT v1.1)* | Mã phiếu · Tên người · Số CP · SĐT *(để lưu hồ sơ)* · Quầy check-in · Giờ check-in | *(MỚI v1.1: bổ sung cột SĐT)* |
| VI. Chữ ký Ban kiểm phiếu | Ô ký tên của từng thành viên | |

---

## PHẦN 3 - RÀNG BUỘC CHUNG MÀN HÌNH KIỂM PHIẾU *(CẬP NHẬT v1.1)*

| Ràng buộc | Nội dung | Xử lý trên UI |
|-----------|---------|---------------|
| RB-03 | Màn hình kiểm phiếu chỉ active sau khi phiên chuyển sang trạng thái Kiểm phiếu | Toàn bộ màn hình disabled nếu trạng thái cuộc họp chưa đúng |
| RB-06 | Mẫu số = CP thu về, tự động = CP phát ra − CP chưa thu về | Mẫu số tự tính, không có ô nhập tay, cập nhật sau mỗi thay đổi |
| RB-09 / BA-RULE-01 | Cấm duyệt nhanh (Bulk Approve) phiếu chưa thu về | Hard filter: loại NOT_RETURNED khỏi danh sách Bulk trước khi hiển thị; không có cơ chế override |
| BA-RULE-04 | Phiếu bầu cử nhân sự không được duyệt qua Bulk Approve | Lọc phiếu bầu cử ra khỏi danh sách Bulk, xử lý riêng qua KP-6 |
| RB-10 *(MỚI)* | Tổng CP phiếu tách = Tổng CP người đó đại diện — đảm bảo tính chính xác khi kiểm phiếu | Hệ thống hiển thị cảnh báo nếu tổng CP các phiếu tách trong 1 nhóm không khớp với tổng CP đại diện ban đầu (phát hiện khi commit Ballot Lifecycle) |
| RB-12 *(MỚI)* | SĐT trong Panel phiếu chưa thu về chỉ hiển thị cho Ban kiểm phiếu và Trưởng ban tổ chức | Row-level security: nhân viên check-in thông thường không thấy cột SĐT trong drilldown |
| KP-LOCK | Sau khi Hoàn tất kiểm phiếu: không sửa, không nhập thêm | Toàn bộ form và nút thao tác bị ẩn, chỉ còn nút Xuất báo cáo |

---

*Tài liệu này là phụ lục của UI Specification và BRD kỹ thuật (brd-uy-quyen-checkin-kiemphieu.md v1.3). Đọc kết hợp với ipo-checkin-v2.2.md và ipo-checkin-screen-v1.1.md để có đầy đủ bức tranh các màn hình.*
*Phiên bản 1.1 — Cập nhật 27/04/2026.*
