# TÀI LIỆU YÊU CẦU NGHIỆP VỤ (BUSINESS REQUIREMENTS DOCUMENT - BRD)
**Dự án**: Hiện đại hóa Hệ thống Quản lý Đại hội Cổ đông (MMS)
**Module**: Quy trình hoạt động tổ chức Đại hội Cổ đông

---

## 1. TỔNG QUAN TÀI LIỆU (EXECUTIVE SUMMARY)
Tài liệu cung cấp các yêu cầu nghiệp vụ chuẩn hóa cho Quy trình tổ chức Đại hội Cổ đông (ĐHCĐ) trên nền tảng MMS Web mới. Mục tiêu là chuyển đổi quy trình từ hệ thống WinForms mã nguồn cứng (hardcoded) sang một kiến trúc web động, hỗ trợ tùy biến các biểu mẫu, báo cáo và xử lý linh hoạt trạng thái vận hành theo thời gian thực (check-in, kiểm phiếu, ủy quyền) trong một môi trường chịu tải cao và đảm bảo tính thống nhất dữ liệu (data consistency).

## 2. PHẠM VI NGHIỆP VỤ
Hệ thống MMS mới hỗ trợ 8 bước theo vòng đời tổ chức một kỳ đại hội cổ đông, từ bước thiết lập trước đại hội đến khi tổng kết phát hành báo cáo. Trong đó, hệ thống tích hợp Module "Quản lý Template Tập trung" đóng vai trò cốt lõi trong việc chuẩn bị các biểu mẫu in ấn tự động.

---

## 3. QUY TRÌNH NGHIỆP VỤ 8 BƯỚC

### Bước 1: Quản lý thông tin Doanh nghiệp & Cuộc họp
**Mục tiêu**: Chuẩn bị nền tảng thông tin cho đơn vị tổ chức và sự kiện hội nghị.
* **Thông tin Doanh nghiệp**: Thiết lập Vốn điều lệ, tổng cổ phần phát hành/có quyền biểu quyết, Tên thay thế, MST, và thông tin Đại diện pháp luật.
* **Thiết lập Cuộc họp**: Tạo phiên làm việc mới (Thường niên/Bất thường), quy định ngày/giờ/địa điểm họp, chỉ định mốc thời gian chốt danh sách từ VSDC.
* **Thiết lập Đại hội**: Thiết lập các nội dung Tờ trình đại hội cổ đông (nội dung sẽ được thể hiện trên Phiếu biểu quyết) và thiết lập nội dung về bầu nhân sự HĐQT và BKS (nội dung sẽ được thể hiện trên Phiếu bầu cử).
* **Trạng thái vòng đời cuộc họp**: `Mới tạo` → `Đang chuẩn bị` → `Check-in` → `Đang họp` → `Kiểm phiếu` → `Hoàn tất`.

### Bước 2: Import Danh sách Cổ đông (Dữ liệu VSDC)
**Mục tiêu**: Nạp dữ liệu cổ đông chốt quyền dự họp từ Trung tâm Lưu ký Chứng khoán (VSDC).
* **Xử lý đặc thù file VSDC gốc**: Hệ thống phải có bộ đọc (parser) khả năng cấu trúc hóa trực tiếp file Excel báo cáo thô từ VSDC *(Tham khảo định dạng cấu trúc tại `ExempleTemplate_file/Mẫu file DS VSDC gui.csv`)*. File gốc chứa nhiều hàng tiêu đề và các ô bị gộp (merged cells). Hệ thống nạp file bắt buộc duy trì tính toàn vẹn bằng cách **đọc cố định đúng trật tự 16 cột tiêu chuẩn** từ VSDC mà không thêm/bớt hay cho phép user cấu hình sai lệch cột.
* **Map thông tin trọng yếu**: Trong số 16 cột nạp tự động, thuật toán hệ thống thiết lập sẵn ánh xạ: **Cột 5 (Số ĐKSH)** làm số định danh chính (CMND/CCCD/Passport); **Cột 10 (Quốc tịch)** lưu làm cơ sở xác định Template in ấn (Đa ngôn ngữ); và lấy duy nhất **Cột 16 (SL Quyền phân bổ Tổng cộng)** làm **Số lượng Cổ phần có quyền biểu quyết** của Cổ đông.
* **Quy trình 4 bước (Wizard)**:
  1. Upload tệp định dạng Excel gốc (.xlsx, .xls) nguyên bản từ VSDC.
  2. Ánh xạ (Map fields) tự động thông qua mảng 16 cột cố định thay vì mapping linh hoạt rủi ro cao.
  3. Preview và Validate: Phát hiện và cảnh báo các bản ghi bị khuyết CMND, Số lượng CP bằng 0 hoặc vi phạm tổng lượng Cổ phần của Vốn điều lệ.
  4. Thực thi nạp Cổ đông (Cho phép nạp bổ sung cập nhật đè lên dữ liệu cũ dựa trên trường định danh CMND/ĐKSH).

