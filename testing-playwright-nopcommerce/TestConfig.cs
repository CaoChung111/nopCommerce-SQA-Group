namespace NopCommerceTests;

/// <summary>
/// Cấu hình và dữ liệu test tập trung - đọc từ appsettings.env
/// </summary>
public static class TestConfig
{
    static TestConfig()
    {
        // Load file .env nếu tồn tại
        var envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.env");
        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
                }
            }
        }
    }

    // ── URL ────────────────────────────────────────────────
    public static string BaseUrl          => Env("BASE_URL", "http://localhost:59580");

    // ── Tài khoản ──────────────────────────────────────────
    public static string AdminEmail       => Env("ADMIN_EMAIL", "admin@yourstore.com");
    public static string AdminPassword    => Env("ADMIN_PASSWORD", "admin123");
    public static string CustomerEmail    => Env("CUSTOMER_EMAIL", "buihoang3425@gmail.com");
    public static string CustomerPassword => Env("CUSTOMER_PASSWORD", "123456");
    public static string InactiveEmail    => Env("INACTIVE_EMAIL", "inactive@gmail.com");
    public static string InactivePassword => Env("INACTIVE_PASSWORD", "123456");
    public static string CustomerBEmail   => Env("CUSTOMER_B_EMAIL", "customerB@test.com");
    public static string CustomerBPassword => Env("CUSTOMER_B_PASSWORD", "123456");

    // ── Browser ────────────────────────────────────────────
    public static bool Headless           => Env("HEADLESS", "true") == "true";
    public static bool AllowMutation      => Env("ALLOW_MUTATION", "true") == "true";

    // ── Sản phẩm ──────────────────────────────────────────
    public static string MacbookPath      => Env("MACBOOK_PRODUCT_PATH", "/apple-macbook-pro");
    public static string MacbookName      => Env("MACBOOK_PRODUCT_NAME", "Apple MacBook Pro");
    public static string AsusPath         => Env("ASUS_PRODUCT_PATH", "/asus-n551jk-xo076h-laptop");
    public static string BuildComputerPath => Env("BUILD_COMPUTER_PATH", "/build-your-own-computer");
    public static string OutOfStockPath   => Env("OUT_OF_STOCK_PATH", "/htc-one-mini-blue");
    public static string DiscountedPath   => Env("DISCOUNTED_PRODUCT_PATH", "/htc-one-m8-android-l-5-0");

    // ── Danh mục ──────────────────────────────────────────
    public static string NotebooksPath    => Env("NOTEBOOKS_PATH", "/notebooks");
    public static string ComputersPath    => Env("COMPUTERS_PATH", "/computers");
    public static string ElectronicsPath  => Env("ELECTRONICS_PATH", "/electronics");
    public static string ApparelPath      => Env("APPAREL_PATH", "/apparel-shoes");

    // ── Tìm kiếm ──────────────────────────────────────────
    public static string SearchKeyword    => Env("SEARCH_KEYWORD", "laptop");
    public static string SkuKeyword       => Env("SKU_KEYWORD", "AP_MBP_13");

    // ── Run ID (unique cho mỗi lần chạy) ─────────────────
    public static string RunId            => DateTime.Now.ToString("yyyyMMddHHmm");

    // ── Helper ────────────────────────────────────────────
    private static string Env(string key, string fallback = "") =>
        System.Environment.GetEnvironmentVariable(key) ?? fallback;

    public static string UniqueEmail(string prefix) =>
        $"{prefix}+{RunId}@test-nop.com";
}
