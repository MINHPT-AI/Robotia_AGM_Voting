# BUSINESS REQUIREMENTS DOCUMENT (BRD)

## Hệ thống Quản lý Đại hội Cổ đông — Robotia AGMPro

\---

|Thuộc tính|Nội dung|
|-|-|
|**Tên tài liệu**|Business Requirements Document - Quy trình Tổ chức Đại hội Cổ đông|
|**Mã tài liệu**|AGM-BRD-001|
|**Phiên bản**|2.3|
|**Ngày phát hành**|28/04/2026|
|**Trạng thái**|Đã duyệt|
|**Căn cứ pháp lý**|Luật Doanh nghiệp 2020 (Điều 115, 141, 144, 145, 148)|
|**Tài liệu liên quan**|BRD Phụ lục Kỹ thuật v1.3 · IPO Check-in v2.2 · IPO Check-in Screen v1.1 · IPO Proxy Ballot v1.1 · Brainstorm Architecture 20/04/2026|

\---

## MỤC LỤC

1. Tổng quan tài liệu (Executive Summary)
2. Phạm vi nghiệp vụ (Business Scope)
3. Các bên liên quan (Stakeholders)
4. Thuật ngữ và định nghĩa (Glossary)
5. Quy trình nghiệp vụ 8 bước (Business Process)
6. Hệ thống quản trị biểu mẫu (Template Management)
7. Vòng đời phiếu biểu quyết (Ballot Lifecycle)
8. Yêu cầu phi chức năng (Non-Functional Requirements)
9. Lộ trình triển khai (Deployment Roadmap)
10. Ma trận truy xuất yêu cầu (Requirements Traceability)

\---

## 1\. TỔNG QUAN TÀI LIỆU (EXECUTIVE SUMMARY)

### 1.1 Mục tiêu dự án

Tài liệu này định nghĩa các yêu cầu nghiệp vụ chuẩn hóa cho hệ thống Robotia AGMPro - nền tảng quản lý toàn bộ vòng đời tổ chức Đại hội Cổ đông (ĐHCĐ) của các tổ chức phát hành (TCPH) tại Việt Nam.

Mục tiêu cốt lõi là chuyển đổi quy trình từ hệ thống WinForms mã nguồn cứng sang kiến trúc web động, hỗ trợ xử lý linh hoạt các tình huống phát sinh trong thực tế vận hành ĐHCĐ, đồng thời đảm bảo tính toàn vẹn dữ liệu pháp lý và tốc độ xử lý cao tại quầy check-in.

### 1.2 Phát biểu vấn đề (Problem Statement)

Hệ thống cũ gặp 4 hạn chế trọng yếu:

**Hạn chế 1 - Cứng nhắc về biểu mẫu:** Template in phiếu được hardcode, không thể tùy biến theo từng TCPH hay đại hội. Mỗi thay đổi yêu cầu can thiệp kỹ thuật.

**Hạn chế 2 - Không xử lý được phiếu phức hợp:** Hệ thống không có cơ chế để một người đại diện cho nhiều cổ đông có thể giữ nhiều phiếu với ý kiến biểu quyết khác nhau (yêu cầu phổ biến tại các tổ chức quản lý quỹ lớn như HSBC, Dragon Capital).

**Hạn chế 3 - Mất kiểm soát phiếu sau khi phát:** Không có công cụ để Ban kiểm phiếu liên hệ thu hồi phiếu chưa nộp, dẫn đến mẫu số nghị quyết bị ảnh hưởng.

**Hạn chế 4 - Không đáp ứng yêu cầu vận hành bổ sung:** Không theo dõi được quà tặng, không tra cứu nhanh STT VSDC khi cổ đông ký nhận.

### 1.3 Phạm vi giải quyết

Hệ thống mới giải quyết toàn bộ 4 hạn chế trên, đồng thời thiết kế theo kiến trúc on-premise để đáp ứng yêu cầu bảo mật dữ liệu tuyệt đối của các công ty đại chúng Việt Nam.

Hệ thống được thiết kế phục vụ các TCPH có **quy mô cổ đông dưới 15.000 người** theo danh sách VSDC chốt, với **số người tham dự trực tiếp tại hội trường dưới 1.000 người** và **tối đa 20 máy POS** (đăng ký + kiểm phiếu) hoạt động đồng thời trên cùng một hạ tầng LAN. Các ngưỡng này là điều kiện thiết kế cho Giai đoạn 1; trường hợp TCPH vượt ngưỡng cần đánh giá riêng về hạ tầng và khả năng mở rộng (xem Mục 8.5).

\---

## 2\. PHẠM VI NGHIỆP VỤ (BUSINESS SCOPE)

### 2.1 Trong phạm vi (In Scope)

* Quản lý thông tin doanh nghiệp và thiết lập cuộc họp
* Import và xử lý dữ liệu cổ đông từ VSDC
* Phát hành thư mời hàng loạt có mã QR, chữ ký và con dấu tự động
* Kết xuất file gửi bưu điện và theo dõi trạng thái giao thư
* Quản lý ủy quyền trước và trong ngày họp
* Check-in, in phiếu và quản lý vòng đời phiếu biểu quyết
* Tách phiếu theo nhóm ý kiến biểu quyết
* Thu thập số điện thoại người tham dự phục vụ thu hồi phiếu
* Theo dõi nhận quà tặng và ký nhận
* Thẩm tra tư cách cổ đông (3 Danh sách)
* Kiểm phiếu biểu quyết và bầu cử
* Xuất báo cáo và biên bản

### 2.2 Ngoài phạm vi (Out of Scope - Giai đoạn 1)

* Bỏ phiếu điện tử từ xa (E-Voting)
* Đăng ký tham dự online qua Internet
* Tích hợp trực tiếp với hệ thống VSDC (import thủ công)
* Ứng dụng di động cho cổ đông

\---

## 3\. CÁC BÊN LIÊN QUAN (STAKEHOLDERS)

|Vai trò|Trách nhiệm trong hệ thống|Quyền hạn|
|-|-|-|
|Trưởng ban tổ chức|Cấu hình cuộc họp, phê duyệt chốt thẩm tra, override đặc biệt|Cao nhất|
|Nhân viên check-in|Check-in cổ đông, in phiếu, ghi SĐT, tick quà tặng|Thao tác tại quầy|
|Trưởng quầy|Hủy/in lại phiếu, ủy quyền tại chỗ, xem SĐT trong panel kiểm phiếu|Cao hơn nhân viên check-in|
|Ban kiểm phiếu|Kiểm phiếu, nhập kết quả, xem SĐT phiếu chưa thu, chốt kết quả|Chỉ Module kiểm phiếu|
|Cổ đông / Người đại diện|Người dùng cuối thực tế (không truy cập hệ thống trực tiếp)|Không áp dụng|

\---

## 4\. THUẬT NGỮ VÀ ĐỊNH NGHĨA (GLOSSARY)

|Thuật ngữ|Định nghĩa|
|-|-|
|**VSDC**|Trung tâm Lưu ký Chứng khoán Việt Nam - nguồn dữ liệu cổ đông chính thức|
|**ĐKSH**|Số Đăng ký sổ hữu - định danh cổ đông trong hệ thống VSDC (Cột 5 file VSDC)|
|**Phiên tham dự (Attendance Record)**|Bản ghi ghi nhận sự hiện diện của một cổ đông tại cuộc họp. Một cổ đông chỉ có tối đa một Phiên tham dự tại một cuộc họp|
|**Người tham dự trực tiếp (Physical Attendee)**|Người vật lý có mặt tại hội trường, định danh bằng CMND/CCCD/Hộ chiếu. Một người có thể đại diện cho nhiều cổ đông|
|**Phiếu (Ballot)**|Đơn vị vật lý phát cho người tham dự. Một người có thể nhận nhiều phiếu khi tách theo nhóm ý kiến biểu quyết|
|**Ballot Lifecycle**|Vòng đời phiếu - cơ chế tự động hủy phiếu cũ và tạo phiếu mới khi có thay đổi ủy quyền|
|**Danh sách 1**|Danh sách Cổ đông dự họp - đếm theo cổ đông VSDC, dùng kiểm tra điều kiện họp (Điều 145)|
|**Danh sách 2**|Danh sách Người tham dự trực tiếp và Phiếu phát ra - đếm theo người vật lý và phiếu, dùng kiểm soát phiếu|
|**Danh sách 3**|Danh sách Cổ đông tham dự và biểu quyết - đếm cổ đông đã nộp phiếu, là mẫu số nghị quyết (Điều 148)|
|**Split Ballot**|Tách phiếu theo nhóm ý kiến biểu quyết - khi một người đại diện nhiều cổ đông có ý định biểu quyết khác nhau|
|**Mẫu số nghị quyết**|Tổng cổ phần đại diện bởi phiếu thực tế thu về (không phải phiếu phát ra)|
|**TCPH**|Tổ chức Phát hành - công ty đại chúng tổ chức ĐHCĐ|
|**Mã ĐH KH**|Mã đơn hàng do hệ thống tự sinh khi tạo thư mời, theo quy tắc `TM-\\\\\\\[Mã CK]-\\\\\\\[STT VSDC]`. Dùng để đối chiếu với file bưu điện|
|**Mã ĐH bưu điện**|Mã đơn hàng do công ty bưu điện cấp sau khi nhận danh sách gửi thư. Dùng để tra cứu trên hệ thống bưu điện và trả lời cổ đông|
|**Placeholder ảnh**|Ảnh chèn sẵn trong file .docx template, có Alt Text là `{{CHU\\\\\\\_KY}}` hoặc `{{CON\\\\\\\_DAU}}`. Hệ thống nhận diện và thay bằng ảnh chữ ký/con dấu thật khi render|

