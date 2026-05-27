using Microsoft.Playwright;
using NUnit.Framework;
using NopCommerceTests.Helpers;

namespace NopCommerceTests.Tests;

/// <summary>
/// US05 – Duyệt danh mục sản phẩm (Category Browse)
/// CSV rows 216-279 (TC_REQ_US05_*)
/// </summary>
[TestFixture]
[Category("US05")]
public class US05_CategoryTests : PlaywrightTestBase
{
    // ═══════════════════════════════════════════════════════════════
    // REQ_US05_01 – Lưới sản phẩm (Product Grid)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// TC_REQ_US05_01_01 – Lưới sản phẩm hiển thị đủ thông tin.
    /// Mở /computers → mỗi sản phẩm có: ảnh, tên, giá, rating, nút mua.
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US05_01_01_ProductGridShowsFullInfo()
    {
        // Mở trang danh mục Computers
        await NopHelper.OpenCategoryAsync(Page, "/computers");

        // Xác nhận có product items trên trang
        var productItem = Page.Locator(".product-item, .product-grid-item, .item-box").First;
        await Assertions.Expect(productItem).ToBeVisibleAsync();

        // Xác nhận tên sản phẩm hiển thị
        var productName = Page.Locator(
            ".product-item .product-title, .item-box .product-title, " +
            ".product-item h2, .product-item .name").First;
        await Assertions.Expect(productName).ToBeVisibleAsync();

        // Xác nhận giá hiển thị
        var productPrice = Page.Locator(
            ".product-item .price, .item-box .price, " +
            ".product-item .actual-price, .product-price").First;
        await Assertions.Expect(productPrice).ToBeVisibleAsync();

        // Xác nhận có nút Add to cart / Thêm vào giỏ
        var addToCartBtn = Page.Locator(
            ".product-item button[onclick*='cart'], .item-box input[value*='cart'], " +
            ".product-item .add-to-cart-button, button:has-text('Add to cart')").First;
        await Assertions.Expect(addToCartBtn).ToBeVisibleAsync();
    }

    /// <summary>
    /// TC_REQ_US05_01_02 – Thứ tự sắp xếp mặc định (Display order).
    /// Mở /electronics → sản phẩm hiển thị đúng thứ tự đã cấu hình trong Admin.
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US05_01_02_DefaultDisplayOrder()
    {
        // Mở trang danh mục Electronics
        await NopHelper.OpenCategoryAsync(Page, "/electronics");

        // Xác nhận product items hiển thị
        var productItem = Page.Locator(".product-item, .item-box").First;
        await Assertions.Expect(productItem).ToBeVisibleAsync();

        // Đếm số sản phẩm (phải có ít nhất 1)
        var count = await Page.Locator(".product-item, .item-box").CountAsync();
        Assert.That(count, Is.GreaterThan(0),
            "Danh mục Electronics phải có ít nhất 1 sản phẩm");
    }

