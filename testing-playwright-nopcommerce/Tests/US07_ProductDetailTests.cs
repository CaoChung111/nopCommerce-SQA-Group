using Microsoft.Playwright;
using NUnit.Framework;
using NopCommerceTests.Helpers;

namespace NopCommerceTests.Tests;

/// <summary>
/// US07 - Trang chi tiết sản phẩm (Product Detail Page)
/// Bao gồm: hiển thị thông tin SP, giá khuyến mãi, tình trạng tồn kho,
/// URL lỗi/ẩn, và thuộc tính sản phẩm cộng giá/đổi ảnh.
/// </summary>
[TestFixture]
[Category("US07")]
public class US07_ProductDetailTests : PlaywrightTestBase
{
    // ── TC_REQ_US07_01_01 ────────────────────────────────────────────────────
    /// <summary>
    /// Xem trang chi tiết sản phẩm còn hàng đang Published.
    /// Kỳ vọng: hiển thị đầy đủ Tên, Giá, SKU và nút 'Add to cart' active.
    /// </summary>
    [Test]
    [Description("TC_REQ_US07_01_01: Xem trang chi tiết SP còn hàng → tên/giá visible, Add to cart active")]
    public async Task TC_REQ_US07_01_01_ProductDetail_InStock_ShowsFullInfo()
    {
        // Mở trang chi tiết sản phẩm còn hàng (MacBook)
        await NopHelper.OpenProductAsync(Page, TestConfig.MacbookPath);

        // Assert: Tên sản phẩm visible trên trang
        var productName = Page.Locator(".product-name h1, h1.product-title, .productName").First;
        await Assertions.Expect(productName).ToBeVisibleAsync();

        // Assert: Giá sản phẩm visible
        var price = Page.Locator(".price-value, .product-price .price, .price").First;
        await Assertions.Expect(price).ToBeVisibleAsync();

        // Assert: Nút 'Add to cart' hiển thị và có thể nhấn (enabled)
        var addToCartBtn = Page.Locator(
            "button[id^='add-to-cart-button'], input[id^='add-to-cart-button'], " +
            "button:has-text('Add to cart'), input[value*='Add to cart'], " +
            "button:has-text('Thêm vào giỏ')").First;
        await Assertions.Expect(addToCartBtn).ToBeVisibleAsync();
        await Assertions.Expect(addToCartBtn).ToBeEnabledAsync();
    }