\---

## 5\. QUY TRÌNH NGHIỆP VỤ 8 BƯỚC (BUSINESS PROCESS)

> \\\\\\\*\\\\\\\*Sơ đồ tổng thể vòng đời cuộc họp:\\\\\\\*\\\\\\\*
> `Mới tạo` → `Đang chuẩn bị` → `Check-in` → `Đang họp` → `Kiểm phiếu` → `Hoàn tất`

\---

### BƯỚC 1 - Quản lý Thông tin Doanh nghiệp và Cuộc họp

**Mục tiêu:** Chuẩn bị nền tảng thông tin cho đơn vị tổ chức và sự kiện.

#### 1.1 Thông tin Doanh nghiệp

Thiết lập một lần, áp dụng cho tất cả cuộc họp của TCPH:

* Tên công ty (tiếng Việt và tiếng Anh)
* Mã số thuế, vốn điều lệ, tổng cổ phần phát hành
* Tổng cổ phần có quyền biểu quyết
* Thông tin đại diện pháp luật
* **Mã chứng khoán (3 ký tự)** — dùng sinh Mã ĐH KH khi phát hành thư mời
* **Ảnh chữ ký** (PNG nền trong suốt) — của người ký thư mời, dùng render tự động
* **Ảnh con dấu** (PNG nền trong suốt) — dấu tròn đỏ của TCPH, dùng render tự động

#### 1.2 Thiết lập Cuộc họp

|Thông tin|Yêu cầu|
|-|-|
|Loại đại hội|Thường niên (AGM) / Bất thường (EGM)|
|Ngày, giờ, địa điểm họp|Bắt buộc|
|Mốc thời gian chốt danh sách VSDC|Bắt buộc - làm cơ sở import|
|Cấu hình quà tặng|Bật/tắt tính năng theo dõi quà tặng (mặc định: tắt)|
|Chế độ in phiếu mặc định|Gộp (IN-1) / Tách theo cổ đông nguồn (IN-2) / Hybrid (IN-3)|
|Ngưỡng thông qua từng nội dung|Cấu hình theo Điều lệ công ty (mặc định: 50% cho nghị quyết thường, 65% cho nghị quyết quan trọng)|

#### 1.3 Thiết lập nội dung Đại hội

* Danh sách nội dung tờ trình biểu quyết (sẽ in trên Phiếu biểu quyết)
* Danh sách ứng viên bầu cử TVHĐQT — danh sách **riêng biệt** (sẽ in trên Phiếu bầu TVHĐQT)
* Danh sách ứng viên bầu cử BKS — danh sách **riêng biệt** (sẽ in trên Phiếu bầu BKS)

> \\\\\\\*\\\\\\\*Nguyên tắc tách Phiếu bầu:\\\\\\\*\\\\\\\* Phiếu bầu TVHĐQT và Phiếu bầu BKS là \\\\\\\*\\\\\\\*hai phiếu độc lập\\\\\\\*\\\\\\\*, được tạo thông tin và in riêng nhau. Lý do: (1) số lượng ứng viên, quy tắc bầu và số ghế của HĐQT và BKS thường khác nhau; (2) kết quả bầu TVHĐQT và BKS được kiểm đếm và công bố riêng biệt trong biên bản; (3) một đại hội có thể bầu HĐQT mà không bầu BKS (hoặc ngược lại) — trường hợp đó chỉ phát phiếu bầu của loại có ứng viên.
>
> \\\\\\\*\\\\\\\*Thực tế phát sinh:\\\\\\\*\\\\\\\* Số lượng và tên các nội dung tờ trình thay đổi theo từng đại hội và từng TCPH. Hệ thống không được hardcode số lượng nội dung. Cấu hình phải hoàn chỉnh trước khi chuyển sang Bước 5 (Check-in) vì nội dung này in trực tiếp lên phiếu.

\---

### BƯỚC 2 - Import Danh sách Cổ đông (Dữ liệu VSDC)

**Mục tiêu:** Nạp dữ liệu cổ đông chốt quyền dự họp từ VSDC làm nền tảng cho toàn bộ quy trình.

#### 2.1 Đặc thù file VSDC

File VSDC là file Excel báo cáo thô với nhiều hàng tiêu đề và ô gộp (merged cells). Hệ thống phải có bộ đọc (parser) xử lý trực tiếp định dạng này mà không yêu cầu người dùng làm sạch file trước.

**16 cột chuẩn theo thứ tự cố định** - không cho phép cấu hình lại:

|Cột|Nội dung|Vai trò trong hệ thống|
|-|-|-|
|1|STT|Số thứ tự cổ đông trên danh sách VSDC - dùng tra cứu ký nhận quà|
|2|Họ và tên|Tên cổ đông|
|3|Mã định danh NĐT (SID)||
|4|Mã nhà đầu tư (Investor code)||
|5|Số ĐKSH|**Định danh chính** (CMND/CCCD/Passport)|
|6|Ngày cấp|Kết hợp với Cột 5 để xác định cổ đông duy nhất|
|7|Địa chỉ||
|8|Email||
|9|Điện thoại|**Nguồn số điện thoại tự động** - lấy khi check-in|
|10|Quốc tịch|Xác định template in song ngữ hay không|
|11-15|Các cột phân loại sở hữu||
|16|SL Quyền phân bổ Tổng cộng|**Số cổ phần có quyền biểu quyết** - nguồn duy nhất|

> \\\\\\\*\\\\\\\*Nguyên tắc quan trọng:\\\\\\\*\\\\\\\* Cột 16 là nguồn dữ liệu DUY NHẤT cho số cổ phần có quyền biểu quyết. Hệ thống không tính toán lại từ các cột khác.

#### 2.2 Quy trình Import 4 bước (Wizard)

**Bước 2.1 - Upload:** Tải lên file Excel nguyên bản (.xlsx, .xls) từ VSDC.

**Bước 2.2 - Ánh xạ tự động:** Hệ thống ánh xạ theo mảng 16 cột cố định, không cần người dùng mapping thủ công.

**Bước 2.3 - Preview và Validate:** Phát hiện và cảnh báo:

* Bản ghi khuyết CMND/ĐKSH
* Số lượng cổ phần bằng 0
* Vi phạm tổng vốn điều lệ
* **Cặp bản ghi trùng Số ĐKSH nhưng khác Ngày cấp** (xử lý xem 2.3)

**Bước 2.4 - Xác nhận nạp:** Cho phép nạp bổ sung đè lên dữ liệu cũ theo trường định danh CMND/ĐKSH.

#### 2.3 Xử lý trùng Số ĐKSH khác Ngày cấp

#### 

#### Thực tế phát sinh: Một người vật lý có thể mở tài khoản chứng khoán tại 2 công ty chứng khoán khác nhau vào 2 thời điểm khác nhau. VSDC tạo ra 2 bản ghi với cùng Số ĐKSH nhưng khác Ngày cấp. Đây không phải lỗi — đây là đặc thù dữ liệu cần xử lý có kiểm soát.

