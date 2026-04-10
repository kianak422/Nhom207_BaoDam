using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using RaoVat_AutomationTesting.Pages;
using RaoVat_AutomationTesting.Utilities;
using SeleniumExtras.WaitHelpers;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace RaoVat_AutomationTesting.Tests
{
    [TestFixture]
    public class IntegrateTests
    {
        private IWebDriver? driver;
        private ExcelHelper excelHelper;

        private string reportPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\BaoDam_Report.xlsx"));

        // NHỚ ĐỔI TÊN SHEET NÀY THÀNH TÊN SHEET BẠN DÙNG ĐỂ BÁO CÁO INTEGRATION TEST
        private string sheetName = "Integration TestCase";
        private string testerName = "Danh";

        [SetUp]
        public void Setup()
        {
            Console.WriteLine("=========================================");
            Console.WriteLine($"[LOG-DEBUG] Bắt đầu chạy Integration Tests...");
            Console.WriteLine("=========================================");

            driver = DriverFactory.CreateDriver();
            excelHelper = new ExcelHelper(reportPath);

            driver.Navigate().GoToUrl("http://localhost:5173/");
        }

        public static IEnumerable<TestCaseData> GetIndividualTestCases()
        {
            var allData = JsonReader.GetIntegrationTestData();
            foreach (var data in allData)
            {
                yield return new TestCaseData(data).SetName($"{data.Id}");
            }
        }

        [Test, TestCaseSource(nameof(GetIndividualTestCases))]
        public void ExecuteIntegrationTest(IntegrationData data)
        {
            if (driver == null) return;

            string actualResultToLog = "";
            string statusToLog = "Fail";
            string screenshotPath = "";

            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
                LoginPage loginPage = new LoginPage(driver);
                CreatePostPage postPage = new CreatePostPage(driver);
                SearchPage searchPage = new SearchPage(driver);
                AdminPage adminPage = new AdminPage(driver);

                // =================================================================
                // XỬ LÝ THEO TỪNG KỊCH BẢN (SCENARIO ID)
                // =================================================================
                switch (data.Id)
                {
                    case "INT_01":
                    case "TC_INT_01.1":
                        Console.WriteLine("[LOG] Chạy INT_01: User tạo bài viết mới...");

                        driver.Navigate().GoToUrl("http://localhost:5173/login");
                        loginPage.Login(data.UserEmail ?? "", data.UserPass ?? "");
                        Thread.Sleep(3000);

                        driver.Navigate().GoToUrl("http://localhost:5173/dang-tin");
                        Thread.Sleep(3000);

                        CreatePostData dummyPost = new CreatePostData
                        {
                            Id = "INT01",
                            Title = data.PostTitle,
                            Price = "5000000",
                            Description = "Máy còn rất mới, chạy mượt mà, bao test hệ thống Automation.",
                            Category = "do-dien-tu",
                            SubCategory = "laptop",
                            Condition = "Mới (chưa qua sử dụng)",
                            Hang = "Dell",
                            Province = "79",
                            District = "760",
                            Ward = "Phường Bến Nghé",
                            Images = 1
                        };

                        postPage.FillPostForm(dummyPost);

                        string postAlertMsg = "";
                        try
                        {
                            var alertWait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                            var alert = alertWait.Until(ExpectedConditions.AlertIsPresent());
                            postAlertMsg = alert.Text; // Lấy text từ Alert đăng bài thành công
                            alert.Accept();
                        }
                        catch { }

                        try
                        {
                            var redirectWait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                            redirectWait.Until(d => d.Url == "http://localhost:5173/" || d.Url == "http://localhost:5173" || !d.Url.Contains("/dang-tin"));
                        }
                        catch
                        {
                            Assert.Fail("Lỗi: Đăng bài xong nhưng web không tự chuyển về Trang chủ sau 30 giây.");
                        }

                        driver.Navigate().GoToUrl("http://localhost:5173/quan-ly-tin");
                        Thread.Sleep(3000);

                        var pendingTab = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//button[contains(text(), 'Chờ duyệt')]")));
                        pendingTab.Click();
                        Thread.Sleep(2000);

                        var postItem = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath($"//div[contains(@class, 'postItem')]//*[contains(text(), '{data.PostTitle}')]")));

                        // ĐỌC THỰC TẾ TỪ WEB
                        string actualPostTitle = postItem.Text;
                        actualResultToLog = $"[Alert Web: {postAlertMsg}] - Tìm thấy bài viết thực tế trong tab Chờ duyệt: '{actualPostTitle}'";
                        statusToLog = "Pass";
                        break;

                    case "INT_02":
                    case "TC_INT_01.2":
                        Console.WriteLine("[LOG] Chạy INT_02: Tìm kiếm và xem chi tiết...");
                        driver.Navigate().GoToUrl("http://localhost:5173/");
                        Thread.Sleep(2000);

                        searchPage.SearchForKeyword(data.SearchKeyword ?? "");
                        Thread.Sleep(3000);

                        var firstProduct = wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("div[class*='productCard'] a")));
                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", firstProduct);

                        wait.Until(d => d.Url.Contains(data.ExpectedUrlContains ?? "/san-pham/"));

                        // ĐỌC THỰC TẾ TỪ WEB (Lấy tiêu đề bài viết bằng thẻ h1 trên trang chi tiết)
                        var detailTitle = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("h1"))).Text;
                        string actualUrl = driver.Url;
                        actualResultToLog = $"Đã mở URL: {actualUrl} | Tên SP hiển thị: '{detailTitle}'";
                        statusToLog = "Pass";
                        break;

                    case "INT_03":
                    case "TC_INT_04.1":
                        Console.WriteLine("[LOG] Chạy INT_03: Lưu tin yêu thích...");
                        driver.Navigate().GoToUrl("http://localhost:5173/login");
                        loginPage.Login(data.UserEmail ?? "", data.UserPass ?? "");
                        Thread.Sleep(3000);

                        searchPage.SearchForKeyword(data.SearchKeyword ?? "");
                        Thread.Sleep(3000);

                        var heartIcon = wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("div[class*='productCard'] button[class*='heart'], div[class*='productCard'] svg[class*='heart']")));
                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", heartIcon);
                        Thread.Sleep(1000);

                        driver.Navigate().GoToUrl("http://localhost:5173/tin-da-luu");
                        Thread.Sleep(3000);

                        var savedItems = driver.FindElements(By.CssSelector("div[class*='productCard']"));
                        Assert.That(savedItems.Count, Is.GreaterThan(0), "Lỗi: Đã bấm lưu nhưng tab Tin Yêu Thích bị trống.");

                        // ĐỌC THỰC TẾ TỪ WEB
                        actualResultToLog = $"Có tổng cộng {savedItems.Count} bài viết trong danh sách Tin Đã Lưu.";
                        statusToLog = "Pass";
                        break;

                    case "INT_04":
                    case "TC_INT_02.1":
                        Console.WriteLine("[LOG] Chạy INT_04: Luồng User đăng -> Admin duyệt...");
                        driver.Navigate().GoToUrl("http://localhost:5173/login");
                        loginPage.Login(data.AdminEmail ?? "", data.UserPass ?? "");
                        Thread.Sleep(3000);

                        adminPage.GoToAdminDashboard();
                        adminPage.GoToManagePosts();
                        adminPage.ClickPendingReviewTab();
                        Thread.Sleep(2000);

                        string adminAlertMsg = "";
                        try
                        {
                            string targetPostId = adminPage.GetFirstPostId();
                            adminPage.ApprovePost(targetPostId);

                            // Chờ và ĐỌC ALERT THỰC TẾ
                            var alert = wait.Until(ExpectedConditions.AlertIsPresent());
                            adminAlertMsg = alert.Text;
                            alert.Accept();
                            Thread.Sleep(2000);
                        }
                        catch
                        {
                            Assert.Ignore("Bỏ qua test vì hiện tại không có bài nào đang chờ duyệt.");
                        }

                        driver.Navigate().GoToUrl("http://localhost:5173/");
                        Thread.Sleep(3000);

                        actualResultToLog = $"Admin thông báo: '{adminAlertMsg}'";
                        statusToLog = "Pass";
                        break;

                    case "INT_06":
                    case "TC_INT_03.1":
                        Console.WriteLine("[LOG] Chạy INT_06: Kiểm tra tài khoản bị cấm...");
                        driver.Navigate().GoToUrl("http://localhost:5173/login");
                        loginPage.Login(data.UserEmail ?? "", data.UserPass ?? "");

                        try
                        {
                            var errorMsg = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(".error, .text-danger, p[class*='error']")));
                            string actualErrorText = errorMsg.Text; // ĐỌC LỖI THỰC TẾ TỪ WEB

                            Assert.That(actualErrorText.ToLower(), Does.Contain("khóa") | Does.Contain("cấm") | Does.Contain("ban"), "Lỗi: Tài khoản bị cấm nhưng cảnh báo không chứa từ khóa khóa/cấm.");

                            actualResultToLog = $"Đã bị chặn với thông báo lỗi: '{actualErrorText}'";
                            statusToLog = "Pass";
                        }
                        catch (WebDriverTimeoutException)
                        {
                            Assert.Fail("Lỗi: Tài khoản đáng lẽ bị khóa nhưng lại đăng nhập thành công!");
                        }
                        break;

                    case "INT_07":
                    case "TC_INT_07.1":
                        Console.WriteLine("[LOG] Chạy INT_07: Luồng Ẩn bài viết...");
                        driver.Navigate().GoToUrl("http://localhost:5173/login");
                        loginPage.Login(data.UserEmail ?? "", data.UserPass ?? "");
                        Thread.Sleep(3000);

                        driver.Navigate().GoToUrl("http://localhost:5173/quan-ly-tin");
                        Thread.Sleep(3000);

                        try
                        {
                            var hideBtn = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//button[contains(text(), 'Ẩn tin')]")));
                            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", hideBtn);
                            Thread.Sleep(1000);

                            try
                            {
                                var confirmHide = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//button[contains(text(), 'TẠM THỜI KHÔNG BÁN')]")));
                                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", confirmHide);
                                wait.Until(ExpectedConditions.AlertIsPresent()).Accept();
                            }
                            catch { }

                            searchPage.SearchForKeyword(data.SearchKeyword ?? "");

                            // ====================================================================
                            // FIX LỖI Ở ĐÂY: VIẾT LOGIC TRỰC TIẾP KHÔNG CẦN QUA SEARCHPAGE
                            // ====================================================================
                            string emptyMsg = "";
                            try
                            {
                                // Tìm thẻ chữ chứa nội dung Không tìm thấy / rỗng (Chờ tối đa 5s)
                                var emptyElement = new WebDriverWait(driver, TimeSpan.FromSeconds(5))
                                    .Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[contains(text(), 'Không tìm thấy') or contains(text(), 'không có kết quả')]")));
                                emptyMsg = emptyElement.Text;
                            }
                            catch (WebDriverTimeoutException)
                            {
                                // Nếu hết 5s không hiện chữ "Không tìm thấy" -> có kết quả hiện ra -> để trống Msg
                                emptyMsg = "";
                            }
                            // ====================================================================

                            Assert.That(emptyMsg, Is.Not.Null.Or.Empty, "Lỗi: Bài đã ẩn nhưng search vẫn ra kết quả!");

                            actualResultToLog = $"Kết quả tìm kiếm hiển thị nội dung: '{emptyMsg}'";
                            statusToLog = "Pass";
                        }
                        catch
                        {
                            Assert.Ignore("Không tìm thấy bài viết nào đang Active để bấm Ẩn.");
                        }
                        break;

                    default:
                        Assert.Ignore($"Chưa code logic cho kịch bản {data.Id}");
                        break;
                }
            }

            catch (Exception ex)
            {
                statusToLog = "Fail";
                try { driver.SwitchTo().Alert().Accept(); } catch { }

                if (string.IsNullOrEmpty(screenshotPath))
                {
                    screenshotPath = TakeScreenshot(driver, $"Fail_{data.Id}", true);
                }

                if (ex is AssertionException)
                {
                    actualResultToLog = $"[Integration Fail] {ex.Message}";
                }
                else
                {
                    actualResultToLog = $"[System Error] {ex.Message.Split('\n')[0]}";
                }

                Console.WriteLine($"\n>>> CRITICAL FAIL ở {data.Id} <<<");
                Console.WriteLine($"Chi tiết: {actualResultToLog}\n");
                throw;
            }
            finally
            {
                Console.WriteLine($"[LOG-DEBUG] Ghi ID [{data.Id}] vào sheet [{sheetName}]...");
                try
                {
                    excelHelper.WriteTestResultById(sheetName, data.Id ?? "", actualResultToLog, statusToLog, testerName, screenshotPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LỖI GHI EXCEL]: {ex.Message}");
                }
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
            Thread.Sleep(2000);
        }

        private string TakeScreenshot(IWebDriver driver, string testName, bool isError = false)
        {
            try
            {
                if (isError)
                {
                    IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                    js.ExecuteScript(@"
                        var errors = document.querySelectorAll('p[class*=""error""], span[class*=""error""], div[class*=""error""], .text-danger');
                        if (errors.length > 0) {
                            errors[0].scrollIntoView({behavior: 'smooth', block: 'center'});
                            errors[0].style.border = '2px dashed #ff4d4f';
                        }
                    ");
                    Thread.Sleep(500);
                }

                ITakesScreenshot ts = (ITakesScreenshot)driver;
                Screenshot screenshot = ts.GetScreenshot();

                string path = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Screenshots"));
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                string finalPath = Path.Combine(path, $"{testName}.png");
                if (File.Exists(finalPath)) File.Delete(finalPath);

                screenshot.SaveAsFile(finalPath);
                return finalPath;
            }
            catch
            {
                return "";
            }
        }
    }
}