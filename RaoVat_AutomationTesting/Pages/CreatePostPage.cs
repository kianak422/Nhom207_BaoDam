using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using RaoVat_AutomationTesting.Utilities;
using System;
using System.IO;
using System.Threading;

namespace RaoVat_AutomationTesting.Pages
{
    public class CreatePostPage
    {
        private IWebDriver driver;
        private WebDriverWait wait;

        public CreatePostPage(IWebDriver driver)
        {
            this.driver = driver;
            this.wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
        }

        private By ConditionSelect => By.CssSelector("[data-testid='post-condition-input']");
        private By HangSelect => By.CssSelector("[data-testid='post-hang-input']");
        private By ProvinceSelect => By.CssSelector("[data-testid='post-province-select']");
        private By DistrictSelect => By.CssSelector("[data-testid='post-district-select']");
        private By WardSelect => By.CssSelector("[data-testid='post-ward-select']");
        private By AddressInput => By.CssSelector("[data-testid='post-street-address-input']");
        private By ImageInput => By.CssSelector("[data-testid='post-image-input']");
        private By SubmitBtn => By.CssSelector("[data-testid='post-submit-button']");
        private By GetSpecificFieldLocator(string fieldName) => By.CssSelector($"[data-testid='post-{fieldName}-input']");

        public void FillPostForm(CreatePostData data)
        {
            // 1. ĐỌC DỮ LIỆU TỪ JSON VÀ CHỌN ĐÚNG DANH MỤC
            string cat = string.IsNullOrEmpty(data.Category) ? "do-dien-tu" : data.Category;
            string subCat = string.IsNullOrEmpty(data.SubCategory) ? "dien-thoai" : data.SubCategory;

            By specificCategoryLocator = By.CssSelector($"[data-testid='category-{cat}-{subCat}']");
            IWebElement categoryItem = null;

            try
            {
                var shortWait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                categoryItem = shortWait.Until(ExpectedConditions.ElementExists(specificCategoryLocator));
            }
            catch (WebDriverTimeoutException)
            {
                throw new Exception($"[LỖI CỤ THỂ]: Không tìm thấy danh mục 'category-{cat}-{subCat}' trên web. Bạn hãy kiểm tra lại file JSON nhé!");
            }

            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", categoryItem);
            Thread.Sleep(500);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", categoryItem);

            Thread.Sleep(2000);

            // ==========================================================
            // 2. ĐIỀN THÔNG TIN CHUNG (ĐÃ CHÈN LOGIC CP31: Xóa max-length)
            // ==========================================================
            var titleEl = wait.Until(ExpectedConditions.ElementIsVisible(GetSpecificFieldLocator("name")));
            titleEl.Clear();

            // Nếu là CP31, dùng JS để bẻ khóa giới hạn 100 ký tự của Frontend
            if (data.Id == "CP31")
            {
                Console.WriteLine("[LOG] CP31: Đang dùng JS xóa thuộc tính maxlength của ô Tiêu đề...");
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].removeAttribute('maxlength');", titleEl);
            }

            titleEl.SendKeys(data.Title ?? "");

            var priceEl = driver.FindElement(GetSpecificFieldLocator("price"));
            priceEl.Clear();
            priceEl.SendKeys(data.Price ?? "");

            var descEl = driver.FindElement(GetSpecificFieldLocator("description"));
            descEl.Clear();
            descEl.SendKeys(data.Description ?? "");

            SelectReactDropdown(ConditionSelect, data.Condition);
            SelectReactDropdown(HangSelect, data.Hang);

            SelectStandardDropdown(ProvinceSelect, data.Province);
            SelectStandardDropdown(DistrictSelect, data.District);
            SelectStandardDropdown(WardSelect, data.Ward);

            if (!string.IsNullOrEmpty(data.AddressDetail))
            {
                var addressEl = driver.FindElement(AddressInput);
                addressEl.Clear();
                addressEl.SendKeys(data.AddressDetail);
            }

