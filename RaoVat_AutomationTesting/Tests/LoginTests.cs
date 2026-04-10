using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using RaoVat_AutomationTesting.Pages;
using RaoVat_AutomationTesting.Utilities;
using SeleniumExtras.WaitHelpers;
using System;
using System.Linq; // QUAN TRỌNG: Thêm dòng này để dùng được hàm .Contains() cho mảng

namespace RaoVat_AutomationTesting.Tests
{
    [TestFixture]
    [Ignore("Đã test xong Login, tạm thời tắt đi để làm Register")]
    public class LoginTests
    {
        private IWebDriver? driver;

        [SetUp]
        public void Setup()
        {
            driver = DriverFactory.CreateDriver();
        }

        [Test, TestCaseSource(typeof(JsonReader), nameof(JsonReader.GetLoginTestData))]
        public void ExecuteLoginTest(LoginData data)
        {
            if (driver == null) return;

            LoginPage loginPage = new LoginPage(driver);
            driver.Navigate().GoToUrl("http://localhost:5173/login");

            loginPage.Login(data.Email ?? "", data.Pass ?? "");

            if (data.Expected == "success")
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait.Until(ExpectedConditions.UrlToBe("http://localhost:5173/"));
                Assert.That(driver.Url, Is.EqualTo("http://localhost:5173/"));
            }
            else
            {
                // --- BẮT ĐẦU BƯỚC 2: LOGIC NHẬN DIỆN LỖI THÔNG MINH ---
                string actualMsg = "";

                // Danh sách các Case ID mà trình duyệt sẽ tự bắt lỗi (HTML5 Validation)
                string[] browserErrorIds = { "L04", "L05", "L06", "L08", "L10", "L11" };

                if (browserErrorIds.Contains(data.Id))
                {
                    // Nếu là L05 thì check ô password, còn lại check ô email
                    string field = (data.Id == "L05") ? "password" : "email";
                    actualMsg = loginPage.GetBrowserValidationMessage(field);
                }
                else
                {
                    // Các case còn lại (sai pass, sai email...) sẽ hiện Alert của React/JS
                    actualMsg = loginPage.GetJavaScriptAlertText();
                }

                Console.WriteLine($"ID: {data.Id} - Lỗi thực tế: {actualMsg}");

                // So sánh (Dùng ?? "" để tránh lỗi null và ToLower để so sánh chuẩn)
                string expectedMsg = (data.Msg ?? "").ToLower();
                Assert.That(actualMsg.ToLower(), Does.Contain(expectedMsg),
                    $"Case {data.Id} lỗi hiện ra không đúng mong đợi!");
                // --- KẾT THÚC BƯỚC 2 ---
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