### Bước 3: Phát hành Thư mời (Template-based)
**Mục tiêu**: Hỗ trợ xuất thư mời cho cổ đông có mã QR tích hợp phục vụ kiểm soát check-in.
* Áp dụng Template `Thư mời` (Loại 1) đã được định cấu hình từ UI Quản lý Template trung tâm.
* Bảng theo dõi trạng thái thư mời từng cổ đông: Đã tạo / Nhóm đã in / Đã phát hành gửi bưu điện / Chuyển hoàn.
* Chức năng tạo hàng loạt (Batch Generate), in thư mời phân trang.

### Bước 4: Ủy quyền trước ngày họp
**Mục tiêu**: Ghi nhận sớm sự ủy quyền đại diện sở hữu cổ phần theo hồ sơ gửi qua bưu điện/văn phòng.
* **Kiểm tra hợp lệ**: Hệ thống xác thực danh tính Cổ đông ủy quyền so khớp với cơ sở dữ liệu VSDC. Tự động tính toán lượng `Cổ phần chưa ủy quyền` (Cổ phần khả dụng).
* **Thành phần**: Cho phép ủy quyền "toàn bộ" hoặc "một phần". Quản lý file chép (scan) đính kèm minh chứng. Cập nhật theo thời gian thực tổng lượng ủy quyền đại diện.

### Bước 5: Check-in, Ủy quyền tại chỗ & In phiếu (Tới hạn Tốc độ)
**Mục tiêu**: Đóng cửa hội trường tại quầy với tốc độ cao nhất qua ứng dụng thiết kế POS-style nhằm tránh rách tuyến tính và conflict.
* **Định danh nhanh**: Quét mã QR, Barcode hoặc tìm chuỗi gốc CCCD.
* **Sinh mã tham dự duy nhất**: Sau khi kiểm tra người tham gia đủ tư cách dự họp, hệ thống tự động sinh ra 1 Mã cổ đông/Mã tham dự duy nhất gắn với cổ đông hoặc người đại diện ủy quyền tham dự họp đó. Mã này sẽ xuất hiện trên Thẻ biểu quyết/Phiếu biểu quyết/Phiếu bầu cử cùng với thông tin cổ đông.
* **Linh hoạt Ủy quyền tại chỗ**: Cổ đông phát sinh nhu cầu ủy quyền ngay tại hội trường. Kéo theo tiến trình kích hoạt **Ballot Lifecycle Cascade Invalidation**.
* **Trạng thái tham gia**: Ghi nhận dự họp Trực tiếp, Thông qua Người Đại diện, hoặc Cả hai (vừa tham dự trực tiếp vừa ủy quyền một phần).
* **Hỗ trợ Workflow**: Combo `F5` hoặc `Ctrl+P` (Xác nhận & In nhanh). Hệ thống tự động render và in các biểu mẫu phát cho Cổ đông dựa trên Template đánh sẵn:
  * Thẻ biểu quyết *(Tham khảo mẫu `ExempleTemplate_file/8.1. The bieu quyet_mau.pdf`)*
  * Phiếu biểu quyết *(Tham khảo mẫu `ExempleTemplate_file/8.2. To phieu bieu quyet_mau.pdf`)*
  * Phiếu bầu cử *(Tham khảo mẫu `ExempleTemplate_file/19.20160405_VIC_Mau the bau TVHDQT.pdf` hoặc `ExempleTemplate_file/221124043905532_11-mau-phieu-bau-1-thanh-vien-hdqt.pdf`)*
* **Reprint Queue**: Các lá phiếu cũ bị hủy sẽ được đặt tự động vào Hàng đợi chờ in lại.

