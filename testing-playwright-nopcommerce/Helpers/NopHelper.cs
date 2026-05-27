using Microsoft.Playwright;

namespace NopCommerceTests.Helpers;

/// <summary>
/// Các utility helper dùng chung cho nopCommerce tests
/// </summary>
public static class NopHelper
{
    // ── Điều hướng ─────────────────────────────────────────────────────────

    /// <summary>Mở trang sản phẩm và chờ load</summary>
    public static async Task OpenProductAsync(IPage page, string path)
    {
        await page.GotoAsync(path, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Assertions.Expect(page.Locator("body")).ToBeVisibleAsync();
    }

    /// <summary>Mở trang danh mục và xác nhận không lỗi kỹ thuật</summary>
    public static async Task OpenCategoryAsync(IPage page, string path)
    {
        await page.GotoAsync(path, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Assertions.Expect(page.Locator("body")).ToBeVisibleAsync();
        await AssertNoTechnicalErrorAsync(page);
    }

    // ── Form helpers ───────────────────────────────────────────────────────

    /// <summary>Fill input nếu tồn tại</summary>
    public static async Task<bool> FillIfPresentAsync(IPage page, string selector, string value)
    {
        var el = page.Locator(selector).First;
        if (await el.CountAsync() == 0) return false;
        await el.FillAsync(value);
        return true;
    }

    /// <summary>Click Save trong form Admin</summary>
    public static async Task SaveAdminFormAsync(IPage page)
    {
        var save = page.Locator(
            "button[name='save'], button[name='save-continue'], button:has-text('Save'), " +
            "button:has-text('Tiết kiệm'), button:has-text('Lưu'), input[name='save'], input[value='Save']").First;
        await Assertions.Expect(save).ToBeVisibleAsync();
        await save.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    /// <summary>Click Search button trong grid Admin</summary>
    public static async Task SearchAdminGridAsync(IPage page, string searchBtnSelector)
    {
        var btn = page.Locator(searchBtnSelector).First;
        if (await btn.CountAsync() > 0)
        {
            await btn.ClickAsync();
            await page.WaitForTimeoutAsync(1000);
        }
    }

    // ── Assertions ─────────────────────────────────────────────────────────

    /// <summary>Xác nhận body chứa text theo regex</summary>
    public static async Task AssertBodyContainsAsync(IPage page, System.Text.RegularExpressions.Regex pattern, string message)
    {
        await Assertions.Expect(page.Locator("body"))
            .ToContainTextAsync(pattern, new LocatorAssertionsToContainTextOptions { Timeout = 10_000 });
    }

    /// <summary>Xác nhận không có lỗi kỹ thuật (500, exception...)</summary>
    public static async Task AssertNoTechnicalErrorAsync(IPage page)
    {
        var noErrorPattern = new System.Text.RegularExpressions.Regex(
            @"error 500|server error|exception|stack trace|nullreference|runtime error",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        await Assertions.Expect(page.Locator("body")).Not.ToContainTextAsync(noErrorPattern);
    }

    /// <summary>Xác nhận có thông báo validation</summary>
    public static async Task ExpectValidationAsync(IPage page)
    {
        var validation = page.Locator(
            ".field-validation-error, .validation-summary-errors, .message-error, .alert-danger")
            .First;

        if (await validation.CountAsync() > 0 && await validation.IsVisibleAsync())
        {
            await Assertions.Expect(validation).ToBeVisibleAsync();
            return;
        }

        var validationText = new System.Text.RegularExpressions.Regex(
            @"required|invalid|error|must|already exists|not valid|
              bắt buộc|không hợp lệ|lỗi|đã tồn tại|vui lòng",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace);
        await Assertions.Expect(page.Locator("body")).ToContainTextAsync(validationText);
    }

    /// <summary>Xác nhận hiển thị thông báo thành công</summary>
    public static async Task ExpectSuccessAsync(IPage page)
    {
        var success = page.Locator(
            ".alert-success, .message-success, .bar-notification.success, .notification-success")
            .First;

        if (await success.CountAsync() > 0 && await success.IsVisibleAsync())
        {
            await Assertions.Expect(success).ToBeVisibleAsync();
            return;
        }

        var successText = new System.Text.RegularExpressions.Regex(
            @"success|successfully|updated|saved|thành công|cập nhật|đã được",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        await Assertions.Expect(page.Locator("body")).ToContainTextAsync(successText);
    }

    // ── Shop helpers ───────────────────────────────────────────────────────

    /// <summary>Tìm kiếm sản phẩm từ trang chủ</summary>
    public static async Task SearchStoreAsync(IPage page, string term)
    {
        await page.GotoAsync("/", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        var input = page.Locator("#small-searchterms, input[name='q'], input.search-box-text").First;
        await Assertions.Expect(input).ToBeVisibleAsync();
        await input.FillAsync(term);

        var btn = page.Locator(
            "button.search-box-button, input.search-box-button, button:has-text('Search'), " +
            "input[value*='Search'], button:has-text('Tìm')").First;

        if (await btn.CountAsync() > 0)
            await btn.ClickAsync();
        else
            await input.PressAsync("Enter");

        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    /// <summary>Click nút Add to cart</summary>
    public static async Task ClickAddToCartAsync(IPage page)
    {
        var btn = page.Locator(
            "button[id^='add-to-cart-button'], input[id^='add-to-cart-button'], " +
            "button:has-text('Add to cart'), input[value*='Add to cart'], " +
            "button:has-text('Thêm vào giỏ')").First;
        await Assertions.Expect(btn).ToBeVisibleAsync();
        await btn.ClickAsync();
        await page.WaitForTimeoutAsync(800);
    }

    /// <summary>Chọn tùy chọn sắp xếp</summary>
    public static async Task SelectSortAsync(IPage page, System.Text.RegularExpressions.Regex label)
    {
        var sort = page.Locator("#products-orderby, select[name='products-orderby']").First;
        await Assertions.Expect(sort).ToBeVisibleAsync();
        var options = await sort.Locator("option").AllTextContentsAsync();
        var match = options.FirstOrDefault(o => label.IsMatch(o));
        if (match != null)
            await sort.SelectOptionAsync(new SelectOptionValue { Label = match });
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    /// <summary>Chọn thuộc tính sản phẩm đầu tiên tìm thấy</summary>
    public static async Task SelectFirstProductAttributeAsync(IPage page)
    {
        var select = page.Locator("select[id^='product_attribute_']").First;
        if (await select.CountAsync() > 0)
        {
            await select.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            return;
        }
        var radio = page.Locator("input[type='radio'][name^='product_attribute_']").First;
        if (await radio.CountAsync() > 0)
        {
            await radio.CheckAsync(new LocatorCheckOptions { Force = true });
            return;
        }
        throw new Exception("Không tìm thấy thuộc tính sản phẩm để chọn.");
    }

    /// <summary>Xác nhận danh sách sản phẩm hiển thị</summary>
    public static async Task ExpectProductListVisibleAsync(IPage page)
    {
        await Assertions.Expect(
            page.Locator(".product-grid, .product-list, .product-item").First)
            .ToBeVisibleAsync();
    }
}
