using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace NopCommerceTests;

/// <summary>
/// Base class cho tất cả test - cấu hình Playwright + tạo Page
/// </summary>
[TestFixture]
public abstract class PlaywrightTestBase : PageTest
{
    // ── Regex dùng chung ────────────────────────────────────────────────────
    protected static readonly System.Text.RegularExpressions.Regex ValidationText =
        new(@"required|invalid|error|must|already exists|not valid|please provide|
              bắt buộc|không hợp lệ|lỗi|đã tồn tại|vui lòng",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace);

    protected static readonly System.Text.RegularExpressions.Regex SuccessText =
        new(@"success|successfully|updated|saved|thành công|cập nhật|đã được",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    protected static readonly System.Text.RegularExpressions.Regex NotFoundText =
        new(@"not found|page not found|404|không tìm thấy",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    protected static readonly System.Text.RegularExpressions.Regex LoginFailText =
        new(@"login was unsuccessful|invalid|no customer account|credentials|wrong|
              disabled|locked|not active|không thành công|không hợp lệ|bị khóa",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace);

    // ── Playwright override ─────────────────────────────────────────────────
    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            BaseURL = TestConfig.BaseUrl,
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
            Locale = "vi-VN",
            TimezoneId = "Asia/Ho_Chi_Minh",
        };
    }

    [SetUp]
    public void SetUp()
    {
        Page.SetDefaultTimeout(15_000);
    }
}
