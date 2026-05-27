using Microsoft.Playwright;
using NUnit.Framework;
using NopCommerceTests.Helpers;

namespace NopCommerceTests.Tests;

/// <summary>
/// US02 - Cập nhật thông tin cá nhân (Profile): REQ_US02_01 đến REQ_US02_03
/// Tổng: 9 test cases
/// CSV dòng 63–114 của SQA_Plan - w4_Test_Case.csv
/// URL trang chỉnh sửa: /customer/info
/// </summary>
[TestFixture]
[Category("US02")]
public class US02_ProfileTests : PlaywrightTestBase
{
    // ── Helper nội bộ: Đăng nhập và điều hướng tới trang Edit Info ──────────
    private async Task GoToCustomerInfoAsync()
    {
        // Đăng nhập với tài khoản customer hợp lệ
        await AuthHelper.LoginAsCustomerAsync(Page);
        // Truy cập trang chỉnh sửa thông tin cá nhân
        await Page.GotoAsync("/customer/info", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        // Đảm bảo trang đã load xong
        await Assertions.Expect(Page.Locator("body")).ToBeVisibleAsync();
    }

    // ── REQ_US02_01: Cập nhật thông tin cơ bản ──────────────────────────────

    /// <summary>
    /// TC_REQ_US02_01_01 - Cập nhật thông tin cá nhân thành công với Email mới chưa tồn tại
    /// Input : Email mới chưa có trong hệ thống, First name=Nguyen, Last name=Van A
    /// Expect: Dữ liệu cập nhật vào bảng Customer, hiển thị thông báo thành công
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US02_01_01_UpdateProfile_NewUniqueEmail_Success()
    {
        await GoToCustomerInfoAsync();

        // Sinh email duy nhất theo timestamp để tránh trùng
        var newEmail = TestConfig.UniqueEmail("profile_update");

        // Xóa và điền email mới (chưa tồn tại trong hệ thống)
        var emailInput = Page.Locator("#Email, input[name='Email']").First;
        await emailInput.ClearAsync();
        await emailInput.FillAsync(newEmail);

        // Cập nhật First name
        var firstNameInput = Page.Locator("#FirstName, input[name='FirstName']").First;
        await firstNameInput.ClearAsync();
        await firstNameInput.FillAsync("Nguyen");

        // Cập nhật Last name
        var lastNameInput = Page.Locator("#LastName, input[name='LastName']").First;
        await lastNameInput.ClearAsync();
        await lastNameInput.FillAsync("Van A");

        // Nhấn nút Save
        var saveBtn = Page.Locator(
            "button:has-text('Save'), input[value='Save'], " +
            "button:has-text('Lưu'), input[value*='Lưu']").First;
        await saveBtn.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Xác nhận thông báo thành công
        await NopHelper.ExpectSuccessAsync(Page);
    }

    /// <summary>
    /// TC_REQ_US02_01_02 - Cập nhật Email thành Email đã tồn tại của khách hàng khác
    /// Input : Email đã thuộc về tài khoản khác (CustomerB)
    /// Expect: Hệ thống báo lỗi Email đã tồn tại, không cho cập nhật
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US02_01_02_UpdateProfile_ExistingEmail_Fail()
    {
        await GoToCustomerInfoAsync();

        // Nhập email đã thuộc tài khoản khác (CustomerB)
        var emailInput = Page.Locator("#Email, input[name='Email']").First;
        await emailInput.ClearAsync();
        await emailInput.FillAsync(TestConfig.CustomerBEmail);

        // Nhấn Save
        var saveBtn = Page.Locator(
            "button:has-text('Save'), input[value='Save'], " +
            "button:has-text('Lưu'), input[value*='Lưu']").First;
        await saveBtn.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Hệ thống phải báo lỗi: email đã tồn tại / already used / đã được sử dụng
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex(
                @"already|exists|used|taken|đã tồn tại|đã được sử dụng",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// TC_REQ_US02_01_03 - Form hiển thị đúng dữ liệu hiện tại khi mở trang Profile
    /// Input : Đã đăng nhập, truy cập /customer/info
    /// Expect: Form hiển thị đúng tất cả dữ liệu hiện tại (Email field phải tồn tại và có giá trị)
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US02_01_03_ProfileForm_DisplaysCurrentData()
    {
        await GoToCustomerInfoAsync();

        // Kiểm tra trường Email tồn tại và hiển thị trên form
        var emailInput = Page.Locator("#Email, input[name='Email']").First;
        await Assertions.Expect(emailInput).ToBeVisibleAsync();

        // Kiểm tra trường First name tồn tại
        var firstNameInput = Page.Locator("#FirstName, input[name='FirstName']").First;
        await Assertions.Expect(firstNameInput).ToBeVisibleAsync();

        // Kiểm tra trường Last name tồn tại
        var lastNameInput = Page.Locator("#LastName, input[name='LastName']").First;
        await Assertions.Expect(lastNameInput).ToBeVisibleAsync();

        // Xác nhận các field có giá trị (không rỗng - dữ liệu hiện tại từ DB)
        var emailValue = await emailInput.InputValueAsync();
        Assert.That(emailValue, Is.Not.Empty, "Email field phải có giá trị hiện tại từ CSDL");
    }

    // ── REQ_US02_02: Trường bắt buộc ────────────────────────────────────────

    /// <summary>
    /// TC_REQ_US02_02_01 - Bỏ trống toàn bộ trường bắt buộc và nhấn Lưu
    /// Input : First name=(trống), Last name=(trống), Email=(trống)
    /// Expect: Hệ thống hiển thị lỗi tại cả 3 trường bắt buộc, chặn lưu
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US02_02_01_EmptyAllRequiredFields_Fail()
    {
        await GoToCustomerInfoAsync();

        // Xóa trống tất cả 3 trường bắt buộc
        var firstNameInput = Page.Locator("#FirstName, input[name='FirstName']").First;
        await firstNameInput.ClearAsync();
        await firstNameInput.FillAsync("");

        var lastNameInput = Page.Locator("#LastName, input[name='LastName']").First;
        await lastNameInput.ClearAsync();
        await lastNameInput.FillAsync("");

        var emailInput = Page.Locator("#Email, input[name='Email']").First;
        await emailInput.ClearAsync();
        await emailInput.FillAsync("");

        // Nhấn Save
        var saveBtn = Page.Locator(
            "button:has-text('Save'), input[value='Save'], " +
            "button:has-text('Lưu'), input[value*='Lưu']").First;
        await saveBtn.ClickAsync();

        // Phải xuất hiện thông báo validation bắt buộc
        await NopHelper.ExpectValidationAsync(Page);
    }

    /// <summary>
    /// TC_REQ_US02_02_02 - Bỏ trống riêng trường Họ (Last name) và nhấn Lưu
    /// Input : First name=Nguyen, Last name=(trống), Email=hợp lệ
    /// Expect: Hệ thống hiển thị lỗi tại trường Họ, chặn lưu
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US02_02_02_EmptyLastName_Fail()
    {
        await GoToCustomerInfoAsync();

        // Điền First name và Email hợp lệ
        var firstNameInput = Page.Locator("#FirstName, input[name='FirstName']").First;
        await firstNameInput.ClearAsync();
        await firstNameInput.FillAsync("Nguyen");

        // Xóa trống Last name
        var lastNameInput = Page.Locator("#LastName, input[name='LastName']").First;
        await lastNameInput.ClearAsync();
        await lastNameInput.FillAsync("");

        // Nhấn Save
        var saveBtn = Page.Locator(
            "button:has-text('Save'), input[value='Save'], " +
            "button:has-text('Lưu'), input[value*='Lưu']").First;
        await saveBtn.ClickAsync();

        // Phải có lỗi tại trường Last name (required / bắt buộc)
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex(
                @"required|must|bắt buộc|last name|họ|please",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// TC_REQ_US02_02_03 - Điền đầy đủ các trường bắt buộc và nhấn Lưu
    /// Input : First name=Nguyen, Last name=Van B, Email=hợp lệ
    /// Expect: Hệ thống lưu thành công, không hiển thị lỗi bắt buộc
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US02_02_03_AllRequiredFields_Success()
    {
        await GoToCustomerInfoAsync();

        // Điền đầy đủ tất cả trường bắt buộc với dữ liệu hợp lệ
        var firstNameInput = Page.Locator("#FirstName, input[name='FirstName']").First;
        await firstNameInput.ClearAsync();
        await firstNameInput.FillAsync("Nguyen");

        var lastNameInput = Page.Locator("#LastName, input[name='LastName']").First;
        await lastNameInput.ClearAsync();
        await lastNameInput.FillAsync("Van B");

        // Giữ nguyên email hiện tại (đã hợp lệ)
        var emailInput = Page.Locator("#Email, input[name='Email']").First;
        var currentEmail = await emailInput.InputValueAsync();
        if (string.IsNullOrEmpty(currentEmail))
            await emailInput.FillAsync(TestConfig.CustomerEmail);

        // Nhấn Save
        var saveBtn = Page.Locator(
            "button:has-text('Save'), input[value='Save'], " +
            "button:has-text('Lưu'), input[value*='Lưu']").First;
        await saveBtn.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Xác nhận lưu thành công
        await NopHelper.ExpectSuccessAsync(Page);
    }

    // ── REQ_US02_03: Định dạng Email ─────────────────────────────────────────

    /// <summary>
    /// TC_REQ_US02_03_01 - Nhập Email không có ký tự @
    /// Input : Email='testdomain.com'
    /// Expect: Hệ thống hiển thị lỗi định dạng Email không hợp lệ, chặn lưu
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US02_03_01_EmailWithoutAt_Fail()
    {
        await GoToCustomerInfoAsync();

        // Điền email không có ký tự @ (sai định dạng)
        var emailInput = Page.Locator("#Email, input[name='Email']").First;
        await emailInput.ClearAsync();
        await emailInput.FillAsync("testdomain.com");

        // Nhấn Save
        var saveBtn = Page.Locator(
            "button:has-text('Save'), input[value='Save'], " +
            "button:has-text('Lưu'), input[value*='Lưu']").First;
        await saveBtn.ClickAsync();

        // Phải có thông báo lỗi định dạng email không hợp lệ
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex(
                @"valid email|invalid email|hợp lệ|không hợp lệ|please enter a valid",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// TC_REQ_US02_03_02 - Nhập Email không có phần domain
    /// Input : Email='test@'
    /// Expect: Hệ thống hiển thị lỗi định dạng Email không hợp lệ, chặn lưu
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US02_03_02_EmailWithoutDomain_Fail()
    {
        await GoToCustomerInfoAsync();

        // Điền email thiếu phần domain sau @
        var emailInput = Page.Locator("#Email, input[name='Email']").First;
        await emailInput.ClearAsync();
        await emailInput.FillAsync("test@");

        // Nhấn Save
        var saveBtn = Page.Locator(
            "button:has-text('Save'), input[value='Save'], " +
            "button:has-text('Lưu'), input[value*='Lưu']").First;
        await saveBtn.ClickAsync();

        // Phải có thông báo lỗi định dạng email không hợp lệ
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex(
                @"valid email|invalid email|hợp lệ|không hợp lệ|please enter a valid",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// TC_REQ_US02_03_03 - Nhập Email đúng định dạng chuẩn
    /// Input : Email='test@example.com'
    /// Expect: Hệ thống chấp nhận và lưu Email thành công
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US02_03_03_ValidEmailFormat_Success()
    {
        await GoToCustomerInfoAsync();

        // Sinh email hợp lệ duy nhất (tránh trùng với tài khoản khác)
        var validEmail = TestConfig.UniqueEmail("valid_format");

        // Điền email đúng định dạng
        var emailInput = Page.Locator("#Email, input[name='Email']").First;
        await emailInput.ClearAsync();
        await emailInput.FillAsync(validEmail);

        // Đảm bảo First name và Last name có giá trị hợp lệ
        var firstNameInput = Page.Locator("#FirstName, input[name='FirstName']").First;
        if (string.IsNullOrEmpty(await firstNameInput.InputValueAsync()))
            await firstNameInput.FillAsync("Test");

        var lastNameInput = Page.Locator("#LastName, input[name='LastName']").First;
        if (string.IsNullOrEmpty(await lastNameInput.InputValueAsync()))
            await lastNameInput.FillAsync("User");

        // Nhấn Save
        var saveBtn = Page.Locator(
            "button:has-text('Save'), input[value='Save'], " +
            "button:has-text('Lưu'), input[value*='Lưu']").First;
        await saveBtn.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Xác nhận lưu thành công
        await NopHelper.ExpectSuccessAsync(Page);
    }
}
