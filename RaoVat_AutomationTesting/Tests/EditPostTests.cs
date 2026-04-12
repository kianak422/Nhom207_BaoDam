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
    public class EditPostTests
    {
        private IWebDriver? driver;
        private ExcelHelper excelHelper;
        private string reportPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\BaoDam_Report.xlsx"));
        private string sheetName = "TCs - F3";
        private string testerName = "Danh";

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Console.WriteLine("=========================================");
            Console.WriteLine($"[LOG-DEBUG] Khởi động OneTimeSetup cho Edit Post...");
            Console.WriteLine("=========================================");

            driver = DriverFactory.CreateDriver();
            excelHelper = new ExcelHelper(reportPath);

            driver.Navigate().GoToUrl("http://localhost:5173/login");
            LoginPage loginPage = new LoginPage(driver);
            loginPage.Login("nvk.970@gmail.com", "123456");

            Thread.Sleep(3000);
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
            wait.Until(ExpectedConditions.UrlToBe("http://localhost:5173/"));
        }

        private string GetErrorMessageRobust(IWebDriver driver)
        {
            try
            {
                Thread.Sleep(500);
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                return (string)js.ExecuteScript(@"
                    var errs = document.querySelectorAll('p[class*=""error""], span[class*=""error""], div[class*=""error""], .text-danger, [class*=""MuiFormHelperText""]');
                    for(var i=0; i<errs.length; i++) {
                        if(errs[i].innerText.trim() !== '') return errs[i].innerText.trim();
                    }

                    var invalidInputs = document.querySelectorAll('input:invalid, select:invalid, textarea:invalid');
                    for(var i=0; i<invalidInputs.length; i++) {
                        if(invalidInputs[i].validationMessage) {
                            return invalidInputs[i].validationMessage; 
                        }
                    }
                    return '';
                ") ?? "";
            }
            catch { return ""; }
        }

        public static IEnumerable<TestCaseData> GetIndividualTestCases()
        {
            var allData = JsonReader.GetEditPostTestData();
            foreach (var data in allData)
            {
                yield return new TestCaseData(data).SetName($"{data.Id}");
            }
        }

        [Test, TestCaseSource(nameof(GetIndividualTestCases))]
        public void ExecuteEditPostTest(CreatePostData data)
        {
            if (driver == null) return;
            EditPostPage editPage = new EditPostPage(driver);
            string actualResultToLog = "";
            string statusToLog = "Fail";
            string screenshotPath = "";

            try
            {
                try { driver.SwitchTo().Alert().Accept(); } catch { }
                // ==========================================================
                // 1. CHUYỂN HƯỚNG TỚI TRANG CẦN TEST
                // ==========================================================
                if (data.Id == "ED10" || data.Id == "ED11")
                {
                    // [ĐÃ CẬP NHẬT] Dùng cứng cái ID mà bạn đã cung cấp để test bảo mật
                    string targetId = "ohCYVPQvUyAy0FfEnUUF";
                    string bypassUrl = $"http://localhost:5173/sua-tin/{targetId}";
                    Console.WriteLine($"[LOG-DEBUG] Đang test IDOR - Truy cập: {bypassUrl}");

                    driver.Navigate().GoToUrl(bypassUrl);
                    Thread.Sleep(2000); // Chờ React kiểm tra quyền (useEffect)
                }
                else
                {
                    // Các case bình thường: Vào Quản lý tin và bấm Sửa
                    driver.Navigate().GoToUrl("http://localhost:5173/quan-ly-tin");
                    Thread.Sleep(3000);

                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                    try
                    {
                        var editButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//button[contains(text(), 'Sửa')]")));
                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", editButton);
                        Thread.Sleep(500);
                        editButton.Click();
                    }
                    catch { Assert.Fail("Lỗi: Không tìm thấy nút 'Sửa'."); }
                }

                // ==========================================================
                // 2. KIỂM TRA ALERT BẢO MẬT NGAY LÚC LOAD TRANG (CHO ED10, ED11)
                // ==========================================================
                try
                {
                    // React sẽ ném Alert đuổi cổ ra gần như ngay lập tức nên chỉ cần chờ 3s
                    var earlyAlertWait = new WebDriverWait(driver, TimeSpan.FromSeconds(3));
                    IAlert earlyAlert = earlyAlertWait.Until(ExpectedConditions.AlertIsPresent());
                    string earlyAlertText = earlyAlert.Text;
                    earlyAlert.Accept();

                    // Nếu là bài test IDOR và bắt đúng Alert chứa chữ "không có quyền"
                    if (data.Id == "ED10" || data.Id == "ED11")
                    {
                        if (earlyAlertText.ToLower().Contains((data.Msg ?? "không có quyền").ToLower()))
                        {
                            actualResultToLog = earlyAlertText;
                            statusToLog = "Pass";
                            screenshotPath = TakeScreenshot(driver, $"Pass_{data.Id}", false);
                            Console.WriteLine($"[LOG-DEBUG] Test IDOR PASS. Đã chặn bằng Alert: {earlyAlertText}");
                            return; // ✅ Test thành công, kết thúc sớm tại đây!
                        }
                    }
                }
                catch (WebDriverTimeoutException)
                {
                    // Nếu không có Alert đầu trang, tiếp tục kịch bản điền form bên dưới
                }

                // Nếu là ED10/ED11 mà chạy xuống tận đây nghĩa là bị HỔNG BẢO MẬT
                if (data.Id == "ED10" || data.Id == "ED11")
                {
                    statusToLog = "Fail";
                    throw new AssertionException("LỖI BẢO MẬT NGHIÊM TRỌNG: Form sửa bài vẫn load ra, không có Alert chặn quyền!");
                }

                // ==========================================================
                // 3. ĐIỀN FORM CHO CÁC CASE CẬP NHẬT BÌNH THƯỜNG
                // ==========================================================
                // ==========================================================
                // 3. ĐIỀN FORM CHO CÁC CASE CẬP NHẬT BÌNH THƯỜNG
                // ==========================================================
                Thread.Sleep(4000); // Đợi load dữ liệu cũ
                editPage.FillEditPostForm(data);

                // [FIX LỖI CƯỚP CLICK]: Dùng JS ẩn luôn cái banner của Firebase Emulator
                try
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript(@"
                        var banner = document.querySelector('.firebase-emulator-warning');
                        if(banner) { banner.style.display = 'none'; }
                    ");
                    Thread.Sleep(200); // Chờ 1 chút cho banner biến mất
                }
                catch { }

                // Cuộn nút Submit ra giữa màn hình cho an toàn
                var submitBtn = driver.FindElement(By.Id("btn-submit-edit"));
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", submitBtn);
                Thread.Sleep(500);

                // Bấm Submit chuẩn bằng Selenium
                try { submitBtn.Submit(); } catch { submitBtn.Click(); }

                // ==========================================================
                // 4. KIỂM TRA KẾT QUẢ THEO EXPECTED (JSON)
                // ==========================================================
                if (data.Expected == "success")
                {
                    try
                    {
                        var alertWait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
                        IAlert alert = alertWait.Until(ExpectedConditions.AlertIsPresent());
                        actualResultToLog = alert.Text;
                        alert.Accept();

                        if (actualResultToLog.ToLower().Contains("xét duyệt") || actualResultToLog.ToLower().Contains("thành công"))
                        {
                            statusToLog = "Pass";
                            screenshotPath = TakeScreenshot(driver, $"Pass_{data.Id}", false);
                        }
                        else
                        {
                            statusToLog = "Fail";
                            throw new AssertionException($"Cập nhật thất bại. Nội dung Alert lạ: {actualResultToLog}");
                        }
                    }
                    catch (WebDriverTimeoutException)
                    {
                        statusToLog = "Fail";
                        throw new AssertionException("Cập nhật thất bại. Lỗi: Không hiển thị Alert thành công.");
                    }
                }
                else // Kịch bản mong đợi lỗi (ED02 - Bắt bong bóng "Vui lòng điền")
                {
                    string actualMsg = GetErrorMessageRobust(driver);
                    string expectedMsg = (data.Msg ?? "").ToLower();

                    // Logic so sánh thông minh
                    if (actualMsg.ToLower().Contains(expectedMsg) || (expectedMsg == "điền" && actualMsg.ToLower().Contains("vui lòng")))
                    {
                        actualResultToLog = actualMsg;
                        statusToLog = "Pass";
                        screenshotPath = TakeScreenshot(driver, $"Pass_{data.Id}", false);
                    }
                    else
                    {
                        statusToLog = "Fail";
                        actualResultToLog = string.IsNullOrEmpty(actualMsg) ? "Không có bong bóng lỗi nào hiện ra" : actualMsg;
                        throw new AssertionException($"Sai text lỗi. Kì vọng: '{expectedMsg}' | Thực tế: '{actualResultToLog}'");
                    }
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

                if (ex is AssertionException) actualResultToLog = ex.Message;
                else actualResultToLog = $"Lỗi Selenium: {ex.Message.Split('\n')[0]}";

                throw;
            }
            finally
            {
                try
                {
                    excelHelper.WriteTestResultById(sheetName, data.Id, actualResultToLog, statusToLog, testerName, screenshotPath);
                }
                catch { }
            }
        }

        [OneTimeTearDown]
        public void OneTimeTeardown()
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
                if (isError || testName.Contains("ED02") || testName.Contains("ED04"))
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
                            }
                        }
                        var invalidInputs = document.querySelectorAll('input:invalid, select:invalid, textarea:invalid');
                        if (invalidInputs.length > 0 && errors.length === 0) {
                            invalidInputs[0].scrollIntoView({behavior: 'smooth', block: 'center'});
                        }
                        for(var i = 0; i < invalidInputs.length; i++) {
                            var el = invalidInputs[i];
                            el.style.border = '3px solid red';
                            el.style.boxShadow = '0 0 10px red';
                        }
                    ");
                    Thread.Sleep(800);
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
            catch { return ""; }
        }
    }
}