#### Thực tế phát sinh bổ sung về thời điểm MERGE: Trước đây BRD quy định MERGE chỉ được phép trước khi bắt đầu Check-in. Tuy nhiên thực tế vận hành cho thấy điều này không khả thi: khi phát thư mời, TCPH vẫn gửi theo 2 dòng địa chỉ riêng biệt như 2 cổ đông độc lập. Chỉ đến khi người đó xuất hiện tại quầy check-in, nhân viên mới có thể xác minh tận mặt "2 dòng VSDC này có thực sự là 1 người không". Do đó MERGE được phép thực hiện ngay tại quầy check-in, với điều kiện có cảnh báo tự động và xác nhận kép tại chỗ.

#### 

#### Hai giai đoạn xử lý:

#### Giai đoạn trước ngày họp (sau import VSDC):

#### Hệ thống phát hiện tự động các cặp trùng ĐKSH và trình Ban tổ chức duyệt sơ bộ:

#### Quyết địnhMô tảXác nhậnKEEP\_SEPARATE (mặc định)Giữ 2 bản ghi độc lập, gửi 2 thư mời riêng1 người phê duyệtLINKĐánh dấu cùng 1 người vật lý, giữ 2 bản ghi, bật cảnh báo tự động tại quầy1 người phê duyệtMERGE (nếu đã chắc chắn)Gộp thành 1 bản ghi, cộng tổng cổ phần, gửi 1 thư mời2 người phê duyệt độc lập

#### Giai đoạn tại quầy check-in (ngày họp):

#### Khi nhân viên quét CCCD hoặc QR của người đến dự, nếu hệ thống phát hiện số ĐKSH trùng với một bản ghi khác (dù trước đó đã đánh dấu KEEP\_SEPARATE hay LINK), màn hình hiển thị cảnh báo màu vàng ngay lập tức:

#### ⚠ PHÁT HIỆN 2 TÀI KHOẢN CÙNG SỐ CCCD

#### &#x20; Tài khoản 1: \[Tên] — Ngày cấp \[A] — \[X] CP — STT VSDC \[n]

#### &#x20; Tài khoản 2: \[Tên] — Ngày cấp \[B] — \[Y] CP — STT VSDC \[m]

#### &#x20; 

#### &#x20; Xác nhận đây có phải 2 tài khoản của cùng 1 người không?

#### &#x20; \[Có — tiến hành MERGE ngay]   \[Không — check-in độc lập]

#### Nếu chọn MERGE tại quầy: yêu cầu xác nhận kép (Trưởng quầy + 1 thành viên), ghi Audit Log đầy đủ với lý do và ảnh CCCD (nếu có thiết bị scan), tổng hợp cổ phần và phát 1 bộ phiếu duy nhất.

#### 

#### Nguyên tắc: Hệ thống không tự động gộp bất kỳ cặp nào. Mọi quyết định do con người xác nhận. MERGE tại quầy không thể hoàn tác sau khi xác nhận — nếu phát hiện sai, phải liên hệ Trưởng ban tổ chức để can thiệp với ghi chú đặc biệt trong Audit Log.

#### 

\---

### BƯỚC 3 - Phát hành Thư mời

**Mục tiêu:** Tạo thư mời hàng loạt cho toàn bộ cổ đông trong DS VSDC, tích hợp chữ ký và con dấu tự động, kết xuất file in và file gửi bưu điện, theo dõi trạng thái giao thư từ đầu đến cuối.

\---

#### 3.1 Điều kiện tiên quyết

Trước khi thực hiện Bước 3, hai điều kiện sau phải hoàn tất:

1. **Danh sách cổ đông đã import thành công** (Bước 2) — DS VSDC là nguồn dữ liệu duy nhất cho phần "Kính gửi" của thư mời.
2. **Template thư mời đã được tạo** trong Module Template chung (xem Mục 6) — người dùng upload file `.docx` mẫu đã soạn sẵn, trong đó có chứa các placeholder dữ liệu và placeholder ảnh theo quy ước.

\---

#### 3.2 Cơ chế Template thư mời

**3.2.1 Placeholder dữ liệu (điền vào phần "Kính gửi")**

Người dùng chèn các placeholder dạng text vào vị trí tương ứng trong file `.docx`:

|Placeholder|Trường dữ liệu|Bắt buộc|
|-|-|-|
|`{{HO\\\\\\\_TEN}}`|Họ và tên cổ đông (Cột 2 VSDC)|Có|
|`{{DIA\\\\\\\_CHI}}`|Địa chỉ (Cột 7 VSDC)|Có|
|`{{DIEN\\\\\\\_THOAI}}`|Điện thoại (Cột 9 VSDC)|Có|
|`{{SO\\\\\\\_DKSK}}`|Số ĐKSH (Cột 5 VSDC)|Tuỳ chọn|
|`{{SO\\\\\\\_CO\\\\\\\_PHAN}}`|Số cổ phần (Cột 16 VSDC)|Tuỳ chọn|
|`{{MA\\\\\\\_QR}}`|Mã QR mã hóa ĐKSH|Tự động sinh|

Người dùng cấu hình bật/tắt 2 trường tuỳ chọn (`{{SO\\\\\\\_DKSK}}` và `{{SO\\\\\\\_CO\\\\\\\_PHAN}}`) tại bước khởi tạo lô thư mời — áp dụng đồng nhất cho toàn bộ lô.

> \\\\\\\*\\\\\\\*Xử lý thiếu dữ liệu bắt buộc:\\\\\\\*\\\\\\\* Nếu một cổ đông không có Địa chỉ hoặc Điện thoại trong VSDC, hệ thống cảnh báo và đưa cổ đông đó vào danh sách "Cần bổ sung" — không tạo thư cho cổ đông này cho đến khi dữ liệu được bổ sung thủ công.

**3.2.2 Placeholder chữ ký và con dấu**

Cơ chế sử dụng **placeholder ảnh trong file `.docx`** — không dùng placeholder text cho chữ ký/con dấu vì vị trí và kích thước ảnh cần được kiểm soát chính xác trong Word.

Quy trình người dùng chuẩn bị template:

1. Tại vị trí ký tên trong Word: chèn ảnh placeholder (ảnh tùy ý có kích thước bằng đúng vùng ký mong muốn)
2. Click chuột phải vào ảnh → **Edit Alt Text** → nhập `{{CHU\\\\\\\_KY}}`
3. Tại vị trí con dấu: tương tự, Alt Text là `{{CON\\\\\\\_DAU}}`
4. Hai ảnh có thể đặt chồng lên nhau trong Word (con dấu đè lên góc dưới chữ ký) — vị trí và kích thước sẽ được giữ nguyên khi render

Khi hệ thống render thư:

* Quét toàn bộ `InlineShape` trong file `.docx` bằng `python-docx`
* Tìm `InlineShape` có `description` (Alt Text) khớp `{{CHU\\\\\\\_KY}}` → thay bằng ảnh chữ ký từ Thông tin doanh nghiệp
* Tìm `InlineShape` có `description` khớp `{{CON\\\\\\\_DAU}}` → thay bằng ảnh con dấu từ Thông tin doanh nghiệp
* Ảnh con dấu phải là PNG nền trong suốt (alpha channel) để lớp đè trông tự nhiên, giống ký và đóng dấu thật

> \\\\\\\*\\\\\\\*Lưu ý kỹ thuật:\\\\\\\*\\\\\\\* Ảnh chữ ký và ảnh con dấu được lưu tập trung tại Thông tin doanh nghiệp (Bước 1), không upload lại mỗi lô thư mời. Nếu chữ ký thay đổi (đổi người ký), chỉ cần cập nhật tại Thông tin doanh nghiệp, tất cả lô thư mời tiếp theo sẽ tự động dùng ảnh mới.

\---

#### 3.3 Quy trình tạo thư mời hàng loạt (6 bước)

**Bước 3.1 — Khởi tạo lô thư mời**

Người dùng chọn:

* Cuộc họp áp dụng
* Template thư mời (từ danh sách đã upload trong Module Template)
* Nguồn danh sách: toàn bộ DS VSDC hoặc chọn lọc theo điều kiện

Hệ thống hiển thị tổng số thư sẽ được tạo và số cổ đông cần bổ sung dữ liệu (nếu có).

**Bước 3.2 — Cấu hình trường thông tin**

|Trường|Loại|Hành vi|
|-|-|-|
|Họ và tên|Bắt buộc|Luôn hiển thị trong thư, không thể bỏ|
|Địa chỉ|Bắt buộc|Luôn hiển thị trong thư, không thể bỏ|
|Điện thoại|Bắt buộc|Luôn hiển thị trong thư, không thể bỏ|
|Số ĐKSK|Tuỳ chọn|Checkbox — người dùng tích để thêm vào phần Kính gửi|
|Số cổ phần|Tuỳ chọn|Checkbox — người dùng tích để thêm vào phần Kính gửi|

