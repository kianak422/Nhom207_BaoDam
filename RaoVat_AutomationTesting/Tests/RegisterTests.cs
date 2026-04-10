using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using RaoVat_AutomationTesting.Pages;
using RaoVat_AutomationTesting.Utilities;
using SeleniumExtras.WaitHelpers;
using System;
using System.Linq;

namespace RaoVat_AutomationTesting.Tests
{
    [TestFixture]
    [Ignore("Đã test xong Register")]
    public class RegisterTests
    {
        private IWebDriver? driver;

        [SetUp]
        public void Setup()
        {
            driver = DriverFactory.CreateDriver();
        }

        [Test, TestCaseSource(typeof(JsonReader), nameof(JsonReader.GetRegisterTestData))]
        public void ExecuteRegisterTest(RegisterData data)
        {
            if (driver == null) return;

            RegisterPage registerPage = new RegisterPage(driver);
            driver.Navigate().GoToUrl("http://localhost:5173/register");

            if (data.Expected == "success" && data.Email != null)
            {
                // Gắn thêm thời gian hiện tại vào email để đảm bảo không bao giờ trùng
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                data.Email = data.Email.Replace("@", $"+{timestamp}@");
            }
            // Gọi hàm đăng ký
            registerPage.Register(data);

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10)); // Chỉ cần đợi 10s là dư dả

            if (data.Expected == "success")
            {
                // 1. Đợi Alert thành công xuất hiện
                IAlert alert = wait.Until(ExpectedConditions.AlertIsPresent());
                string successAlert = alert.Text;
                Console.WriteLine($"ID: {data.Id} - Alert: {successAlert}");

                // 2. BẮT BUỘC PHẢI ACCEPT ALERT THÌ WEB MỚI CHUYỂN TRANG ĐƯỢC
                alert.Accept();

                // 3. Đợi nó chuyển về trang login
                wait.Until(ExpectedConditions.UrlToBe("http://localhost:5173/"));
                Assert.That(driver.Url, Is.EqualTo("http://localhost:5173/"));
            }
            else
            {
                // --- LOGIC NHẬN DIỆN LỖI THÔNG MINH ---
                string actualMsg = "";
                string[] browserErrorIds = { "R03", "R04", "R05", "R07", "R09", "R12", "R14", "R15" };

                if (browserErrorIds.Contains(data.Id))
                {
                    string field = "email";
                    if (data.Id == "R05" || data.Id == "R07") field = "password";
                    else if (data.Id == "R12") field = "confirmpass";
                    else if (data.Id == "R15") field = "name";

                    // Note: Browser validation hiển thị ngay, không cần wait lâu
                    actualMsg = registerPage.GetBrowserValidationMessage(field);
                }
                else
                {
                    // Các lỗi logic React tự check -> PHẢI ĐỢI ALERT HIỆN RA
                    try
                    {
                        IAlert alert = wait.Until(ExpectedConditions.AlertIsPresent());
                        actualMsg = alert.Text;
                        alert.Accept(); // Đọc xong nhớ đóng Alert lại cho gọn
                    }
                    catch (WebDriverTimeoutException)
                    {
                        actualMsg = "Không tìm thấy Alert thông báo lỗi sau 10s!";
                    }
                }

                Console.WriteLine($"ID: {data.Id} - Lỗi thực tế: {actualMsg}");

                string expectedMsg = (data.Msg ?? "").ToLower();
                Assert.That(actualMsg.ToLower(), Does.Contain(expectedMsg),
                    $"Case {data.Id} lỗi hiện ra không đúng mong đợi!");
            }
        }

            [TearDown]
        public void Teardown()
        {
            if (driver != null)
            {
                driver.Quit();
                driver.Dispose();
            }
        }
    }
}