            // ==========================================================
            // 3. UPLOAD FILE (ĐÃ CHÈN LOGIC CP33: Truyền data.Id để fake file txt)
            // ==========================================================
            if (data.Images > 0)
            {
                string filePaths = CreateRealDummyImagesAndGetPaths(data.Images, data.Id);
                driver.FindElement(ImageInput).SendKeys(filePaths);

                Thread.Sleep(3000);

                try
                {
                    var alertWait = new WebDriverWait(driver, TimeSpan.FromSeconds(1));
                    if (alertWait.Until(ExpectedConditions.AlertIsPresent()) != null) return;
                }
                catch { }
            }

            try
            {
                driver.FindElement(By.TagName("body")).Click();
                Thread.Sleep(1000);
            }
            catch { }

            // ==========================================================
            // 4. BẤM SUBMIT (ĐÃ CHÈN LOGIC CP32: Spam Click)
            // ==========================================================
            var btn = driver.FindElement(SubmitBtn);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", btn);
            Thread.Sleep(1500);

            if (data.Id == "CP32")
            {
                Console.WriteLine("[LOG] CP32: Đang spam click 5 lần liên tục để test duplicate data...");
                for (int i = 0; i < 5; i++)
                {
                    try { btn.Click(); } catch { }
                }
            }
            else
            {
                try
                {
                    wait.Until(ExpectedConditions.ElementToBeClickable(btn)).Click();
                }
                catch (ElementClickInterceptedException)
                {
                    btn.SendKeys(Keys.Enter);
                }
                catch (Exception)
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", btn);
                }
            }

            Console.WriteLine("[LOG] Đã bấm Submit. Bắt đầu theo dõi kết quả...");
            Thread.Sleep(1000);
        }

        private void SelectStandardDropdown(By locator, string? value)
        {
            if (string.IsNullOrEmpty(value)) return;
            try
            {
                var element = wait.Until(ExpectedConditions.ElementExists(locator));
                wait.Until(d => new SelectElement(d.FindElement(locator)).Options.Count > 1);
                var selectElement = new SelectElement(element);
                try { selectElement.SelectByValue(value); } catch { selectElement.SelectByText(value); }
                Thread.Sleep(500);
            }
            catch { }
        }

        private void SelectReactDropdown(By locator, string? value)
        {
            if (string.IsNullOrEmpty(value)) return;
            try
            {
                var inputEl = driver.FindElement(locator);
                if (inputEl.TagName.ToLower() == "select")
                {
                    var selectElement = new SelectElement(inputEl);
                    selectElement.SelectByText(value);
                    return;
                }

                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", inputEl);
                Thread.Sleep(200);
                inputEl.Click();
                Thread.Sleep(500);

                var option = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath($"//*[text()='{value}']")));
                option.Click();
                Thread.Sleep(300);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cảnh báo Dropdown] Không thể chọn {value}. Lỗi: {ex.Message}");
            }
        }

        // ==========================================================
        // CẬP NHẬT HÀM UPLOAD: Hỗ trợ tạo file giả mạo cho case CP33
        // ==========================================================
        private string CreateRealDummyImagesAndGetPaths(int count, string testId)
        {
            // Nếu testId là CP33, ta sẽ chọn up file giả mạo (.txt), ngược lại up file hoa.png bình thường
            string fileName = (testId == "CP33") ? "fake_virus.txt" : "hoa.png";
            string relativePath = $@"..\..\..\..\{fileName}";
            string realImagePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath));

            // Tự động tạo một file text ảo nếu hệ thống chưa có file fake_virus.txt
            if (testId == "CP33" && !File.Exists(realImagePath))
            {
                File.WriteAllText(realImagePath, "This is a fake executable file to test bypass upload filter.");
            }

            string paths = "";
            for (int i = 0; i < count; i++)
            {
                paths += realImagePath;
                if (i < count - 1) paths += "\n";
            }
            return paths;
        }

        public string GetReactErrorMessage()
        {
            try
            {
                Thread.Sleep(500);
                var errorElements = driver.FindElements(By.CssSelector("p[class*='errorText']"));
                foreach (var el in errorElements)
                {
                    if (!string.IsNullOrEmpty(el.Text)) return el.Text;
                }
                return "";
            }
            catch { return ""; }
        }
    }
}