**Bước 3.3 — Preview mẫu**

Hệ thống render thử 1 thư hoàn chỉnh (lấy dữ liệu cổ đông đầu tiên trong DS) để người dùng kiểm tra bố cục, chữ ký, con dấu trước khi chạy hàng loạt. Nếu có sai lệch, người dùng quay lại chỉnh template và upload lại — không cần tạo lại toàn bộ cấu hình.

**Bước 3.4 — Tạo hàng loạt**

Hệ thống sinh N thư (N = số cổ đông hợp lệ trong lô), gộp tất cả vào 1 file duy nhất theo thứ tự STT VSDC. Hai định dạng xuất ra song song:

* `.docx` — phù hợp khi cần chỉnh sửa thêm hoặc chỉnh format trước khi in
* `.pdf` — phù hợp để in trực tiếp, không bị lệch font trên máy in

**Bước 3.5 — Kết xuất file gửi bưu điện**

Song song với file thư, hệ thống tự động xuất 1 file Excel riêng để giao cho bưu điện. File này gồm đúng 4 cột theo thứ tự STT VSDC:

|Cột|Nội dung|Quy tắc sinh|
|-|-|-|
|Mã ĐH KH|Mã đơn hàng khách hàng|`TM-\\\\\\\[Mã CK]-\\\\\\\[STT VSDC]` — ví dụ: `TM-ASM-001`|
|Họ và tên|Tên người nhận|Từ Cột 2 VSDC|
|Địa chỉ|Địa chỉ giao thư|Từ Cột 7 VSDC|
|Điện thoại|Số điện thoại|Từ Cột 9 VSDC|

Mã CK lấy từ cấu hình Thông tin doanh nghiệp. STT là số thứ tự trong DS VSDC — nhất quán xuyên suốt toàn bộ quy trình. Người dùng điền cột "Mã ĐH KH" khi nộp danh sách cho bưu điện, làm căn cứ đối chiếu sau này.

**Bước 3.6 — Xác nhận phát hành**

Sau khi in và gửi bưu điện, người dùng cập nhật trạng thái lô thư trên hệ thống. Mỗi thư trong lô chuyển sang trạng thái `Đã gửi bưu điện`.

\---

#### 3.4 Vòng đời trạng thái thư mời

```
Đã tạo → Đã in → Đã gửi bưu điện → Đang vận chuyển → Giao thành công
                                                      ↘ Chuyển hoàn
```

3 trạng thái đầu do người dùng cập nhật thủ công theo hành động thực tế. 3 trạng thái cuối (`Đang vận chuyển`, `Giao thành công`, `Chuyển hoàn`) được cập nhật tự động từ file báo cáo bưu điện (xem Mục 3.6).

\---

#### 3.5 Màn hình quản lý danh sách thư mời

Màn hình hiển thị toàn bộ thư mời của một cuộc họp với các cột:

|Cột|Nội dung|
|-|-|
|STT|Số thứ tự VSDC|
|Mã ĐH KH|Mã hệ thống tự sinh — dùng đối chiếu|
|Mã ĐH bưu điện|Mã do bưu điện cấp sau khi nhận danh sách — dùng tra cứu trên hệ thống bưu điện và trả lời cổ đông|
|Họ tên cổ đông||
|Số ĐKSK||
|Trạng thái|Chip màu theo vòng đời|
|Ngày phát|Từ báo cáo bưu điện|
|Thao tác|Nút **Xem** và nút **Sửa**|

**Tìm kiếm:** Hỗ trợ tra cứu theo Họ tên, Số ĐKSK, Mã ĐH KH, và **Mã ĐH bưu điện** — để nhân viên có thể tra ngay khi cổ đông đọc mã đơn bưu điện qua điện thoại.

**Nút Xem:** Mở modal hiển thị đầy đủ thông tin bao gồm:

* Mã ĐH KH và Mã ĐH bưu điện (hiển thị song song)
* Họ tên, Địa chỉ, Điện thoại
* Trạng thái, Ngày phát, Người ký nhận (khi giao thành công)
* Lý do hoàn (khi chuyển hoàn)

**Nút Sửa:** Mở modal cho phép chỉnh sửa Địa chỉ và Điện thoại (2 trường dễ sai), kèm trường ghi chú lý do sửa thủ công. Trạng thái không được sửa tay — chỉ cập nhật qua file bưu điện.

\---

#### 3.6 Theo dõi giao thư qua bưu điện

**3.6.1 Cơ chế map Mã ĐH KH**

Khi bưu điện gửi trả file báo cáo kết quả giao thư (thường hàng tuần), hệ thống map dựa trên **Mã ĐH KH** (cột do phía gửi tự điền khi nộp danh sách). Đây là map 1-1, chính xác 100%, không cần fuzzy matching.

> \\\\\\\*\\\\\\\*Lưu ý:\\\\\\\*\\\\\\\* Trường hợp bưu điện không điền lại Mã ĐH KH trong file báo cáo, hệ thống tự động fallback sang fuzzy match Họ tên + Điện thoại và đưa các dòng này vào tab "Cần xem xét" để người dùng xác nhận thủ công.

**3.6.2 Trường thông tin lấy từ file bưu điện**

|Trường trong file bưu điện|Lưu vào|Ghi chú|
|-|-|-|
|Mã ĐH bưu điện (cột B)|`invitation\\\\\\\_letters.postal\\\\\\\_order\\\\\\\_code`|Hiển thị trên màn hình và trong modal Xem|
|Trạng thái (cột D)|`invitation\\\\\\\_letters.postal\\\\\\\_status`|Cập nhật vòng đời trạng thái|
|Ngày phát (cột J)|`invitation\\\\\\\_letters.delivery\\\\\\\_date`||
|Người ký nhận (cột I)|`invitation\\\\\\\_letters.recipient\\\\\\\_name`|Hiển thị trong modal Xem khi giao thành công|
|Lý do không thành công (cột K)|`invitation\\\\\\\_letters.return\\\\\\\_reason`|Hiển thị trong modal Xem khi chuyển hoàn|

**3.6.3 Quy tắc upload nhiều lần**

Bưu điện gửi báo cáo theo tuần nên cùng 1 lô thư mời có thể được cập nhật nhiều lần. Quy tắc áp dụng mỗi lần upload:

* Dòng chưa có trạng thái cuối → cập nhật theo file mới nhất
* Dòng đã ở `Giao thành công` hoặc `Chuyển hoàn` → **không ghi đè**, giữ nguyên
* Mỗi lần upload lưu 1 bản ghi lịch sử riêng: tên file, ngày giờ upload, người upload, số dòng cập nhật
* Không xóa lịch sử các lần upload trước

**3.6.4 Dashboard theo dõi**

Màn hình "Gửi bưu điện \& theo dõi" hiển thị:

* Thống kê tổng hợp: tổng thư, đã giao thành công, đang vận chuyển, chuyển hoàn
* Khu vực upload file báo cáo mới
* Lịch sử tất cả các lần upload với số liệu kết quả từng lần

\---

#### 3.7 Yêu cầu kỹ thuật bổ sung

* **Đa ngôn ngữ:** Tự động chọn template tiếng Anh/song ngữ cho cổ đông nước ngoài (dựa trên Cột 10 VSDC)
* **Mã QR:** Mã hóa Số ĐKSH cổ đông, in trên thư mời để quét tại quầy check-in
* **Render engine:** `python-docx` xử lý placeholder và thay ảnh, `LibreOffice` chuyển đổi sang PDF
* **Batch size:** Hệ thống xử lý batch theo nhóm, hiển thị progress bar — không block UI khi tạo lô lớn (>500 thư)

\---

### BƯỚC 4 - Ủy quyền Trước Ngày Họp

**Mục tiêu:** Ghi nhận quan hệ ủy quyền từ hồ sơ gửi qua bưu điện hoặc nộp trực tiếp trước ngày họp.

#### 4.1 Quy tắc nghiệp vụ ủy quyền

**UQ-01 - Phạm vi ủy quyền:** Cổ đông được ủy quyền toàn bộ hoặc một phần cổ phần cho một hoặc nhiều người. Tổng cổ phần ủy quyền không được vượt số cổ phần VSDC.

**UQ-02 - Tối đa 1 tầng ủy quyền:** Người đang nhận ủy quyền không được ủy quyền tiếp phần cổ phần đó cho bên thứ ba. Người đó vẫn có thể ủy quyền cổ phần riêng của mình.

