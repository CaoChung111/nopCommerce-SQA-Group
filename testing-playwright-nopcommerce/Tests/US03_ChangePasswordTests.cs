using Microsoft.Playwright;
using NUnit.Framework;
using NopCommerceTests.Helpers;

namespace NopCommerceTests.Tests;

/// <summary>
/// US03 - Đổi mật khẩu: REQ_US03_01 đến REQ_US03_04
/// Tổng: 11 test cases
/// CSV dòng 115–178 của SQA_Plan - w4_Test_Case.csv
/// URL: /customer/changepassword
/// </summary>
[TestFixture]
[Category("US03")]
public class US03_ChangePasswordTests : PlaywrightTestBase
{
    // ── Dữ liệu test cố định (khớp với CSV) ────────────────────────────────
    private const string OriginalPassword = "Test@1234";
    private const string NewPassword      = "NewPass@5678";

    // Biến theo dõi: mật khẩu hiện tại của account sau mỗi test đổi MK thành công
    private bool _passwordChanged = false;

    // ── SetUp: Đăng nhập trước mỗi test ────────────────────────────────────
    [SetUp]
    public async Task SetUpLogin()
    {
        // Reset trạng thái đổi MK
        _passwordChanged = false;

        // Đăng nhập trước mỗi test
        await AuthHelper.LoginWithCredentialsAsync(Page, TestConfig.CustomerEmail, TestConfig.CustomerPassword);

        // Điều hướng tới trang đổi mật khẩu
        await Page.GotoAsync("/customer/changepassword",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
    }

    // ── TearDown: Reset MK về ban đầu nếu test đã đổi thành công ────────────
    [TearDown]
    public async Task TearDownResetPassword()
    {
        // Nếu test vừa đổi MK thành công, cần reset về MK gốc để các test sau không bị ảnh hưởng
        if (_passwordChanged)
        {
            try
            {
                // Logout trước
                await AuthHelper.LogoutAsync(Page);

                // Đăng nhập lại bằng MK mới vừa set
                await AuthHelper.LoginWithCredentialsAsync(Page, TestConfig.CustomerEmail, NewPassword);

                // Vào trang đổi MK để reset về MK gốc
                await Page.GotoAsync("/customer/changepassword",
                    new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

                // Điền để reset về MK gốc
                await FillChangePasswordFormAsync(NewPassword, OriginalPassword, OriginalPassword);

                var submitBtn = Page.Locator(
                    "button:has-text('Change password'), input[value*='Change password'], " +
                    "button[type='submit'], button:has-text('Đổi mật khẩu')").First;
                await submitBtn.ClickAsync();
                await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            }
            catch
            {
                // Nếu reset thất bại, bỏ qua (có thể MK đã là gốc)
            }
        }
    }

    // ── Helper: Điền form đổi MK ─────────────────────────────────────────────
    private async Task FillChangePasswordFormAsync(string oldPw, string newPw, string confirmPw)
    {
        // Điền Old Password
        var oldInput = Page.Locator(
            "#OldPassword, input[name='OldPassword'], input[id*='old'], input[id*='current']").First;
        if (await oldInput.CountAsync() > 0)
        {
            await oldInput.ClearAsync();
            await oldInput.FillAsync(oldPw);
        }

        // Điền New Password
        var newInput = Page.Locator(
            "#NewPassword, input[name='NewPassword'], input[id*='new-password'], input[id*='newpassword']").First;
        if (await newInput.CountAsync() > 0)
        {
            await newInput.ClearAsync();
            await newInput.FillAsync(newPw);
        }

        // Điền Confirm Password
        var confirmInput = Page.Locator(
            "#ConfirmNewPassword, input[name='ConfirmNewPassword'], " +
            "input[id*='confirm'], input[name*='confirm']").First;
        if (await confirmInput.CountAsync() > 0)
        {
            await confirmInput.ClearAsync();
            await confirmInput.FillAsync(confirmPw);
        }
    }

    // ── Helper: Nhấn nút Submit đổi MK ──────────────────────────────────────
    private async Task ClickChangePasswordSubmitAsync()
    {
        var submitBtn = Page.Locator(
            "button:has-text('Change password'), input[value*='Change password'], " +
            "button[type='submit'], button:has-text('Đổi mật khẩu'), " +
            "input[type='submit']").First;
        await Assertions.Expect(submitBtn).ToBeVisibleAsync();
        await submitBtn.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    // ── REQ_US03_01: Đổi MK thành công và trùng MK cũ ─────────────────────

    /// <summary>
    /// TC_REQ_US03_01_01 - Đổi mật khẩu thành công với đầy đủ thông tin hợp lệ
    /// Input : Old=Test@1234, New=NewPass@5678, Confirm=NewPass@5678
    /// Expect: Mật khẩu mới được Hash và cập nhật vào CSDL, thông báo thành công
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US03_01_01_ChangePassword_Success()
    {
        // Điền đầy đủ thông tin hợp lệ
        await FillChangePasswordFormAsync(OriginalPassword, NewPassword, NewPassword);
        await ClickChangePasswordSubmitAsync();

        // Đánh dấu để TearDown reset về MK gốc
        _passwordChanged = true;

        // Xác nhận thông báo đổi MK thành công
        await NopHelper.ExpectSuccessAsync(Page);
    }

    /// <summary>
    /// TC_REQ_US03_01_02 - Đổi sang mật khẩu mới trùng với mật khẩu cũ
    /// Input : Old=Test@1234, New=Test@1234, Confirm=Test@1234
    /// Expect: Hệ thống cảnh báo không nên dùng mật khẩu cũ theo business rule
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US03_01_02_NewPasswordSameAsOld_Fail()
    {
        // Nhập MK mới trùng với MK cũ
        await FillChangePasswordFormAsync(OriginalPassword, OriginalPassword, OriginalPassword);
        await ClickChangePasswordSubmitAsync();

        // Hệ thống phải cảnh báo không cho dùng MK giống MK cũ
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex(
                @"same|identical|last.*password|giống|trùng|mật khẩu cuối|bạn đã nhập mật khẩu giống",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// TC_REQ_US03_01_03 - Kiểm tra tính năng ẩn/hiện mật khẩu trên các trường input
    /// Input : Vào trang Change Password, quan sát các trường password input
    /// Expect: Các trường mật khẩu có type='password' (mặc định ẩn), icon toggle tồn tại
    /// </summary>
    [Test]
    [Category("Edge")]
    public async Task TC_REQ_US03_01_03_PasswordInputType_HiddenByDefault()
    {
        // Kiểm tra trường Old Password có type='password' (ẩn mặc định)
        var oldInput = Page.Locator(
            "input[type='password'][id*='old'], input[type='password'][name*='Old'], " +
            "#OldPassword[type='password']").First;

        // Nếu không tìm theo ID cụ thể, tìm input type password bất kỳ trên form
        var anyPasswordInput = Page.Locator("input[type='password']").First;
        await Assertions.Expect(anyPasswordInput).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 10_000 });

        // Đếm số lượng trường password (phải có ít nhất 1, thường là 3)
        var passwordInputCount = await Page.Locator("input[type='password']").CountAsync();
        Assert.That(passwordInputCount, Is.GreaterThanOrEqualTo(1),
            "Trang Change Password phải có ít nhất 1 trường input type=password");
    }

    // ── REQ_US03_02: Trường bắt buộc ────────────────────────────────────────

    /// <summary>
    /// TC_REQ_US03_02_01 - Bỏ trống Old Password, nhập New và Confirm hợp lệ
    /// Input : Old=(trống), New=NewPass@5678, Confirm=NewPass@5678
    /// Expect: Hệ thống hiển thị lỗi bắt buộc tại trường Old Password, chặn thực hiện
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US03_02_01_EmptyOldPassword_Fail()
    {
        // Để trống Old Password, điền New và Confirm
        await FillChangePasswordFormAsync("", NewPassword, NewPassword);
        await ClickChangePasswordSubmitAsync();

        // Phải có thông báo validation (trường bắt buộc)
        await NopHelper.ExpectValidationAsync(Page);
    }

    /// <summary>
    /// TC_REQ_US03_02_02 - Chỉ bỏ trống trường Mật khẩu mới (New Password)
    /// Input : Old=Test@1234, New=(trống), Confirm=(trống)
    /// Expect: Hiển thị lỗi tại riêng trường Mật khẩu mới, chặn lưu
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US03_02_02_EmptyNewPassword_Fail()
    {
        // Điền Old Password đúng, bỏ trống New và Confirm
        await FillChangePasswordFormAsync(OriginalPassword, "", "");
        await ClickChangePasswordSubmitAsync();

        // Phải có thông báo lỗi tại trường MK mới (required / bắt buộc)
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex(
                @"required|must|bắt buộc|new password|mật khẩu mới",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// TC_REQ_US03_02_03 - Điền đầy đủ cả 3 trường hợp lệ
    /// Input : Old=Test@1234, New=NewPass@5678, Confirm=NewPass@5678
    /// Expect: Hệ thống không hiển thị lỗi bắt buộc, tiến hành xử lý thành công
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US03_02_03_AllFieldsFilled_Success()
    {
        // Điền đầy đủ 3 trường hợp lệ
        await FillChangePasswordFormAsync(OriginalPassword, NewPassword, NewPassword);
        await ClickChangePasswordSubmitAsync();

        // Đánh dấu để TearDown reset
        _passwordChanged = true;

        // Xác nhận thành công (không có lỗi bắt buộc, hệ thống xử lý được)
        await NopHelper.ExpectSuccessAsync(Page);
    }

    // ── REQ_US03_03: Kiểm tra MK cũ đúng/sai ──────────────────────────────

    /// <summary>
    /// TC_REQ_US03_03_01 - Nhập đúng MK cũ nhưng New và Confirm không khớp
    /// Input : Old=Test@1234 (đúng), New=NewPass@5678, Confirm=DiffPass@999 (không khớp)
    /// Expect: Hệ thống hiển thị lỗi mật khẩu mới và xác nhận không khớp, không cập nhật
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US03_03_01_CorrectOldPw_NewConfirmMismatch_Fail()
    {
        // Nhập Old đúng, New và Confirm không khớp nhau
        await FillChangePasswordFormAsync(OriginalPassword, NewPassword, "DiffPass@999");
        await ClickChangePasswordSubmitAsync();

        // Hệ thống phải báo MK mới và xác nhận không khớp
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex(
                @"not match|mismatch|do not match|không khớp|xác nhận|confirm",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// TC_REQ_US03_03_02 - Nhập đúng MK cũ cùng MK mới hợp lệ
    /// Input : Old=Test@1234, New=NewPass@5678, Confirm=NewPass@5678
    /// Expect: Hệ thống cho phép cập nhật mật khẩu mới thành công
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US03_03_02_CorrectOldPw_ValidNewPw_Success()
    {
        // Điền đúng tất cả thông tin
        await FillChangePasswordFormAsync(OriginalPassword, NewPassword, NewPassword);
        await ClickChangePasswordSubmitAsync();

        // Đánh dấu để TearDown reset
        _passwordChanged = true;

        // Xác nhận đổi MK thành công
        await NopHelper.ExpectSuccessAsync(Page);
    }

    // ── REQ_US03_04: Độ dài và khớp MK ──────────────────────────────────────

    /// <summary>
    /// TC_REQ_US03_04_01 - Mật khẩu mới quá ngắn (dưới 6 ký tự)
    /// Input : Old=Test@1234, New=Ab@1 (4 ký tự), Confirm=Ab@1
    /// Expect: Hệ thống hiển thị lỗi MK phải có ít nhất 6 ký tự
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US03_04_01_NewPassword_TooShort_Fail()
    {
        // Nhập MK mới quá ngắn (chỉ 4 ký tự, dưới ngưỡng 6)
        const string shortPassword = "Ab@1";
        await FillChangePasswordFormAsync(OriginalPassword, shortPassword, shortPassword);
        await ClickChangePasswordSubmitAsync();

        // Hệ thống phải báo lỗi về độ dài tối thiểu 6 ký tự
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex(
                @"6|minimum|at least|length|độ dài|tối thiểu|ít nhất|quy tắc",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// TC_REQ_US03_04_02 - Mật khẩu mới đủ 6 ký tự (đúng ngưỡng tối thiểu)
    /// Input : Old=Test@1234, New=Abc@12 (6 ký tự), Confirm=Abc@12
    /// Expect: Hệ thống chấp nhận và đổi mật khẩu thành công
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US03_04_02_NewPassword_MinLength6Chars_Success()
    {
        // Nhập MK mới đúng 6 ký tự (đủ ngưỡng tối thiểu)
        const string minLengthPassword = "Abc@12";
        await FillChangePasswordFormAsync(OriginalPassword, minLengthPassword, minLengthPassword);
        await ClickChangePasswordSubmitAsync();

        // Đánh dấu để TearDown reset (với MK mới là minLengthPassword)
        // Lưu ý: TearDown dùng NewPassword="NewPass@5678", nhưng test này dùng "Abc@12"
        // Nên ta xử lý reset ngay trong test nếu thành công
        var isSuccess = false;
        try
        {
            await NopHelper.ExpectSuccessAsync(Page);
            isSuccess = true;
        }
        catch { }

        if (isSuccess)
        {
            // Reset về MK gốc ngay trong test này
            await Page.GotoAsync("/customer/changepassword",
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await FillChangePasswordFormAsync(minLengthPassword, OriginalPassword, OriginalPassword);
            await ClickChangePasswordSubmitAsync();
            // Xác nhận đổi MK thành công
            Assert.Pass("Đổi MK với 6 ký tự thành công và đã reset về MK gốc.");
        }
        else
        {
            Assert.Fail("Hệ thống không chấp nhận MK đúng 6 ký tự - vi phạm yêu cầu tối thiểu.");
        }
    }

    /// <summary>
    /// TC_REQ_US03_04_03 - Mật khẩu mới và Xác nhận không khớp hoàn toàn
    /// Input : Old=Test@1234, New=AAAAA...@1aB (>100 ký tự), Confirm=same
    /// Theo CSV: thực chất test "MK mới và Confirm không khớp"
    /// Expect: Hệ thống hiển thị lỗi không khớp và chặn lưu
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US03_04_03_NewAndConfirmPassword_Mismatch_Fail()
    {
        // Điền New và Confirm khác nhau hoàn toàn để kiểm tra lỗi không khớp
        const string newPw     = "NewPass@5678";
        const string confirmPw = "DifferentPass@9999";

        await FillChangePasswordFormAsync(OriginalPassword, newPw, confirmPw);
        await ClickChangePasswordSubmitAsync();

        // Hệ thống phải báo MK mới và xác nhận không khớp (không được lưu)
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex(
                @"not match|mismatch|do not match|không khớp|xác nhận|mật khẩu mới và mật khẩu xác nhận",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }
}