### Bước 6: Thẩm tra tư cách Cổ đông (Snapshots)
**Mục tiêu**: Thẩm tra tư cách cổ đông được thực hiện theo thời điểm và chỉ đóng trước khi bỏ phiếu.
* **Chốt để khai mạc**: Về nguyên tắc đến một thời gian nhất định, khi số phiếu biểu quyết đủ số lượng để tổ chức cuộc họp thì Ban kiểm tra tư cách sẽ chốt tại thời điểm đó và công bố để tổ chức họp.
* **Chốt trước phiên bỏ phiếu**: Đến trước phiên bỏ phiếu, Ban kiểm tra tư cách cổ đông sẽ cập nhật lại một lần nữa số lượng cổ đông tham dự họp và số phiếu biểu quyết tính đến thời điểm đó.
* Hiển thị cảnh báo trực tuyến (tín hiệu xanh nếu Tỷ lệ > 50%).
* Tính tổng Cổ phần trực tiếp/Ủy quyền, so sánh tổng sở hữu VSDC.
* Phát hành báo cáo/biên bản Thẩm tra Tư cách sử dụng Template động Loại 5 *(Tham khảo tài liệu mẫu `ExempleTemplate_file/9. Bao cao tham tra tu cach co dong.doc`)*.

### Bước 7: Kiểm phiếu & Xác nhận kết quả
**Mục tiêu**: Ghi nhận và quyết toán các lựa chọn Bầu cử / Biểu quyết từ cổ đông.
* **Hỗ trợ 5 Loại phiếu/Bầu cử**: Tính năng gom chung module báo cáo. Xử lý các nghiệp vụ Kiểm phiếu Bầu Dồn phiếu (Cumulative Voting) và Biểu quyết thường.
* **Quét và Ghi nhận Data**: Ghi nhận dữ liệu từ phiếu thông qua 2 cách: (1) Quét mã QR/Barcode trên phiếu tải tự động hoặc (2) Gõ tay mã cổ đông (hỗ trợ autocomplete gợi ý mã gần đúng).
* **Nguyên tắc nhập liệu mặc định**: 
  * Đối với **Phiếu biểu quyết**: Mặc định toàn bộ các nội dung là "Tán thành". Nếu trên phiếu có chọn "Không tán thành" hoặc "Ý kiến khác", nhân viên kiểm phiếu mới thực hiện thao tác tích chuột hoặc dùng phím tắt để chuyển đến ô tương ứng.
  * Đối với **Phiếu bầu cử**: Mặc định hệ thống tự chia đều tỷ lệ phiếu bầu cho các ứng cử viên.
* **Tính toán kết quả**: 
  * Đánh giá tổng quát: Tính tổng thu, tổng phát, đánh giá lượng phiếu Hợp lệ / Không hợp lệ dựa trên tổng số Cổ phần phát ra với quyền.
  * Đánh giá theo nội dung: Đối với từng nội dung biểu quyết cụ thể, hệ thống sẽ phân loại chi tiết theo phiếu Hợp lệ (Tán thành, Không tán thành, Ý kiến khác) và Không hợp lệ. Tỷ lệ phần trăm (%) tương ứng cho từng trạng thái bầu chọn sẽ được tính dựa trên mẫu số là **tổng số phiếu hợp lệ thu về**.

### Bước 8: Report Center (Hệ thống Báo cáo Kết xuất)
**Mục tiêu**: Xuất bảng kê, biên bản cuộc họp và cung cấp snapshot các danh sách làm căn cứ lưu trữ và thông báo UBCKNN.
* Phân ra 4 nhóm: 
  * Nhóm A: Kết quả Biểu quyết & Biên bản Kiểm phiếu *(Tham khảo mẫu biên bản tại `ExempleTemplate_file/10. Bien ban kiem phieu DHDCD.docx`)*.
  * Nhóm B: Kết quả Bầu cử nhân sự.
  * Nhóm C: Xuất 3 dạng danh sách cổ đông kiểm toán (Template Loại 6).
  * Nhóm D: Các báo cáo tổng hợp và Audit Log.

---

## 4. QUẢN TRỊ BIỂU MẪU (TEMPLATE MANAGEMENT SYSTEM)
Đây là hệ thống lõi tách rời cung cấp biểu mẫu đa năng dưới dạng file Word (`.docx`).
* Tích hợp 6 mẫu định dạng chia loại rõ ràng: Thư mời, Thẻ BQ, Phiếu BQ, Phiếu bầu HĐQT/BKS, Biên bản kiểm tra Tư cách, Biên bản Kiểm phiếu.
* **Hỗ trợ đa ngôn ngữ linh hoạt**: Hệ thống cho phép thiết lập và tải lên nhiều cấu hình template dựa theo tiêu chuẩn của từng Công ty (Tiếng Việt riêng, Tiếng Anh riêng, hoặc Song ngữ). Đặc biệt có cơ chế tự động nhận diện thông tin cổ đông (ví dụ: cổ đông nước ngoài) để tự kích hoạt và in ấn Thư mời, Thẻ hay các loại Phiếu bầu bằng Mẫu biểu Tiếng Anh / Song ngữ tương ứng.
* Giao diện tải khuôn mẫu và tự động ánh xạ Data Fields (Mã CĐ, Barcode, Bảng NQT). Hỗ trợ khóa bảo mật (Lock Finalize) biểu mẫu. Template chỉ cấu hình 1 lần trước đại hội.
* Xử lý đoạn tài liệu rỗng (Dynamic Section) loại bỏ các khu vực rỗng một cách tự động trước khi chuyển đổi PDF phát hành thông qua LibreOffice Render tích hợp.

