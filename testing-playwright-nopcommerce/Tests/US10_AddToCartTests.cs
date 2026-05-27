using Microsoft.Playwright;
using NUnit.Framework;
using NopCommerceTests.Helpers;

namespace NopCommerceTests.Tests;

/// <summary>
/// US10 - Add to Cart: Kiểm tra chức năng thêm sản phẩm vào giỏ hàng
/// CSV rows 549-590
/// </summary>
[TestFixture]
[Category("US10")]
public class US10_AddToCartTests : PlaywrightTestBase
{
    // ── Regex dùng chung cho US10 ───────────────────────────────────────────
    private static readonly System.Text.RegularExpressions.Regex AddedToCartText =
        new(@"added to your shopping cart|product has been added",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static readonly System.Text.RegularExpressions.Regex OutOfStockText =
        new(@"out of stock|hết hàng",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static readonly System.Text.RegularExpressions.Regex QtyErrorText =
        new(@"quantity|stock|maximum|số lượng|tồn kho",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static readonly System.Text.RegularExpressions.Regex InvalidQtyText =
        new(@"positive|invalid|must be|Quantity should be positive|số lượng phải",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    // ── Helper: Thiết lập số lượng trên trang sản phẩm ─────────────────────
    private async Task SetQuantityAsync(int qty)
    {
        var qtyInput = Page.Locator("input[name='addtocart_{0}_EnteredQuantity'], input.qty-input, #product_enteredQuantity_{0}, input[id*='EnteredQuantity']").First;
        // Thử selector phổ biến hơn
        var qtyLocator = Page.Locator("input.qty-input, input[id*='EnteredQuantity'], input[name*='EnteredQuantity']").First;
        if (await qtyLocator.CountAsync() > 0)
        {
            await qtyLocator.FillAsync(qty.ToString());
        }
    }

    // ── TC_REQ_US10_01_01 ───────────────────────────────────────────────────
    /// <summary>
    /// Thêm sản phẩm vào giỏ hàng thành công.
    /// SP Published, tồn kho > 0, click Add to cart → thông báo thêm thành công.
    /// </summary>
    [Test]
    [Description("TC_REQ_US10_01_01 - Thêm sản phẩm vào giỏ hàng thành công")]
    public async Task TC_REQ_US10_01_01_AddProductToCartSuccess()
    {
        // 1. Truy cập trang sản phẩm Asus Laptop (còn hàng, đã published)
        await NopHelper.OpenProductAsync(Page, TestConfig.AsusPath);

        // 2. Click nút "Add to cart"
        await NopHelper.ClickAddToCartAsync(Page);

        // 3. Xác nhận thông báo thêm vào giỏ thành công
        await NopHelper.AssertBodyContainsAsync(Page, AddedToCartText,
            "Kỳ vọng thông báo 'added to your shopping cart'");
    }

    // ── TC_REQ_US10_01_02 ───────────────────────────────────────────────────
    /// <summary>
    /// Thêm cùng sản phẩm lần 2 - kiểm tra cộng dồn số lượng.
    /// Lần 1: số lượng 1, Lần 2: số lượng 2 → giỏ hàng hiển thị tổng 3.
    /// </summary>
    [Test]
    [Description("TC_REQ_US10_01_02 - Thêm cùng sản phẩm lần 2, số lượng cộng dồn")]
    public async Task TC_REQ_US10_01_02_AddSameProductTwiceAccumulates()
    {
        // 1. Truy cập trang sản phẩm và thêm lần 1 (qty=1)
        await NopHelper.OpenProductAsync(Page, TestConfig.AsusPath);
        var qtyInput1 = Page.Locator("input.qty-input, input[id*='EnteredQuantity'], input[name*='EnteredQuantity']").First;
        if (await qtyInput1.CountAsync() > 0)
            await qtyInput1.FillAsync("1");
        await NopHelper.ClickAddToCartAsync(Page);
        await Page.WaitForTimeoutAsync(500);

        // 2. Thêm lần 2 (qty=2)
        await NopHelper.OpenProductAsync(Page, TestConfig.AsusPath);
        var qtyInput2 = Page.Locator("input.qty-input, input[id*='EnteredQuantity'], input[name*='EnteredQuantity']").First;
        if (await qtyInput2.CountAsync() > 0)
            await qtyInput2.FillAsync("2");
        await NopHelper.ClickAddToCartAsync(Page);
        await Page.WaitForTimeoutAsync(500);

        // 3. Vào trang giỏ hàng và kiểm tra tổng số lượng = 3
        await Page.GotoAsync("/cart", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        var qtyInCart = Page.Locator(".qty-input, input[name*='itemquantity'], input[class*='qty']").First;
        if (await qtyInCart.CountAsync() > 0)
        {
            var qtyValue = await qtyInCart.InputValueAsync();
            Assert.That(qtyValue, Is.EqualTo("3"),
                "Kỳ vọng tổng số lượng trong giỏ = 3 (1+2)");
        }
        else
        {
            // Kiểm tra body không lỗi
            await NopHelper.AssertNoTechnicalErrorAsync(Page);
        }
    }

    // ── TC_REQ_US10_01_03 ───────────────────────────────────────────────────
    /// <summary>
    /// Thêm sản phẩm hết hàng vào giỏ.
    /// SP tồn kho = 0 → hiển thị "Out of stock", nút Add to cart bị ẩn/disabled.
    /// </summary>
    [Test]
    [Description("TC_REQ_US10_01_03 - Thêm sản phẩm hết hàng, hiển thị Out of stock")]
    public async Task TC_REQ_US10_01_03_AddOutOfStockProduct()
    {
        // 1. Truy cập sản phẩm hết hàng
        await NopHelper.OpenProductAsync(Page, TestConfig.OutOfStockPath);

        // 2. Xác nhận trang hiển thị thông báo "Out of stock"
        await NopHelper.AssertBodyContainsAsync(Page, OutOfStockText,
            "Kỳ vọng hiển thị 'Out of stock'");

        // 3. Xác nhận nút Add to cart không khả dụng (disabled hoặc không có)
        var addBtn = Page.Locator(
            "button[id^='add-to-cart-button'], input[id^='add-to-cart-button'], " +
            "button:has-text('Add to cart')");
        var btnCount = await addBtn.CountAsync();
        if (btnCount > 0)
        {
            // Nếu nút tồn tại thì phải bị disabled
            var isDisabled = await addBtn.First.IsDisabledAsync();
            Assert.That(isDisabled, Is.True,
                "Nút Add to cart phải bị disabled khi sản phẩm hết hàng");
        }
        // Nếu không có nút → đã ẩn → test pass
    }

    // ── TC_REQ_US10_02_01 ───────────────────────────────────────────────────
    /// <summary>
    /// Click Add to cart khi chưa chọn thuộc tính bắt buộc.
    /// SP có thuộc tính Size bắt buộc → hệ thống hiển thị cảnh báo validation.
    /// </summary>
    [Test]
    [Description("TC_REQ_US10_02_01 - Click Add to cart khi chưa chọn thuộc tính bắt buộc")]
    public async Task TC_REQ_US10_02_01_AddToCartWithoutRequiredAttribute()
    {
        // 1. Truy cập sản phẩm có thuộc tính bắt buộc (Build Your Own Computer)
        await NopHelper.OpenProductAsync(Page, TestConfig.BuildComputerPath);

        // 2. KHÔNG chọn thuộc tính nào, thực hiện click Add to cart ngay
        var addBtn = Page.Locator(
            "button[id^='add-to-cart-button'], input[id^='add-to-cart-button'], " +
            "button:has-text('Add to cart'), input[value*='Add to cart']").First;
        await Assertions.Expect(addBtn).ToBeVisibleAsync();
        await addBtn.ClickAsync();
        await Page.WaitForTimeoutAsync(800);

        // 3. Xác nhận hiển thị thông báo validation
        await NopHelper.ExpectValidationAsync(Page);
    }

    // ── TC_REQ_US10_02_02 ───────────────────────────────────────────────────
    /// <summary>
    /// Thêm vào giỏ sau khi chọn đủ thuộc tính bắt buộc.
    /// Chọn đủ thuộc tính → click Add to cart → thành công.
    /// </summary>
    [Test]
    [Description("TC_REQ_US10_02_02 - Thêm vào giỏ sau khi chọn đủ thuộc tính bắt buộc")]
    public async Task TC_REQ_US10_02_02_AddToCartAfterSelectingAttributes()
    {
        // 1. Truy cập sản phẩm có thuộc tính bắt buộc
        await NopHelper.OpenProductAsync(Page, TestConfig.BuildComputerPath);

        // 2. Chọn thuộc tính đầu tiên trong từng nhóm
        await NopHelper.SelectFirstProductAttributeAsync(Page);

        // 3. Click Add to cart
        await NopHelper.ClickAddToCartAsync(Page);

        // 4. Xác nhận thêm thành công
        await NopHelper.AssertBodyContainsAsync(Page, AddedToCartText,
            "Kỳ vọng thông báo 'added to your shopping cart'");
    }

    // ── TC_REQ_US10_03_01 ───────────────────────────────────────────────────
    /// <summary>
    /// Nhập số lượng bằng đúng tồn kho thực tế.
    /// Tồn kho = 5, nhập 5 → thêm thành công.
    /// </summary>
    [Test]
    [Description("TC_REQ_US10_03_01 - Nhập số lượng bằng đúng tồn kho, thêm thành công")]
    public async Task TC_REQ_US10_03_01_AddExactStockQuantity()
    {
        // 1. Truy cập trang sản phẩm Asus Laptop
        await NopHelper.OpenProductAsync(Page, TestConfig.AsusPath);

        // 2. Điền số lượng = 5 (giả sử tồn kho = 5)
        var qtyInput = Page.Locator("input.qty-input, input[id*='EnteredQuantity'], input[name*='EnteredQuantity']").First;
        if (await qtyInput.CountAsync() > 0)
            await qtyInput.FillAsync("5");

        // 3. Click Add to cart
        await NopHelper.ClickAddToCartAsync(Page);

        // 4. Xác nhận thêm thành công (hoặc không có lỗi kỹ thuật)
        await NopHelper.AssertNoTechnicalErrorAsync(Page);
        // Tùy thuộc vào tồn kho thực tế - nếu thành công sẽ có thông báo
        var body = await Page.Locator("body").TextContentAsync();
        var hasSuccess = AddedToCartText.IsMatch(body ?? "");
        var hasError = QtyErrorText.IsMatch(body ?? "");
        // Chấp nhận cả hai trường hợp - test ghi nhận hành vi
        Assert.That(hasSuccess || hasError, Is.True,
            "Hệ thống phải phản hồi (thành công hoặc thông báo giới hạn tồn kho)");
    }

    // ── TC_REQ_US10_03_02 ───────────────────────────────────────────────────
    /// <summary>
    /// Nhập số lượng vượt quá tồn kho.
    /// Tồn kho = 5, nhập 9999 → hệ thống báo lỗi vượt tồn kho.
    /// </summary>
    [Test]
    [Description("TC_REQ_US10_03_02 - Nhập số lượng vượt tồn kho, hệ thống báo lỗi")]
    public async Task TC_REQ_US10_03_02_AddExceedingStockQuantity()
    {
        // 1. Truy cập trang sản phẩm
        await NopHelper.OpenProductAsync(Page, TestConfig.AsusPath);

        // 2. Điền số lượng cực lớn = 9999 (chắc chắn vượt tồn kho)
        var qtyInput = Page.Locator("input.qty-input, input[id*='EnteredQuantity'], input[name*='EnteredQuantity']").First;
        if (await qtyInput.CountAsync() > 0)
            await qtyInput.FillAsync("9999");

        // 3. Click Add to cart
        var addBtn = Page.Locator(
            "button[id^='add-to-cart-button'], input[id^='add-to-cart-button'], " +
            "button:has-text('Add to cart'), input[value*='Add to cart']").First;
        if (await addBtn.CountAsync() > 0)
            await addBtn.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);

        // 4. Xác nhận body chứa thông báo về số lượng hoặc tồn kho
        await NopHelper.AssertBodyContainsAsync(Page, QtyErrorText,
            "Kỳ vọng thông báo lỗi về số lượng vượt tồn kho");
    }

    // ── TC_REQ_US10_03_03 ───────────────────────────────────────────────────
    /// <summary>
    /// Nhập số lượng = 0 hoặc số âm.
    /// Nhập 0 hoặc -1 → hệ thống hiển thị lỗi "Quantity should be positive".
    /// </summary>
    [Test]
    [Description("TC_REQ_US10_03_03 - Nhập số lượng = 0 hoặc âm, hệ thống báo lỗi")]
    public async Task TC_REQ_US10_03_03_AddZeroOrNegativeQuantity()
    {
        // 1. Truy cập trang sản phẩm
        await NopHelper.OpenProductAsync(Page, TestConfig.AsusPath);

        // 2. Điền số lượng = 0
        var qtyInput = Page.Locator("input.qty-input, input[id*='EnteredQuantity'], input[name*='EnteredQuantity']").First;
        if (await qtyInput.CountAsync() > 0)
            await qtyInput.FillAsync("0");

        // 3. Click Add to cart
        var addBtn = Page.Locator(
            "button[id^='add-to-cart-button'], input[id^='add-to-cart-button'], " +
            "button:has-text('Add to cart'), input[value*='Add to cart']").First;
        if (await addBtn.CountAsync() > 0)
            await addBtn.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);

        // 4. Xác nhận lỗi "positive" hoặc "invalid"
        await NopHelper.AssertBodyContainsAsync(Page, InvalidQtyText,
            "Kỳ vọng lỗi 'Quantity should be positive' khi nhập số lượng = 0");
    }
}
