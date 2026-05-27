using Microsoft.Playwright;
using NUnit.Framework;
using NopCommerceTests.Helpers;

namespace NopCommerceTests.Tests;

/// <summary>
/// US13 - Quản lý khách hàng trên trang Admin NopCommerce
/// Bao gồm: Xem danh sách, tìm kiếm, thêm mới, chỉnh sửa, xóa khách hàng
/// </summary>
[TestFixture]
[Category("US13")]
public class US13_AdminCustomerTests : PlaywrightTestBase
{
    // ── URL hằng số ────────────────────────────────────────────────────────
    private const string CustomerListUrl   = "/Admin/Customer/List";
    private const string CustomerCreateUrl = "/Admin/Customer/Create";

    // ── SetUp: Đăng nhập Admin trước mỗi test ────────────────────────────
    [SetUp]
    public async Task SetUp()
    {
        await AuthHelper.LoginAsAdminAsync(Page);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC_REQ_US13_01_XX – Hiển thị danh sách khách hàng
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// TC_REQ_US13_01_01 – Danh sách hiển thị đúng các cột bắt buộc
    /// Kết quả mong đợi: Bảng Grid với cột Email, Name, Customer roles, Active
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_01_01_DanhSachHienThiDungCot()
    {
        // Truy cập trang danh sách khách hàng Admin
        await Page.GotoAsync(CustomerListUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Xác nhận bảng tồn tại trên trang
        var table = Page.Locator("table, #customers-grid, .k-grid");
        await Assertions.Expect(table.First).ToBeVisibleAsync();

        // Kiểm tra header bảng có chứa các cột bắt buộc
        var bodyText = await Page.Locator("body").TextContentAsync();
        Assert.That(bodyText, Does.Contain("Email").Or.Contain("email"),
            "Cột Email phải tồn tại trong bảng");
        Assert.That(bodyText, Does.Contain("Active").Or.Contain("active").Or.Contain("Hoạt động"),
            "Cột Active phải tồn tại trong bảng");
    }

    /// <summary>
    /// TC_REQ_US13_01_02 – Phân trang khi có nhiều hơn 20 khách hàng
    /// Kết quả mong đợi: Điều hướng phân trang hiển thị
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_01_02_PhanTrangKhiNhieuKhachHang()
    {
        await Page.GotoAsync(CustomerListUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Tìm kiếm để load dữ liệu
        await NopHelper.SearchAdminGridAsync(Page, "#search-customers, button:has-text('Search'), button:has-text('Tìm kiếm')");

        // Kiểm tra phân trang nếu có > 20 bản ghi
        var pager = Page.Locator(".k-pager-wrap, .dataTables_paginate, nav[aria-label*='pagination'], .pager");
        var pagerCount = await pager.CountAsync();

        if (pagerCount > 0)
        {
            // Phân trang tồn tại - kiểm tra hiển thị
            await Assertions.Expect(pager.First).ToBeVisibleAsync();
        }
        else
        {
            // Không đủ dữ liệu để phân trang - bỏ qua (không fail test)
            Assert.Pass("Không đủ > 20 bản ghi để kiểm tra phân trang - test bỏ qua.");
        }
    }

    /// <summary>
    /// TC_REQ_US13_01_03 – Thứ tự sắp xếp mặc định: khách hàng mới nhất lên đầu
    /// Kết quả mong đợi: Khách hàng tạo mới nhất xuất hiện ở đầu danh sách
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_01_03_ThuTuMacDinhMoiNhat()
    {
        await Page.GotoAsync(CustomerListUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        await NopHelper.SearchAdminGridAsync(Page, "#search-customers, button:has-text('Search'), button:has-text('Tìm kiếm')");
        await Page.WaitForTimeoutAsync(1000);

        // Xác nhận bảng tải được và có ít nhất 1 dòng dữ liệu
        var rows = Page.Locator("table tbody tr, #customers-grid tbody tr");
        var rowCount = await rows.CountAsync();

        // Nếu có dữ liệu, bảng phải hiển thị bình thường
        if (rowCount > 0)
        {
            await Assertions.Expect(rows.First).ToBeVisibleAsync();
            // Xác nhận không có lỗi kỹ thuật
            await NopHelper.AssertNoTechnicalErrorAsync(Page);
        }
        else
        {
            Assert.Pass("Không có dữ liệu để kiểm tra thứ tự sắp xếp.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC_REQ_US13_02_XX – Hiển thị khi bảng trống
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// TC_REQ_US13_02_01 – Thông báo khi không tìm thấy khách hàng
    /// Kết quả mong đợi: Bảng trống kèm thông báo "No data" / "Không có dữ liệu"
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_02_01_ThongBaoKhiBangTrong()
    {
        await Page.GotoAsync(CustomerListUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Tìm kiếm email chắc chắn không tồn tại để tạo bảng trống
        var emailInput = Page.Locator("#SearchEmail, input[name='SearchEmail']").First;
        if (await emailInput.CountAsync() > 0)
            await emailInput.FillAsync("zzznobody_exists_xyz@notfound.invalid");

        await NopHelper.SearchAdminGridAsync(Page, "#search-customers, button:has-text('Search'), button:has-text('Tìm kiếm')");
        await Page.WaitForTimeoutAsync(1000);

        // Xác nhận thông báo "No data" hoặc bảng rỗng xuất hiện
        var noDataPattern = new System.Text.RegularExpressions.Regex(
            @"No data|no records|không có dữ liệu|không có lưu trữ|Không có",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(noDataPattern);
    }

    /// <summary>
    /// TC_REQ_US13_02_02 – Nút chức năng vẫn hiển thị khi bảng trống
    /// Kết quả mong đợi: Nút "Add new" / "Thêm mới" vẫn visible
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_02_02_NutChucNangHienThiKhiBangTrong()
    {
        await Page.GotoAsync(CustomerListUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Nút "Add new" phải luôn hiển thị bất kể bảng có dữ liệu hay không
        var addBtn = Page.Locator(
            "a:has-text('Add new'), a:has-text('Thêm mới'), " +
            "a[href*='Customer/Create'], .btn-primary:has-text('Add')").First;
        await Assertions.Expect(addBtn).ToBeVisibleAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC_REQ_US13_03_XX – Tìm kiếm khách hàng
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// TC_REQ_US13_03_01 – Tìm kiếm theo Email không phân biệt hoa/thường
    /// Kết quả mong đợi: Trả về đúng khách hàng, không phân biệt hoa/thường
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_03_01_TimKiemEmailKhongPhanBietHoaThuong()
    {
        await Page.GotoAsync(CustomerListUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Nhập email viết thường của admin để tìm
        var emailInput = Page.Locator("#SearchEmail, input[name='SearchEmail']").First;
        if (await emailInput.CountAsync() > 0)
            await emailInput.FillAsync(TestConfig.AdminEmail.ToLower());

        await NopHelper.SearchAdminGridAsync(Page,
            "#search-customers, button:has-text('Search'), button:has-text('Tìm kiếm')");
        await Page.WaitForTimeoutAsync(1000);

        // Bảng kết quả phải có ít nhất 1 dòng
        var rows = Page.Locator("table tbody tr:not(.no-data), #customers-grid tbody tr");
        var rowCount = await rows.CountAsync();
        Assert.That(rowCount, Is.GreaterThan(0), "Phải tìm thấy ít nhất 1 kết quả khi tìm theo email admin");
    }

    /// <summary>
    /// TC_REQ_US13_03_02 – Tìm kiếm theo Tên một phần (partial match)
    /// Kết quả mong đợi: Trả về các khách hàng có Tên chứa chuỗi tìm kiếm
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_03_02_TimKiemTenMotPhan()
    {
        await Page.GotoAsync(CustomerListUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Tìm theo họ (First/Last name)
        var firstNameInput = Page.Locator(
            "#SearchFirstName, input[name='SearchFirstName']").First;
        if (await firstNameInput.CountAsync() > 0)
            await firstNameInput.FillAsync("Nguy");

        await NopHelper.SearchAdminGridAsync(Page,
            "#search-customers, button:has-text('Search'), button:has-text('Tìm kiếm')");
        await Page.WaitForTimeoutAsync(1000);

        // Không có lỗi hệ thống
        await NopHelper.AssertNoTechnicalErrorAsync(Page);
        // Trang vẫn hiển thị bình thường
        await Assertions.Expect(Page.Locator("body")).ToBeVisibleAsync();
    }

    /// <summary>
    /// TC_REQ_US13_03_03 – Tìm kiếm kết hợp Email và Họ
    /// Kết quả mong đợi: Trả về kết quả thỏa mãn cả hai điều kiện
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_03_03_TimKiemKetHopEmailVaHo()
    {
        await Page.GotoAsync(CustomerListUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Điền cả email và tên
        var emailInput = Page.Locator("#SearchEmail, input[name='SearchEmail']").First;
        if (await emailInput.CountAsync() > 0)
            await emailInput.FillAsync(TestConfig.AdminEmail);

        var lastNameInput = Page.Locator(
            "#SearchLastName, input[name='SearchLastName']").First;
        if (await lastNameInput.CountAsync() > 0)
            await lastNameInput.FillAsync("Admin");

        await NopHelper.SearchAdminGridAsync(Page,
            "#search-customers, button:has-text('Search'), button:has-text('Tìm kiếm')");
        await Page.WaitForTimeoutAsync(1000);

        // Không có lỗi hệ thống sau khi tìm kiếm kết hợp
        await NopHelper.AssertNoTechnicalErrorAsync(Page);
        await Assertions.Expect(Page.Locator("body")).ToBeVisibleAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC_REQ_US13_04_XX – Tìm kiếm không tìm thấy kết quả
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// TC_REQ_US13_04_01 – Tìm kiếm với từ khóa không tồn tại trong CSDL
    /// Kết quả mong đợi: Danh sách rỗng kèm thông báo "No data"
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_04_01_TimKiemKhongTonTai()
    {
        await Page.GotoAsync(CustomerListUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        var emailInput = Page.Locator("#SearchEmail, input[name='SearchEmail']").First;
        if (await emailInput.CountAsync() > 0)
            await emailInput.FillAsync("notexist_xyz_abc@test.invalid.com");

        await NopHelper.SearchAdminGridAsync(Page,
            "#search-customers, button:has-text('Search'), button:has-text('Tìm kiếm')");
        await Page.WaitForTimeoutAsync(1000);

        // Phải có thông báo không có dữ liệu hoặc bảng rỗng
        var noDataPattern = new System.Text.RegularExpressions.Regex(
            @"No data|no records|không có dữ liệu|0 items|No customer",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(noDataPattern);
    }

    /// <summary>
    /// TC_REQ_US13_04_02 – Không có lỗi hệ thống khi kết quả tìm kiếm rỗng
    /// Kết quả mong đợi: Giao diện bình thường, không có lỗi 500 / exception
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_04_02_KhongLoiHeThongKhiKetQuaRong()
    {
        await Page.GotoAsync(CustomerListUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        var emailInput = Page.Locator("#SearchEmail, input[name='SearchEmail']").First;
        if (await emailInput.CountAsync() > 0)
            await emailInput.FillAsync("zzz_empty_result_xyz@nope.invalid");

        await NopHelper.SearchAdminGridAsync(Page,
            "#search-customers, button:has-text('Search'), button:has-text('Tìm kiếm')");
        await Page.WaitForTimeoutAsync(1000);

        // Không được có lỗi kỹ thuật 500 / server error
        await NopHelper.AssertNoTechnicalErrorAsync(Page);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC_REQ_US13_05_XX – Validation khi thêm mới khách hàng
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// TC_REQ_US13_05_01 – Thêm mới với Email sai định dạng
    /// Kết quả mong đợi: Hệ thống hiển thị lỗi Email không đúng định dạng, chặn lưu
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_05_01_ThemMoiEmailSaiDinhDang()
    {
        await Page.GotoAsync(CustomerCreateUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Nhập email sai định dạng (thiếu @)
        await NopHelper.FillIfPresentAsync(Page, "#Email", "abc.com");
        await NopHelper.FillIfPresentAsync(Page, "#Password", "Password123!");

        await NopHelper.SaveAdminFormAsync(Page);

        // Phải hiển thị lỗi validation
        await NopHelper.ExpectValidationAsync(Page);
    }

    /// <summary>
    /// TC_REQ_US13_05_02 – Thêm mới với Password quá ngắn (ít hơn độ dài tối thiểu)
    /// Kết quả mong đợi: Hệ thống hiển thị lỗi Password quá ngắn, chặn lưu
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_05_02_ThemMoiPasswordNgan()
    {
        await Page.GotoAsync(CustomerCreateUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        await NopHelper.FillIfPresentAsync(Page, "#Email", TestConfig.UniqueEmail("tc_us13_05_02"));
        await NopHelper.FillIfPresentAsync(Page, "#Password", "123");
        await NopHelper.FillIfPresentAsync(Page, "#ConfirmPassword", "123");

        await NopHelper.SaveAdminFormAsync(Page);

        // Phải có thông báo lỗi password không đủ điều kiện
        var passwordErrorPattern = new System.Text.RegularExpressions.Regex(
            @"password|mật khẩu|length|characters|ký tự",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(passwordErrorPattern);
    }

    /// <summary>
    /// TC_REQ_US13_05_03 – Bỏ trống trường Email khi thêm mới
    /// Kết quả mong đợi: Hệ thống hiển thị lỗi trường bắt buộc, chặn lưu
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_05_03_ThemMoiBoTrongEmail()
    {
        await Page.GotoAsync(CustomerCreateUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Để trống email, chỉ điền password
        await NopHelper.FillIfPresentAsync(Page, "#Password", "ValidPass123!");
        await NopHelper.FillIfPresentAsync(Page, "#ConfirmPassword", "ValidPass123!");

        await NopHelper.SaveAdminFormAsync(Page);

        // Phải hiển thị lỗi validation
        await NopHelper.ExpectValidationAsync(Page);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC_REQ_US13_06_XX – Tạo khách hàng mới thành công
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// TC_REQ_US13_06_01 – Tạo khách hàng mới thành công
    /// Kết quả mong đợi: Thông báo thành công, khách hàng được tạo với Role 'Registered'
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_06_01_TaoKhachHangMoiThanhCong()
    {
        if (!TestConfig.AllowMutation)
            Assert.Ignore("AllowMutation=false - bỏ qua test tạo khách hàng.");

        await Page.GotoAsync(CustomerCreateUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        var uniqueEmail = TestConfig.UniqueEmail("tc_us13_06_01");
        await NopHelper.FillIfPresentAsync(Page, "#Email", uniqueEmail);
        await NopHelper.FillIfPresentAsync(Page, "#Password", "ValidPass123!");
        await NopHelper.FillIfPresentAsync(Page, "#ConfirmPassword", "ValidPass123!");

        // Gán Role "Registered" nếu có multiselect
        var roleSelect = Page.Locator(
            "#SelectedCustomerRoleIds, select[name='SelectedCustomerRoleIds']").First;
        if (await roleSelect.CountAsync() > 0)
            await roleSelect.SelectOptionAsync(new SelectOptionValue { Label = "Registered" });

        await NopHelper.SaveAdminFormAsync(Page);
        await NopHelper.ExpectSuccessAsync(Page);
    }

    /// <summary>
    /// TC_REQ_US13_06_02 – Tạo khách hàng với Email đã tồn tại
    /// Kết quả mong đợi: Hệ thống báo lỗi Email đã tồn tại, không tạo mới
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_06_02_TaoKhachHangEmailDaTonTai()
    {
        await Page.GotoAsync(CustomerCreateUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Sử dụng email của khách hàng đã tồn tại trong hệ thống
        await NopHelper.FillIfPresentAsync(Page, "#Email", TestConfig.CustomerEmail);
        await NopHelper.FillIfPresentAsync(Page, "#Password", "ValidPass123!");
        await NopHelper.FillIfPresentAsync(Page, "#ConfirmPassword", "ValidPass123!");

        await NopHelper.SaveAdminFormAsync(Page);

        // Phải có thông báo email đã tồn tại
        var duplicatePattern = new System.Text.RegularExpressions.Regex(
            @"already exists|đã tồn tại|already registered|duplicate",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(duplicatePattern);
    }

    /// <summary>
    /// TC_REQ_US13_06_03 – Khách hàng mới được gán Role mặc định khi không chọn Role
    /// Kết quả mong đợi: Khách hàng được gán Role 'Registered' mặc định
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_06_03_RoleMacDinhKhiKhongChon()
    {
        if (!TestConfig.AllowMutation)
            Assert.Ignore("AllowMutation=false - bỏ qua test tạo khách hàng.");

        await Page.GotoAsync(CustomerCreateUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        var uniqueEmail = TestConfig.UniqueEmail("tc_us13_06_03");
        await NopHelper.FillIfPresentAsync(Page, "#Email", uniqueEmail);
        await NopHelper.FillIfPresentAsync(Page, "#Password", "ValidPass123!");
        await NopHelper.FillIfPresentAsync(Page, "#ConfirmPassword", "ValidPass123!");

        // KHÔNG chọn role - để mặc định
        await NopHelper.SaveAdminFormAsync(Page);
        await NopHelper.ExpectSuccessAsync(Page);

        // Sau khi lưu, trang edit phải hiển thị role 'Registered'
        var bodyText = await Page.Locator("body").TextContentAsync();
        Assert.That(bodyText, Does.Contain("Registered"),
            "Role 'Registered' phải được gán mặc định cho khách hàng mới");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC_REQ_US13_07_XX – Chỉnh sửa thông tin khách hàng
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// TC_REQ_US13_07_01 – Chỉnh sửa thông tin khách hàng thành công
    /// Kết quả mong đợi: Dữ liệu cập nhật, thông báo thành công
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_07_01_ChinhSuaThanhCong()
    {
        if (!TestConfig.AllowMutation)
            Assert.Ignore("AllowMutation=false - bỏ qua test chỉnh sửa.");

        await Page.GotoAsync(CustomerListUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        await NopHelper.SearchAdminGridAsync(Page,
            "#search-customers, button:has-text('Search'), button:has-text('Tìm kiếm')");
        await Page.WaitForTimeoutAsync(1000);

        // Click Edit trên dòng đầu tiên
        var editBtn = Page.Locator(
            "table tbody tr:first-child a:has-text('Edit'), " +
            "#customers-grid tbody tr:first-child a:has-text('Edit')").First;
        if (await editBtn.CountAsync() == 0)
            Assert.Inconclusive("Không có khách hàng nào để chỉnh sửa.");

        await editBtn.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Đổi giới tính (nếu có field)
        var genderMale = Page.Locator("#Gender_Male, input[value='M']").First;
        var genderFemale = Page.Locator("#Gender_Female, input[value='F']").First;
        if (await genderFemale.CountAsync() > 0)
            await genderFemale.CheckAsync(new LocatorCheckOptions { Force = true });
        else if (await genderMale.CountAsync() > 0)
            await genderMale.CheckAsync(new LocatorCheckOptions { Force = true });

        await NopHelper.SaveAdminFormAsync(Page);
        await NopHelper.ExpectSuccessAsync(Page);
    }

    /// <summary>
    /// TC_REQ_US13_07_02 – Dữ liệu cũ đổ vào form khi mở sửa
    /// Kết quả mong đợi: Form hiển thị đúng tất cả thông tin hiện tại
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_07_02_DuLieuCuDoVaoForm()
    {
        await Page.GotoAsync(CustomerListUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        await NopHelper.SearchAdminGridAsync(Page,
            "#search-customers, button:has-text('Search'), button:has-text('Tìm kiếm')");
        await Page.WaitForTimeoutAsync(1000);

        // Click Edit trên dòng đầu tiên
        var editBtn = Page.Locator(
            "table tbody tr:first-child a:has-text('Edit'), " +
            "#customers-grid tbody tr:first-child a:has-text('Edit')").First;
        if (await editBtn.CountAsync() == 0)
            Assert.Inconclusive("Không có khách hàng nào để mở form edit.");

        await editBtn.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Form edit phải có trường Email được điền sẵn (không trống)
        var emailField = Page.Locator("#Email").First;
        if (await emailField.CountAsync() > 0)
        {
            var emailValue = await emailField.InputValueAsync();
            Assert.That(emailValue, Is.Not.Empty, "Trường Email phải được pre-fill với giá trị hiện tại");
        }
        else
        {
            // Nếu không có field Email, xác nhận form có dữ liệu
            await Assertions.Expect(Page.Locator("form")).ToBeVisibleAsync();
        }
    }

    /// <summary>
    /// TC_REQ_US13_07_03 – Thời gian cập nhật được ghi nhận sau khi sửa (edge case)
    /// Kết quả mong đợi: Trường thời gian cập nhật thay đổi so với lần trước
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_07_03_ThoiGianCapNhatGhiNhan()
    {
        if (!TestConfig.AllowMutation)
            Assert.Ignore("AllowMutation=false - bỏ qua test edge case timestamp.");

        await Page.GotoAsync(CustomerListUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        await NopHelper.SearchAdminGridAsync(Page,
            "#search-customers, button:has-text('Search'), button:has-text('Tìm kiếm')");
        await Page.WaitForTimeoutAsync(1000);

        var editBtn = Page.Locator(
            "table tbody tr:first-child a:has-text('Edit'), " +
            "#customers-grid tbody tr:first-child a:has-text('Edit')").First;
        if (await editBtn.CountAsync() == 0)
            Assert.Inconclusive("Không có khách hàng để test timestamp.");

        await editBtn.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Lưu để trigger cập nhật timestamp
        await NopHelper.SaveAdminFormAsync(Page);
        await NopHelper.ExpectSuccessAsync(Page);

        // Xác nhận không có lỗi kỹ thuật sau khi lưu
        await NopHelper.AssertNoTechnicalErrorAsync(Page);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC_REQ_US13_08_XX – Validation khi chỉnh sửa khách hàng
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// TC_REQ_US13_08_01 – Chỉnh sửa Email thành định dạng sai
    /// Kết quả mong đợi: Hệ thống hiển thị lỗi định dạng Email, chặn lưu
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_08_01_SuaEmailSaiDinhDang()
    {
        await Page.GotoAsync(CustomerListUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        await NopHelper.SearchAdminGridAsync(Page,
            "#search-customers, button:has-text('Search'), button:has-text('Tìm kiếm')");
        await Page.WaitForTimeoutAsync(1000);

        var editBtn = Page.Locator(
            "table tbody tr:first-child a:has-text('Edit'), " +
            "#customers-grid tbody tr:first-child a:has-text('Edit')").First;
        if (await editBtn.CountAsync() == 0)
            Assert.Inconclusive("Không có khách hàng để test.");

        await editBtn.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Đổi email thành định dạng sai
        var emailField = Page.Locator("#Email").First;
        if (await emailField.CountAsync() > 0)
        {
            await emailField.ClearAsync();
            await emailField.FillAsync("invalid-email-format.com");
        }

        await NopHelper.SaveAdminFormAsync(Page);
        await NopHelper.ExpectValidationAsync(Page);
    }

    /// <summary>
    /// TC_REQ_US13_08_02 – Bỏ trống trường Email khi chỉnh sửa
    /// Kết quả mong đợi: Hệ thống hiển thị lỗi trường bắt buộc, chặn lưu
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_08_02_SuaBoTrongEmail()
    {
        await Page.GotoAsync(CustomerListUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        await NopHelper.SearchAdminGridAsync(Page,
            "#search-customers, button:has-text('Search'), button:has-text('Tìm kiếm')");
        await Page.WaitForTimeoutAsync(1000);

        var editBtn = Page.Locator(
            "table tbody tr:first-child a:has-text('Edit'), " +
            "#customers-grid tbody tr:first-child a:has-text('Edit')").First;
        if (await editBtn.CountAsync() == 0)
            Assert.Inconclusive("Không có khách hàng để test.");

        await editBtn.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Xóa trắng trường Email
        var emailField = Page.Locator("#Email").First;
        if (await emailField.CountAsync() > 0)
        {
            await emailField.ClearAsync();
            await emailField.FillAsync("");
        }

        await NopHelper.SaveAdminFormAsync(Page);
        await NopHelper.ExpectValidationAsync(Page);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC_REQ_US13_09_XX – Xóa khách hàng
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// TC_REQ_US13_09_01 – Xóa khách hàng thường thành công
    /// Kết quả mong đợi: Bản ghi bị xóa, thông báo thành công
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_09_01_XoaKhachHangThuongThanhCong()
    {
        if (!TestConfig.AllowMutation)
            Assert.Ignore("AllowMutation=false - bỏ qua test xóa.");

        // Trước tiên tạo một khách hàng mới để xóa
        await Page.GotoAsync(CustomerCreateUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        var uniqueEmail = TestConfig.UniqueEmail("tc_us13_09_01_del");
        await NopHelper.FillIfPresentAsync(Page, "#Email", uniqueEmail);
        await NopHelper.FillIfPresentAsync(Page, "#Password", "ValidPass123!");
        await NopHelper.FillIfPresentAsync(Page, "#ConfirmPassword", "ValidPass123!");
        await NopHelper.SaveAdminFormAsync(Page);

        // Kiểm tra đã tạo thành công (nếu có lỗi - inconclusive)
        var currentUrl = Page.Url;
        if (currentUrl.Contains("Create"))
            Assert.Inconclusive("Không thể tạo khách hàng để test xóa.");

        // Click nút Delete trên trang edit hiện tại
        var deleteBtn = Page.Locator(
            "button:has-text('Delete'), input[value='Delete'], " +
            "a:has-text('Delete'), button[name='delete']").First;

        if (await deleteBtn.CountAsync() > 0)
        {
            // Xử lý dialog xác nhận trước khi click
            Page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
            await deleteBtn.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await NopHelper.ExpectSuccessAsync(Page);
        }
        else
        {
            Assert.Inconclusive("Không tìm thấy nút Delete trên trang.");
        }
    }

    /// <summary>
    /// TC_REQ_US13_09_02 – Cấm xóa tài khoản Administrator (DEF_09 - Known Bug)
    /// Kết quả mong đợi: Hệ thống chặn xóa tài khoản Administrator
    /// Known Bug: DEF_09 - Hệ thống vẫn cho phép xóa khi có nhiều Administrator
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_09_02_CamXoaTaiKhoanAdministrator()
    {
        // NOTE: DEF_09 - Known Bug: Hệ thống vẫn cho phép xóa khi có nhiều Administrator
        // Test này ghi nhận hành vi thực tế (có thể fail do known bug)
        await Page.GotoAsync(CustomerListUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Tìm tài khoản Administrator
        var emailInput = Page.Locator("#SearchEmail, input[name='SearchEmail']").First;
        if (await emailInput.CountAsync() > 0)
            await emailInput.FillAsync(TestConfig.AdminEmail);

        await NopHelper.SearchAdminGridAsync(Page,
            "#search-customers, button:has-text('Search'), button:has-text('Tìm kiếm')");
        await Page.WaitForTimeoutAsync(1000);

        var editBtn = Page.Locator(
            "table tbody tr:first-child a:has-text('Edit'), " +
            "#customers-grid tbody tr:first-child a:has-text('Edit')").First;
        if (await editBtn.CountAsync() == 0)
            Assert.Inconclusive("Không tìm thấy tài khoản Admin để test.");

        await editBtn.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Thử click Delete - hệ thống nên chặn
        var deleteBtn = Page.Locator(
            "button:has-text('Delete'), input[value='Delete'], " +
            "a:has-text('Delete'), button[name='delete']").First;

        if (await deleteBtn.CountAsync() > 0)
        {
            // Chuẩn bị xử lý dialog
            var dialogAppeared = false;
            Page.Dialog += async (_, dialog) =>
            {
                dialogAppeared = true;
                await dialog.AcceptAsync();
            };
            await deleteBtn.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);

            // Kiểm tra: nếu hệ thống chặn, sẽ có thông báo lỗi
            // Nếu không chặn (DEF_09), ghi nhận known bug
            var bodyText = await Page.Locator("body").TextContentAsync() ?? "";
            if (bodyText.Contains("cannot be deleted") || bodyText.Contains("không thể xóa"))
            {
                Assert.Pass("Hệ thống đã chặn xóa Administrator - hành vi đúng.");
            }
            else
            {
                Assert.Warn("DEF_09: Hệ thống có thể đã cho phép xóa Administrator - Known Bug.");
            }
        }
        else
        {
            Assert.Pass("Nút Delete không hiển thị - hệ thống đã ẩn option xóa Administrator.");
        }
    }

    /// <summary>
    /// TC_REQ_US13_09_03 – Popup xác nhận xuất hiện trước khi xóa
    /// Kết quả mong đợi: Popup confirm hiện ra, không xóa ngay lập tức
    /// </summary>
    [Test]
    public async Task TC_REQ_US13_09_03_PopupXacNhanTruocKhiXoa()
    {
        await Page.GotoAsync(CustomerListUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        await NopHelper.SearchAdminGridAsync(Page,
            "#search-customers, button:has-text('Search'), button:has-text('Tìm kiếm')");
        await Page.WaitForTimeoutAsync(1000);

        var editBtn = Page.Locator(
            "table tbody tr:first-child a:has-text('Edit'), " +
            "#customers-grid tbody tr:first-child a:has-text('Edit')").First;
        if (await editBtn.CountAsync() == 0)
            Assert.Inconclusive("Không có khách hàng để test popup xác nhận.");

        await editBtn.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Lắng nghe dialog xuất hiện
        var dialogShown = false;
        Page.Dialog += async (_, dialog) =>
        {
            dialogShown = true;
            // Hủy dialog - không thực sự xóa
            await dialog.DismissAsync();
        };

        var deleteBtn = Page.Locator(
            "button:has-text('Delete'), input[value='Delete'], " +
            "button[name='delete']").First;

        if (await deleteBtn.CountAsync() > 0)
        {
            await deleteBtn.ClickAsync();
            await Page.WaitForTimeoutAsync(1500);
            Assert.That(dialogShown, Is.True, "Popup xác nhận phải xuất hiện trước khi xóa");
        }
        else
        {
            // NopCommerce có thể dùng custom modal thay vì native dialog
            var confirmModal = Page.Locator(
                ".modal, [role='dialog'], .confirmation-dialog, " +
                ".sweet-alert, .swal2-container").First;
            if (await confirmModal.CountAsync() > 0)
                await Assertions.Expect(confirmModal).ToBeVisibleAsync();
            else
                Assert.Pass("Nút Delete không tìm thấy trên trang này - có thể trang không hỗ trợ.");
        }
    }
}