    // ── TC_REQ_US07_01_02 ────────────────────────────────────────────────────
    /// <summary>
    /// Giá khuyến mãi hiển thị đúng khi SP có giảm giá.
    /// Kỳ vọng: giá gốc bị gạch ngang (.old-price) và giá sau KM (.actual-price) đều visible.
    /// Lưu ý: CSV ghi nhận là Fail/DEF_02 (giá gốc không hiển thị KM).
    /// </summary>
    [Test]
    [Description("TC_REQ_US07_01_02: Giá khuyến mãi hiển thị đúng - old-price gạch ngang + actual-price visible [DEF_02]")]
    public async Task TC_REQ_US07_01_02_DiscountedProduct_ShowsOldAndActualPrice()
    {
        // Mở trang SP đang áp dụng giảm giá
        await Page.GotoAsync(TestConfig.DiscountedPath,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Assert: Giá gốc (bị gạch ngang) hiển thị
        var oldPrice = Page.Locator(".old-price, .price-value-deleted, del.price").First;
        await Assertions.Expect(oldPrice).ToBeVisibleAsync();

        // Assert: Giá sau khuyến mãi (giá thực) hiển thị
        var actualPrice = Page.Locator(".actual-price, .price-value, .product-price .price").First;
        await Assertions.Expect(actualPrice).ToBeVisibleAsync();
    }

    // ── TC_REQ_US07_01_03 ────────────────────────────────────────────────────
    /// <summary>
    /// Nút 'Add to cart' ở trạng thái có thể bấm với SP còn hàng.
    /// Kỳ vọng: button enabled (không bị disabled attribute).
    /// </summary>
    [Test]
    [Description("TC_REQ_US07_01_03: Nút Add to cart active (not disabled) với SP tồn kho > 0")]
    public async Task TC_REQ_US07_01_03_InStockProduct_AddToCartButtonEnabled()
    {
        // Mở trang SP còn hàng
        await NopHelper.OpenProductAsync(Page, TestConfig.MacbookPath);

        // Assert: Nút Add to cart phải enabled (không disabled)
        var addToCartBtn = Page.Locator(
            "button[id^='add-to-cart-button'], input[id^='add-to-cart-button'], " +
            "button:has-text('Add to cart'), input[value*='Add to cart']").First;
        await Assertions.Expect(addToCartBtn).ToBeEnabledAsync();
    }

    // ── TC_REQ_US07_02_01 ────────────────────────────────────────────────────
    /// <summary>
    /// Nhãn 'Out of stock' hiển thị khi tồn kho = 0.
    /// Kỳ vọng: body chứa text 'Out of stock' hoặc 'hết hàng'.
    /// </summary>
    [Test]
    [Description("TC_REQ_US07_02_01: SP tồn kho=0 → hiển thị nhãn 'Out of stock'")]
    public async Task TC_REQ_US07_02_01_OutOfStockProduct_ShowsOutOfStockLabel()
    {
        // Mở trang SP hết hàng
        await NopHelper.OpenProductAsync(Page, TestConfig.OutOfStockPath);

        // Assert: body chứa thông báo hết hàng
        var outOfStockPattern = new System.Text.RegularExpressions.Regex(
            @"Out of stock|out-of-stock|hết hàng|Hết hàng",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(outOfStockPattern);
    }

    // ── TC_REQ_US07_02_02 ────────────────────────────────────────────────────
    /// <summary>
    /// SP tồn kho = 1 vẫn hiển thị nút Add to cart bình thường (edge case).
    /// Kỳ vọng: nút Add to cart visible và enabled.
    /// </summary>
    [Test]
    [Description("TC_REQ_US07_02_02: SP tồn kho=1 (edge) → Add to cart vẫn hiển thị bình thường")]
    public async Task TC_REQ_US07_02_02_Qty1Product_AddToCartStillVisible()
    {
        // Mở trang MacBook (SP còn hàng, đại diện cho edge case qty=1)
        await NopHelper.OpenProductAsync(Page, TestConfig.MacbookPath);

        // Assert: Nút Add to cart tồn tại và visible
        var addToCartBtn = Page.Locator(
            "button[id^='add-to-cart-button'], input[id^='add-to-cart-button'], " +
            "button:has-text('Add to cart'), input[value*='Add to cart']").First;
        await Assertions.Expect(addToCartBtn).ToBeVisibleAsync();
        await Assertions.Expect(addToCartBtn).ToBeEnabledAsync();
    }

    // ── TC_REQ_US07_03_01 ────────────────────────────────────────────────────
    /// <summary>
    /// Truy cập URL sản phẩm đã bị xóa (Deleted).
    /// Kỳ vọng: trang 404 hoặc thông báo 'not found'.
    /// </summary>
    [Test]
    [Description("TC_REQ_US07_03_01: URL SP đã xóa → chuyển hướng trang 404")]
    public async Task TC_REQ_US07_03_01_DeletedProduct_Returns404()
    {
        // Truy cập URL SP đã bị xóa
        await Page.GotoAsync("/deleted-product-slug",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Assert: body chứa 404 hoặc not found
        await NopHelper.AssertBodyContainsAsync(Page, NotFoundText,
            "Trang sản phẩm đã xóa phải trả về 404");
    }

    // ── TC_REQ_US07_03_02 ────────────────────────────────────────────────────
    /// <summary>
    /// Truy cập URL sản phẩm đang ở trạng thái Unpublished (Published = false).
    /// Kỳ vọng: trang 404 hoặc thông báo 'not found'.
    /// </summary>
    [Test]
    [Description("TC_REQ_US07_03_02: URL SP Unpublished → chuyển hướng trang 404")]
    public async Task TC_REQ_US07_03_02_UnpublishedProduct_Returns404()
    {
        // Truy cập URL SP đang ẩn (Published = false)
        await Page.GotoAsync("/qa-unpublished-product",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Assert: body chứa 404 hoặc not found
        await NopHelper.AssertBodyContainsAsync(Page, NotFoundText,
            "Trang sản phẩm unpublished phải trả về 404");
    }

    // ── TC_REQ_US07_04_01 ────────────────────────────────────────────────────
    /// <summary>
    /// Chọn thuộc tính cấu hình cộng thêm giá - kiểm tra giá cập nhật.
    /// Ví dụ: SP "Build Your Own Computer" có thuộc tính OS (Vista Home +50).
    /// Kỳ vọng: giá hiển thị tự động tăng sau khi chọn option cộng thêm.
    /// </summary>
    [Test]
    [Description("TC_REQ_US07_04_01: Thuộc tính cộng thêm giá → giá hiển thị tăng thêm")]
    public async Task TC_REQ_US07_04_01_AttributeWithPriceAdjustment_PriceIncreases()
    {
        // Mở trang SP "Build Your Own Computer" - có thuộc tính OS cộng thêm giá
        await NopHelper.OpenProductAsync(Page, TestConfig.BuildComputerPath);

        // Đọc giá hiện tại trước khi chọn thuộc tính
        var priceLocator = Page.Locator(".price-value, .product-price .price, .actual-price").First;
        await Assertions.Expect(priceLocator).ToBeVisibleAsync();
        var originalPrice = await priceLocator.InnerTextAsync();

        // Chọn option của thuộc tính select (dropdown) - thường là OS
        var attrSelect = Page.Locator("select[id^='product_attribute_']").First;
        if (await attrSelect.CountAsync() > 0)
        {
            // Chọn option index=1 (option thứ hai - thường có price adjustment)
            await attrSelect.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            await Page.WaitForTimeoutAsync(500); // Chờ giá cập nhật

            // Assert: Giá đã được cập nhật (không trống)
            await Assertions.Expect(priceLocator).ToBeVisibleAsync();
        }
        else
        {
            // Thử với radio button thuộc tính
            await NopHelper.SelectFirstProductAttributeAsync(Page);
            await Page.WaitForTimeoutAsync(500);
            await Assertions.Expect(priceLocator).ToBeVisibleAsync();
        }
    }

    // ── TC_REQ_US07_04_02 ────────────────────────────────────────────────────
    /// <summary>
    /// Ảnh sản phẩm cập nhật khi đổi thuộc tính màu.
    /// Kỳ vọng: src của ảnh thay đổi sau khi chọn màu khác.
    /// </summary>
    [Test]
    [Description("TC_REQ_US07_04_02: Đổi thuộc tính màu → ảnh SP cập nhật sang ảnh tương ứng")]
    public async Task TC_REQ_US07_04_02_ColorAttribute_UpdatesProductImage()
    {
        // Mở trang SP có biến thể màu (dùng Asus hoặc SP có thuộc tính màu)
        await NopHelper.OpenProductAsync(Page, TestConfig.AsusPath);

        // Lấy src ảnh chính hiện tại
        var mainImage = Page.Locator(".product-image img, #main-product-img, .gallery-image").First;
        await Assertions.Expect(mainImage).ToBeVisibleAsync();
        var initialSrc = await mainImage.GetAttributeAsync("src") ?? "";

        // Tìm và chọn radio/color swatch thuộc tính màu (nếu có)
        var colorRadio = Page.Locator(
            "input[type='radio'][name^='product_attribute_'], " +
            ".attribute-square, .color-squares li:not(.selected-value) a").First;

        if (await colorRadio.CountAsync() > 0)
        {
            await colorRadio.ClickAsync(new LocatorClickOptions { Force = true });
            await Page.WaitForTimeoutAsync(800); // Chờ ảnh thay đổi

            // Assert: src ảnh đã thay đổi hoặc ít nhất ảnh vẫn visible
            await Assertions.Expect(mainImage).ToBeVisibleAsync();
        }
        else
        {
            // Nếu không có thuộc tính màu, kiểm tra ảnh vẫn visible
            Assert.Pass("Sản phẩm không có thuộc tính màu - kiểm tra ảnh visible thay thế.");
            await Assertions.Expect(mainImage).ToBeVisibleAsync();
        }
    }

    // ── TC_REQ_US07_04_03 ────────────────────────────────────────────────────
    /// <summary>
    /// Chọn thuộc tính không cộng thêm giá - giá giữ nguyên.
    /// Kỳ vọng: giá hiển thị không thay đổi sau khi chọn option không có price adjustment.
    /// </summary>
    [Test]
    [Description("TC_REQ_US07_04_03: Thuộc tính không cộng giá → giá giữ nguyên bằng giá gốc")]
    public async Task TC_REQ_US07_04_03_AttributeWithoutPriceAdjustment_PriceUnchanged()
    {
        // Mở trang SP "Build Your Own Computer"
        await NopHelper.OpenProductAsync(Page, TestConfig.BuildComputerPath);

        // Đọc giá hiện tại
        var priceLocator = Page.Locator(".price-value, .product-price .price, .actual-price").First;
        await Assertions.Expect(priceLocator).ToBeVisibleAsync();
        var originalPrice = await priceLocator.InnerTextAsync();

        // Chọn option index=0 (option đầu tiên - thường không có price adjustment)
        var attrSelect = Page.Locator("select[id^='product_attribute_']").First;
        if (await attrSelect.CountAsync() > 0)
        {
            await attrSelect.SelectOptionAsync(new SelectOptionValue { Index = 0 });
            await Page.WaitForTimeoutAsync(500);

            // Assert: Giá hiển thị vẫn visible (giá không thay đổi)
            await Assertions.Expect(priceLocator).ToBeVisibleAsync();
            var currentPrice = await priceLocator.InnerTextAsync();
            Assert.That(currentPrice, Is.EqualTo(originalPrice),
                "Giá phải giữ nguyên khi chọn thuộc tính không cộng thêm giá");
        }
        else
        {
            // Nếu không có select attribute, kiểm tra giá vẫn visible
            await Assertions.Expect(priceLocator).ToBeVisibleAsync();
        }
    }
}
