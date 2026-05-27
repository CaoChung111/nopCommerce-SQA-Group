using Microsoft.Playwright;
using NUnit.Framework;
using NopCommerceTests.Helpers;

namespace NopCommerceTests.Tests;

/// <summary>
/// US12 - Thanh toán (Checkout)
/// Tổng: 16 test cases
/// </summary>
[TestFixture]
[Category("US12")]
public class US12_CheckoutTests : PlaywrightTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await AuthHelper.LoginAsCustomerAsync(Page);
        // Ensure cart has at least 1 item
        await NopHelper.OpenProductAsync(Page, TestConfig.MacbookPath);
        var btn = Page.Locator("button[id^='add-to-cart-button']").First;
        if (await btn.IsVisibleAsync())
        {
            await btn.ClickAsync();
            await Page.WaitForTimeoutAsync(1500);
        }
    }

    [Test]
    public async Task TC_REQ_US12_01_01_CheckoutWithoutTermsOfService()
    {
        await Page.GotoAsync("/cart", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Page.Locator("#termsofservice").UncheckAsync();
        await Page.Locator("#checkout").ClickAsync();
        
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex(@"terms of service", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    [Test]
    public async Task TC_REQ_US12_01_02_CheckoutWithTermsOfService()
    {
        await Page.GotoAsync("/cart", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Page.Locator("#termsofservice").CheckAsync();
        await Page.Locator("#checkout").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        
        await Assertions.Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("checkout", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    [Test]
    public async Task TC_REQ_US12_01_03_TermsOfServiceResetOnReload()
    {
        await Page.GotoAsync("/cart", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Page.Locator("#termsofservice").CheckAsync();
        await Page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        
        var isChecked = await Page.Locator("#termsofservice").IsCheckedAsync();
        Assert.That(isChecked, Is.False, "Checkbox Terms of Service phải reset về mặc định sau khi reload trang.");
    }

    [Test]
    public async Task TC_REQ_US12_02_01_CheckoutWizardVisible()
    {
        await TC_REQ_US12_01_02_CheckoutWithTermsOfService();
        await Assertions.Expect(Page.Locator(".checkout-page, .opc")).ToBeVisibleAsync();
    }

    [Test]
    public async Task TC_REQ_US12_02_02_CheckoutRedirectToLoginIfNotLoggedIn()
    {
        await AuthHelper.LogoutAsync(Page);
        // Add to cart without login
        await NopHelper.OpenProductAsync(Page, TestConfig.MacbookPath);
        var btn = Page.Locator("button[id^='add-to-cart-button']").First;
        if (await btn.IsVisibleAsync())
        {
            await btn.ClickAsync();
            await Page.WaitForTimeoutAsync(1500);
        }
        
        await Page.GotoAsync("/cart", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        var terms = Page.Locator("#termsofservice").First;
        if (await terms.CountAsync() > 0)
            await terms.CheckAsync();
            
        await Page.Locator("#checkout").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        
        // redirect to login
        await Assertions.Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("login", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    [Test]
    public async Task TC_REQ_US12_02_03_CheckoutEmptyCart()
    {
        // empty cart first
        await Page.GotoAsync("/cart", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        var removeBtns = await Page.Locator("button.remove-btn").AllAsync();
        foreach (var rBtn in removeBtns)
        {
            await rBtn.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
        }
        
        await Page.GotoAsync("/checkout", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Assertions.Expect(Page).Not.ToHaveURLAsync(new System.Text.RegularExpressions.Regex("checkout", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex(@"cart is empty", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    [Test]
    public async Task TC_REQ_US12_03_01_SelectExistingAddress()
    {
        await TC_REQ_US12_01_02_CheckoutWithTermsOfService();
        
        var addressSelect = Page.Locator("#billing-address-select").First;
        if (await addressSelect.CountAsync() > 0)
        {
            // Select existing
            await addressSelect.SelectOptionAsync(new SelectOptionValue { Index = 0 });
            await Page.Locator(".new-address-next-step-button").ClickAsync();
            
            // Should move to shipping
            await Assertions.Expect(Page.Locator("#opc-shipping")).ToBeVisibleAsync();
        }
    }

    [Test]
    public async Task TC_REQ_US12_04_01_EmptyNameInNewAddress()
    {
        await TC_REQ_US12_01_02_CheckoutWithTermsOfService();
        var addressSelect = Page.Locator("#billing-address-select").First;
        if (await addressSelect.CountAsync() > 0)
        {
            await addressSelect.SelectOptionAsync(new SelectOptionValue { Label = "New Address" });
        }
        
        await Page.Locator("#BillingNewAddress_FirstName").FillAsync("");
        await Page.Locator(".new-address-next-step-button").ClickAsync();
        
        await NopHelper.ExpectValidationAsync(Page);
    }
    
    [Test]
    public async Task TC_REQ_US12_05_01_SelectShippingAndPayment()
    {
        await TC_REQ_US12_03_01_SelectExistingAddress();
        
        // Wait for shipping methods to load
        await Page.WaitForTimeoutAsync(1000);
        var shippingNext = Page.Locator(".shipping-method-next-step-button");
        if (await shippingNext.IsVisibleAsync())
        {
            await shippingNext.ClickAsync();
        }
        
        await Page.WaitForTimeoutAsync(1000);
        var paymentNext = Page.Locator(".payment-method-next-step-button");
        if (await paymentNext.IsVisibleAsync())
        {
            await paymentNext.ClickAsync();
        }
        
        await Page.WaitForTimeoutAsync(1000);
        var paymentInfoNext = Page.Locator(".payment-info-next-step-button");
        if (await paymentInfoNext.IsVisibleAsync())
        {
            await paymentInfoNext.ClickAsync();
        }
        
        // Assert confirm step
        await Assertions.Expect(Page.Locator("#opc-confirm_order")).ToBeVisibleAsync();
    }

    [Test]
    public async Task TC_REQ_US12_06_01_ConfirmOrderSuccess()
    {
        await TC_REQ_US12_05_01_SelectShippingAndPayment();
        
        await Page.Locator(".confirm-order-next-step-button").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex(@"thank you|successfully processed", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }
    
    [Test]
    public async Task TC_REQ_US12_06_02_OrderCreatedAndCartEmpty()
    {
        await TC_REQ_US12_06_01_ConfirmOrderSuccess();
        
        await Page.GotoAsync("/cart", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Assertions.Expect(Page.Locator("body")).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex(@"cart is empty", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }
}
