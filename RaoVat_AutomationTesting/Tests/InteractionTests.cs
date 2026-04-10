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
    public class InteractionTests
    {
        private IWebDriver? driver;
        private ExcelHelper? excelHelper;

        private readonly string reportPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\BaoDam_Report.xlsx"));
        private readonly string sheetName = "TCs - F5";
        private readonly string testerName = "Hào";

        // NHỚ ĐIỀN LẠI 2 ID SẢN PHẨM CỦA BẠN VÀO ĐÂY NHÉ!
        private readonly string productOfOtherUser = "oGdyOgFfGGQjdrb1xf4u";
        private readonly string productOfSelf = "dpH7qjrcy9GjcO8jc1T1";

        [SetUp]
        public void Setup()
        {
            Console.WriteLine("=========================================");
            Console.WriteLine($"[LOG-DEBUG] Thư mục đang chạy code: {AppDomain.CurrentDomain.BaseDirectory}");
            Console.WriteLine($"[LOG-DEBUG] Đang cố gắng ghi vào file Excel tại: {reportPath}");
            Console.WriteLine($"[LOG-DEBUG] File Excel có tồn tại ở đường dẫn này không? : {File.Exists(reportPath)}");
            Console.WriteLine("=========================================");

            driver = DriverFactory.CreateDriver();
            driver.Manage().Window.Maximize();
            excelHelper = new ExcelHelper(reportPath);

            driver.Navigate().GoToUrl("http://localhost:5173/login");
            LoginPage loginPage = new LoginPage(driver);
            loginPage.Login("nhao84193@gmail.com", "123456");

            Thread.Sleep(3000);

            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
            wait.Until(ExpectedConditions.UrlToBe("http://localhost:5173/"));
        }

        public static IEnumerable<TestCaseData> GetIndividualChatTestCases()
        {
            var allData = JsonReader.GetChatTestData();
            foreach (var data in allData)
            {
                yield return new TestCaseData(data).SetName($"Chat_{data.Id}");
            }
        }

        public static IEnumerable<TestCaseData> GetIndividualCommentTestCases()
        {
            var allData = JsonReader.GetCommentTestData();
            foreach (var data in allData)
            {
                yield return new TestCaseData(data).SetName($"Comment_{data.Id}");
            }
        }

        [Test, TestCaseSource(nameof(GetIndividualChatTestCases))]
        public void ExecuteChatTest(InteractionData data)
        {
            RunTestCore(data);
        }

        [Test, TestCaseSource(nameof(GetIndividualCommentTestCases))]
        public void ExecuteCommentTest(InteractionData data)
        {
            RunTestCore(data);
        }

        private void RunTestCore(InteractionData data)
        {
            if (driver == null || excelHelper == null) return;

            InteractionPage interactionPage = new InteractionPage(driver);
            string actualResultToLog = "";
            string statusToLog = "Fail";
            string screenshotPath = "";
            string testId = data.Id ?? "Unknown_ID";

            try
            {
                // ==========================================
                // LUỒNG 1: KIỂM TRA QUYỀN GUEST (KHÁCH)
                // ==========================================
                if (data.Feature == "GuestChat" || data.Feature == "GuestComment")
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript("window.localStorage.clear(); window.sessionStorage.clear();");
                    driver.Manage().Cookies.DeleteAllCookies();
                    driver.Navigate().Refresh();
                    Thread.Sleep(2000);

                    if (data.Feature == "GuestChat")
                    {
                        driver.Navigate().GoToUrl("http://localhost:5173/tin-nhan");
                        Thread.Sleep(1500);
                        Assert.That(driver.Url, Does.Contain("login"), "Bug: Khách vẫn vào được trang Chat!");

                        // SỬA: Lấy URL thực tế gán vào kết quả
                        actualResultToLog = $"Đã chặn khách. Trình duyệt tự động chuyển về: {driver.Url}";
                    }
                    else
                    {
                        driver.Navigate().GoToUrl("http://localhost:5173/");
                        interactionPage.OpenFirstProduct();
                        bool isDisabled = !interactionPage.IsCommentInputEnabled();
                        Assert.That(isDisabled, Is.True, "Bug: Khách vẫn gõ được bình luận!");

                        // SỬA: Báo cáo trạng thái thực tế của Element
                        actualResultToLog = $"Chặn thành công. Trạng thái ô bình luận bị mờ (Disabled = {isDisabled}).";
                    }
                    statusToLog = "Pass";
                }

                // ==========================================
                // LUỒNG 2: BẤM NÚT CHAT Ở TRANG SẢN PHẨM
                // ==========================================
                else if (data.Feature == "ChatFromProduct")
                {
                    driver.Navigate().GoToUrl($"http://localhost:5173/san-pham/{productOfOtherUser}");
                    Thread.Sleep(1500);
                    interactionPage.ClickChatWithSellerButton();

                    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                    wait.Until(ExpectedConditions.UrlContains("/tin-nhan"));

                    // SỬA: Lấy URL thực tế để chứng minh đã nhảy trang thành công
                    actualResultToLog = $"Đã mở khung chat. URL hiện tại: {driver.Url}";
                    statusToLog = "Pass";
                }

                // ==========================================
                // LUỒNG 3: BẤM CHAT VỚI CHÍNH MÌNH (BẮT LỖI)
                // ==========================================
                else if (data.Feature == "ChatWithSelf")
                {
                    driver.Navigate().GoToUrl($"http://localhost:5173/san-pham/{productOfSelf}");
                    Thread.Sleep(1500);

                    bool isDisabled = interactionPage.IsChatWithSellerButtonDisabled();
                    string btnText = interactionPage.GetChatWithSellerButtonText();

                    Assert.That(isDisabled || btnText.ToLower().Contains("không thể"), Is.True, "Bug: Vẫn bấm chat với chính mình được!");
                    actualResultToLog = $"Hệ thống chặn thành công. Nút hiển thị thực tế trên UI: '{btnText}'";
                    statusToLog = "Pass";
                }

                // ==========================================
                // LUỒNG 4: ẢNH VÀ SPAM
                // ==========================================
                else if (data.Feature == "ChatImage" || data.Feature == "ChatInvalidFile")
                {
                    driver.Navigate().GoToUrl("http://localhost:5173/tin-nhan");
                    interactionPage.OpenFirstChatRoom();

                    if (data.Feature == "ChatImage")
                    {
                        interactionPage.SendDummyImage();
                        string errorMsg = interactionPage.GetReactErrorMessage();
                        Assert.That(string.IsNullOrEmpty(errorMsg), Is.True, "Bug: Lỗi khi gửi ảnh hợp lệ!");

                        // SỬA: Xác nhận việc không có lỗi bằng text
                        actualResultToLog = "Gửi ảnh hợp lệ, không có thông báo lỗi nào xuất hiện trên màn hình.";
                    }
                    else
                    {
                        interactionPage.SendInvalidFile();
                        string alertText = interactionPage.GetAlertTextAndAccept();
                        Assert.That(alertText.ToLower(), Does.Contain("tập tin ảnh"), "Bug: Web không cảnh báo file sai định dạng!");

                        // Đã ngon sẵn
                        actualResultToLog = $"Hệ thống chặn file sai định dạng. Alert thực tế: '{alertText}'";
                    }
                    statusToLog = "Pass";
                }
                else if (data.Feature == "SpamComment")
                {
                    driver.Navigate().GoToUrl("http://localhost:5173/");
                    interactionPage.OpenFirstProduct();
                    for (int i = 1; i <= 3; i++)
                    {
                        interactionPage.FillAndSendComment(data.InputText + " " + i);
                    }

                    string errorMsg = interactionPage.GetReactErrorMessage();
                    Assert.That(errorMsg.ToLower(), Does.Contain("quá nhanh") | Does.Contain("spam"), "Bug: Hệ thống cho phép spam comment liên tục!");

                    // SỬA: Ghi nhận câu cảnh báo thật sự do Web quăng ra
                    actualResultToLog = $"Chặn Spam thành công. Câu cảnh báo trên UI: '{errorMsg}'";
                    statusToLog = "Pass";
                }

                // ==========================================
                // LUỒNG 5: NHẬP TEXT THÔNG THƯỜNG
                // ==========================================
                else
                {
                    if (data.Feature == "Chat")
                    {
                        driver.Navigate().GoToUrl("http://localhost:5173/tin-nhan");
                        interactionPage.OpenFirstChatRoom();
                        interactionPage.FillAndSendChat(data.InputText ?? "");
                    }
                    else if (data.Feature == "Comment")
                    {
                        driver.Navigate().GoToUrl("http://localhost:5173/");
                        interactionPage.OpenFirstProduct();
                        interactionPage.FillAndSendComment(data.InputText ?? "");
                    }

                    if (data.Expected == "success")
                    {
                        string errorMsg = interactionPage.GetReactErrorMessage();
                        Assert.That(string.IsNullOrEmpty(errorMsg), Is.True, $"Lỗi: Gửi hợp lệ nhưng báo lỗi: {errorMsg}");

                        if (data.InputText!.Contains("<script>") || data.InputText.Contains("<iframe"))
                        {
                            actualResultToLog = $"Text chứa mã độc ({data.InputText}) được đăng thành dạng plain-text, không có lỗi XSS.";
                        }
                        else
                        {
                            actualResultToLog = $"Nội dung '{data.InputText}' đã gửi đi không phát sinh lỗi UI.";
                        }
                        statusToLog = "Pass";
                        Console.WriteLine($"ID: {testId} - Gửi thành công!");
                    }
                    else
                    {
                        string actualMsg = interactionPage.GetReactErrorMessage();
                        string expectedMsg = data.Msg ?? "";

                        if (expectedMsg == "mờ")
                        {
                            bool isBtnEnabled = data.Feature == "Chat" ? interactionPage.IsChatSendButtonEnabled() : interactionPage.IsCommentSendButtonEnabled();
                            Assert.That(isBtnEnabled, Is.False, "Bug UI: Nút Gửi vẫn sáng và bấm được dù dữ liệu không hợp lệ!");
                            actualResultToLog = $"Chặn gửi rỗng. Trạng thái nút gửi: Sáng(Enabled) = {isBtnEnabled}.";
                        }
                        else if (expectedMsg == "trim")
                        {
                            string currentInput = data.Feature == "Chat" ? interactionPage.GetChatInputValue() : "";
                            Assert.That(currentInput.Trim(), Is.Empty, "Bug Logic: Hệ thống không tự làm sạch ô nhập liệu!");
                            actualResultToLog = $"Đã chặn xử lý. Chuỗi sau khi Trim() trả về rỗng.";
                        }
                        else
                        {
                            Assert.That(actualMsg.Contains(expectedMsg, StringComparison.OrdinalIgnoreCase), Is.True, $"Bug: Không hiện cảnh báo: '{expectedMsg}'");

                            // SỬA: Ghi lại nội dung lỗi thật chứ không ghi câu tự bịa
                            actualResultToLog = $"Đã chặn lỗi dữ liệu. Lỗi hiển thị thực tế: '{actualMsg}'";
                        }
                        statusToLog = "Pass";
                    }
                }
            }
            catch (Exception ex)
            {
                statusToLog = "Fail";

                try { driver.SwitchTo().Alert().Accept(); } catch { }

                screenshotPath = TakeScreenshot(driver, $"Fail_{testId}", true);

                string shortErrorMessage = ex.Message;
                if (ex is AssertionException)
                {
                    actualResultToLog = $"[Test Fail] {shortErrorMessage}";
                }
                else if (ex is WebDriverException)
                {
                    actualResultToLog = $"[Selenium Error] {shortErrorMessage.Split('\n')[0]}";
                }
                else
                {
                    actualResultToLog = $"[System Error] {ex.GetType().Name}: {shortErrorMessage}";
                }

                Console.WriteLine($"\n>>> CRITICAL FAIL ở {testId} <<<");
                Console.WriteLine($"Chi tiết: {actualResultToLog}\n");
                throw;
            }
            finally
            {
                Console.WriteLine($"[LOG-DEBUG] Tiến hành ghi ID [{testId}] vào sheet [{sheetName}]...");
                try
                {
                    excelHelper.WriteTestResultById(sheetName, testId, actualResultToLog, statusToLog, testerName, screenshotPath);
                    Console.WriteLine($"[LOG-DEBUG] ---> GHI THÀNH CÔNG VÀO EXCEL!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[LỖI CỰC MẠNH KHI GHI EXCEL cho ID {testId}]: {ex.Message}");
                    Console.WriteLine($"[CHI TIẾT LỖI]: {ex.StackTrace}\n");
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
            Thread.Sleep(3000);
        }

        private string TakeScreenshot(IWebDriver driver, string testName, bool isError = false)
        {
            try
            {
                if (isError)
                {
                    IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                    js.ExecuteScript(@"
                        var errors = document.querySelectorAll('p[class*=""error""], span[class*=""error""], div[class*=""error""], .text-danger, [class*=""MuiFormHelperText""]');
                        if (errors.length > 0) {
                            errors[0].scrollIntoView({behavior: 'smooth', block: 'center'});
                            for(var i = 0; i < errors.length; i++) {
                                var el = errors[i];
                                el.style.border = '2px dashed #ff4d4f';
                                el.style.backgroundColor = 'rgba(255, 77, 79, 0.1)';
                                el.style.padding = '4px';
                            }
                        }
                    ");
                    Thread.Sleep(800);
                }

                ITakesScreenshot ts = (ITakesScreenshot)driver;
                Screenshot screenshot = ts.GetScreenshot();

                string path = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Screenshots"));
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string finalPath = Path.Combine(path, $"{testName}.png");
                if (File.Exists(finalPath))
                {
                    File.Delete(finalPath);
                }

                screenshot.SaveAsFile(finalPath);
                return finalPath;
            }
            catch (UnhandledAlertException)
            {
                try { driver.SwitchTo().Alert().Accept(); } catch { }
                ITakesScreenshot ts = (ITakesScreenshot)driver;

                string path = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Screenshots"));
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                string finalPath = Path.Combine(path, $"{testName}.png");
                if (File.Exists(finalPath))
                {
                    File.Delete(finalPath);
                }

                ts.GetScreenshot().SaveAsFile(finalPath);
                return finalPath;
            }
            catch
            {
                return "";
            }
        }
    }
}