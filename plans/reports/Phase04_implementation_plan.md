# Phase 4 — VSDC Excel Import Wizard: Kế hoạch Thực thi (v2 - Final)

## Mục tiêu

Import danh sách cổ đông từ file Excel VSDC (16 cột cố định) qua Wizard 4 bước.
- **Performance target**: Parse + Import 1,000 dòng < 10 giây.
- **Chỉ hỗ trợ file `.xlsx`** — Không CSV.

---

## Phần 0: Phân tích Cấu trúc File Excel VSDC

> File VSDC là **báo cáo thô** từ VSDC, KHÔNG phải spreadsheet data sạch.

### 0.1 — Bố cục tổng thể

```
DÒNG 1-12:  Phần mở đầu báo cáo (SKIP)
DÒNG 13:    ⭐ HEADER CHÍNH (tìm "STT")
DÒNG 14:    SUB-HEADER (Chưa lưu ký | Lưu ký | Tổng cộng)
DÒNG 15:    DÒNG SỐ CỘT (1 | 2 | 3 | ... | 16)
DÒNG 16+:   DỮ LIỆU + SECTION HEADERS xen kẽ:
             I. MÔI GIỚI TRONG NƯỚC
               1. Cá nhân → DATA ROWS
               2. Tổ chức → DATA ROWS
             II. MÔI GIỚI NƯỚC NGOÀI
               1. Cá nhân → DATA ROWS
               2. Tổ chức → DATA ROWS
             TỔNG CỘNG → DỪNG ĐỌC
CUỐI:       Chữ ký + Footer
```

### 0.2 — Merged Cells: 29 cột vật lý → 16 cột logic

| Cột logic | Tên trường | Cột Excel (1-based) | Field DB |
|:---------:|-----------|:-------------------:|----------|
| 1 | STT | **2** (B) | `VsdcRow` |
| 2 | Họ và tên | **3** (C) | `FullName` ✅ |
| 3 | Mã SID | **4** (D) | `Sid` |
| 4 | Mã NĐT | **6** (F) | `InvestorCode` |
| 5 | **Số ĐKSH** | **8** (H) | `IdNumber` ✅ PK |
| 6 | Ngày cấp | **10** (J) | `IdIssueDate` |
| 7 | Địa chỉ | **12** (L) | `Address` |
| 8 | Email | **14** (N) | `Email` |
| 9 | Điện thoại | **15** (O) | `Phone` |
| 10 | **Quốc tịch** | **17** (Q) | `Nationality` ✅ |
| 11 | CP Chưa lưu ký | **18** (R) | `SharesNonDeposit` |
| 12 | CP Lưu ký | **20** (T) | `SharesDeposit` |
| 13 | CP Tổng | **21** (U) | `SharesTotal` |
| 14 | QBQ Chưa lưu ký | **23** (W) | `RightsNonDeposit` |
| 15 | QBQ Lưu ký | **26** (Z) | `RightsDeposit` |
| 16 | **QBQ Tổng** | **27** (AA) | `VotingRights` ✅ |

### 0.3 — Số format VN: dấu chấm = hàng nghìn

`18.600` = 18,600 · `7.952.200` = 7,952,200 · Parser phải `Replace(".", "")`.

### 0.4 — Ngày format: dd/MM/yyyy (+ xử lý OADate từ Excel)

---

## Phần 1: 9 Vấn đề phát hiện & Biện pháp khắc phục

### 🔴 CRITICAL

| # | Vấn đề | Fix |
|---|--------|-----|
| 1 | Entity `Shareholder` THIẾU fields: `VsdcRow`, `DisplayOrder`, `IsOrganization`, `IsForeign` | Thêm fields + tạo migration |
| 2 | Section header detection lỗi do merged cell (text có thể ở col C/D, không phải col B) | Check cả 3 cột (STT + Họ tên + SID) |
| 3 | Performance: EF AddRange+SaveChanges ~5-8s cho 1000 rows | Dùng raw SQL `INSERT ... ON CONFLICT` + `unnest()` → ~1-2s |
| 4 | MediatR assembly scan phải cover `Mms.Infrastructure` | Verify `RegisterServicesFromAssembly` |

### 🟡 IMPORTANT

| # | Vấn đề | Fix |
|---|--------|-----|
| 5 | Upload file size limit chưa đặt | Set explicit 20MB + validate MIME `.xlsx` |
| 6 | `currentSubSection` không reset khi chuyển section | Reset = "" khi gặp section mới |
| 7 | ClosedXML không support `.xls` cũ → exception khó hiểu | Check extension + magic bytes trước parse |
| 8 | Progress reporting cho user khi parse/import lâu | `MudProgressLinear` hiển thị % |
| 9 | Re-import: nếu file mới thiếu CĐ cũ → giữ nguyên (an toàn) | Upsert only, KHÔNG auto-delete |

---

## Phần 2: Thứ tự Thực thi (8 Bước)

### BƯỚC 1 — Verify Domain Entity + Migration
### BƯỚC 2 — NuGet ClosedXML
### BƯỚC 3 — Parsing Pipeline (VsdcParser + Mapper + Validator)
### BƯỚC 4 — Application Layer (DTOs + Commands + Queries)
### BƯỚC 5 — Infrastructure Handlers (Bulk Upsert SQL)
### BƯỚC 6 — UI Wizard 4 bước
### BƯỚC 7 — Register MediatR + Navigation
### BƯỚC 8 — Build + Migration + Test

Chi tiết code xem Execution Prompt gốc từ User.
