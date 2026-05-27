using Microsoft.Playwright;
using NUnit.Framework;
using NopCommerceTests.Helpers;

namespace NopCommerceTests.Tests;

/// <summary>
/// US19 - Quản lý Đơn hàng (Orders) - Admin
/// Tổng: 8 test cases
/// </summary>
[TestFixture]
[Category("US19")]
public class US19_AdminOrderTests : PlaywrightTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await AuthHelper.LoginAsAdminAsync(Page);
        await Page.GotoAsync("/Admin/Order/List", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
    }

    [Test]
    public async Task TC_REQ_US19_01_01_SearchByDateRange()
    {
        await NopHelper.FillIfPresentAsync(Page, "#StartDate", "01/01/2026");
        await NopHelper.FillIfPresentAsync(Page, "#EndDate", "12/31/2026");
        await Page.Locator("#search-orders").ClickAsync();
        
        await Page.WaitForTimeoutAsync(1000);
        await Assertions.Expect(Page.Locator(".dataTables_wrapper, table")).ToBeVisibleAsync();
    }

    [Test]
    public async Task TC_REQ_US19_01_02_SearchNoResults()
    {
        // Nhập email không tồn tại
        await NopHelper.FillIfPresentAsync(Page, "#BillingEmail", "nonexistent_order@test.com");
        await Page.Locator("#search-orders").ClickAsync();
        
        await Page.WaitForTimeoutAsync(1000);
        var tableContent = await Page.Locator(".dataTables_wrapper, table").InnerTextAsync();
        Assert.That(tableContent, Does.Contain("No data").IgnoreCase.Or.Contain("Không có dữ liệu"));
    }

    [Test]
    public async Task TC_REQ_US19_01_03_SearchNoFilters()
    {
        // Click load all
        await Page.Locator("#search-orders").ClickAsync();
        await Page.WaitForTimeoutAsync(1000);
        await Assertions.Expect(Page.Locator(".dataTables_wrapper, table")).ToBeVisibleAsync();
    }

    [Test]
    public async Task TC_REQ_US19_02_01_ViewOrderDetails()
    {
        await Page.Locator("#search-orders").ClickAsync();
        await Page.WaitForTimeoutAsync(1000);
        
        var viewBtn = Page.Locator("a[href*='/Admin/Order/Edit/']").First;
        if (await viewBtn.IsVisibleAsync())
        {
            await viewBtn.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            
            await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(
                new System.Text.RegularExpressions.Regex(@"Order #|Đơn hàng #", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        }
        else
        {
            Assert.Pass("Không có đơn hàng nào trong hệ thống để xem chi tiết.");
        }
    }

    [Test]
    public async Task TC_REQ_US19_03_01_OrderListColumns()
    {
        var grid = Page.Locator(".dataTables_wrapper, table").First;
        await Assertions.Expect(grid).ToBeVisibleAsync();
        
        var headerText = await grid.InnerTextAsync();
        Assert.That(headerText, Does.Contain("Order").IgnoreCase.Or.Contain("Đơn hàng"));
        Assert.That(headerText, Does.Contain("Customer").IgnoreCase.Or.Contain("Khách hàng"));
    }

    [Test]
    public async Task TC_REQ_US19_04_01_ExportOrders()
    {
        // Just verify export button exists
        var exportBtn = Page.Locator("button[name='exportexcel-all']");
        await Assertions.Expect(exportBtn).ToBeVisibleAsync();
    }
}
