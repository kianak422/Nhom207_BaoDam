using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using RaoVat_AutomationTesting.Pages;
using RaoVat_AutomationTesting.Utilities;
using SeleniumExtras.WaitHelpers;
using System;
using System.IO;
using System.Collections.Generic;

namespace RaoVat_AutomationTesting.Tests
{
    [TestFixture]
    public class CreatePostTests
    {
        private IWebDriver? driver;
        private ExcelHelper excelHelper;

        private string reportPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\BaoDam_Report.xlsx"));

        private string sheetName = "TCs - F3";
        private string testerName = "Danh";

        [OneTimeSetUp] // ĐÃ ĐỔI THÀNH ONETIMESETUP
        public void Setup()
        {
            Console.WriteLine("=========================================");
            Console.WriteLine($"[LOG-DEBUG] Thư mục đang chạy code: {AppDomain.CurrentDomain.BaseDirectory}");
            Console.WriteLine($"[LOG-DEBUG] Đang cố gắng ghi vào file Excel tại: {reportPath}");
            Console.WriteLine($"[LOG-DEBUG] File Excel có tồn tại ở đường dẫn này không? : {File.Exists(reportPath)}");
            Console.WriteLine("=========================================");

            driver = DriverFactory.CreateDriver();
            excelHelper = new ExcelHelper(reportPath);

            driver.Navigate().GoToUrl("http://localhost:5173/login");
            LoginPage loginPage = new LoginPage(driver);
            loginPage.Login("nvk.970@gmail.com", "123456");

            // FIX LỖI 403 TOKEN
            System.Threading.Thread.Sleep(3000);

            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
            wait.Until(ExpectedConditions.UrlToBe("http://localhost:5173/"));
        }

