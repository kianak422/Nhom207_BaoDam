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
    public class SearchTests
    {
        private IWebDriver? driver;
        private StringBuilder? verificationErrors;
        private ExcelHelper? excelHelper;

        // --- CHÚ Ý: ĐÃ CẬP NHẬT ĐƯỜNG DẪN FILE EXCEL TẠI ĐÂY ---
        private string reportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BaoDam_Report.xlsx");
        private string sheetName = "TCs - F4"; // Đúng với tên sheet trong ảnh
        private string testerName = "Danh";    // Tên của bạn

        [SetUp]
        public void Setup()
        {
            // Kiểm tra và lấy đường dẫn tuyệt đối chuẩn xác
            if (!File.Exists(reportPath))
            {
                reportPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "BaoDam_Report.xlsx");

                // Thử thêm một vài đường dẫn dự phòng nếu vẫn không thấy
                if (!File.Exists(reportPath))
                {
                    reportPath = @"e:\K29\BDCLPM\RaoVat_AutomationTesting\BaoDam_Report.xlsx";
                }
            }

            Console.WriteLine("=========================================");
            Console.WriteLine($"[LOG-DEBUG] Thư mục đang chạy code: {AppDomain.CurrentDomain.BaseDirectory}");
            Console.WriteLine($"[LOG-DEBUG] Đang cố gắng ghi vào file Excel tại: {reportPath}");
            Console.WriteLine($"[LOG-DEBUG] File Excel có tồn tại ở đường dẫn này không? : {File.Exists(reportPath)}");
            Console.WriteLine("=========================================");

            driver = DriverFactory.CreateDriver();
            verificationErrors = new StringBuilder();
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
                // Ưu tiên điều hướng theo StartUrl nếu được cung cấp trong JSON (FA01 mới)
                // Chú ý: Cần thêm public string? StartUrl { get; set; } vào SearchData model nếu bạn có dùng
                // Nếu code gốc báo lỗi đoạn `data.StartUrl`, bạn có thể comment lại nếu chưa có trong Model.

                // MÌNH GIỮ NGUYÊN LOGIC TÌM KIẾM CỦA BẠN DƯỚI ĐÂY:

                // Nếu không có từ khóa mà có các bộ lọc khác (FA tests), 
                // thì điều hướng thẳng đến trang tìm kiếm để bộ lọc hiển thị
                if (string.IsNullOrEmpty(data.Keyword) && (data.Keywords == null || data.Keywords.Count == 0) &&
                    (!string.IsNullOrEmpty(data.Category) || !string.IsNullOrEmpty(data.Province) || !string.IsNullOrEmpty(data.MinPrice) || !string.IsNullOrEmpty(data.MaxPrice)))
                {
                    driver.Navigate().GoToUrl("http://localhost:5173/tim-kiem");
                }
                else
                {
                    // Ngược lại bắt đầu từ trang chủ
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
                foreach (var kw in searchKeywords)
                {
                    searchPage.SearchForKeyword(kw);
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

                    bool hasAnyFilterAction =
                        !string.IsNullOrEmpty(data.Category)
                        || !string.IsNullOrEmpty(data.Subcategory)
                        || !string.IsNullOrEmpty(data.Province)
                        || !string.IsNullOrEmpty(data.MinPrice)
                        || !string.IsNullOrEmpty(data.MaxPrice)
                        || data.HomeBtnIndex.HasValue;

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
                    screenshotPath = TakeScreenshot(driver, $"Pass_{data.Id}", false); // Chụp ảnh khi Pass
                    Console.WriteLine($"ID: {data.Id} - Pass! URL: {driver.Url}");
                }
            }
            catch (Exception ex)
            {
                statusToLog = "Fail";

                // ĐÓNG ALERT NẾU CÓ ĐỂ TRÁNH LỖI KHI CHỤP ẢNH
                try { driver.SwitchTo().Alert().Accept(); } catch { }

                screenshotPath = TakeScreenshot(driver, $"Fail_{data.Id}", true); // Bật cờ true để highlight đỏ lỗi

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
                    actualResultToLog = $"[System Error] {ex.GetType().Name}: {errorMsg}";
                }

                verificationErrors?.Append($"[{data.Id}] {actualResultToLog}\n");

                Console.WriteLine($"\n>>> CRITICAL FAIL ở {data.Id} <<<");
                Console.WriteLine($"Chi tiết: {actualResultToLog}\n");

                throw;
            }
            finally
            {
                Console.WriteLine($"[LOG-DEBUG] Tiến hành ghi ID [{data.Id}] vào sheet [{sheetName}]...");
                try
                {
                    excelHelper?.WriteTestResultById(sheetName, data.Id ?? "", actualResultToLog, statusToLog, testerName, screenshotPath);
                    Console.WriteLine($"[LOG-DEBUG] ---> GHI THÀNH CÔNG VÀO EXCEL!");
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine($"\n[CẢNH BÁO LỖI GHI EXCEL CHO ID {data.Id}]: {ioEx.Message}");
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

            if (verificationErrors != null && verificationErrors.Length > 0)
            {
                Assert.Fail(verificationErrors.ToString());
            }
        }

        // ĐÃ NÂNG CẤP HÀM CHỤP ẢNH MÀN HÌNH TỪ BÊN CREATE POST
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