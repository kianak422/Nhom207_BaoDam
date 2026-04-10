using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace RaoVat_AutomationTesting.Utilities
{
    public static class DriverFactory
    {
        public static IWebDriver CreateDriver()
        {
            ChromeOptions options = new ChromeOptions();

            // Ép tiếng Việt để test message cho chuẩn
            options.AddArgument("--lang=vi-VN");

            // Mày có thể thêm các option khác ở đây sau này
            // ví dụ: options.AddArgument("--headless"); (chạy ẩn không hiện trình duyệt)

            IWebDriver driver = new ChromeDriver(options);
            driver.Manage().Window.Maximize();
            return driver;
        }
    }
}