        private string GetErrorMessageRobust(IWebDriver driver)
        {
            try
            {
                System.Threading.Thread.Sleep(500);
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                return (string)js.ExecuteScript(@"
                    var errs = document.querySelectorAll('p[class*=""error""], span[class*=""error""], div[class*=""error""], .text-danger, [class*=""MuiFormHelperText""]');
                    for(var i=0; i<errs.length; i++) {
                        if(errs[i].innerText.trim() !== '') return errs[i].innerText.trim();
                    }
                    return '';
                ") ?? "";
            }
            catch { return ""; }
        }

        // =================================================================
        // HÀM BỌC DỮ LIỆU MỚI: TÁCH TỪNG TEST CASE CHO TEST EXPLORER
        // =================================================================
        public static IEnumerable<TestCaseData> GetIndividualTestCases()
        {
            var allData = JsonReader.GetCreatePostTestData();
            foreach (var data in allData)
            {
                yield return new TestCaseData(data).SetName($"{data.Id}");
            }
        }

        // =================================================================
        // HÀM THỰC THI TEST CHÍNH
        // =================================================================
        [Test, TestCaseSource(nameof(GetIndividualTestCases))]
        public void ExecuteCreatePostTest(CreatePostData data)
        {
            if (driver == null) return;

            CreatePostPage postPage = new CreatePostPage(driver);
            string actualResultToLog = "";
            string statusToLog = "Fail";
            string screenshotPath = "";

            try
            {
                driver.Navigate().GoToUrl("http://localhost:5173/dang-tin");

                // BẮT BUỘC PHẢI F5 LẠI TRANG ĐỂ DỌN RÁC DOM DO CHẠY ONETIMESETUP CHUNG 1 TAB
                driver.Navigate().Refresh();

                // === CHÈN FIX CHO LỖI RELOAD TRANG CỦA REACT ===
                System.Threading.Thread.Sleep(4000);

                // =========================================================
                // 1. TÁCH RIÊNG LUỒNG CHẠY CHO CÁC TEST CASE KIỂM TRA UI DROPDOWN
                // =========================================================
                if (data.Id == "CP20")
                {
                    // Gọi hàm kiểm tra Dropdown Phường/Xã và nhận chuỗi kết quả thực tế
                    actualResultToLog = postPage.ValidateCP20_WardDropdownEnabled(data);
                    statusToLog = "Pass";
                    screenshotPath = TakeScreenshot(driver, $"Pass_{data.Id}", false);
                    Console.WriteLine($"ID: {data.Id} - {actualResultToLog}");
                }
                else if (data.Id == "CP22")
                {
                    // Gọi hàm kiểm tra Reset Dropdown và nhận chuỗi kết quả thực tế
                    actualResultToLog = postPage.ValidateCP22_DropdownReset(data);
                    statusToLog = "Pass";
                    screenshotPath = TakeScreenshot(driver, $"Pass_{data.Id}", false);
                    Console.WriteLine($"ID: {data.Id} - {actualResultToLog}");
                }
                // =========================================================
                // 2. LUỒNG CHẠY CHO CÁC TEST CASE NHẬP FORM BÌNH THƯỜNG
                // =========================================================
                else
                {
                    postPage.FillPostForm(data);

                    if (data.Expected == "success")
                    {
                        try
                        {
                            // 1. ĐỢI ALERT THÔNG MINH: Tối đa 120 giây
                            var alertWait = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                            IAlert alert = alertWait.Until(ExpectedConditions.AlertIsPresent());

                            actualResultToLog = alert.Text;
                            alert.Accept();
                        }
                        catch (WebDriverTimeoutException) { }

                        try
                        {
                            // 2. ĐỢI CHUYỂN TRANG THÔNG MINH: Tối đa 120 giây về Trang chủ
                            var homePageWait = new WebDriverWait(driver, TimeSpan.FromSeconds(120));

                            homePageWait.Until(d => d.Url == "http://localhost:5173/" || d.Url == "http://localhost:5173" || !d.Url.Contains("/dang-tin"));

                            System.Threading.Thread.Sleep(1000); // Trang load xong thì nghỉ 1 nhịp rồi chụp hình
                            screenshotPath = TakeScreenshot(driver, $"Pass_{data.Id}", false);

                            statusToLog = "Pass";
                            if (string.IsNullOrEmpty(actualResultToLog)) actualResultToLog = "Đăng thành công và đã chuyển về Trang chủ";

                            Console.WriteLine($"ID: {data.Id} - Đăng thành công! Thông báo: {actualResultToLog}");
                        }
                        catch (WebDriverTimeoutException)
                        {
                            string errorMsg = GetErrorMessageRobust(driver);
                            actualResultToLog = string.IsNullOrEmpty(errorMsg) ? "Đăng thất bại: Quá 120 giây vẫn không chuyển về Trang chủ được." : errorMsg;
                            statusToLog = "Fail";
                            Assert.Fail($"Lỗi: {actualResultToLog}");
                        }
                    }
                    else
                    {
                        string actualMsg = "";
                        try
                        {
                            var shortWait = new WebDriverWait(driver, TimeSpan.FromSeconds(2));
                            IAlert alert = shortWait.Until(ExpectedConditions.AlertIsPresent());
                            actualMsg = alert.Text;
                            alert.Accept();
                        }
                        catch (WebDriverTimeoutException)
                        {
                            actualMsg = GetErrorMessageRobust(driver);
                        }

                        actualResultToLog = string.IsNullOrEmpty(actualMsg) ? "Không hiển thị thông báo lỗi" : actualMsg;
                        Console.WriteLine($"ID: {data.Id} - Lỗi thực tế: {actualResultToLog}");

                        string expectedMsg = (data.Msg ?? "").ToLower();

                        if (actualResultToLog.ToLower().Contains(expectedMsg))
                        {
                            statusToLog = "Pass";
                        }
                        else
                        {
                            statusToLog = "Fail";
                            Assert.Fail($"Sai text lỗi. Text mong đợi: '{expectedMsg}' | Text thực tế: '{actualResultToLog}'");
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

                if (string.IsNullOrEmpty(actualResultToLog) || (!ex.Message.Contains("Sai text lỗi") && !ex.Message.Contains("Lỗi UI") && !(ex is AssertionException)))
                {
                    actualResultToLog = $"Lỗi Selenium: {ex.Message.Split('\n')[0]}";
                }

                Console.WriteLine($"\n>>> CRITICAL FAIL ở {data.Id} <<<");
                Console.WriteLine($"Chi tiết: {actualResultToLog}\n");
                throw;
            }
            finally
            {
                Console.WriteLine($"[LOG-DEBUG] Tiến hành ghi ID [{data.Id}] vào sheet [{sheetName}]...");
                try
                {
                    excelHelper.WriteTestResultById(sheetName, data.Id, actualResultToLog, statusToLog, testerName, screenshotPath);
                    Console.WriteLine($"[LOG-DEBUG] ---> GHI THÀNH CÔNG VÀO EXCEL!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[LỖI CỰC MẠNH KHI GHI EXCEL]: {ex.Message}");
                    Console.WriteLine($"[CHI TIẾT LỖI]: {ex.StackTrace}\n");
                }
            }
        }

        [OneTimeTearDown] // ĐÃ ĐỔI THÀNH ONETIMETERARDOWN
        public void Teardown()
        {
            if (driver != null)
            {
                driver.Quit();
                driver.Dispose();
            }

            // Chống spam & khóa file
            System.Threading.Thread.Sleep(3000);
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
                    System.Threading.Thread.Sleep(800);
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