**UQ-03 - Xác thực người nhận ủy quyền:**

* Nếu là cổ đông trong VSDC: xác thực qua Số ĐKSH
* Nếu không phải cổ đông (nhân viên, luật sư, đại diện tổ chức...): ghi nhận CMND/CCCD, họ tên, đơn vị, và **số điện thoại** (khuyến nghị mạnh - phục vụ liên hệ thu hồi phiếu trong ngày họp)

**UQ-04 - Trạng thái ủy quyền:** `PENDING` → `CONFIRMED` (sau khi người nhận check-in) / `CANCELLED` / `SUPERSEDED`

> \\\\\\\*\\\\\\\*Thực tế phát sinh:\\\\\\\*\\\\\\\* Người nhận ủy quyền bên ngoài VSDC (không có Cột 9 điện thoại) là nhóm rủi ro nhất khi cần thu hồi phiếu. Trường số điện thoại trong bảng `proxy\\\\\\\_recipients` cần được khuyến nghị điền ngay từ bước này.

#### 4.2 Màn hình SC-01 - Luồng ủy quyền

Xem chi tiết tại tài liệu: **IPO Proxy Ballot v1.1 - Phần SC-01**.

Tóm tắt 5 tình huống chính:

|Mã|Tình huống|
|-|-|
|UQ-1|Ủy quyền toàn bộ cho 1 người|
|UQ-2|Ủy quyền một phần cho 1 người|
|UQ-3|Ủy quyền cho nhiều người (nhiều lần ghi nhận)|
|UQ-4|Hủy ủy quyền đã ghi nhận|
|UQ-5|Người nhận ủy quyền không phải cổ đông (nhập mới vào `proxy\\\\\\\_recipients`)|

\---

### BƯỚC 5 - Check-in, Ủy quyền Tại chỗ và In Phiếu

**Mục tiêu:** Hoàn tất xác nhận tư cách tham dự, phát phiếu biểu quyết và ghi nhận các thông tin vận hành (SĐT, quà tặng). Đây là bước có yêu cầu tốc độ cao nhất - mục tiêu dưới 30 giây mỗi giao dịch.

#### 5.1 Các phương thức định danh người tham dự

|Phương thức|Điều kiện|
|-|-|
|Quét mã QR|Cổ đông đã nhận thư mời có QR|
|Quét Barcode|Cổ đông đã nhận thư mời có barcode|
|Tìm kiếm văn bản (CMND/ĐKSH/Tên)|Khi không có thư mời hoặc QR hỏng|

Yêu cầu: kết quả tra cứu hiển thị dưới 1 giây.

#### 5.2 Ba Danh sách tham dự chính thức

> \\\\\\\*\\\\\\\*Thực tế phát sinh và Nguyên tắc then chốt:\\\\\\\*\\\\\\\* Trên thực tế vận hành ĐHCĐ tồn tại 3 khái niệm "danh sách tham dự" phục vụ 3 mục đích pháp lý và nghiệp vụ khác nhau, nhưng nhiều hệ thống trước đây chỉ quản lý 1 danh sách. Điều này dẫn đến nhầm lẫn giữa "số người có mặt", "số cổ đông đủ điều kiện" và "số cổ đông thực sự biểu quyết". Hệ thống phải duy trì đồng thời và phân biệt rõ 3 danh sách sau.

**Danh sách 1 - Cổ đông dự họp (Attending Shareholders List)**

* **Mục đích pháp lý:** Kiểm tra điều kiện tiến hành họp theo Điều 145 Luật Doanh nghiệp 2020
* **Đơn vị đếm:** Cổ đông theo VSDC (không phải người vật lý)
* **Cách tính:** Mỗi cổ đông có Phiên tham dự ACTIVE được tính 1 lần, bất kể trực tiếp hay ủy quyền
* **Thời điểm chốt:** Tại Chốt lần 1 (khai mạc) và Chốt lần 2 (trước bỏ phiếu)
* **Trường thông tin chính:** STT VSDC, Họ tên cổ đông, Số ĐKSH, Số CP, Hình thức tham dự, Người đại diện

**Danh sách 2 - Người tham dự trực tiếp và Phiếu phát ra (Physical Attendee \& Ballot Issuance List)**

* **Mục đích nghiệp vụ:** Kiểm soát tổng số phiếu phát ra, phục vụ đối chiếu khi kiểm phiếu và liên hệ thu hồi phiếu chưa nộp
* **Đơn vị đếm:** Người vật lý (theo CMND/CCCD/Hộ chiếu) và số phiếu. Số phiếu có thể lớn hơn số người khi có Split Ballot
* **Trường thông tin chính:**

|Trường|Nguồn|Bắt buộc|
|-|-|-|
|STT người tham dự|Tự động theo thứ tự check-in|Có|
|Họ tên người tham dự|VSDC hoặc nhập tay|Có|
|CMND/CCCD/Hộ chiếu|VSDC hoặc nhập tay|Có|
|Số điện thoại|VSDC Cột 9 (tự động) hoặc nhập tay|Khuyến nghị mạnh|
|Thời gian check-in|Hệ thống ghi tự động|Có|
|Số phiếu được phát|Đếm phiếu ACTIVE|Có|
|Chi tiết từng phiếu|Mã phiếu, CP đại diện, đại diện cho ai, STT VSDC cổ đông gốc|Có|
|Trạng thái nhận quà|Chưa nhận / Đã nhận|Khi đại hội có quà tặng|

**Danh sách 3 - Cổ đông tham dự và biểu quyết (Voting Shareholder List)**

* **Mục đích pháp lý:** Xác định mẫu số nghị quyết theo Điều 148 Luật Doanh nghiệp 2020
* **Đơn vị đếm:** Cổ đông VSDC đã nộp phiếu (subset của Danh sách 1)
* **Thời điểm xác định:** Chỉ sau khi Ban kiểm phiếu hoàn tất thu và nhập toàn bộ phiếu
* **Trường thông tin chính:** STT VSDC, Họ tên cổ đông, Số ĐKSH, Số CP, Mã phiếu tương ứng, Kết quả biểu quyết

> \\\\\\\*\\\\\\\*Nguyên tắc:\\\\\\\*\\\\\\\* Danh sách 3 ⊆ Danh sách 1. Cổ đông trong Danh sách 1 nhưng không trong Danh sách 3 là cổ đông đã dự họp nhưng không nộp phiếu (về sớm). Cổ phần này bị loại khỏi mẫu số nghị quyết một cách tự nhiên mà không cần cơ chế check-out.

#### 5.3 Cấu hình in phiếu và Split Ballot

**5.3.0 Bốn loại thẻ/phiếu phát tại check-in**

Khi một cổ đông hoàn tất check-in, hệ thống in và phát đồng thời theo gói:

|Loại|Điều kiện phát|Mô tả|
|-|-|-|
|**Thẻ biểu quyết** (Template 2)|Luôn phát|Thẻ định danh tham dự — cổ đông giữ trong suốt đại hội|
|**Phiếu biểu quyết** (Template 3)|Luôn phát|Dùng để bỏ phiếu các nội dung tờ trình|
|**Phiếu bầu TVHĐQT** (Template 4a)|Chỉ phát khi đại hội có bầu cử HĐQT|Phiếu bầu thành viên Hội đồng quản trị|
|**Phiếu bầu BKS** (Template 4b)|Chỉ phát khi đại hội có bầu cử BKS|Phiếu bầu thành viên Ban kiểm soát|

> \\\\\\\*\\\\\\\*Nguyên tắc in gói:\\\\\\\*\\\\\\\* Hệ thống in tất cả loại phiếu áp dụng trong 1 lệnh in duy nhất. Nhân viên không cần chọn từng loại thủ công. Cấu hình bật/tắt bầu cử HĐQT và BKS thực hiện ở cấp cuộc họp tại Bước 1 — tự động áp dụng cho toàn bộ check-in.
>
> \\\\\\\*\\\\\\\*Cấu hình template và mã (QR/Barcode):\\\\\\\*\\\\\\\* Mỗi loại thẻ/phiếu được gán 1 template riêng (chọn từ Module Template mẫu) và loại mã riêng (QR hoặc Barcode) tại thời điểm thiết lập đại hội. Cấu hình này không thay đổi trong suốt ngày họp.

**5.3.1 Ba chế độ in — áp dụng cho Phiếu biểu quyết và Phiếu bầu**