## 5. QUY TẮC HIỆU LỰC DỮ LIỆU ĐỘNG (BALLOT LIFECYCLE)
Toàn bộ việc cấp mới, tiêu hủy và bù trừ phiếu bầu phải tuân thủ nghiêm ngặt **Ballot Lifecycle Cascade Service**:
1. Lần Check-in đầu tiên: Tạo `ACTIVE` phiếu.
2. Xung đột Ủy quyền mới/Hủy ủy quyền: Tự động ghi chú đánh dấu `INVALIDATED` lên phiếu chủ cũ báo cáo cho quầy in (Reprint Queue), tái tạo thế bản phiếu biểu quyết bù vào lượng cổ phần cập nhật (`REGENERATE`).
3. Dữ liệu ghi nhận: Tính nhật ký đóng dấu Timestamp (`created_at`, `invalidated_at`) và Audit tracking (Ai điều chỉnh, trên bàn nào, máy tính POS nào).
4. Khóa giao dịch phân tán lạc quan (Optimistic Concurrency) được áp dụng tại môi trường làm việc > 5 thiết bị Check-in độc lập trên LAN Local.

## 6. LỘ TRÌNH TRIỂN KHAI & BẢO MẬT DỮ LIỆU (COMPETITIVE ADVANTAGE)
Hệ thống được thiết kế theo lộ trình 2 giai đoạn nhằm đáp ứng tâm lý bảo mật khắt khe của các Công ty Đại chúng, đồng thời tạo ưu thế cạnh tranh đặc biệt khi chào bán giải pháp:

* **Giai đoạn 1 (MVP-Core - Phục vụ trực tiếp & Cài đặt On-Premise)**:
  * **Chủ quyền Dữ liệu tuyệt đối (Vũ khí cạnh tranh)**: Mỗi Tổ chức phát hành sẽ triển khai cài đặt một hệ thống đóng gói chạy độc lập trên máy chủ của họ. Toàn bộ dữ liệu VSDC và thông tin bầu cử mang tính nội bộ được lưu trữ **100% tại máy tính local của khách hàng**. Hệ thống cam kết không bóc tách hay truyền tải dữ liệu khách hàng cho bên thứ ba (kể cả nhà cung cấp nền tảng). Sau khi cài đặt, khách hàng có toàn quyền kiểm soát, khai thác hoặc xóa bỏ dữ liệu của mình.
  * **Offline Local LAN**: Ứng dụng hoạt động mượt mà, độc lập trên mạng LAN cục bộ tại nơi tổ chức đại hội mà không cần kết nối Internet ngoài, hạn chế tối đa rủi ro đường truyền. Áp dụng SignalR push realtime lên màn hình máy chiếu một cách trơn tru.
  * **Database Transaction (Tính toàn vẹn)**: Mọi thao tác check-in hay ủy quyền chéo được nén trong các Transaction giao dịch nguyên tử (Atomic). Nếu có sự cố bất ngờ ngắt điện, CSDL tự động ROLLBACK bảo vệ tránh mất phương hướng dữ liệu mồ côi (orphaned data).
  * **Performance**: POS hoạt động dựa vào thiết kế ưu tiên phím tắt. Tốc độ đọc thẻ, quét QRCode hiển thị dưới 1 giây.

* **Giai đoạn 2 (Scale-up - E-Voting & Đăng ký Online)**:
  * Chỉ khi mô hình lõi On-premise tĩnh (Giai đoạn 1) đã chạy đủ tải vững chắc, hệ thống mới tiến hành mở rộng giao tiếp Internet mở.
  * Xây dựng Cổng **Đăng ký tham dự Online** và cung cấp nền tảng **Bỏ phiếu Điện tử (E-Voting / E-AGM)** từ xa cho các vị trí cổ đông không tới hội trường.
