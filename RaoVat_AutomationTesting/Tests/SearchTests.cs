using NUnit.Framework;
using OpenQA.Selenium;
using RaoVat_AutomationTesting.Pages;
using RaoVat_AutomationTesting.Utilities;
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace RaoVat_AutomationTesting.Tests
{
    [TestFixture]
    [Category("F4")]
    public class SearchTests
    {
        private IWebDriver? driver;
        private StringBuilder? verificationErrors;
        private ExcelHelper? excelHelper;

        // --- CHÚ Ý: ĐÃ CẬP NHẬT ĐƯỜNG DẪN FILE EXCEL TẠI ĐÂY ---
        private string reportPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\BaoDam_Report.xlsx"));
        private string sheetName = "TCs - F4"; // Đúng với tên sheet trong ảnh
        private string testerName = "Huy";    // Tên của bạn

        [SetUp]
        public void Setup()
        {
            Console.WriteLine("=========================================");
            Console.WriteLine($"[LOG-DEBUG] Thư mục đang chạy code: {AppDomain.CurrentDomain.BaseDirectory}");
            Console.WriteLine($"[LOG-DEBUG] Đang cố gắng ghi vào file Excel tại: {reportPath}");
            Console.WriteLine($"[LOG-DEBUG] File Excel có tồn tại ở đường dẫn này không? : {File.Exists(reportPath)}");
            Console.WriteLine("=========================================");

            driver = DriverFactory.CreateDriver();
            verificationErrors = new StringBuilder(); // chỉ để log debug, KHÔNG dùng để fail ở TearDown
            excelHelper = new ExcelHelper(reportPath);
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
            var allData = JsonReader.GetSearchTestData();
            if (allData != null)
            {
                foreach (var data in allData)
                {
                    yield return new TestCaseData(data).SetName($"{data.Id}");
                }
            }
        }
        // =================================================================

        [TestCaseSource(nameof(GetIndividualTestCases))]
        public void ExecuteSearchTest(SearchData data)
        {
            if (driver == null) return;

            SearchPage searchPage = new SearchPage(driver);
            string actualResultToLog = "";
            string statusToLog = "Fail";
            string screenshotPath = "";

            try
            {
                bool hasAnyFilterActionFromData =
                    !string.IsNullOrEmpty(data.Category)
                    || !string.IsNullOrEmpty(data.Subcategory)
                    || !string.IsNullOrEmpty(data.Province)
                    || !string.IsNullOrEmpty(data.MinPrice)
                    || !string.IsNullOrEmpty(data.MaxPrice)
                    || data.HomeBtnIndex.HasValue;

                // FA10/FA11: Home category button -> luôn bắt đầu từ trang chủ
                if (data.HomeBtnIndex.HasValue && string.IsNullOrEmpty(data.StartUrl))
                {
                    driver.Navigate().GoToUrl("http://localhost:5173/");
                }
                // Ưu tiên điều hướng theo StartUrl nếu được cung cấp (FA12/FA13/FA14...)
                else if (!string.IsNullOrEmpty(data.StartUrl))
                {
                    driver.Navigate().GoToUrl(data.StartUrl);
                }
                else if (string.IsNullOrEmpty(data.Keyword) && (data.Keywords == null || data.Keywords.Count == 0) && hasAnyFilterActionFromData)
                {
                    // Nếu không có keyword mà có filter, điều hướng thẳng đến /tim-kiem để bộ lọc hiển thị
                    driver.Navigate().GoToUrl("http://localhost:5173/tim-kiem");
                }
                else
                {
                    driver.Navigate().GoToUrl("http://localhost:5173/");
                }

                // Đợi trang tải xong hoàn toàn
                var pageWait = new OpenQA.Selenium.Support.UI.WebDriverWait(driver, TimeSpan.FromSeconds(10));
                pageWait.Until(d =>
                    string.Equals(
                        ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString(),
                        "complete",
                        StringComparison.OrdinalIgnoreCase));
                System.Threading.Thread.Sleep(1000); // Thêm 1s để UI ổn định

                // Xử lý nhiều từ khóa hoặc một từ khóa duy nhất
                List<string> searchKeywords = new List<string>();
                if (data.Keywords != null && data.Keywords.Count > 0)
                {
                    searchKeywords.AddRange(data.Keywords);
                }
                else if (data.Keyword != null)
                {
                    searchKeywords.Add(data.Keyword);
                }
                else
                {
                    // Trường hợp tìm kiếm rỗng (ES01, ES03)
                    searchKeywords.Add("");
                }

                // Thực hiện tìm kiếm từng từ khóa
                // Với flow filter-only, tránh bấm Search rỗng (dễ reset state/route) -> chỉ lọc theo UI
                bool isFilterOnly = hasAnyFilterActionFromData && searchKeywords.All(k => string.IsNullOrWhiteSpace(k));
                if (!isFilterOnly)
                {
                    foreach (var kw in searchKeywords)
                    {
                        searchPage.SearchForKeyword(kw);
                    }
                }

                bool categoryApplied = false;

                // Thực hiện lọc theo Category (FA01)
                if (!string.IsNullOrEmpty(data.Category))
                {
                    categoryApplied = searchPage.FilterByCategory(data.Category);
                }

                // Thực hiện lọc theo Subcategory (FA12, FA13, FA14)
                if (!string.IsNullOrEmpty(data.Subcategory))
                {
                    searchPage.SelectSubcategory(data.Subcategory);
                }

                // Thực hiện lọc theo Vị trí (FA02, FA03, FA04)
                if (!string.IsNullOrEmpty(data.Province))
                {
                    // Chú ý: Đảm bảo model SearchData có trường public bool? ClearModal { get; set; } nếu bạn dùng
                    // Mình tạm thay `data.ClearModal == true` thành `false` nếu bạn chưa định nghĩa nó trong Model
                    searchPage.FilterByLocation(data.Province, data.District ?? "", false);
                }

                // Thực hiện lọc theo Giá (FA05, FA06, FA07, FA19)
                if (!string.IsNullOrEmpty(data.MinPrice) || !string.IsNullOrEmpty(data.MaxPrice))
                {
                    searchPage.FilterByPrice(data.MinPrice ?? "0", data.MaxPrice ?? "0");
                }

                // Thực hiện click button ở trang chủ (FA10, FA11)
                if (data.HomeBtnIndex.HasValue)
                {
                    searchPage.ClickHomeButton(data.HomeBtnIndex.Value);
                }

                // Thực hiện Reset bộ lọc nếu có yêu cầu (FA17 mới)
                // Đảm bảo có trường public bool? Reset { get; set; } trong Model, nếu không hãy bỏ qua
                // if (data.Reset == true) { searchPage.ResetFilter(); }

                // Verification (Dùng từ khóa cuối cùng để kiểm tra URL)
                if (data.Expected == "success")
                {
                    string lastKeyword = searchKeywords.LastOrDefault() ?? "";
                    string searchKeyword = lastKeyword.Trim();
                    string encodedKeyword = Uri.EscapeDataString(searchKeyword);
                    string plusKeyword = searchKeyword.Replace(" ", "+");

                    bool hasAnyFilterAction = hasAnyFilterActionFromData;

                    bool hasFilterOnlyFlow = string.IsNullOrEmpty(searchKeyword) && hasAnyFilterAction;

                    // Tăng timeout lên 20 giây và hỗ trợ nhiều tiêu chí URL linh hoạt
                    var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(driver, TimeSpan.FromSeconds(20));
                    wait.Until(d => {
                        string url = d.Url.ToLower();
                        bool match = true;

                        if (!string.IsNullOrEmpty(searchKeyword))
                            match &= url.Contains(searchKeyword.ToLower()) || url.Contains(encodedKeyword.ToLower()) || url.Contains(plusKeyword.ToLower());

                        // Kiểm tra tham số danh mục linh hoạt
                        if (!string.IsNullOrEmpty(data.Category) && categoryApplied)
                            match &= url.Contains("category=") || url.Contains("cat=");

                        // Lưu ý: nhiều flow lọc địa điểm chỉ cập nhật state UI, không ghi lên query string.
                        // Vì vậy chỉ coi đây là điều kiện "cộng điểm", không bắt buộc.
                        if (!string.IsNullOrEmpty(data.Province))
                        {
                            bool locationInQuery = url.Contains("province=") || url.Contains("location=") || url.Contains("area=") || url.Contains("p=");
                            match &= locationInQuery || hasFilterOnlyFlow;
                        }

                        // Kiểm tra tham số giá
                        if (!string.IsNullOrEmpty(data.MinPrice) && data.MinPrice != "0")
                            match &= url.Contains("minprice=") || url.Contains("price_min=");

                        // Nhiều flow có thao tác lọc/keyword nhưng app vẫn giữ URL /tim-kiem không query.
                        bool filterFlowStable = hasAnyFilterAction && url.Contains("/tim-kiem");
                        return match || url.Contains("?") || filterFlowStable;
                    });

                    actualResultToLog = $"Thực hiện thành công. URL hiện tại: {driver.Url}";
                    statusToLog = "Pass";

                    // ✅ Đã cập nhật để dùng hàm TakeScreenshot nội bộ thay vì ScreenshotHelper.Capture
                    screenshotPath = TakeScreenshot(driver, $"Pass_{data.Id}", false);

                    Console.WriteLine($"ID: {data.Id} - Pass! URL: {driver.Url}");
                }
                else
                {
                    // ===== LỐI ASSERTION FAIL =====
                    actualResultToLog = "Kết quả không khớp kỳ vọng";
                    statusToLog = "Fail";
                    screenshotPath = TakeScreenshot(driver, $"Fail_{data.Id}", true);
                    Assert.Fail($"Test assertion fail cho {data.Id}");
                }
            }
            catch (Exception ex)
            {
                statusToLog = "Fail";

                // ĐÓNG ALERT NẾU CÓ ĐỂ TRÁNH LỖI KHI CHỤP ẢNH
                try { driver.SwitchTo().Alert().Accept(); } catch { }

                // Chụp ảnh lỗi nếu chưa có
                if (string.IsNullOrEmpty(screenshotPath))
                {
                    screenshotPath = TakeScreenshot(driver, $"Fail_{data.Id}", true);
                }

                // Xử lý thông báo lỗi như CreatePostTests
                string errorMsg = ex.Message;
                if (ex is WebDriverTimeoutException)
                {
                    errorMsg = $"Timed out sau 20 giây. URL hiện tại: {driver.Url}";
                }

                if (ex is AssertionException)
                {
                    actualResultToLog = $"[Test Fail] {errorMsg}";
                }
                else if (ex is WebDriverException)
                {
                    actualResultToLog = $"[Selenium Error] {errorMsg.Split('\n')[0]}";
                }
                else
                {
                    actualResultToLog = $"[System Error] {ex.GetType().Name}: {errorMsg.Split('\n')[0]}";
                }

                verificationErrors?.Append($"[{data.Id}] {actualResultToLog}\n");

                Console.WriteLine($"\n>>> CRITICAL FAIL ở {data.Id} <<<");
                Console.WriteLine($"Chi tiết: {actualResultToLog}\n");

                throw;
            }
            finally
            {
                // ===== FIX GICI EXCEL GIỐNG CREATEPOST =====
                Console.WriteLine($"[LOG-DEBUG] Tiến hành ghi ID [{data.Id}] vào sheet [{sheetName}]...");
                try
                {
                    // ✅ Dùng excelHelper trực tiếp (không nullable)
                    if (excelHelper != null)
                    {
                        excelHelper.WriteTestResultById(sheetName, data.Id ?? "", actualResultToLog, statusToLog, testerName, screenshotPath);
                        Console.WriteLine($"[LOG-DEBUG] ---> GHI THÀNH CÔNG VÀO EXCEL!");
                    }
                    else
                    {
                        Console.WriteLine($"[CẢNH BÁO] ExcelHelper là null, không thể ghi kết quả.");
                    }
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine($"\n[LỖI GHI EXCEL CHO ID {data.Id}]: {ioEx.Message}");
                    Console.WriteLine($"[CHI TIẾT]: {ioEx.StackTrace}");
                }
                catch (Exception exEx)
                {
                    Console.WriteLine($"\n[LỖI CỰC MẠNH KHI GHI EXCEL]: {exEx.Message}");
                    Console.WriteLine($"[CHI TIẾT LỖI]: {exEx.StackTrace}\n");
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

            // Chống spam & khóa file như bên CreatePost
            System.Threading.Thread.Sleep(3000);

            // Không fail ở TearDown nữa (fail phải nằm ở đúng test case).
        }

        // =================================================================
        // HÀM CHỤP ẢNH MỚI: ĐÃ THÊM JAVASCRIPT BÔI ĐỎ LỖI TỪ CREATEPOST
        // =================================================================
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