|Chế độ|Mô tả|Ví dụ|
|-|-|-|
|IN-1 Gộp (Consolidated)|Mỗi người tham dự nhận 1 phiếu tổng hợp tất cả cổ phần đại diện|HSBC đại diện 5 quỹ → 1 phiếu "HSBC (đại diện Quỹ A, B, C, D, E) - 4.500.000 CP"|
|IN-2 Tách theo nguồn (Split by Source)|Mỗi cổ đông nguồn 1 phiếu riêng|HSBC đại diện 5 quỹ → 5 phiếu|
|IN-3 Hybrid|Mặc định gộp, override tại quầy theo từng cổ đông nguồn||

**5.3.2 Split Ballot by Voting Position (Tách phiếu theo nhóm ý kiến biểu quyết)**

> \\\\\\\*\\\\\\\*Thực tế phát sinh:\\\\\\\*\\\\\\\* Các tổ chức quản lý quỹ lớn (HSBC, Dragon Capital, Vietcombank...) thường nhận ủy quyền từ nhiều quỹ. Trên thực tế, có những cuộc họp mà 2 trong 5 quỹ có ý định biểu quyết khác 3 quỹ kia đối với một nội dung cụ thể. Người đại diện yêu cầu được tách phiếu thành nhiều phiếu để có thể điền ý kiến riêng cho từng nhóm. Hệ thống cũ không hỗ trợ điều này, buộc người đại diện phải bỏ phiếu theo 1 ý kiến duy nhất và vi phạm nghĩa vụ ủy thác.

**Nguyên tắc:**

* Việc phân nhóm do người tham dự tự khai báo tại quầy check-in
* Tên trên phiếu là tên người tham dự vật lý, kèm danh sách cổ đông gốc trong ngoặc
* Ràng buộc cứng: tổng CP các phiếu tách = tổng CP người đó đại diện (RB-10)
* Mỗi cổ đông nguồn chỉ xuất hiện trong 1 nhóm phiếu (RB-11)
* Khi có Ballot Lifecycle ảnh hưởng đến người đang có phiếu tách, toàn bộ phiếu tách bị hủy và cần cấu hình lại (L8)

**Mã phiếu khi tách:** `\\\\\\\[Mã tham dự]-\\\\\\\[Số thứ tự phiếu]`
Ví dụ: `AST-2026-00089-1`, `AST-2026-00089-2`, `AST-2026-00089-3`

**Nội dung in trên phiếu tách:**

```
Người biểu quyết:  HSBC Securities (Vietnam) Company Limited
Đại diện UQ:       Quỹ A Vietnam Equity Fund, Quỹ B Dragon Capital II
Mã phiếu:         AST-2026-00089-1
Số cổ phần:       1.800.000
```

Xem chi tiết luồng xử lý tại: **IPO Check-in v2.2 - Tình huống SPLIT**.

#### 5.4 Quản lý số điện thoại người tham dự

> \\\\\\\*\\\\\\\*Thực tế phát sinh:\\\\\\\*\\\\\\\* Đến lúc bỏ phiếu, nhiều người tham dự trực tiếp quên không nộp phiếu. Ban kiểm phiếu cần liên hệ ngay những người này. Không có số điện thoại trong danh sách là điểm mù nghiêm trọng. Một số TCPH phải cử nhân viên đi tìm trong hội trường theo tên, rất mất thời gian và không phải lúc nào cũng kịp.

**Nguồn dữ liệu theo thứ tự ưu tiên:**

1. **Cột 9 file VSDC** - tự động lấy khi cổ đông có trong VSDC
2. **Bảng `proxy\\\\\\\_recipients`** - nếu người được ủy quyền đã từng được nhập trước đó
3. **Nhập thủ công tại quầy** - cho người không có trong VSDC và chưa từng nhập

**Lưu trữ 2 nơi:**

* `attendance\\\\\\\_records.phone\\\\\\\_number` - dùng trong ngày họp (Danh sách 2)
* `proxy\\\\\\\_recipients.phone\\\\\\\_number` - lưu lâu dài để tái sử dụng các cuộc họp sau

**Phân quyền xem:** Chỉ Trưởng quầy, Ban kiểm phiếu và Trưởng ban tổ chức. Nhân viên check-in không xem được danh sách SĐT của người khác.

#### 5.5 Quản lý quà tặng và ký nhận

> \\\\\\\*\\\\\\\*Thực tế phát sinh:\\\\\\\*\\\\\\\* Nhiều TCPH tặng quà cho cổ đông tham dự. Quà tặng cần được phân phát có kiểm soát và cổ đông phải ký nhận trên danh sách in sẵn từ VSDC. Hai vấn đề phát sinh: (a) không biết ai đã nhận, ai chưa; (b) khi có người đại diện nhiều cổ đông đến nhận quà, không biết phải tra số thứ tự nào trên danh sách giấy để ký.

**Yêu cầu:**

* Tính năng bật/tắt ở cấp cuộc họp (mặc định tắt)
* Nhân viên tick "Đã nhận quà" trên màn hình sau khi phát
* Hệ thống hiển thị **STT VSDC (Cột 1) của tất cả cổ đông gốc** mà người này đại diện - không phải STT của chính người đại diện
* Ví dụ: HSBC đại diện 5 quỹ → hiển thị STT của cả 5 quỹ trên danh sách VSDC để hướng dẫn ký nhận

**Màn hình hướng dẫn ký nhận:**

```
KÝ NHẬN QUÀ TẶNG - HSBC Securities (Vietnam)
Đại diện cho 5 cổ đông:
  STT 124  Quỹ A Vietnam Equity Fund      1.200.000 CP
  STT 215  Quỹ B Dragon Capital II          800.000 CP
  STT 389  Quỹ C Vina Value Fund          1.000.000 CP
  STT 412  Quỹ D Asia Alpha Fund            700.000 CP
  STT 503  Quỹ E KITMC Vietnam              800.000 CP
→ Đề nghị ký nhận tại STT 124, 215, 389, 412, 503
  trên danh sách VSDC in sẵn.
```

#### 5.6 Các tình huống check-in

Xem chi tiết luồng IPO tại: **IPO Check-in v2.2**.

Tóm tắt 9 tình huống hệ thống phải xử lý:

|Mã|Tình huống|
|-|-|
|F1|Cổ đông trực tiếp toàn phần|
|F2|Người nhận ủy quyền (không phải cổ đông hoặc đến với tư cách đại diện)|
|F3|Kết hợp: cổ đông giữ một phần, ủy quyền một phần|
|F4|Người nhận ủy quyền đồng thời là cổ đông trực tiếp|
|SPLIT|Tách phiếu theo nhóm ý kiến biểu quyết|
|CK-4|Cổ đông đến nhưng đã ủy quyền toàn bộ, muốn hủy ủy quyền|
|L-ONSITE|Ủy quyền tại chỗ (cổ đông đã check-in muốn ủy quyền)|
|L-CANCEL|Người nhận ủy quyền muốn trả lại ủy quyền|
|LINK|Phát hiện trùng Số ĐKSH|

#### 5.7 Reprint Queue

Khi Ballot Lifecycle kích hoạt hủy phiếu, bản ghi phiếu bị hủy được đưa tự động vào hàng đợi in lại (Reprint Queue). Phím tắt `F5` hoặc `Ctrl+P` để xác nhận và in ngay từ đầu hàng đợi.

\---

### BƯỚC 6 - Thẩm tra Tư cách Cổ đông

**Mục tiêu:** Xác nhận cuộc họp đủ điều kiện tiến hành và ghi nhận chính thức tổng hợp tham dự.

#### 6.1 Hai thời điểm chốt

**Chốt lần 1 - Để khai mạc:**

Khi tổng cổ phần của cổ đông dự họp vượt ngưỡng 50% (hoặc tỷ lệ quy định trong Điều lệ), Ban kiểm tra tư cách chốt và công bố để tổ chức họp. Hệ thống tạo snapshot số liệu làm căn cứ khai mạc. Hiển thị tín hiệu xanh khi đủ điều kiện.

**Chốt lần 2 - Trước phiên bỏ phiếu:**

Cập nhật bổ sung cổ đông đến muộn sau Chốt lần 1. Con số Chốt lần 2 luôn ≥ Chốt lần 1 vì hệ thống không theo dõi việc cổ đông rời hội trường.

> \\\\\\\*\\\\\\\*Nguyên tắc:\\\\\\\*\\\\\\\* Cổ đông về sớm vẫn được tính vào Danh sách 1 (Cổ đông dự họp) - ảnh hưởng đến điều kiện tiến hành họp. Nhưng nếu họ không nộp phiếu, họ bị loại tự nhiên khỏi Danh sách 3 (mẫu số nghị quyết). Hệ thống không cần cơ chế check-out vì 2 khái niệm này được tính theo 2 cơ chế khác nhau.

