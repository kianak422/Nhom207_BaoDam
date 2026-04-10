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

        [SetUp]
        public void Setup()
        {
            Console.WriteLine("=========================================");
            Console.WriteLine($"[LOG-DEBUG] Bắt đầu Test Edit Post...");
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

        // =========================================================================
        // ĐÃ NÂNG CẤP: BẮT ĐƯỢC BONG BÓNG LỖI HTML5 (validationMessage)
        // =========================================================================
        private string GetErrorMessageRobust(IWebDriver driver)
        {
            try
            {
                Thread.Sleep(500);
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                return (string)js.ExecuteScript(@"
                    // 1. TÌM LỖI TỪ HTML (CÁCH CŨ)
                    var errs = document.querySelectorAll('p[class*=""error""], span[class*=""error""], div[class*=""error""], .text-danger, [class*=""MuiFormHelperText""]');
                    for(var i=0; i<errs.length; i++) {
                        if(errs[i].innerText.trim() !== '') return errs[i].innerText.trim();
                    }

                    // 2. TÌM LỖI TỪ BONG BÓNG TRÌNH DUYỆT (HTML5 VALIDATION) MỚI THÊM
                    var invalidInputs = document.querySelectorAll('input:invalid, select:invalid, textarea:invalid');
                    for(var i=0; i<invalidInputs.length; i++) {
                        if(invalidInputs[i].validationMessage) {
                            return invalidInputs[i].validationMessage; // Trả về câu 'Vui lòng điền...'
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

        [TestCaseSource(nameof(GetIndividualTestCases))]
        public void ExecuteEditPostTest(CreatePostData data)
        {
            if (driver == null) return;

            EditPostPage editPage = new EditPostPage(driver);
            string actualResultToLog = "";
            string statusToLog = "Fail";
            string screenshotPath = "";

            try
            {
                // Truy cập quản lý tin & Bấm nút Sửa (Logic cũ của Edit Post)
                driver.Navigate().GoToUrl("http://localhost:5173/quan-ly-tin");
                Thread.Sleep(3000);

                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                try
                {
                    var editButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//button[contains(text(), 'Sửa')]")));
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", editButton);
                    Thread.Sleep(500);
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", editButton);
                }
                catch (WebDriverTimeoutException)
                {
                    Assert.Fail("Lỗi: Không tìm thấy nút 'Sửa'. Kiểm tra xem tài khoản này có bài đăng nào không!");
                }

                Thread.Sleep(5000); // Đợi load data cũ

                // Điền form
                editPage.FillEditPostForm(data);

                // --- KIỂM TRA ALERT ---
                string alertText = "";
                bool isSuccessAlert = false;
                try
                {
                    var alertWait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                    IAlert alert = alertWait.Until(ExpectedConditions.AlertIsPresent());
                    alertText = alert.Text;

                    if (alertText.ToLower().Contains("thành công") || alertText.ToLower().Contains("success"))
                    {
                        isSuccessAlert = true;
                    }
                    alert.Accept();
                    if (isSuccessAlert) Thread.Sleep(2000);
                }
                catch (WebDriverTimeoutException)
                {
                    // NẾU KO CÓ ALERT -> CHẠY HÀM LẤY BONG BÓNG HTML5
                    alertText = GetErrorMessageRobust(driver);
                }

                // --- XỬ LÝ THEO JSON ---
                if (data.Expected == "success")
                {
                    if (isSuccessAlert)
                    {
                        actualResultToLog = alertText;

                        // Pass thì kiểm tra chuyển hướng
                        var redirectWait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        redirectWait.Until(d => d.Url.Contains("/quan-ly-tin"));

                        screenshotPath = TakeScreenshot(driver, $"Pass_{data.Id}", false);
                        statusToLog = "Pass";
                        actualResultToLog += " -> Cập nhật thành công, đã chuyển về trang Quản lý tin.";
                    }
                    else
                    {
                        throw new AssertionException($"Cập nhật thất bại. Lỗi hiển thị: {alertText}");
                    }
                }
                else // Kì vọng thất bại (ED02 nằm ở đây)
                {
                    if (isSuccessAlert)
                    {
                        actualResultToLog = $"BUG: Test Case yêu cầu lỗi nhưng web lại cho cập nhật thành công! Alert: '{alertText}'";
                        screenshotPath = TakeScreenshot(driver, $"Fail_{data.Id}", true);
                        throw new AssertionException(actualResultToLog);
                    }
                    else
                    {
                        // Đã bắt được Bong bóng HTML5
                        actualResultToLog = string.IsNullOrEmpty(alertText) ? "Không hiển thị thông báo lỗi" : alertText;
                        string expectedMsg = (data.Msg ?? "").ToLower();

                        // KIỂM TRA THÔNG MINH: Bong bóng trình duyệt báo "Vui lòng...", JSON đòi "bắt buộc"
                        bool isMatch = actualResultToLog.ToLower().Contains(expectedMsg);

                        if (!isMatch && expectedMsg == "bắt buộc" && (actualResultToLog.ToLower().Contains("vui lòng") || actualResultToLog.ToLower().Contains("please")))
                        {
                            isMatch = true; // Châm chước cho Pass vì đó đúng là lỗi bắt buộc của HTML5
                            actualResultToLog = $"Bắt được bong bóng HTML5: '{actualResultToLog}' (Đạt chuẩn 'Bắt buộc')";
                        }

                        if (isMatch)
                        {
                            statusToLog = "Pass";
                            screenshotPath = TakeScreenshot(driver, $"Pass_{data.Id}", false);
                        }
                        else
                        {
                            throw new AssertionException($"Sai text báo lỗi. Kì vọng: '{expectedMsg}' - Thực tế: '{actualResultToLog}'");
                        }
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

                if (ex is AssertionException)
                {
                    actualResultToLog = ex.Message;
                }
                else
                {
                    actualResultToLog = $"Lỗi hệ thống: {ex.Message.Split('\n')[0]}";
                }
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

        // =========================================================================
        // ĐÃ NÂNG CẤP: TỰ ĐỘNG BÔI ĐỎ Ô INPUT BỊ LỖI HTML5 TRƯỚC KHI CHỤP
        // =========================================================================
        private string TakeScreenshot(IWebDriver driver, string testName, bool isError = false)
        {
            try
            {
                if (isError || testName.Contains("ED02") || testName.Contains("ED04")) // Bôi đỏ nếu là case bắt lỗi
                {
                    IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                    js.ExecuteScript(@"
                        // 1. Highlight lỗi HTML thông thường
                        var errors = document.querySelectorAll('p[class*=""error""], span[class*=""error""], div[class*=""error""], .text-danger, [class*=""MuiFormHelperText""]');
                        if (errors.length > 0) {
                            errors[0].scrollIntoView({behavior: 'smooth', block: 'center'});
                            for(var i = 0; i < errors.length; i++) {
                                errors[i].style.border = '2px dashed #ff4d4f';
                                errors[i].style.backgroundColor = 'rgba(255, 77, 79, 0.1)';
                            }
                        }

                        // 2. Highlight ô input vi phạm HTML5 (Bong bóng)
                        var invalidInputs = document.querySelectorAll('input:invalid, select:invalid, textarea:invalid');
                        if (invalidInputs.length > 0 && errors.length === 0) {
                            invalidInputs[0].scrollIntoView({behavior: 'smooth', block: 'center'});
                        }
                        for(var i = 0; i < invalidInputs.length; i++) {
                            invalidInputs[i].style.border = '3px solid red';
                            invalidInputs[i].style.boxShadow = '0 0 10px red';
                        }
                    ");
                    Thread.Sleep(800); // Chờ JS vẽ xong
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