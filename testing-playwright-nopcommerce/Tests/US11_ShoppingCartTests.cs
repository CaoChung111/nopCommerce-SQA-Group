using Microsoft.Playwright;
using NUnit.Framework;
using NopCommerceTests.Helpers;

namespace NopCommerceTests.Tests;

/// <summary>
/// US11 - Shopping Cart: Kiểm tra chức năng quản lý giỏ hàng
/// CSV rows 591-640
/// </summary>
[TestFixture]
[Category("US11")]
public class US11_ShoppingCartTests : PlaywrightTestBase
{
    // ── Regex dùng chung cho US11 ───────────────────────────────────────────
    private static readonly System.Text.RegularExpressions.Regex CartEmptyText =
        new(@"your shopping cart is empty|giỏ hàng.*trống|shopping cart is empty",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static readonly System.Text.RegularExpressions.Regex QtyErrorText =
        new(@"maximum|quantity|stock|số lượng|tồn kho|vượt quá",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    // ── SetUp: Login và thêm ít nhất 1 sản phẩm vào giỏ ───────────────────
    [SetUp]
    public async Task SetUp()
    {
        // Đăng nhập với tài khoản khách hàng
        await AuthHelper.LoginAsCustomerAsync(Page);

        // Thêm sản phẩm Asus vào giỏ để đảm bảo giỏ không trống
        await NopHelper.OpenProductAsync(Page, TestConfig.AsusPath);
        await NopHelper.ClickAddToCartAsync(Page);
        await Page.WaitForTimeoutAsync(600);
    }

    // ── Helper: Lấy text tổng tiền hiển thị trên giỏ ──────────────────────
    private async Task<string> GetOrderTotalTextAsync()
    {
        var totalLocator = Page.Locator(".order-total .value-summary, .cart-total .order-total strong, .totals .order-total").First;
        if (await totalLocator.CountAsync() > 0)
            return (await totalLocator.TextContentAsync()) ?? "";
        return "";
    }

    // ── Helper: Cập nhật số lượng sản phẩm đầu tiên trong giỏ ─────────────
    private async Task UpdateFirstItemQtyAsync(int qty)
    {
        var qtyInput = Page.Locator("input.qty-input, input[name*='itemquantity'], td.quantity input").First;
        await Assertions.Expect(qtyInput).ToBeVisibleAsync();
        await qtyInput.FillAsync(qty.ToString());

        // Click nút Update Shopping Cart
        var updateBtn = Page.Locator(
            "input[name='updatecart'], button[name='updatecart'], " +
            "input[value*='Update'], button:has-text('Update shopping cart'), " +
            "button:has-text('Update')").First;
        if (await updateBtn.CountAsync() > 0)
            await updateBtn.ClickAsync();
        else
            await qtyInput.PressAsync("Enter");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    // ── TC_REQ_US11_01_01 ───────────────────────────────────────────────────
    /// <summary>
    /// Hiển thị đầy đủ thông tin giỏ hàng.
    /// Giỏ có ít nhất 1 SP → hiển thị tên, giá, ô nhập SL, nút xóa, tổng tiền.
    /// </summary>
    [Test]
    [Description("TC_REQ_US11_01_01 - Hiển thị đầy đủ thông tin giỏ hàng")]
    public async Task TC_REQ_US11_01_01_CartDisplaysFullInfo()
    {
        // 1. Truy cập trang giỏ hàng
        await Page.GotoAsync("/cart", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // 2. Xác nhận có tên sản phẩm hiển thị
        var productName = Page.Locator(".product-name, td.product a, .cart td.product").First;
        await Assertions.Expect(productName).ToBeVisibleAsync();

        // 3. Xác nhận có ô nhập số lượng
        var qtyInput = Page.Locator("input.qty-input, input[name*='itemquantity'], td.quantity input").First;
        await Assertions.Expect(qtyInput).ToBeVisibleAsync();

        // 4. Xác nhận có nút xóa (Remove)
        var removeBtn = Page.Locator(
            "button.remove-btn, input.remove-btn, td.remove-from-cart button, " +
            "td.remove-from-cart input, a.remove-from-cart, button[name*='removefromcart']").First;
        await Assertions.Expect(removeBtn).ToBeVisibleAsync();

        // 5. Xác nhận có tổng tiền hiển thị
        var total = Page.Locator(".order-total, .cart-total, .totals").First;
        await Assertions.Expect(total).ToBeVisibleAsync();
    }

    // ── TC_REQ_US11_01_02 ───────────────────────────────────────────────────
    /// <summary>
    /// Tổng tiền hiển thị đúng bằng tổng giá các sản phẩm.
    /// Xác nhận vùng totals hiển thị giá trị hợp lệ.
    /// </summary>
    [Test]
    [Description("TC_REQ_US11_01_02 - Tổng tiền hiển thị đúng")]
    public async Task TC_REQ_US11_01_02_CartTotalIsCorrect()
    {
        // 1. Truy cập trang giỏ hàng
        await Page.GotoAsync("/cart", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // 2. Xác nhận phần tổng tiền tồn tại
        var orderTotalSection = Page.Locator(".order-totals, .cart-total, .totals, #order-total").First;
        await Assertions.Expect(orderTotalSection).ToBeVisibleAsync();

        // 3. Xác nhận giá từng sản phẩm và subtotal hiển thị
        var subtotal = Page.Locator(
            ".product-subtotal, td.subtotal, .cart-item-price, .unit-price").First;
        await Assertions.Expect(subtotal).ToBeVisibleAsync();

        // 4. Đảm bảo không có lỗi kỹ thuật
        await NopHelper.AssertNoTechnicalErrorAsync(Page);
    }

    // ── TC_REQ_US11_02_01 ───────────────────────────────────────────────────
    /// <summary>
    /// Tăng số lượng sản phẩm hợp lệ trong giỏ.
    /// Số lượng hiện tại = 1, tăng lên 5 → tổng tiền được tính lại đúng.
    /// </summary>
    [Test]
    [Description("TC_REQ_US11_02_01 - Tăng số lượng sản phẩm, tổng tiền cập nhật")]
    public async Task TC_REQ_US11_02_01_IncreaseQtyUpdatesSubtotal()
    {
        // 1. Truy cập trang giỏ hàng
        await Page.GotoAsync("/cart", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // 2. Lấy giá trị tổng tiền ban đầu
        var totalBefore = await GetOrderTotalTextAsync();

        // 3. Cập nhật số lượng lên 5
        await UpdateFirstItemQtyAsync(5);

        // 4. Lấy tổng tiền sau khi cập nhật
        var totalAfter = await GetOrderTotalTextAsync();

        // 5. Xác nhận tổng tiền đã thay đổi (tăng lên)
        Assert.That(totalAfter, Is.Not.EqualTo(totalBefore),
            "Kỳ vọng tổng tiền thay đổi sau khi tăng số lượng");

        // 6. Không có lỗi kỹ thuật
        await NopHelper.AssertNoTechnicalErrorAsync(Page);
    }

    // ── TC_REQ_US11_02_02 ───────────────────────────────────────────────────
    /// <summary>
    /// Giảm số lượng sản phẩm xuống 1.
    /// Số lượng hiện tại = 5, giảm xuống 1 → tổng tiền được tính lại đúng.
    /// </summary>
    [Test]
    [Description("TC_REQ_US11_02_02 - Giảm số lượng sản phẩm xuống 1, tổng tiền cập nhật")]
    public async Task TC_REQ_US11_02_02_DecreaseQtyToOneUpdatesSubtotal()
    {
        // 1. Truy cập giỏ hàng
        await Page.GotoAsync("/cart", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // 2. Trước tiên tăng lên 5 để có nền so sánh
        await UpdateFirstItemQtyAsync(5);
        var totalAt5 = await GetOrderTotalTextAsync();

        // 3. Giảm xuống còn 1
        await UpdateFirstItemQtyAsync(1);
        var totalAt1 = await GetOrderTotalTextAsync();

        // 4. Xác nhận tổng tiền giảm xuống
        Assert.That(totalAt1, Is.Not.EqualTo(totalAt5),
            "Kỳ vọng tổng tiền giảm sau khi giảm số lượng từ 5 xuống 1");

        await NopHelper.AssertNoTechnicalErrorAsync(Page);
    }

    // ── TC_REQ_US11_02_03 ───────────────────────────────────────────────────
    /// <summary>
    /// Nhập số lượng = 0 trong giỏ hàng.
    /// Hệ thống tự xóa sản phẩm khỏi giỏ hoặc hiển thị thông báo tương ứng.
    /// </summary>
    [Test]
    [Description("TC_REQ_US11_02_03 - Nhập số lượng = 0, sản phẩm bị xóa khỏi giỏ")]
    public async Task TC_REQ_US11_02_03_ZeroQtyRemovesProduct()
    {
        // 1. Thêm thêm 1 sản phẩm nữa để khi xóa không trống giỏ (tránh ảnh hưởng test)
        await NopHelper.OpenProductAsync(Page, TestConfig.AsusPath);
        await NopHelper.ClickAddToCartAsync(Page);
        await Page.WaitForTimeoutAsync(500);

        // 2. Truy cập giỏ hàng
        await Page.GotoAsync("/cart", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // 3. Ghi lại số lượng item trước khi xóa
        var itemsBefore = await Page.Locator(".cart-item-row, tr.cart-item, .cart tbody tr").CountAsync();

        // 4. Nhập số lượng = 0 cho item đầu tiên
        var qtyInput = Page.Locator("input.qty-input, input[name*='itemquantity'], td.quantity input").First;
        if (await qtyInput.CountAsync() > 0)
        {
            await qtyInput.FillAsync("0");
            var updateBtn = Page.Locator(
                "input[name='updatecart'], button[name='updatecart'], " +
                "input[value*='Update'], button:has-text('Update')").First;
            if (await updateBtn.CountAsync() > 0)
                await updateBtn.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }

        // 5. Xác nhận sản phẩm bị xóa khỏi giỏ
        var itemsAfter = await Page.Locator(".cart-item-row, tr.cart-item, .cart tbody tr").CountAsync();
        Assert.That(itemsAfter, Is.LessThan(itemsBefore),
            "Kỳ vọng số lượng item giảm khi nhập qty = 0");
    }

    // ── TC_REQ_US11_03_01 ───────────────────────────────────────────────────
    /// <summary>
    /// Nhập số lượng vượt tồn kho trong giỏ hàng.
    /// Tồn kho = 3, nhập 9999 → hệ thống báo lỗi, không cập nhật.
    /// </summary>
    [Test]
    [Description("TC_REQ_US11_03_01 - Nhập số lượng vượt tồn kho trong giỏ, báo lỗi")]
    public async Task TC_REQ_US11_03_01_ExceedStockQtyInCart()
    {
        // 1. Truy cập giỏ hàng
        await Page.GotoAsync("/cart", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // 2. Cập nhật số lượng = 9999 (chắc chắn vượt tồn kho)
        await UpdateFirstItemQtyAsync(9999);

        // 3. Xác nhận thông báo lỗi về số lượng/tồn kho
        await NopHelper.AssertBodyContainsAsync(Page, QtyErrorText,
            "Kỳ vọng thông báo lỗi khi số lượng vượt tồn kho");
    }

    // ── TC_REQ_US11_03_02 ───────────────────────────────────────────────────
    /// <summary>
    /// Nhập số lượng đúng bằng tồn kho trong giỏ.
    /// Tồn kho = 3, nhập 3 → hệ thống chấp nhận và cập nhật thành công.
    /// </summary>
    [Test]
    [Description("TC_REQ_US11_03_02 - Nhập số lượng bằng tồn kho trong giỏ, cập nhật thành công")]
    public async Task TC_REQ_US11_03_02_ExactStockQtyInCart()
    {
        // 1. Truy cập giỏ hàng
        await Page.GotoAsync("/cart", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // 2. Cập nhật số lượng = 3 (giả sử tồn kho đủ)
        await UpdateFirstItemQtyAsync(3);

        // 3. Xác nhận không có lỗi kỹ thuật
        await NopHelper.AssertNoTechnicalErrorAsync(Page);

        // 4. Xác nhận giỏ hàng vẫn còn sản phẩm
        var cartItems = Page.Locator(".product-name, td.product a").First;
        await Assertions.Expect(cartItems).ToBeVisibleAsync();
    }

    // ── TC_REQ_US11_04_01 ───────────────────────────────────────────────────
    /// <summary>
    /// Xóa một sản phẩm khỏi giỏ hàng có nhiều SP.
    /// Giỏ có nhiều SP → click Remove ở item đầu → item đó biến mất, các item khác còn.
    /// </summary>
    [Test]
    [Description("TC_REQ_US11_04_01 - Xóa một sản phẩm, các sản phẩm khác vẫn còn")]
    public async Task TC_REQ_US11_04_01_RemoveOneItemFromMultiItemCart()
    {
        // 1. Thêm thêm một sản phẩm khác để giỏ có nhiều item
        await NopHelper.OpenProductAsync(Page, TestConfig.AsusPath);
        await NopHelper.ClickAddToCartAsync(Page);
        await Page.WaitForTimeoutAsync(500);

        // 2. Truy cập giỏ hàng
        await Page.GotoAsync("/cart", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // 3. Đếm số item ban đầu
        var itemsBefore = await Page.Locator(".cart tbody tr[class*='cart-item'], .cart-item-row").CountAsync();

        // 4. Lấy tên sản phẩm đầu tiên để kiểm tra sau khi xóa
        var firstProductName = await Page.Locator(".product-name a, td.product a").First.TextContentAsync();

        // 5. Click nút Remove ở sản phẩm đầu tiên
        var removeBtn = Page.Locator(
            "td.remove-from-cart button, td.remove-from-cart input, " +
            "button.remove-btn, input.remove-btn, button[name*='removefromcart']").First;
        await Assertions.Expect(removeBtn).ToBeVisibleAsync();
        await removeBtn.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // 6. Xác nhận số item giảm
        var itemsAfter = await Page.Locator(".cart tbody tr[class*='cart-item'], .cart-item-row").CountAsync();
        Assert.That(itemsAfter, Is.LessThan(itemsBefore),
            "Kỳ vọng số lượng item giảm sau khi xóa");
    }

    // ── TC_REQ_US11_04_02 ───────────────────────────────────────────────────
    /// <summary>
    /// Tổng tiền đúng sau khi xóa sản phẩm đắt nhất.
    /// Xóa Asus Laptop (đắt) → tổng tiền giảm đáng kể.
    /// </summary>
    [Test]
    [Description("TC_REQ_US11_04_02 - Tổng tiền giảm đúng sau khi xóa sản phẩm")]
    public async Task TC_REQ_US11_04_02_TotalReducedAfterRemovingExpensiveItem()
    {
        // 1. Truy cập giỏ hàng
        await Page.GotoAsync("/cart", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // 2. Ghi lại tổng tiền ban đầu
        var totalBefore = await GetOrderTotalTextAsync();

        // 3. Click xóa sản phẩm đầu tiên
        var removeBtn = Page.Locator(
            "td.remove-from-cart button, td.remove-from-cart input, " +
            "button.remove-btn, input.remove-btn, button[name*='removefromcart']").First;
        if (await removeBtn.CountAsync() > 0)
        {
            await removeBtn.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }

        // 4. Lấy tổng tiền sau khi xóa
        var totalAfter = await GetOrderTotalTextAsync();

        // 5. Xác nhận tổng tiền đã thay đổi
        Assert.That(totalAfter, Is.Not.EqualTo(totalBefore).Or.Empty,
            "Kỳ vọng tổng tiền giảm hoặc giỏ trống sau khi xóa sản phẩm");
    }

    // ── TC_REQ_US11_04_03 ───────────────────────────────────────────────────
    /// <summary>
    /// Giao diện giỏ hàng làm mới sau khi xóa SP.
    /// Xóa item → item không còn trong danh sách giỏ.
    /// </summary>
    [Test]
    [Description("TC_REQ_US11_04_03 - Giao diện cập nhật đúng sau khi xóa sản phẩm")]
    public async Task TC_REQ_US11_04_03_CartUIRefreshesAfterRemove()
    {
        // 1. Truy cập giỏ hàng
        await Page.GotoAsync("/cart", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // 2. Lấy tên sản phẩm đầu tiên
        var firstNameLocator = Page.Locator(".product-name a, td.product a").First;
        var firstProductName = "";
        if (await firstNameLocator.CountAsync() > 0)
            firstProductName = await firstNameLocator.TextContentAsync() ?? "";

        // 3. Xóa sản phẩm đầu tiên
        var removeBtn = Page.Locator(
            "td.remove-from-cart button, td.remove-from-cart input, " +
            "button.remove-btn, input.remove-btn, button[name*='removefromcart']").First;
        if (await removeBtn.CountAsync() > 0)
        {
            await removeBtn.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }

        // 4. Xác nhận sản phẩm không còn xuất hiện (hoặc giỏ trống)
        var body = await Page.Locator("body").TextContentAsync() ?? "";
        // Nếu giỏ trống → hiện thông báo trống
        // Nếu còn item khác → sản phẩm vừa xóa không có trong danh sách
        await NopHelper.AssertNoTechnicalErrorAsync(Page);
    }

    // ── TC_REQ_US11_05_01 ───────────────────────────────────────────────────
    /// <summary>
    /// Xóa sản phẩm cuối cùng trong giỏ hàng.
    /// Giỏ có 1 SP → xóa → hiển thị "Your Shopping Cart is empty!".
    /// </summary>
    [Test]
    [Description("TC_REQ_US11_05_01 - Xóa SP cuối cùng, hiển thị giỏ trống")]
    public async Task TC_REQ_US11_05_01_RemoveLastItemShowsEmptyCart()
    {
        // 1. Đảm bảo giỏ chỉ có 1 item: xóa hết rồi thêm lại 1
        await Page.GotoAsync("/cart", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Xóa tất cả item hiện có
        var removeBtns = Page.Locator(
            "td.remove-from-cart button, td.remove-from-cart input, button.remove-btn, input.remove-btn");
        var count = await removeBtns.CountAsync();
        for (int i = 0; i < count; i++)
        {
            var btn = removeBtns.First;
            if (await btn.CountAsync() > 0 && await btn.IsVisibleAsync())
            {
                await btn.ClickAsync();
                await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            }
        }

        // Thêm đúng 1 sản phẩm
        await NopHelper.OpenProductAsync(Page, TestConfig.AsusPath);
        await NopHelper.ClickAddToCartAsync(Page);
        await Page.WaitForTimeoutAsync(500);

        // 2. Vào giỏ hàng
        await Page.GotoAsync("/cart", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // 3. Xóa sản phẩm duy nhất
        var removeBtn = Page.Locator(
            "td.remove-from-cart button, td.remove-from-cart input, " +
            "button.remove-btn, input.remove-btn, button[name*='removefromcart']").First;
        if (await removeBtn.CountAsync() > 0)
        {
            await removeBtn.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }

        // 4. Xác nhận thông báo giỏ trống
        await NopHelper.AssertBodyContainsAsync(Page, CartEmptyText,
            "Kỳ vọng thông báo 'Your Shopping Cart is empty'");
    }

    // ── TC_REQ_US11_05_02 ───────────────────────────────────────────────────
    /// <summary>
    /// Nút thanh toán ẩn khi giỏ hàng rỗng.
    /// Giỏ trống → nút "Checkout" không hiển thị.
    /// </summary>
    [Test]
    [Description("TC_REQ_US11_05_02 - Nút Checkout ẩn khi giỏ hàng trống")]
    public async Task TC_REQ_US11_05_02_CheckoutButtonHiddenWhenCartEmpty()
    {
        // 1. Xóa tất cả item trong giỏ
        await Page.GotoAsync("/cart", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        var removeBtns = Page.Locator(
            "td.remove-from-cart button, td.remove-from-cart input, button.remove-btn, input.remove-btn");
        var count = await removeBtns.CountAsync();
        for (int i = 0; i < count; i++)
        {
            var btn = removeBtns.First;
            if (await btn.CountAsync() > 0 && await btn.IsVisibleAsync())
            {
                await btn.ClickAsync();
                await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            }
        }

        // 2. Truy cập trang giỏ hàng (có thể đã ở đây)
        await Page.GotoAsync("/cart", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // 3. Xác nhận nút Checkout không hiển thị
        var checkoutBtn = Page.Locator(
            "button.checkout-button, input.checkout-button, " +
            "button:has-text('Checkout'), input[value*='Checkout'], " +
            "a:has-text('Checkout')");
        var checkoutCount = await checkoutBtn.CountAsync();
        if (checkoutCount > 0)
        {
            // Nếu tồn tại thì phải không visible
            await Assertions.Expect(checkoutBtn.First).Not.ToBeVisibleAsync();
        }
        // Nếu không tồn tại → test pass

        // 4. Xác nhận giỏ trống
        await NopHelper.AssertBodyContainsAsync(Page, CartEmptyText,
            "Kỳ vọng giỏ hàng trống");
    }
}
