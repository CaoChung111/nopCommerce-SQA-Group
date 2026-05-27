using Microsoft.Playwright;

namespace NopCommerceTests.Helpers;

/// <summary>
/// Helper đăng nhập / đăng xuất cho nopCommerce
/// </summary>
public static class AuthHelper
{
    /// <summary>Đăng nhập với tài khoản Admin</summary>
    public static async Task LoginAsAdminAsync(IPage page)
    {
        await page.GotoAsync("/Admin", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await FillLoginFormAsync(page, TestConfig.AdminEmail, TestConfig.AdminPassword);
        // Xác nhận vào được trang Admin
        await Assertions.Expect(page.Locator("body"))
            .ToContainTextAsync(new System.Text.RegularExpressions.Regex(
                @"Dashboard|Administration|Logout|Log out|bảng điều khiển",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    /// <summary>Đăng nhập với tài khoản Customer thông thường</summary>
    public static async Task LoginAsCustomerAsync(IPage page)
    {
        await LoginWithCredentialsAsync(page, TestConfig.CustomerEmail, TestConfig.CustomerPassword);
        await Assertions.Expect(
            page.Locator("a[href*='/logout'], a:has-text('Log out'), a:has-text('Đăng xuất')").First)
            .ToBeVisibleAsync();
    }

    /// <summary>Đăng nhập với email/password tùy chỉnh</summary>
    public static async Task LoginWithCredentialsAsync(IPage page, string email, string password)
    {
        await page.GotoAsync("/login", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await FillLoginFormAsync(page, email, password);
    }

    /// <summary>Điền form đăng nhập và submit</summary>
    private static async Task FillLoginFormAsync(IPage page, string email, string password)
    {
        var emailInput = page.Locator("input[name='Email'], #Email").First;
        await emailInput.FillAsync(email);

        var passwordInput = page.Locator("input[name='Password'], #Password").First;
        await passwordInput.FillAsync(password);

        var loginBtn = page.Locator(
            ".login-button, button:has-text('Log in'), input.login-button, " +
            "input[type='submit'][value*='Log in'], button:has-text('Đăng nhập')").First;
        await loginBtn.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    /// <summary>Đăng xuất</summary>
    public static async Task LogoutAsync(IPage page)
    {
        try
        {
            await page.GotoAsync("/logout", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        }
        catch
        {
            var logoutLink = page.GetByRole(AriaRole.Link, new() { NameRegex = new System.Text.RegularExpressions.Regex(@"log out|logout|đăng xuất", System.Text.RegularExpressions.RegexOptions.IgnoreCase) }).First;
            if (await logoutLink.CountAsync() > 0)
                await logoutLink.ClickAsync();
        }
    }
}