#### 6.2 Báo cáo thẩm tra 3 tầng

**Tầng 1 - Theo cổ đông (Danh sách 1):**

|Chỉ tiêu|Công thức|
|-|-|
|Tổng cổ đông có quyền dự họp|Tổng cổ đông trong VSDC (cố định)|
|Tổng cổ phần có quyền biểu quyết|Tổng Cột 16 VSDC (cố định)|
|Số cổ đông đã dự họp|Đếm cổ đông có Phiên tham dự ACTIVE (real-time)|
|Tổng cổ phần dự họp|Tổng CP phiếu ACTIVE (real-time)|
|Tỷ lệ dự họp (%)|CP dự họp / Tổng CP BQ × 100|

**Tầng 2 - Theo người vật lý và phiếu (Danh sách 2):**

|Chỉ tiêu|Ghi chú|
|-|-|
|Tổng người tham dự trực tiếp|Đếm theo CMND (thông tin tham khảo)|
|Tổng phiếu đã phát ra|Có thể lớn hơn số người khi có Split Ballot|
|Tổng phiếu thu về|Cập nhật sau mỗi lần nhập kiểm phiếu|
|Tổng phiếu chưa thu|Phát ra - Thu về|

**Tầng 3 - Theo biểu quyết thực tế (Danh sách 3):**

|Chỉ tiêu|Ghi chú|
|-|-|
|Số cổ đông đã biểu quyết|Cổ đông có phiếu COUNTED|
|Số cổ đông không biểu quyết|Phiếu NOT\_RETURNED|
|Mẫu số nghị quyết|Tổng CP phiếu thu về - **công thức duy nhất**|

#### 6.3 Phát hành báo cáo

Hệ thống xuất Báo cáo Thẩm tra Tư cách Cổ đông sử dụng Template Loại 5 với đầy đủ số liệu 3 tầng.

\---

### BƯỚC 7 - Kiểm phiếu và Xác nhận Kết quả

**Mục tiêu:** Ghi nhận, tổng hợp và công bố kết quả biểu quyết và bầu cử.

#### 7.1 Nguyên tắc mẫu số biểu quyết

> \\\\\\\*\\\\\\\*Nguyên tắc cốt lõi - không có ngoại lệ:\\\\\\\*\\\\\\\*

**Mẫu số = Tổng cổ phần đại diện bởi phiếu thực tế thu về (kể cả phiếu không hợp lệ)**

Đây là "Cổ đông tham dự và biểu quyết" theo Điều 148 Luật Doanh nghiệp 2020. Hệ thống chỉ có một công thức mẫu số, không có chế độ thay thế hay tùy chỉnh.

|Tình huống|Kết quả|
|-|-|
|Tất cả phiếu thu về|Mẫu số = Tổng CP dự họp (hai con số trùng nhau)|
|Có phiếu không thu về|Mẫu số < Tổng CP dự họp (người về sớm tự loại khỏi phép tính)|

> \\\\\\\*\\\\\\\*Lưu ý quan trọng:\\\\\\\*\\\\\\\* Bước 7 trong BRD gốc (v1.0) mô tả mẫu số là "tổng số phiếu hợp lệ thu về". Đây là cách diễn đạt không chính xác. Theo Điều 148 Luật Doanh nghiệp 2020 và thực tiễn pháp lý, \\\\\\\*\\\\\\\*phiếu không hợp lệ vẫn được tính vào mẫu số\\\\\\\*\\\\\\\* (vì đã thu về) nhưng không được tính vào tử số. Hệ thống thực thi đúng theo Luật, không theo cách diễn đạt trong BRD gốc.

#### 7.2 Phân loại phiếu

|Loại|Vào mẫu số|Vào tử số|
|-|-|-|
|Hợp lệ - Tán thành|Có|Có|
|Hợp lệ - Không tán thành|Có|Không|
|Hợp lệ - Ý kiến khác|Có|Không|
|Không hợp lệ (tẩy xóa, thiếu ký...)|**Có**|Không|
|Không thu về (về sớm, thất lạc)|**Không**|Không|

#### 7.3 Ghi nhận phiếu

**Phương thức 1 - Quét mã:** Quét QR/Barcode trên phiếu, hệ thống tự động tải thông tin. Hỗ trợ nhận diện phiếu tách theo hậu tố (-1, -2, -3) và hiển thị ngữ cảnh nhóm phiếu tách.

**Phương thức 2 - Nhập tay:** Gõ mã tham dự hoặc CMND với autocomplete. Dùng khi QR/Barcode hỏng.

#### 7.4 Nguyên tắc nhập liệu mặc định

* **Phiếu biểu quyết:** Mặc định toàn bộ nội dung = "Tán thành". Nhân viên chỉ thao tác khi có ý kiến khác.
* **Phiếu bầu cử:** Mặc định chia đều điểm bầu cho tất cả ứng viên (Cumulative Voting). Điều chỉnh khi phiếu có phân bổ khác.

#### 7.5 Phiếu chưa thu về và thu hồi

Khi phiếu chưa thu về, Ban kiểm phiếu mở Panel "Phiếu chưa thu về" từ Summary Bar. Panel hiển thị:

```
Mã phiếu          Người dự họp          CP        SĐT             Check-in
AST-2026-00045    Nguyễn Văn A          500.000   0912 345 678    08:32
AST-2026-00089-1  HSBC (Quỹ A, B, C)  1.200.000  0243 823 1234   09:15
AST-2026-00089-2  HSBC (Quỹ D)          150.000  0243 823 1234   09:15
AST-2026-00134    Trần Thị B            100.000  —               10:02
```

Nút "Sao chép số" để gọi điện trực tiếp. Trần Thị B không có SĐT hiển thị cảnh báo màu cam.

> \\\\\\\*\\\\\\\*Thực tế phát sinh:\\\\\\\*\\\\\\\* Đây là một trong những vấn đề thực tế phổ biến nhất tại ĐHCĐ. Tỷ lệ phiếu không thu về có thể lên đến 10-15% tại một số đại hội có nhiều cổ đông nhỏ lẻ. Việc có SĐT trong danh sách này là then chốt để Ban kiểm phiếu hành động kịp thời trong 30-60 phút trước khi đóng phiên bỏ phiếu.

#### 7.6 Kiểm phiếu nhanh (Bulk Approve Mode)

> \\\\\\\*\\\\\\\*Thực tế phát sinh:\\\\\\\*\\\\\\\* 80-90% phiếu tán thành 100% toàn bộ nội dung. Nhập từng phiếu tán thành riêng lẻ vừa chậm vừa không cần thiết. Ban kiểm phiếu thường chia xấp phiếu thành 2: xấp có ý kiến khác (nhập tay) và xấp tán thành (duyệt nhanh).

Điều kiện kích hoạt: đã nhập ít nhất 1 phiếu có ý kiến khác (xác nhận xấp có ý kiến đã được xử lý).

**Ràng buộc cứng không thể override:** Phiếu chưa thu về (NOT\_RETURNED) không bao giờ được đưa vào danh sách duyệt nhanh, dù người dùng yêu cầu.

Phiếu tách được xử lý độc lập trong Bulk Approve - mỗi phiếu tách là 1 tờ riêng biệt, có thể có kết quả khác nhau.

Xem chi tiết tại: **IPO Proxy Ballot v1.1 - Tình huống KP-5**.

#### 7.7 Hoàn tất và chốt kết quả

Yêu cầu xác nhận kép (2 thành viên Ban kiểm phiếu). Sau khi chốt, toàn bộ dữ liệu kiểm phiếu bị khóa - không thể chỉnh sửa.

Xuất Biên bản Kiểm phiếu (.docx) bao gồm:

* Tổng hợp số liệu (phân loại phiếu thường và phiếu tách)
* Kết quả từng nội dung tờ trình
* Kết quả bầu cử nhân sự (nếu có)
* Danh sách phiếu không hợp lệ
* Danh sách phiếu không thu về (kèm SĐT)
* Chữ ký Ban kiểm phiếu

\---

### BƯỚC 8 - Report Center (Hệ thống Báo cáo Kết xuất)

**Mục tiêu:** Xuất bảng kê, biên bản cuộc họp và snapshot các danh sách làm căn cứ lưu trữ và thông báo UBCKNN.

#### 8.1 Bốn nhóm báo cáo

