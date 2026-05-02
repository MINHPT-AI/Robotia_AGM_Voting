# Thiết kế Hệ thống (Design System) - Obsidian Blueprint

Bản tóm tắt thiết kế chuẩn (Design System) này áp dụng cho toàn bộ dự án **Quản lý Đại hội Cổ đông (MMS)**. Tất cả các thiết kế giao diện, component, và stylesheet từ Giai đoạn 03 trở đi phải bắt buộc tuân theo bảng tiêu chuẩn này.

## 1. Bảng màu (Color Palette)

Hệ thống sử dụng tông màu hiện đại, chuyên nghiệp với sắc Xanh (Primary) làm chủ đạo, kết hợp với các gam màu tối (Secondary, Tertiary) để tạo độ tương phản mạnh mẽ và rõ ràng.

*   **Primary (Màu chủ đạo)**: `#1275BC`
    *   *Sử dụng cho*: Các hành động chính (Primary actions), Nút bấm chính, Header chính, Links, Trạng thái Active.
*   **Secondary (Màu phụ)**: `#14171C`
    *   *Sử dụng cho*: Văn bản tiêu đề tối màu, Nút bấm Inverted/Tương phản cao, Sidebar background, Dark mode elements.
*   **Tertiary (Màu cấp 3)**: `#393939`
    *   *Sử dụng cho*: Nội dung văn bản chính (Body text), Text phụ, Viền hoặc các thành phần UI kém nổi bật hơn.
*   **Neutral (Màu trung tính)**: `#F5F7F9`
    *   *Sử dụng cho*: Nền ứng dụng (Background), Background của Card/Panel, Background của các Input/Search form chưa focus.
*   **Cảnh báo/Nguy hiểm (Ngoại lệ)**: (Màu Đỏ theo ảnh tham chiếu biểu tượng thùng rác)
    *   *Sử dụng cho*: Nút xóa, Text lỗi, Cảnh báo quan trọng.

## 2. Nghệ thuật chữ (Typography)

Hệ thống kết hợp sử dụng 2 Font chữ hiện đại: **Manrope** (tạo sự trẻ trung, rõ ràng cho các khối text lớn) và **Inter** (siêu đọc cho các nhãn UI kích thước nhỏ).

*   **Headline (Tiêu đề)**: Font `Manrope`
    *   *Đặc điểm*: Rõ ràng, hiện đại. Thích hợp cho `<h1>` đến `<h6>`.
    *   *Độ đậm*: Bold (700) hoặc Semi-Bold (600).
*   **Body (Văn bản thường)**: Font `Manrope`
    *   *Đặc điểm*: Dễ đọc trên khối văn bản dài. Thích hợp cho `<p>`, mô tả.
    *   *Độ đậm*: Regular (400) hoặc Medium (500).
*   **Label (Nhãn / Giao diện nhỏ)**: Font `Inter`
    *   *Đặc điểm*: Gọn gàng, độ nét cao ở kích thước nhỏ. Thích hợp cho Label của Input, Text trong Button, Tags, Placeholder, Tooltips.
    *   *Độ đậm*: Medium (500) hoặc Semi-Bold (600).

## 3. Các thành phần UI (UI Components)

Dựa trên Blueprint, hệ thống sử dụng các hình khối vuông vức nhưng có độ bo góc (Border-Radius) mẻ, nhẹ nhàng (khoảng `4px` đến `8px`) để tạo nét thanh lịch.

*   **Nút bấm (Buttons)**:
    *   *Primary*: Nền `#1275BC`, chữ trắng (`#FFFFFF`).
    *   *Inverted*: Nền `#14171C`, chữ trắng (`#FFFFFF`).
    *   *Outlined*: Nền trong suốt, Viền `#1275BC` hoặc `#393939`.
    *   *Secondary/Ghost*: Nền `#F5F7F9` hoặc trong suốt, chữ `#14171C`.
    *   *Action Icons (Edit/Delete)*: Có thể sử dụng nút vuông với icon ở giữa, màu Đỏ cho xóa, Xanh hoặc Xám cho Sửa/Settings.
*   **Ô nhập liệu (Inputs & Search)**:
    *   Biến thể mặc định nền hơi xám ngà `#F5F7F9` (hoặc nhạt hơn), không viền bật lên.
    *   Khi User ấn (Focus): hiện viền màu Primary `#1275BC`.
    *   Biểu tượng (Icon) canh trái và Label nằm trong.
*   **Thanh điều hướng (Navigation)**:
    *   Tab hoặc Menu được highlight cẩn thận bằng đường viền đáy hoặc màu nền Primary.

## 4. Tích hợp MudBlazor (MudTheme)

Các thông số này sẽ trực tiếp mapping vào cấu hình `MudTheme` trong .NET/Blazor:
*   Palette `Primary`: `#1275BC`
*   Palette `Secondary`: `#14171C`
*   Palette `Tertiary`: `#393939`
*   Palette `Background` & `DrawerBackground`: `#F5F7F9`
*   Typography `Default` chuyển thành `Manrope`.
*   Typography `Button` và `Subtitle` (các nhãn nhỏ) ưu tiên dùng `Inter`.

---

**Cập nhật lần cuối:** 2026-04-21
**Mục tiêu áp dụng bắt đầu từ:** Phase 03.
