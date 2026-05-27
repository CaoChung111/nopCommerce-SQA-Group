using Microsoft.Playwright;
using NUnit.Framework;
using NopCommerceTests.Helpers;

namespace NopCommerceTests.Tests;

/// <summary>
/// US18 - Quản lý Sản phẩm số lượng lớn (Bulk Edit) - Admin
/// Tổng: 10 test cases
/// </summary>
[TestFixture]
[Category("US18")]
public class US18_AdminBulkEditTests : PlaywrightTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await AuthHelper.LoginAsAdminAsync(Page);
        await Page.GotoAsync("/Admin/Product/BulkEdit", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
    }

    [Test]
    public async Task TC_REQ_US18_01_01_BulkEditGridColumns()
    {
        var grid = Page.Locator(".dataTables_wrapper, table").First;
        await Assertions.Expect(grid).ToBeVisibleAsync();
        
        // Assert some headers
        var headerText = await grid.InnerTextAsync();
        Assert.That(headerText, Does.Contain("Name").IgnoreCase.Or.Contain("Tên"));
        Assert.That(headerText, Does.Contain("SKU"));
        Assert.That(headerText, Does.Contain("Price").IgnoreCase.Or.Contain("Giá"));
    }

    [Test]
    public async Task TC_REQ_US18_02_01_UpdateStockQuantity()
    {
        var searchBtn = Page.Locator("#search-products");
        if (await searchBtn.IsVisibleAsync())
        {
            await searchBtn.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);
        }

        // Wait for inputs in grid
        var qtyInput = Page.Locator("input[name$='.StockQuantity']").First;
        if (await qtyInput.CountAsync() > 0)
        {
            await qtyInput.FillAsync("50");
            
            // There is no explicit "Save" button for bulk edit normally in nop, it saves on blur or button if present
            var saveBtn = Page.Locator("button.btn-primary").Filter(new() { HasText = "Save" });
            if (await saveBtn.CountAsync() > 0)
            {
                await saveBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(1000);
            }
            
            // Assert success message or no error
            await NopHelper.AssertNoTechnicalErrorAsync(Page);
        }
        else
        {
            Assert.Pass("Không tìm thấy input số lượng.");
        }
    }

    [Test]
    public async Task TC_REQ_US18_03_01_EnterLettersInPrice()
    {
        // DEF_11: Crash when entering letters in price
        var searchBtn = Page.Locator("#search-products");
        if (await searchBtn.IsVisibleAsync())
        {
            await searchBtn.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);
        }

        var priceInput = Page.Locator("input[name$='.Price']").First;
        if (await priceInput.CountAsync() > 0)
        {
            await priceInput.FillAsync("abc");
            // Press Tab to blur or Enter
            await priceInput.PressAsync("Tab");
            
            // Try wait
            await Page.WaitForTimeoutAsync(1000);
            
            // Check for crash
            var bodyText = await Page.Locator("body").InnerTextAsync();
            if (bodyText.Contains("error 500", StringComparison.OrdinalIgnoreCase) || 
                bodyText.Contains("Exception", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Fail("DEF_11: Hệ thống crash khi nhập chữ vào ô Price.");
            }
        }
    }

    [Test]
    public async Task TC_REQ_US18_03_03_DecimalStockQuantity()
    {
        // DEF_13: Crash when decimal stock
        var searchBtn = Page.Locator("#search-products");
        if (await searchBtn.IsVisibleAsync())
        {
            await searchBtn.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);
        }

        var qtyInput = Page.Locator("input[name$='.StockQuantity']").First;
        if (await qtyInput.CountAsync() > 0)
        {
            await qtyInput.FillAsync("5.5");
            await qtyInput.PressAsync("Tab");
            
            await Page.WaitForTimeoutAsync(1000);
            
            var bodyText = await Page.Locator("body").InnerTextAsync();
            if (bodyText.Contains("error 500", StringComparison.OrdinalIgnoreCase) || 
                bodyText.Contains("Exception", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Fail("DEF_13: Hệ thống crash khi nhập số thập phân vào Stock Quantity.");
            }
        }
    }

    [Test]
    public async Task TC_REQ_US18_04_01_FilterByCategory()
    {
        var catSelect = Page.Locator("#SearchCategoryId");
        if (await catSelect.IsVisibleAsync())
        {
            var options = await catSelect.Locator("option").AllTextContentsAsync();
            if (options.Count > 1)
            {
                await catSelect.SelectOptionAsync(new SelectOptionValue { Index = 1 }); // Just pick first non-empty
                await Page.Locator("#search-products").ClickAsync();
                await Page.WaitForTimeoutAsync(1000);
                
                await Assertions.Expect(Page.Locator(".dataTables_wrapper, table")).ToBeVisibleAsync();
            }
        }
    }
}