**Nhóm A - Kết quả Biểu quyết và Biên bản Kiểm phiếu**

* Biên bản kiểm phiếu đầy đủ (xuất .docx từ Template)
* Bảng kết quả từng nội dung biểu quyết

**Nhóm B - Kết quả Bầu cử Nhân sự**

* Bảng điểm từng ứng viên
* Danh sách trúng cử theo ngưỡng

**Nhóm C - Ba Danh sách Cổ đông**

|Danh sách|Nội dung|Mục đích|
|-|-|-|
|Danh sách 1|Cổ đông dự họp|Căn cứ điều kiện họp - lưu trữ pháp lý|
|Danh sách 2|Người tham dự trực tiếp và phiếu phát ra|Kiểm toán nội bộ và đối chiếu|
|Danh sách 3|Cổ đông tham dự và biểu quyết|Căn cứ mẫu số nghị quyết - lưu trữ pháp lý|

**Nhóm D - Báo cáo Tổng hợp và Audit Log**

* Audit Log toàn bộ giao dịch (ai làm gì, lúc nào, trên thiết bị nào)
* Lịch sử Ballot Lifecycle
* Báo cáo tổng hợp vận hành đại hội

\---

## 6\. HỆ THỐNG QUẢN TRỊ BIỂU MẪU (TEMPLATE MANAGEMENT SYSTEM)

### 6.1 Tổng quan

Template Management là module lõi tách rời, cung cấp biểu mẫu in ấn động dưới dạng file Word (.docx) với ánh xạ data fields tự động.

### 6.2 Bảy loại template

|Loại|Tên|Khi nào dùng|Chọn mẫu|QR / Barcode|
|-|-|-|-|-|
|1|Thư mời|Bước 3 - Phát hành thư mời|Từ Module Template|QR hoặc Barcode, do người dùng chọn|
|2|Thẻ biểu quyết|Bước 5 - In tại check-in|Từ Module Template|QR hoặc Barcode, do người dùng chọn|
|3|Phiếu biểu quyết|Bước 5 - In tại check-in|Từ Module Template|QR hoặc Barcode, do người dùng chọn|
|4a|Phiếu bầu TVHĐQT|Bước 5 - In tại check-in (khi có bầu cử HĐQT)|Từ Module Template|QR hoặc Barcode, do người dùng chọn|
|4b|Phiếu bầu BKS|Bước 5 - In tại check-in (khi có bầu cử BKS)|Từ Module Template|QR hoặc Barcode, do người dùng chọn|
|5|Biên bản Thẩm tra Tư cách|Bước 6 - Báo cáo thẩm tra|Từ Module Template|Không áp dụng|
|6|Biên bản Kiểm phiếu|Bước 7 - Chốt kết quả|Từ Module Template|Không áp dụng|

> \\\\\\\*\\\\\\\*Gói phiếu đầy đủ cho 1 cổ đông:\\\\\\\*\\\\\\\* Một cổ đông đủ điều kiện khi check-in sẽ nhận đủ 4 loại: \\\\\\\*\\\\\\\*(1) Thẻ biểu quyết\\\\\\\*\\\\\\\* — định danh tham dự; \\\\\\\*\\\\\\\*(2) Phiếu biểu quyết\\\\\\\*\\\\\\\* — bỏ phiếu các tờ trình; \\\\\\\*\\\\\\\*(3) Phiếu bầu TVHĐQT\\\\\\\*\\\\\\\* — bỏ phiếu bầu Hội đồng quản trị (nếu đại hội có nội dung bầu HĐQT); \\\\\\\*\\\\\\\*(4) Phiếu bầu BKS\\\\\\\*\\\\\\\* — bỏ phiếu bầu Ban kiểm soát (nếu đại hội có nội dung bầu BKS). Phiếu bầu TVHĐQT và Phiếu bầu BKS là hai tờ riêng biệt, được tạo và in độc lập.
>
> \\\\\\\*\\\\\\\*Cấu hình QR / Barcode:\\\\\\\*\\\\\\\* Mỗi loại thẻ/phiếu được cấu hình riêng loại mã (QR hoặc Barcode) do người dùng tùy chọn tại thời điểm thiết lập template cho đại hội. Mã nhúng trên thẻ/phiếu là mã định danh phiếu, dùng để quét tại quầy kiểm phiếu (Bước 7).

### 6.3 Yêu cầu kỹ thuật

* **Đa ngôn ngữ:** Hệ thống tự động chọn template tiếng Anh hoặc song ngữ dựa trên Cột 10 (Quốc tịch) file VSDC
* **Dynamic Section:** Tự động loại bỏ các phần trống trước khi render PDF
* **Lock Finalize:** Khóa template sau khi cấu hình, không cho chỉnh sửa sau khi đã sử dụng trong đại hội
* **Render:** Chuyển đổi sang PDF thông qua LibreOffice Render tích hợp

\---

## 7\. VÒNG ĐỜI PHIẾU BIỂU QUYẾT (BALLOT LIFECYCLE)

### 7.1 Sơ đồ trạng thái

```
\\\\\\\[Check-in lần đầu] → ACTIVE ────────────────→ INVALIDATED
                        │                           │
                        │                     \\\\\\\[Reprint Queue]
                        │                           │
                        └──────────────────→ ACTIVE (mới)

\\\\\\\[Kiểm phiếu thu về]    → COUNTED
\\\\\\\[Không thu về]         → NOT\\\\\\\_RETURNED (sau khi chốt kiểm phiếu)
```

### 7.2 Bảng tình huống Ballot Lifecycle (L1-L8)

|Mã|Sự kiện kích hoạt|Phiếu bị hủy|Phiếu mới|
|-|-|-|-|
|L1|X hủy ủy quyền cho Y (Y đã có phiếu) → X tự tham dự|Phiếu Y phần CP của X|Phiếu X + Phiếu Y mới (chỉ CP riêng của Y)|
|L2|X hủy ủy quyền cho Y (Y chưa check-in)|Chỉ hủy bản ghi UQ|Phiếu X trực tiếp khi X check-in|
|L3|X đã check-in → ủy quyền toàn bộ cho Y|Phiếu X|Phiếu Y cộng thêm CP của X|
|L4|X đã check-in → ủy quyền một phần cho Y|Phiếu X|Phiếu X mới (CP giữ lại) + Phiếu Y (CP ủy quyền)|
|L5|X đã check-in → rút lui hoàn toàn|Phiếu X|Không có|
|L6|X hủy ủy quyền cho Y (Y đang có phiếu gộp nhiều người)|Phiếu Y gộp|Phiếu Y mới (trừ phần CP của X)|
|L7|Y đang có phiếu → C ủy quyền thêm cho Y|Phiếu Y cũ|Phiếu Y mới (cộng thêm CP của C)|
|L8 *(MỚI)*|Ballot Lifecycle xảy ra với người đang có phiếu tách|Toàn bộ phiếu tách|Yêu cầu cấu hình lại nhóm phiếu tại quầy|

> \\\\\\\*\\\\\\\*Nguyên tắc L8:\\\\\\\*\\\\\\\* Hệ thống không thể tự động tái cấu trúc nhóm phiếu tách vì phân nhóm là quyết định của người tham dự. Hệ thống hủy toàn bộ, tính lại tổng CP, và yêu cầu người đó ra quầy cấu hình lại.

### 7.3 Các ràng buộc cứng (Hard Constraints)

|Mã|Ràng buộc|
|-|-|
|RB-01|Tổng CP phiếu ACTIVE = Tổng CP đã check-in của cổ đông (bảo toàn cổ phần)|
|RB-02|Tối đa 1 tầng ủy quyền|
|RB-03|Khóa mọi thay đổi sau khi khai BQ|
|RB-04|Mỗi cổ đông chỉ có 1 Phiên tham dự ACTIVE tại 1 cuộc họp|
|RB-05|Không tạo phiếu mới khi phiếu cũ chưa INVALIDATED|
|RB-06|Mẫu số nghị quyết = CP thu về, không phải CP phát ra|
|RB-07|MERGE chỉ được phép trước khi bắt đầu Check-in|
|RB-08|MERGE không thể hoàn tác sau khi xác nhận|
|RB-09|Cấm gán Tán thành cho phiếu chưa thu về qua Bulk Approve|
|RB-10|Tổng CP phiếu tách = Tổng CP người đó đại diện|
|RB-11|Mỗi cổ đông nguồn chỉ xuất hiện trong 1 nhóm phiếu tách|
|RB-12|Số điện thoại chỉ hiển thị cho người có quyền xem|

\---

## 

