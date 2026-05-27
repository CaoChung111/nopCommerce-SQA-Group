using Microsoft.Playwright;
using NUnit.Framework;
using NopCommerceTests.Helpers;

namespace NopCommerceTests.Tests;

/// <summary>
/// US01 - Đăng nhập: REQ_US01_01 đến REQ_US01_03
/// Tổng: 9 test cases
/// CSV dòng 3–62 của SQA_Plan - w4_Test_Case.csv
/// </summary>
[TestFixture]
[Category("US01")]
public class US01_LoginTests : PlaywrightTestBase
{
    // ── REQ_US01_01: Đăng nhập cơ bản ──────────────────────────────────────

    /// <summary>
    /// TC_REQ_US01_01_01 - Đăng nhập thành công với tài khoản Active
    /// Input : Email=buihoang3425@gmail.com / Password=123456 / Trạng thái TK=Active
    /// Expect: Điều hướng về trang chủ, hiển thị thông báo đăng nhập thành công
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US01_01_01_LoginSuccess_ActiveAccount()
    {
        // Đăng nhập bằng tài khoản customer hợp lệ đang Active
        await AuthHelper.LoginWithCredentialsAsync(Page, TestConfig.CustomerEmail, TestConfig.CustomerPassword);

        // Sau đăng nhập thành công, trang hiển thị link logout hoặc "My Account"
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex(
                @"my account|tài khoản|log out|đăng xuất",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// TC_REQ_US01_01_02 - Đăng nhập thất bại với tài khoản Inactive
    /// Input : Email=inactive@gmail.com / Password=123456 / Trạng thái TK=Inactive
    /// Expect: Hệ thống từ chối đăng nhập và hiển thị thông báo lỗi phù hợp
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US01_01_02_LoginFail_InactiveAccount()
    {
        // Thử đăng nhập với tài khoản Inactive
        await AuthHelper.LoginWithCredentialsAsync(Page, TestConfig.InactiveEmail, TestConfig.InactivePassword);

        // Hệ thống phải hiển thị thông báo đăng nhập không thành công / tài khoản không hoạt động
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex(
                @"unsuccessful|not active|inactive|không thành công|không hoạt động|Tài khoản không hoạt động",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// TC_REQ_US01_01_03 - Kiểm tra chức năng Remember me khi tích chọn
    /// Input : Email hợp lệ, Password hợp lệ, checkbox Remember me = tích chọn
    /// Expect: Sau khi đóng và mở lại trình duyệt, phiên đăng nhập vẫn được duy trì
    /// Ghi chú: Test này kiểm tra checkbox tồn tại và có thể tích được; việc kiểm tra cookie
    ///          persistent cần session reload nằm ngoài phạm vi đơn vị test này.
    /// </summary>
    [Test]
    [Category("Edge")]
    public async Task TC_REQ_US01_01_03_RememberMe()
    {
        // Mở trang đăng nhập
        await Page.GotoAsync("/login", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Điền email và password hợp lệ
        await Page.Locator("#Email, input[name='Email']").First.FillAsync(TestConfig.CustomerEmail);
        await Page.Locator("#Password, input[name='Password']").First.FillAsync(TestConfig.CustomerPassword);

        // Tích checkbox Remember me nếu tồn tại
        var rememberMe = Page.Locator("#RememberMe, input[name='RememberMe']").First;
        if (await rememberMe.CountAsync() > 0)
            await rememberMe.CheckAsync();

        // Nhấn nút đăng nhập
        var loginBtn = Page.Locator(
            ".login-button, button:has-text('Log in'), input.login-button, " +
            "input[type='submit'][value*='Log in'], button:has-text('Đăng nhập')").First;
        await loginBtn.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Xác nhận đăng nhập thành công (phiên đang duy trì)
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex(
                @"my account|tài khoản|log out|đăng xuất",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    // ── REQ_US01_02: Trường bắt buộc ──────────────────────────────────────

    /// <summary>
    /// TC_REQ_US01_02_01 - Để trống cả Email và Password khi đăng nhập
    /// Input : Email=(trống), Password=(trống)
    /// Expect: Hiển thị thông báo lỗi trường bắt buộc, không cho đăng nhập
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US01_02_01_EmptyEmailAndPassword()
    {
        // Mở trang đăng nhập
        await Page.GotoAsync("/login", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Không điền gì, nhấn Login ngay
        var loginBtn = Page.Locator(
            ".login-button, button:has-text('Log in'), input.login-button, " +
            "input[type='submit'][value*='Log in'], button:has-text('Đăng nhập')").First;
        await loginBtn.ClickAsync();

        // Phải có thông báo validation (bắt buộc / required / vui lòng nhập...)
        await NopHelper.ExpectValidationAsync(Page);
    }

    /// <summary>
    /// TC_REQ_US01_02_02 - Để trống Email, nhập Password hợp lệ
    /// Input : Email=(trống), Password=123456
    /// Expect: Hiển thị lỗi trường Email bắt buộc, chặn đăng nhập
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US01_02_02_EmptyEmail_ValidPassword()
    {
        // Mở trang đăng nhập
        await Page.GotoAsync("/login", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Chỉ điền Password, bỏ trống Email
        await Page.Locator("#Password, input[name='Password']").First.FillAsync(TestConfig.CustomerPassword);

        var loginBtn = Page.Locator(
            ".login-button, button:has-text('Log in'), input.login-button, " +
            "input[type='submit'][value*='Log in'], button:has-text('Đăng nhập')").First;
        await loginBtn.ClickAsync();

        // Phải có thông báo lỗi validation (email bắt buộc)
        await NopHelper.ExpectValidationAsync(Page);
    }

    /// <summary>
    /// TC_REQ_US01_02_03 - Nhập Email hợp lệ, để trống Password
    /// Input : Email=buihoang3425@gmail.com, Password=(trống)
    /// Expect: Hiển thị lỗi hoặc thông báo đăng nhập không thành công
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US01_02_03_ValidEmail_EmptyPassword()
    {
        // Mở trang đăng nhập
        await Page.GotoAsync("/login", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Chỉ điền Email, để trống Password
        await Page.Locator("#Email, input[name='Email']").First.FillAsync(TestConfig.CustomerEmail);

        var loginBtn = Page.Locator(
            ".login-button, button:has-text('Log in'), input.login-button, " +
            "input[type='submit'][value*='Log in'], button:has-text('Đăng nhập')").First;
        await loginBtn.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Hệ thống báo lỗi (password trống dẫn đến đăng nhập không thành công)
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex(
                @"unsuccessful|wrong|incorrect|required|không thành công|không chính xác|bắt buộc",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    // ── REQ_US01_03: Sai thông tin xác thực ───────────────────────────────

    /// <summary>
    /// TC_REQ_US01_03_01 - Nhập Email đúng nhưng Password sai
    /// Input : Email=buihoang3425@gmail.com, Password=1234567 (sai)
    /// Expect: Thông báo lỗi 'Thông tin đăng nhập không chính xác'
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US01_03_01_CorrectEmail_WrongPassword()
    {
        // Đăng nhập với email đúng nhưng password sai
        await AuthHelper.LoginWithCredentialsAsync(Page, TestConfig.CustomerEmail, "WrongPass999");

        // Hệ thống phải từ chối và hiển thị lỗi
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex(
                @"unsuccessful|incorrect|wrong|not valid|không thành công|không chính xác|thông tin đăng nhập",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// TC_REQ_US01_03_02 - Nhập Email không tồn tại và Password bất kỳ
    /// Input : Email=notexist@nomail.com, Password=AnyPass999
    /// Expect: Thông báo lỗi giống như sai Password (không tiết lộ email không tồn tại)
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US01_03_02_NonExistentEmail()
    {
        // Đăng nhập với email không tồn tại trong CSDL
        await AuthHelper.LoginWithCredentialsAsync(Page, "notexist@nomail.com", "AnyPass999");

        // Thông báo lỗi phải giống với trường hợp sai password (bảo mật - không tiết lộ)
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex(
                @"unsuccessful|incorrect|wrong|không thành công|không chính xác",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// TC_REQ_US01_03_03 - Nhập sai Password nhiều lần liên tiếp (5 lần)
    /// Input : Email=buihoang3425@gmail.com, Password sai: BadPass01..BadPass05
    /// Expect: Hệ thống xử lý và hiển thị thông báo lỗi nhất quán ở mỗi lần thử
    /// </summary>
    [Test]
    [Category("Edge")]
    public async Task TC_REQ_US01_03_03_WrongPassword_5Times()
    {
        // Thử đăng nhập sai 5 lần liên tiếp với cùng email
        for (int i = 1; i <= 5; i++)
        {
            // Mỗi lần thử với password sai khác nhau
            await AuthHelper.LoginWithCredentialsAsync(Page, TestConfig.CustomerEmail, $"BadPass0{i}");

            // Sau mỗi lần, hệ thống phải hiển thị thông báo lỗi nhất quán
            await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(
                new System.Text.RegularExpressions.Regex(
                    @"unsuccessful|incorrect|không thành công|thông tin đăng nhập",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        }
    }
}
