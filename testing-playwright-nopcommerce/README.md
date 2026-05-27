# NopCommerce Playwright C# Tests

## Cấu trúc project

```
csharp-playwright/
├── NopCommerceTests.csproj      # Project file
├── appsettings.env              # ⚠️ Cấu hình URL + tài khoản (cần chỉnh sửa)
├── .runsettings                 # NUnit runner config
├── TestConfig.cs                # Đọc biến môi trường
├── PlaywrightTestBase.cs        # Base class cho tất cả tests
├── Helpers/
│   ├── AuthHelper.cs            # Đăng nhập / Đăng xuất
│   └── NopHelper.cs             # Utilities chung (search, cart, assertions...)
└── Tests/
    ├── US01_LoginTests.cs       # Đăng nhập (10 TCs)
    ├── US02_ProfileTests.cs     # Cập nhật thông tin (9 TCs)
    ├── US03_ChangePasswordTests.cs  # Đổi mật khẩu (11 TCs)
    ├── US04_SearchTests.cs      # Tìm kiếm (13 TCs)
    ├── US05_CategoryTests.cs    # Danh mục (15 TCs)
    ├── US06_FilterTests.cs      # Bộ lọc (13 TCs)
    ├── US07_ProductDetailTests.cs   # Chi tiết SP (10 TCs)
    ├── US08_ReviewTests.cs      # Đánh giá (12 TCs)
    ├── US09_AddressTests.cs     # Địa chỉ (13 TCs)
    ├── US10_AddToCartTests.cs   # Thêm giỏ hàng (8 TCs)
    ├── US11_ShoppingCartTests.cs    # Giỏ hàng (11 TCs)
    ├── US12_CheckoutTests.cs    # Checkout (13 TCs)
    ├── US13_AdminCustomerTests.cs   # Admin - Khách hàng (17 TCs)
    ├── US14_AdminRolesTests.cs  # Admin - Vai trò (16 TCs)
    ├── US15_AdminCategoryTests.cs   # Admin - Danh mục (17 TCs)
    ├── US16_AdminSpecAttrTests.cs   # Admin - Spec Attributes (14 TCs)
    ├── US17_AdminProductTests.cs    # Admin - Sản phẩm (14 TCs)
    ├── US18_AdminBulkEditTests.cs   # Admin - Bulk Edit (10 TCs)
    └── US19_AdminOrderTests.cs  # Admin - Đơn hàng (8 TCs)
```

## Tổng số test cases: ~225 TCs

## ⚙️ Cấu hình trước khi chạy

### Bước 1: Chỉnh sửa `appsettings.env`

```env
BASE_URL=http://localhost:59580          # ← Sửa thành URL nopCommerce của bạn
ADMIN_EMAIL=admin@yourstore.com          # ← Email Admin
ADMIN_PASSWORD=admin123                  # ← Password Admin  
CUSTOMER_EMAIL=buihoang3425@gmail.com    # ← Email Customer
CUSTOMER_PASSWORD=123456                 # ← Password Customer
INACTIVE_EMAIL=inactive@gmail.com        # ← Tài khoản inactive (cho test negative)
```

### Bước 2: Cài đặt .NET 8 SDK (nếu chưa có)
```
https://dotnet.microsoft.com/download/dotnet/8.0
```

### Bước 3: Restore packages và cài Playwright browser
```powershell
cd "csharp-playwright"
dotnet restore
dotnet build
pwsh bin/Debug/net8.0/playwright.ps1 install chromium
```

## 🚀 Chạy tests

### Chạy tất cả tests
```powershell
dotnet test
```

### Chạy tests có headed (xem browser)
```powershell
$env:HEADLESS="false"; dotnet test
```

### Chạy theo nhóm US
```powershell
dotnet test --filter "Category=US01"        # Chỉ chạy US01 - Login
dotnet test --filter "Category=US01|US02"   # US01 + US02
dotnet test --filter "Category~=Admin"      # Tất cả Admin tests
```

### Chạy 1 test cụ thể
```powershell
dotnet test --filter "FullyQualifiedName~TC_REQ_US01_01_01"
```

### Xem kết quả HTML
```powershell
dotnet test --logger "html;logfilename=test-results.html"
```

## ℹ️ Ghi chú về Test Cases đặc biệt

| TC | Ghi chú |
|----|---------|
| TC_REQ_US07_01_02 | DEF_02: Giá khuyến mãi không hiển thị đúng (known bug) |
| TC_REQ_US08_05_01 | DEF_03: Cho gửi đánh giá < 10 ký tự (known bug) |
| TC_REQ_US08_05_02 | DEF_04: Không giới hạn độ dài đánh giá (known bug) |
| TC_REQ_US09_02_02 | DEF_05: Không validate định dạng SĐT (known bug) |
| TC_REQ_US09_05_01 | DEF_06: Cho xóa địa chỉ có đơn hàng (known bug) |
| TC_REQ_US13_09_02 | DEF_09: Cho xóa tài khoản Admin (known bug) |
| TC_REQ_US17_02_03 | DEF_10: Giá âm được lưu (known bug) |
| TC_REQ_US18_03_01 | DEF_11: Crash khi nhập chữ vào Price (known bug) |
| TC_REQ_US18_03_03 | DEF_13: Crash khi nhập SL thập phân (known bug) |

Các test có known bug vẫn được chạy nhưng sẽ **FAIL** - đây là hành vi mong đợi để ghi nhận bug.
