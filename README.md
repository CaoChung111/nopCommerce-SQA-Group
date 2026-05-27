# ĐẢM BẢO CHẤT LƯỢNG CHO PHẦN MỀM NOPCOMMERCE

Dự án này lưu trữ toàn bộ các tài liệu, kịch bản và mã nguồn phục vụ cho quá trình kiểm thử tự động (Automation Testing), kiểm thử hiệu năng (Performance Testing), kiểm thử bảo mật (Security Testing) và lập kế hoạch đảm bảo chất lượng phần mềm cho hệ thống thương mại điện tử **nopCommerce**.

## 👥 Thông tin nhóm thực hiện
- **Nhóm - Lớp:** 1-20252IT6085001
- **Thành viên:**
  - Nguyễn Văn Tiến Anh - 2023604685
  - Bùi Cao Chung - 2023604761
  - Vũ Mạnh Cường - 2023604002
  - Bùi Huy Hoàng - 2023604824

---

## 📂 Cấu trúc thư mục

Dưới đây là mô tả chi tiết về các thành phần bên trong thư mục dự án:

### 1. `SQA_nopCommerce.xlsx`
Tài liệu Excel chứa **tất cả các sheet quá trình của việc đảm bảo chất lượng phần mềm**, không chỉ bao gồm Kế hoạch kiểm thử (Test Plan) và Kịch bản kiểm thử (Test Cases) mà còn các hoạt động quản lý chất lượng khác xuyên suốt vòng đời phát triển/kiểm thử hệ thống nopCommerce.

### 2. `testing-playwright-nopcommerce/` (Kiểm thử chức năng giao diện - UI Automation)
Thư mục chứa mã nguồn kiểm thử tự động cho giao diện web sử dụng **Playwright** với ngôn ngữ **C# (.NET 8)**.
- Đã bao phủ **đầy đủ 20 User Stories (US)**, từ các chức năng cơ bản của người dùng (Đăng nhập, Giỏ hàng, Đánh giá,...) đến các chức năng quản trị viên (Admin - Sản phẩm, Danh mục, Đơn hàng,...).
- Framework sử dụng: **MSTest / NUnit**.
- Vui lòng xem hướng dẫn cài đặt và chạy test chi tiết tại file [README.md trong thư mục này](./testing-playwright-nopcommerce/README.md).

### 3. `testing-jmeter/` (Kiểm thử hiệu năng - Performance Testing)
Thư mục chứa các kịch bản kiểm thử hiệu năng và tải.
- File cấu hình chính: `Test-Nopcommerce-Performance.jmx`.
- Công cụ sử dụng: **Apache JMeter**.
- Kịch bản này được sử dụng để giả lập tải lượng người dùng truy cập vào hệ thống nopCommerce nhằm đánh giá hiệu năng của hệ thống (Response Time, Throughput, Error Rate,...).

### 4. `testing-OWASP-ZAP/` (Kiểm thử bảo mật - Security Testing)
Thư mục chứa các báo cáo và cấu hình kiểm thử bảo mật lỗ hổng ứng dụng web.
- Công cụ sử dụng: **OWASP ZAP (Zed Attack Proxy)**.
- Bao gồm file báo cáo HTML (ví dụ: `2026-05-22-ZAP-Report-.html`) liệt kê các rủi ro bảo mật tiềm ẩn và các lỗ hổng đã được quét trên hệ thống nopCommerce.

---

## 🚀 Tóm tắt các công cụ và công nghệ

- **Quản lý quy trình SQA:** Excel
- **UI Automation Testing:** Playwright, C# (.NET 8), NUnit/MSTest
- **Performance Testing:** Apache JMeter
- **Security Testing:** OWASP ZAP
