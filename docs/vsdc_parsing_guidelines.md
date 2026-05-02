# Hướng dẫn Phân tích & Bóc tách Dữ liệu File VSDC (VSDC Parsing Guidelines)

Tài liệu này đúc kết toàn bộ kinh nghiệm, đặc thù và thuật toán chuẩn xác nhất để bóc tách (parse) file Excel danh sách cổ đông do **Trung tâm Lưu ký Chứng khoán Việt Nam (VSDC)** cung cấp. 

Bất kỳ AI Agent hoặc Lập trình viên nào khi xây dựng tính năng Import VSDC bằng **bất kỳ ngôn ngữ/framework nào** (C#, Python, Java, Node.js...) đều **phải tuân thủ** các nguyên tắc trong tài liệu này để tránh việc đọc sai lệch dữ liệu tài chính nghiêm trọng.

---

## 1. Bản chất và Đặc thù của File VSDC
File VSDC gửi cho tổ chức phát hành không phải là một bảng dữ liệu Data Table chuẩn, mà là một **Báo cáo dạng in ấn (Report Layout)** xuất ra định dạng Excel. Điều này sinh ra các đặc thù cực kỳ "độc hại" đối với việc lập trình bóc tách:

1. **Cell Merging (Ô gộp đứt gãy):** File nhìn có 16 cột nội dung (STT, Họ tên, CMND, Tổng CK...), nhưng số cột vật lý trên file Excel thường lên tới **29 cột**. Các ô bị merge một cách lộn xộn khiến việc truy cập cột theo Index tĩnh (như Column A, Column F, Index [0], Index [5]) là **chắc chắn sẽ lấy sai dữ liệu**.
2. **Không có cột Phân loại:** Danh sách VSDC **không có** cột nào ghi chữ "Cá nhân/Tổ chức" hay "Trong nước/Nước ngoài". Thay vào đó, dữ liệu được trình bày theo dạng Khối (Section). Ví dụ: Khối "I. MÔI GIỚI TRONG NƯỚC" chứa khối con "1. Cá nhân", bên dưới khối con mới là các dòng dữ liệu.
3. **Định dạng số Việt Nam:** Số lượng cổ phiếu được định dạng hàng nghìn bằng dấu chấm (`.`). Ví dụ: `18.600` là *Mười tám nghìn sáu trăm*, nếu parse bằng locale chuẩn Mỹ/Quốc tế có thể bị hiểu nhầm thành `18.6` (Mười tám phẩy sáu).

---

## 2. Phương pháp thuật toán Bóc tách (State Machine & Dynamic Map)

Thuật toán parse file VSDC bắt buộc phải trải qua **3 Giai đoạn** tuần tự:

### Giai đoạn 1: Quét tìm Dòng Tiêu đề (Find Header Row)
- Không được giả định dòng Tiêu đề nằm ở dòng số mấy.
- **Cách làm:** Quét từ dòng 1 đến dòng 50, kiểm tra cell ở 5 cột vật lý đầu tiên. Nếu có ô nào chứa chữ `"STT"` (tuyệt đối) HOẶC `"Số ĐKSH"`, ta xác định đó chính là dòng Tiêu đề (Header Row).

### Giai đoạn 2: Xây dựng Bản đồ Cột Động (Dynamic Column Mapping)
- Ngay phía dưới dòng Tiêu đề (Header Row + 1 hoặc + 2) **luôn luôn** có một dòng chứa các con số thứ tự cột từ `1` đến `16`. Đây là "chìa khóa vàng" của file VSDC.
- **Cách làm:** Duyệt toàn bộ các ô vật lý trên dòng chứa số này (từ index 1 đến tối đa 50).
  - Nếu giá trị ô là `"1"`, lưu `map[1] = physicalIndex`.
  - Nếu giá trị ô là `"2"`, lưu `map[2] = physicalIndex`.
  - Làm lần lượt đến `"16"`.
  - Từ đây về sau, khi lấy cột "Số ĐKSH" (cột số 5), ta chỉ cần lấy giá trị tại biến `map[5]`. **Điều này triệt tiêu hoàn toàn lỗi lệch cột do Merged Cells.**

### Giai đoạn 3: Đọc Dữ liệu bằng Máy trạng thái (Read with State Machine)
- **Cách làm:** Sử dụng 2 biến `currentSection` (Trong/Ngoài nước) và `currentSubSection` (Cá nhân/Tổ chức) lặp qua từng dòng kể từ sau dòng đếm số.
- VSDC chia section qua các cụm từ (regex hoặc startsWith):
  - Nhận tên khối cha: Bắt đầu bằng `"I."` hoặc `"II."` (Ví dụ: "I. MÔI GIỚI TRONG NƯỚC"). Lập tức gán `currentSection = "TRONG_NUOC"`, **đồng thời bắt buộc phải reset** `currentSubSection = null` (để tránh tag nhầm Tổ chức của vùng trước xuống Cá nhân vùng sau).
  - Nhận tên khối con: Bắt đầu bằng `"1."` hoặc `"2."` (Ví dụ: "1. Cá nhân"). Lập tức gán `currentSubSection = "CA_NHAN"`.
- **Nhận diện dòng Dữ liệu hợp lệ (Data Row):**
  - Đọc giá trị tại `map[2]` (Họ Tên) và `map[5]` (CMND/Số ĐKSH).
  - Dòng dữ liệu thật là dòng **TỒN TẠI** cả hai giá trị này (không rỗng). Bỏ qua hoàn toàn các dòng "Tổng cộng", "Cộng tiểu khoản" vì lúc đó Tên/ĐKSH sẽ rỗng hoặc chứa chữ "Tổng".

---

## 3. Kỹ thuật Parse Dữ liệu tinh chuẩn (Data Transformation)

### a. Xử lý Thời gian (Ngày Cấp CMND/ĐKSH)
- Ô ngày tháng trên Excel có thể mang 2 hình thái:
  - **Dạng Text:** Dưới dạng chuỗi `"24/05/2018"` (Phổ biến).
  - **Dạng Cấu trúc Excel (OADate):** Dưới dạng số thập phân của Excel (Ví dụ `43244`).
- Thư viện đọc Excel (ví dụ ClosedXML của C#, pandas của Python) phải bắt được cả 2 dạng này và fallback bằng cách parse string định dạng `dd/MM/yyyy`.

### b. Xử lý Số học (Cổ phiếu & Quyền Biểu Quyết)
- **Luật:** Xóa bỏ hoàn toàn khoảng trắng và ký tự dấu chấm `.` (Dấu nghìn của VN) ra khỏi chuỗi trước khi cast sang số nguyên (Integer/Long).
- **Tuyệt đối Không Xóa Dấu Phẩy `,`**: Một số VSDC quốc tế hoặc bản báo biểu khác có dùng dấu `,` để biểu diễn phân số, việc xóa mù quáng có lỗi, chỉ bắt string `.Replace(".", "")`.

### c. Các Tác nhân Dữ liệu Lỗi Tĩnh (Known Corruptions)
- Chữ ký, phần Footer nằm cuối file sẽ sinh ra rác. Việc kiểm tra `Nếu ô [Họ Tên] null => Bỏ qua dòng` của Máy Trạng Thái ở GĐ 3 sẽ tự động lọc sạch được rác phần Footer này.

---

## 4. Chiến lược Database & Nhập liệu (Database Strategy)

Tâm lý chung của lập trình viên là thiết lập Ràng buộc cơ sở dữ liệu `Unique Index (MeetingId, IdNumber)` (Số CCCD là duy nhất trong sự kiện ĐHCĐ). **Đây là tư duy sai trong ngữ cảnh VSDC.**

### a. Nới lỏng Ràng buộc (Permissive Constraints)
- VSDC **luôn có trường hợp một ID trùng lặp**. Ví dụ: Cùng một số CCCD `0351...2985` nhưng lại hiển thị thành 2 dòng riêng biệt do quá khứ họ mở tài khoản bằng hệ CMND cũ 9 số, sau đó update một phần trên hệ thống lưu ký với 2 Ngày Cấp khác nhau ở các tài khoản của các Công ty Chứng khoán khác nhau.
- **Quy tắc:** Database Cổ đông ĐHCĐ **CHO PHÉP** trùng IDNumber. Quản lý nhận diện Cổ đông lúc chốt danh sách hoặc In Thẻ sẽ tiến hành gom (Group) theo Tên + Tổng số Cổ phần.

### b. Chiến lược Ghi đè (Wipe-and-Reload)
- Việc dùng thao tác Upsert phức tạp (`SQL ON CONFLICT DO UPDATE`) không thực sự an toàn với file VSDC và chạy rất chậm do giới hạn ORM (Entity Framework, Hibernate) khi có dữ liệu lồng.
- Bản chất file VSDC là một **Hồ sơ Chốt tại một thời điểm (Snapshot)**.
- **Quy tắc Import Tối ưu nhất:** 
  1. Yêu cầu giao diện (UI) Xác nhận Xóa dữ liệu cũ.
  2. Dùng Raw SQL `DELETE FROM Shareholders WHERE MeetingId = X`.
  3. Duyệt mảng Data Row đã bóc tách được (qua GĐ3) và thực hiện **Bulk Insert** một lần duy nhất. Tốc độ sẽ tăng hàng chục lần (Import 1 triệu dòng dưới 10 giây qua COPY/Bulk). 
  4. Phải khóa tính năng **Import Re-upload** nếu Sự kiện ĐHCĐ đã chuyển trạng thái từ `Setup` sang `CheckIn` (Bắt đầu đón khách) để chống mất đồng bộ cấu trúc Vé Bầu.

---
*(Văn bản này được tạo bởi AI kiến trúc phân tích trực tiếp từ Dự án Robotia AGM VSDC Pilot vào năm 2026).*
