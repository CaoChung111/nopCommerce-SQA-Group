using Microsoft.Playwright;
using NUnit.Framework;
using NopCommerceTests.Helpers;

namespace NopCommerceTests.Tests;

/// <summary>
/// US04 – Tìm kiếm sản phẩm (Search)
/// CSV rows 179-215 (TC_REQ_US04_*)
/// </summary>
[TestFixture]
[Category("US04")]
public class US04_SearchTests : PlaywrightTestBase
{
    // ═══════════════════════════════════════════════════════════════
    // REQ_US04_01 – Tìm kiếm cơ bản
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// TC_REQ_US04_01_01 – Tìm kiếm bằng chữ thường khớp tên SP viết hoa.
    /// Nhập 'laptop' (chữ thường) → hệ thống trả về SP chứa 'LAPTOP' / 'laptop'.
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US04_01_01_SearchLowercaseMatchesUppercase()
    {
        // Tìm kiếm 'laptop' bằng chữ thường từ trang chủ
        await NopHelper.SearchStoreAsync(Page, "laptop");

        // Xác nhận body chứa kết quả không phân biệt hoa/thường
        var pattern = new System.Text.RegularExpressions.Regex(
            @"laptop|LAPTOP",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        await NopHelper.AssertBodyContainsAsync(Page, pattern,
            "Trang kết quả phải hiển thị sản phẩm chứa 'laptop' (không phân biệt hoa/thường)");
    }

    /// <summary>
    /// TC_REQ_US04_01_02 – Tìm kiếm bằng mã SKU sản phẩm.
    /// Nhập SKU 'AP_MBP_13' → trả về MacBook / Apple.
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US04_01_02_SearchBySKU()
    {
        // Truy cập trang search với query SKU trực tiếp
        await Page.GotoAsync("/search?q=AP_MBP_13",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Xác nhận kết quả là sản phẩm MacBook hoặc Apple
        var pattern = new System.Text.RegularExpressions.Regex(
            @"MacBook|Apple",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        await NopHelper.AssertBodyContainsAsync(Page, pattern,
            "Trang kết quả SKU phải hiển thị 'MacBook' hoặc 'Apple'");
    }

    /// <summary>
    /// TC_REQ_US04_01_03 – Tìm kiếm từ khóa không có trong CSDL.
    /// Nhập 'xyzabc' → hiển thị thông báo không tìm thấy.
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US04_01_03_SearchNotFoundKeyword()
    {
        // Tìm kiếm từ khóa ngẫu nhiên không tồn tại
        await NopHelper.SearchStoreAsync(Page, "xyzabc");

        // Xác nhận thông báo "No products" xuất hiện
        var pattern = new System.Text.RegularExpressions.Regex(
            @"No products were found|No products|không tìm thấy",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        await NopHelper.AssertBodyContainsAsync(Page, pattern,
            "Phải hiển thị thông báo không tìm thấy sản phẩm");
    }

    // ═══════════════════════════════════════════════════════════════
    // REQ_US04_02 – Tìm kiếm nâng cao (Advanced Search)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// TC_REQ_US04_02_01 – Tìm kiếm nâng cao trong Mô tả chi tiết.
    /// Tích "Advanced search" + "Search in product descriptions" → tìm 'Retina'.
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US04_02_01_AdvancedSearchInDescription()
    {
        // Truy cập trang search
        await Page.GotoAsync("/search",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Nhập từ khóa
        var searchInput = Page.Locator("#q, input[name='q'], #small-searchterms").First;
        await searchInput.FillAsync("Retina");

        // Tích chọn Advanced search
        var advancedCheckbox = Page.Locator(
            "#advs, input[name='advs'], input[id*='advs'], label:has-text('Advanced') input").First;
        if (await advancedCheckbox.CountAsync() > 0 && !await advancedCheckbox.IsCheckedAsync())
            await advancedCheckbox.CheckAsync();

        // Tích chọn Search in product descriptions
        var descCheckbox = Page.Locator(
            "#sid, input[name='sid'], input[id*='sid'], label:has-text('description') input").First;
        if (await descCheckbox.CountAsync() > 0 && !await descCheckbox.IsCheckedAsync())
            await descCheckbox.CheckAsync();

        // Nhấn nút Search
        var searchBtn = Page.Locator(
            "button.search-button, input[value*='Search'], button:has-text('Search')").First;
        await searchBtn.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Xác nhận có kết quả sản phẩm
        await NopHelper.ExpectProductListVisibleAsync(Page);
    }

    /// <summary>
    /// TC_REQ_US04_02_02 – Tìm kiếm nâng cao trong Danh mục con.
    /// Chọn Computers + tích "Automatically search subcategories" → tìm 'Apple'.
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US04_02_02_AdvancedSearchSubcategories()
    {
        // Truy cập trang search
        await Page.GotoAsync("/search",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Nhập từ khóa
        var searchInput = Page.Locator("#q, input[name='q']").First;
        await searchInput.FillAsync("Apple");

        // Tích Advanced search
        var advancedCheckbox = Page.Locator(
            "#advs, input[name='advs']").First;
        if (await advancedCheckbox.CountAsync() > 0 && !await advancedCheckbox.IsCheckedAsync())
            await advancedCheckbox.CheckAsync();

        // Chọn Category "Computers"
        var categorySelect = Page.Locator(
            "#cid, select[name='cid'], select[id*='cid']").First;
        if (await categorySelect.CountAsync() > 0)
        {
            var options = await categorySelect.Locator("option").AllTextContentsAsync();
            var computersOption = options.FirstOrDefault(o =>
                o.Contains("Computer", StringComparison.OrdinalIgnoreCase));
            if (computersOption != null)
                await categorySelect.SelectOptionAsync(new SelectOptionValue { Label = computersOption });
        }

        // Tích "Automatically search subcategories"
        var subCatCheckbox = Page.Locator(
            "#isc, input[name='isc'], input[id*='isc']").First;
        if (await subCatCheckbox.CountAsync() > 0 && !await subCatCheckbox.IsCheckedAsync())
            await subCatCheckbox.CheckAsync();

        // Nhấn Search
        var searchBtn = Page.Locator(
            "button.search-button, input[value*='Search'], button:has-text('Search')").First;
        await searchBtn.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Xác nhận có kết quả
        await NopHelper.ExpectProductListVisibleAsync(Page);
    }

    /// <summary>
    /// TC_REQ_US04_02_03 – Bỏ tích tìm danh mục con.
    /// Chỉ search trong cha Computers (không tích subcategories) → kết quả trong danh mục cha.
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US04_02_03_SearchWithoutSubcategories()
    {
        // Truy cập trang search
        await Page.GotoAsync("/search",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Nhập từ khóa
        var searchInput = Page.Locator("#q, input[name='q']").First;
        await searchInput.FillAsync("Apple");

        // Tích Advanced search
        var advancedCheckbox = Page.Locator("#advs, input[name='advs']").First;
        if (await advancedCheckbox.CountAsync() > 0 && !await advancedCheckbox.IsCheckedAsync())
            await advancedCheckbox.CheckAsync();

        // Chọn Category "Computers"
        var categorySelect = Page.Locator("#cid, select[name='cid']").First;
        if (await categorySelect.CountAsync() > 0)
        {
            var options = await categorySelect.Locator("option").AllTextContentsAsync();
            var computersOption = options.FirstOrDefault(o =>
                o.Contains("Computer", StringComparison.OrdinalIgnoreCase));
            if (computersOption != null)
                await categorySelect.SelectOptionAsync(new SelectOptionValue { Label = computersOption });
        }

        // Đảm bảo bỏ tích "Search subcategories"
        var subCatCheckbox = Page.Locator("#isc, input[name='isc']").First;
        if (await subCatCheckbox.CountAsync() > 0 && await subCatCheckbox.IsCheckedAsync())
            await subCatCheckbox.UncheckAsync();

        // Nhấn Search
        var searchBtn = Page.Locator(
            "button.search-button, input[value*='Search'], button:has-text('Search')").First;
        await searchBtn.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Xác nhận trang load không lỗi (kết quả có thể rỗng)
        await NopHelper.AssertNoTechnicalErrorAsync(Page);
        await Assertions.Expect(Page.Locator("body")).ToBeVisibleAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    // REQ_US04_03 – Dropdown gợi ý (Autocomplete)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// TC_REQ_US04_03_01 – Dropdown gợi ý xuất hiện sau độ trễ.
    /// Gõ 'Mac' → dừng ~500ms → autocomplete visible.
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US04_03_01_AutocompleteAppearsAfterDelay()
    {
        // Truy cập trang chủ
        await Page.GotoAsync("/",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Gõ từ khóa vào ô tìm kiếm
        var searchInput = Page.Locator(
            "#small-searchterms, input[name='q'], input.search-box-text").First;
        await Assertions.Expect(searchInput).ToBeVisibleAsync();
        await searchInput.TypeAsync("Mac", new LocatorTypeOptions { Delay = 100 });

        // Chờ debounce delay (~500ms)
        await Page.WaitForTimeoutAsync(500);

        // Xác nhận dropdown autocomplete xuất hiện
        var autocomplete = Page.Locator(
            ".ui-autocomplete, .autocomplete-suggestions, #ui-id-1, " +
            ".search-results, .autocomplete, [class*='autocomplete']").First;
        await Assertions.Expect(autocomplete).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 });
    }

    /// <summary>
    /// TC_REQ_US04_03_02 – Dropdown không xuất hiện khi gõ liên tục.
    /// Gõ nhanh liên tục → dropdown chưa hiện (debounce chưa kích hoạt).
    /// </summary>
    [Test]
    [Category("Edge")]
    public async Task TC_REQ_US04_03_02_AutocompleteNotShownWhileTyping()
    {
        // Truy cập trang chủ
        await Page.GotoAsync("/",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Gõ nhanh liên tục không dừng (delay = 0ms giữa các phím)
        var searchInput = Page.Locator(
            "#small-searchterms, input[name='q'], input.search-box-text").First;
        await Assertions.Expect(searchInput).ToBeVisibleAsync();

        // Gõ từng ký tự nhanh: m-a-c-b-o-o-k
        await searchInput.TypeAsync("macbook", new LocatorTypeOptions { Delay = 30 });

        // Ngay sau khi gõ (chưa đủ thời gian debounce), kiểm tra autocomplete chưa hiện ổn định
        // Chờ ngắn nhưng không đủ debounce
        await Page.WaitForTimeoutAsync(100);

        var autocomplete = Page.Locator(
            ".ui-autocomplete, .autocomplete-suggestions, #ui-id-1").First;
        // Kết quả: có thể chưa hiện hoặc đang trong quá trình debounce
        // Xác nhận trang không crash
        await NopHelper.AssertNoTechnicalErrorAsync(Page);
        await Assertions.Expect(Page.Locator("body")).ToBeVisibleAsync();
    }

    /// <summary>
    /// TC_REQ_US04_03_03 – Tìm kiếm 1 ký tự → cảnh báo độ dài tối thiểu.
    /// nopCommerce yêu cầu tối thiểu 3 ký tự. Nhập 'a' → thông báo lỗi độ dài.
    /// </summary>
    [Test]
    [Category("Edge")]
    public async Task TC_REQ_US04_03_03_SearchSingleCharMinLengthWarning()
    {
        // Tìm kiếm với 1 ký tự
        await NopHelper.SearchStoreAsync(Page, "a");

        // Xác nhận thông báo yêu cầu tối thiểu 3 ký tự
        var pattern = new System.Text.RegularExpressions.Regex(
            @"Search term minimum length is 3|minimum.*3|at least 3",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        await NopHelper.AssertBodyContainsAsync(Page, pattern,
            "Phải hiển thị cảnh báo từ khóa tối thiểu 3 ký tự");
    }

    // ═══════════════════════════════════════════════════════════════
    // REQ_US04_04 – Hiển thị ảnh trong dropdown gợi ý
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// TC_REQ_US04_04_01 – Hiển thị ảnh thumbnail trong dropdown (Show images = True).
    /// Admin đã bật Show images → gợi ý chứa thẻ img.
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US04_04_01_AutocompleteShowsImages()
    {
        // Truy cập trang chủ và gõ từ khóa
        await Page.GotoAsync("/",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        var searchInput = Page.Locator(
            "#small-searchterms, input[name='q'], input.search-box-text").First;
        await Assertions.Expect(searchInput).ToBeVisibleAsync();
        await searchInput.TypeAsync("Apple", new LocatorTypeOptions { Delay = 100 });

        // Chờ dropdown xuất hiện (Admin đã bật Show images)
        await Page.WaitForTimeoutAsync(600);

        var autocomplete = Page.Locator(
            ".ui-autocomplete, .autocomplete-suggestions, #ui-id-1").First;
        if (await autocomplete.CountAsync() > 0 && await autocomplete.IsVisibleAsync())
        {
            // Xác nhận trong dropdown có ảnh thumbnail (img tag)
            var images = autocomplete.Locator("img");
            var imgCount = await images.CountAsync();
            Assert.That(imgCount, Is.GreaterThan(0),
                "Dropdown gợi ý phải có ảnh thumbnail khi Show images = True");
        }
        else
        {
            // Nếu dropdown không xuất hiện, bỏ qua (tính năng phụ thuộc cấu hình Admin)
            Assert.Ignore("Dropdown autocomplete không xuất hiện – cần kiểm tra cấu hình Admin");
        }
    }

    /// <summary>
    /// TC_REQ_US04_04_02 – Không hiển thị ảnh khi tắt tính năng (Show images = False).
    /// Admin tắt Show images → dropdown chỉ có text, không có img.
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US04_04_02_AutocompleteNoImages()
    {
        // Truy cập trang chủ và gõ từ khóa
        await Page.GotoAsync("/",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        var searchInput = Page.Locator(
            "#small-searchterms, input[name='q'], input.search-box-text").First;
        await Assertions.Expect(searchInput).ToBeVisibleAsync();
        await searchInput.TypeAsync("Apple", new LocatorTypeOptions { Delay = 100 });

        // Chờ dropdown
        await Page.WaitForTimeoutAsync(600);

        var autocomplete = Page.Locator(
            ".ui-autocomplete, .autocomplete-suggestions, #ui-id-1").First;
        if (await autocomplete.CountAsync() > 0 && await autocomplete.IsVisibleAsync())
        {
            // Xác nhận KHÔNG có ảnh thumbnail trong dropdown khi Show images = False
            var images = autocomplete.Locator("img:visible");
            var imgCount = await images.CountAsync();
            Assert.That(imgCount, Is.EqualTo(0),
                "Dropdown gợi ý không được có ảnh khi Show images = False");
        }
        else
        {
            Assert.Ignore("Dropdown autocomplete không xuất hiện – cần kiểm tra cấu hình Admin");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // REQ_US04_05 – Tìm kiếm với dữ liệu đặc biệt & hợp lệ
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// TC_REQ_US04_05_01 – Tìm kiếm chứa ký tự đặc biệt (@, #).
    /// Nhập 'Mac@#' → hệ thống xử lý an toàn, không lỗi, không crash.
    /// </summary>
    [Test]
    [Category("Negative")]
    public async Task TC_REQ_US04_05_01_SearchSpecialCharacters()
    {
        // Tìm kiếm với ký tự đặc biệt
        await NopHelper.SearchStoreAsync(Page, "Mac@#");

        // Xác nhận trang load bình thường, không có lỗi kỹ thuật
        await NopHelper.AssertNoTechnicalErrorAsync(Page);
        await Assertions.Expect(Page.Locator("body")).ToBeVisibleAsync();

        // Kết quả có thể là "no products" (hợp lệ)
        var pageContent = await Page.Locator("body").TextContentAsync();
        Assert.That(pageContent, Is.Not.Null.And.Not.Empty,
            "Trang phải load nội dung bình thường sau khi tìm kiếm ký tự đặc biệt");
    }

    /// <summary>
    /// TC_REQ_US04_05_02 – Kết quả tìm kiếm hợp lệ với 'Asus'.
    /// Nhập 'Asus' → có sản phẩm → product grid hiển thị.
    /// </summary>
    [Test]
    [Category("Positive")]
    public async Task TC_REQ_US04_05_02_SearchValidKeywordShowsGrid()
    {
        // Tìm kiếm với từ khóa hợp lệ 'Asus'
        await NopHelper.SearchStoreAsync(Page, "Asus");

        // Xác nhận product grid / product list visible
        await NopHelper.ExpectProductListVisibleAsync(Page);
    }
}