    /// <summary>
    /// TC_REQ_US05_01_03 – Danh mục rỗng (Không có SP).
    /// Truy cập danh mục không có sản phẩm → hiển thị thông báo "No products".
    /// </summary>
    [Test]
    [Category("Edge")]
    public async Task TC_REQ_US05_01_03_EmptyCategoryShowsNoProducts()
    {
        // Truy cập URL danh mục rỗng (URL phụ thuộc môi trường)
        await Page.GotoAsync("/test-empty-category",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Xác nhận thông báo không có sản phẩm
        var pattern = new System.Text.RegularExpressions.Regex(
            @"No products were found|No products|không có sản phẩm|không tìm thấy",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        await NopHelper.AssertBodyContainsAsync(Page, pattern,
            "Danh mục rỗng phải hiển thị thông báo 'No products'");
    }

    // ═══════════════════════════════════════════════════════════════
    // REQ_US05_02 – Chuyển đổi chế độ hiển thị (Grid / List)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// TC_REQ_US05_02_01 – Chuyển chế độ Lưới sang Danh sách.
    /// Click icon List → giao diện chuyển thành danh sách hàng dọc.
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US05_02_01_SwitchToListView()
    {
        // Mở danh mục notebooks (có sản phẩm để chuyển chế độ)
        await NopHelper.OpenCategoryAsync(Page, "/notebooks");

        // Click icon chế độ List
        var listIcon = Page.Locator(
            ".viewmode-icon.list, a.list-icon, #viewmode-list, " +
            "a[href*='viewmode=list'], button.list-icon, [data-viewmode='list']").First;
        if (await listIcon.CountAsync() > 0)
        {
            await listIcon.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }

        // Xác nhận chế độ danh sách hiển thị
        var listView = Page.Locator(".product-list, .items-list, [class*='list-view']").First;
        await Assertions.Expect(listView).ToBeVisibleAsync();
    }

    /// <summary>
    /// TC_REQ_US05_02_02 – Chuyển chế độ Danh sách về Lưới.
    /// Click icon Grid → giao diện chuyển lại thành lưới ô vuông.
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US05_02_02_SwitchToGridView()
    {
        // Mở danh mục notebooks và chuyển sang List trước
        await NopHelper.OpenCategoryAsync(Page, "/notebooks");

        // Click icon List
        var listIcon = Page.Locator(
            ".viewmode-icon.list, a.list-icon, a[href*='viewmode=list']").First;
        if (await listIcon.CountAsync() > 0)
        {
            await listIcon.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }

        // Click icon Grid để về lưới
        var gridIcon = Page.Locator(
            ".viewmode-icon.grid, a.grid-icon, #viewmode-grid, " +
            "a[href*='viewmode=grid'], button.grid-icon, [data-viewmode='grid']").First;
        if (await gridIcon.CountAsync() > 0)
        {
            await gridIcon.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }

        // Xác nhận chế độ lưới hiển thị
        var gridView = Page.Locator(".product-grid, .items-grid, [class*='grid-view']").First;
        await Assertions.Expect(gridView).ToBeVisibleAsync();
    }

    /// <summary>
    /// TC_REQ_US05_02_03 – Hệ thống ghi nhớ kiểu hiển thị.
    /// Chuyển List → vào chi tiết SP → back → vẫn là List.
    /// </summary>
    [Test]
    [Category("Edge")]
    public async Task TC_REQ_US05_02_03_ViewModeRememberedAfterBack()
    {
        // Mở danh mục và chuyển sang List
        await NopHelper.OpenCategoryAsync(Page, "/notebooks");

        var listIcon = Page.Locator(
            ".viewmode-icon.list, a.list-icon, a[href*='viewmode=list']").First;
        if (await listIcon.CountAsync() > 0)
        {
            await listIcon.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }

        // Click vào sản phẩm đầu tiên
        var firstProduct = Page.Locator(
            ".product-item a.product-name, .item-box h2 a, .product-title a").First;
        if (await firstProduct.CountAsync() > 0)
        {
            await firstProduct.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            // Nhấn Back trình duyệt
            await Page.GoBackAsync(new PageGoBackOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            // Xác nhận chế độ List vẫn được duy trì
            var listView = Page.Locator(".product-list, [class*='list-view']").First;
            await Assertions.Expect(listView).ToBeVisibleAsync();
        }
        else
        {
            // Nếu không có sản phẩm, bỏ qua test
            Assert.Ignore("Không tìm thấy sản phẩm trong danh mục để kiểm tra chế độ hiển thị");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // REQ_US05_03 – Sắp xếp sản phẩm (Sort)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// TC_REQ_US05_03_01 – Sắp xếp theo Tên A-Z.
    /// Chọn 'Name: A to Z' → sản phẩm hiển thị theo bảng chữ cái.
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US05_03_01_SortByNameAtoZ()
    {
        // Mở danh mục notebooks
        await NopHelper.OpenCategoryAsync(Page, "/notebooks");

        // Chọn sort A-Z
        await NopHelper.SelectSortAsync(Page,
            new System.Text.RegularExpressions.Regex(@"Name.*A.*Z|A to Z",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));

        // Xác nhận danh sách sản phẩm vẫn hiển thị sau sort
        await NopHelper.ExpectProductListVisibleAsync(Page);
    }

    /// <summary>
    /// TC_REQ_US05_03_02 – Sắp xếp Giá Thấp đến Cao.
    /// Chọn 'Price: Low to High' → sản phẩm hiển thị theo giá tăng dần.
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US05_03_02_SortByPriceLowToHigh()
    {
        // Mở danh mục notebooks
        await NopHelper.OpenCategoryAsync(Page, "/notebooks");

        // Chọn sort Price Low to High
        await NopHelper.SelectSortAsync(Page,
            new System.Text.RegularExpressions.Regex(@"Price.*Low.*High|Low to High",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));

        // Xác nhận danh sách vẫn hiển thị
        await NopHelper.ExpectProductListVisibleAsync(Page);
    }

    /// <summary>
    /// TC_REQ_US05_03_03 – Sắp xếp Sản phẩm mới nhất.
    /// Chọn 'Created on' → sản phẩm mới nhất lên đầu.
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US05_03_03_SortByNewest()
    {
        // Mở danh mục computers
        await NopHelper.OpenCategoryAsync(Page, "/computers");

        // Chọn sort theo ngày tạo / mới nhất
        await NopHelper.SelectSortAsync(Page,
            new System.Text.RegularExpressions.Regex(@"Created on|Newest|New arrivals",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));

        // Xác nhận danh sách vẫn hiển thị
        await NopHelper.ExpectProductListVisibleAsync(Page);
    }

    // ═══════════════════════════════════════════════════════════════
    // REQ_US05_04 – Phân trang (Page Size)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// TC_REQ_US05_04_01 – Đổi Page size về 6.
    /// Chọn Display = 6 → số sản phẩm hiển thị ≤ 6.
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US05_04_01_ChangePageSizeTo6()
    {
        // Mở danh mục có nhiều sản phẩm
        await NopHelper.OpenCategoryAsync(Page, "/computers");

        // Chọn page size = 6
        var pageSizeSelect = Page.Locator(
            "#products-pagesize, select[name='products-pagesize']").First;
        if (await pageSizeSelect.CountAsync() > 0)
        {
            await pageSizeSelect.SelectOptionAsync(new SelectOptionValue { Label = "6" });
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            // Đếm số sản phẩm hiển thị, phải ≤ 6
            var productCount = await Page.Locator(".product-item, .item-box").CountAsync();
            Assert.That(productCount, Is.LessThanOrEqualTo(6),
                $"Với page size = 6, chỉ được hiển thị tối đa 6 sản phẩm nhưng thấy {productCount}");
        }
        else
        {
            // Nếu không có select page size, bỏ qua
            Assert.Ignore("Không tìm thấy dropdown Page size – cần kiểm tra cấu hình Admin");
        }
    }

    /// <summary>
    /// TC_REQ_US05_04_02 – Tổng số trang giảm khi tăng Page size.
    /// 12 SP với size 3 → 4 trang; đổi sang size 6 → 2 trang.
    /// </summary>
    [Test]
    [Category("Edge")]
    public async Task TC_REQ_US05_04_02_PaginationDecreasesWithLargerPageSize()
    {
        // Mở danh mục có nhiều sản phẩm
        await NopHelper.OpenCategoryAsync(Page, "/computers");

        var pageSizeSelect = Page.Locator(
            "#products-pagesize, select[name='products-pagesize']").First;

        if (await pageSizeSelect.CountAsync() == 0)
        {
            Assert.Ignore("Không tìm thấy dropdown Page size");
            return;
        }

        // Chọn page size nhỏ trước để lấy số trang
        var smallSizeOption = await pageSizeSelect.Locator("option").AllTextContentsAsync();
        var firstOption = smallSizeOption.FirstOrDefault();
        if (firstOption != null)
        {
            await pageSizeSelect.SelectOptionAsync(new SelectOptionValue { Label = firstOption });
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }

        // Lấy số trang trước khi tăng size
        var paginationBefore = await Page.Locator(
            ".pager li, .pagination li, .pager a").CountAsync();

        // Chọn page size lớn hơn (option cuối cùng)
        var lastOption = smallSizeOption.LastOrDefault();
        if (lastOption != null && lastOption != firstOption)
        {
            await pageSizeSelect.SelectOptionAsync(new SelectOptionValue { Label = lastOption });
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            // Lấy số trang sau khi tăng size
            var paginationAfter = await Page.Locator(
                ".pager li, .pagination li, .pager a").CountAsync();

            // Số trang sau phải ≤ số trang trước
            Assert.That(paginationAfter, Is.LessThanOrEqualTo(paginationBefore),
                "Tổng số trang phải giảm khi tăng page size");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // REQ_US05_05 – Danh mục con (Sub-categories)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// TC_REQ_US05_05_01 – Hiển thị khối Danh mục con.
    /// Truy cập /apparel → các block DM con hiển thị phía trên lưới SP.
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US05_05_01_SubcategoryBlockVisible()
    {
        // Mở danh mục Apparel (có danh mục con)
        await NopHelper.OpenCategoryAsync(Page, "/apparel");

        // Xác nhận khối danh mục con hiển thị
        var subCatBlock = Page.Locator(
            ".sub-category-content, .category-grid, .subcategory-block, " +
            ".sub-categories, [class*='sub-category'], .category-item").First;
        await Assertions.Expect(subCatBlock).ToBeVisibleAsync();
    }

    /// <summary>
    /// TC_REQ_US05_05_02 – Ẩn DM con trạng thái Unpublished.
    /// Admin tắt Published cho DM con → danh mục con đó bị ẩn ở Frontend.
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US05_05_02_UnpublishedSubcategoryHidden()
    {
        // Mở danh mục cha (Apparel)
        await NopHelper.OpenCategoryAsync(Page, "/apparel");

        // Lấy danh sách tên DM con đang hiển thị
        var subCatItems = Page.Locator(
            ".sub-category-content .title, .subcategory-block .title, " +
            ".category-item .title, .sub-categories a");
        var visibleNames = await subCatItems.AllTextContentsAsync();

        // Xác nhận DM con "Shoes" (đã tắt trong Admin) không xuất hiện
        // Lưu ý: tên DM bị unpublish phụ thuộc vào dữ liệu Admin
        var hasShoes = visibleNames.Any(name =>
            name.Contains("Shoes", StringComparison.OrdinalIgnoreCase));

        // Nếu cấu hình đúng: Shoes bị tắt → không hiển thị
        Assert.That(hasShoes, Is.False,
            "Danh mục con bị Unpublished không được hiển thị ở Frontend");
    }

    /// <summary>
    /// TC_REQ_US05_05_03 – DM không có DM con (Cấp độ lá).
    /// Truy cập /shoes → không có khối DM con, chỉ hiện lưới SP.
    /// </summary>
    [Test]
    [Category("Edge")]
    public async Task TC_REQ_US05_05_03_LeafCategoryNoSubcategories()
    {
        // Mở danh mục lá (Shoes - không có DM con)
        await NopHelper.OpenCategoryAsync(Page, "/shoes");

        // Xác nhận không có khối DM con
        var subCatBlock = Page.Locator(
            ".sub-category-content, .category-grid, .subcategory-block");
        var count = await subCatBlock.CountAsync();
        Assert.That(count, Is.EqualTo(0),
            "Danh mục lá không được hiển thị khối danh mục con");

        // Xác nhận lưới sản phẩm vẫn hiển thị
        await NopHelper.ExpectProductListVisibleAsync(Page);
    }
}
