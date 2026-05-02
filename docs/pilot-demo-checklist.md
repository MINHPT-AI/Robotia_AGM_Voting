# 🎬 Pilot Demo Checklist — Robotia AGM Voting System

> Sử dụng checklist này cho buổi demo trước stakeholders.
> Mục tiêu: chứng minh hệ thống đã sẵn sàng cho pilot.

---

## Pre-Demo Setup (15 phút trước)

- [ ] Docker Desktop đang chạy
- [ ] `docker compose -f docker/docker-compose.yml up -d --build`
- [ ] Truy cập http://localhost:8080 → thấy trang Login
- [ ] Chuẩn bị file VSDC mẫu (50-100 rows) trên Desktop
- [ ] Chuẩn bị file VSDC 1000 rows cho performance demo

---

## Demo Script

### 1. Authentication & Security (2 phút)

| # | Action | Expected | ✅ |
|---|--------|----------|---|
| 1.1 | Mở http://localhost:8080 | Redirect → /login | |
| 1.2 | Nhập sai password → Login | Hiện thông báo lỗi, không crash | |
| 1.3 | Nhập admin / Admin@123 → Login | Redirect → /change-password (lần đầu) | |
| 1.4 | Đổi mật khẩu → Submit | Redirect → Dashboard | |

### 2. Company Management (2 phút)

| # | Action | Expected | ✅ |
|---|--------|----------|---|
| 2.1 | Menu → Công ty → Thêm mới | Form tạo công ty hiện ra | |
| 2.2 | Điền thông tin → Lưu | Thông báo thành công, danh sách cập nhật | |
| 2.3 | Click vào công ty → Sửa → Lưu | Thông tin được cập nhật | |

### 3. Meeting CRUD (3 phút)

| # | Action | Expected | ✅ |
|---|--------|----------|---|
| 3.1 | Menu → Cuộc họp → Tạo mới | Form tạo cuộc họp | |
| 3.2 | Nhập tiêu đề, ngày, địa điểm → Lưu | Trạng thái = "Mới" | |
| 3.3 | Thêm 2 nghị quyết + 2 ứng viên | Hiển thị đúng số lượng | |
| 3.4 | Clone cuộc họp | Bản sao xuất hiện (có " (Bản sao)") | |
| 3.5 | Xóa cuộc họp (không có cổ đông) | Soft-delete thành công | |

### 4. ⭐ VSDC Import — Main Feature (5 phút)

| # | Action | Expected | ✅ |
|---|--------|----------|---|
| 4.1 | Chọn cuộc họp → Tab Cổ đông | Trang import hiển thị | |
| 4.2 | Upload file VSDC 50 rows | Parsing + validation warnings | |
| 4.3 | Click Import → Xác nhận | 50 rows imported thành công | |
| 4.4 | Upload lại cùng file → Import | Vẫn 50 rows (Wipe-and-Reload) | |
| 4.5 | **Upload file 1000 rows → Import** | **Hoàn thành < 10 giây** ⚡ | |
| 4.6 | Kiểm tra DataGrid cổ đông | 1000 rows hiển thị, scroll mượt | |

### 5. Performance Benchmark (1 phút)

| Metric | Target | Actual | ✅ |
|--------|--------|--------|---|
| Import 1,000 rows | < 10s | ~2s | |
| Page load (meetings list) | < 3s | | |
| Login response | < 2s | | |

---

## Post-Demo

- [ ] Teardown: `docker compose -f docker/docker-compose.yml down -v`
- [ ] Ghi nhận feedback từ stakeholders
- [ ] Cập nhật `docs/project_journey.md` với kết quả demo

---

## Test Results Summary

```
✅ Unit Tests:        39/39 passed
✅ Integration Tests: 11/11 passed (Testcontainers + PostgreSQL 16)
✅ Performance Gate:  1,000 rows imported in ~2 seconds
🔲 E2E Tests:        4 scenarios scaffolded (run with docker-compose)
```

## Quality Gate Checklist

- [x] All unit tests pass (39)
- [x] All integration tests pass (11)
- [x] Performance benchmark met (1000 rows < 10s → ~2s actual)
- [x] E2E tests scaffolded with Playwright
- [x] CI pipeline configured (3 jobs)
- [x] Docker build succeeds
- [x] Quick Start Guide written
- [x] This Pilot Demo Checklist written
