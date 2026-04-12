using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using RaoVat_AutomationTesting.Utilities;
using System;
using System.IO;
using System.Threading;

namespace RaoVat_AutomationTesting.Pages
{
    public class EditPostPage
    {
        private IWebDriver driver;
        private WebDriverWait wait;

        public EditPostPage(IWebDriver driver)
        {
            this.driver = driver;
            this.wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
        }

        // ==========================================================
        // CẬP NHẬT CHUẨN THEO ID TRONG FILE JSX BẠN GỬI
        // ==========================================================
        private By TitleInput => By.Id("name"); // field.name của tiêu đề là 'name'
        private By PriceInput => By.Id("price");
        private By DescriptionInput => By.Id("description");
        private By ConditionSelect => By.Id("condition");
        private By HangSelect => By.Id("hang");
        private By AddressInput => By.Id("streetAddress");
        private By ImageInput => By.Id("imageUpload");
        private By SubmitBtn => By.Id("btn-submit-edit");

        public void FillEditPostForm(CreatePostData data)
        {
            // 1. ĐỢI FORM RENDER XONG (Dùng ID 'name' cực chuẩn)
            IWebElement titleEl;
            try
            {
                titleEl = wait.Until(ExpectedConditions.ElementIsVisible(TitleInput));
            }
            catch (WebDriverTimeoutException)
            {
                throw new Exception("[LỖI]: Trang Edit không load được dữ liệu cũ. Kiểm tra ID bài viết!");
            }

            // 2. XỬ LÝ XÓA ẢNH CŨ
            if (data.Id == "ED11" || data.Id == "ED13" || data.Images > 0)
            {
                var oldImageRemoveBtns = driver.FindElements(By.CssSelector("[id^='btn-remove-old-image-']"));
                foreach (var removeBtn in oldImageRemoveBtns)
                {
                    try { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", removeBtn); Thread.Sleep(200); } catch { }
                }
            }

            // ==========================================================
            // 3. ĐIỀN THÔNG TIN 
            // ==========================================================
            titleEl.Clear();
            if (data.Id == "ED13") // Bypass maxlength
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].removeAttribute('maxlength');", titleEl);
            }
            titleEl.SendKeys(data.Title ?? "");

            // --- KỊCH BẢN RIÊNG CHO ED12: TEST REACT READ-ONLY ---
            if (data.Id == "ED12")
            {
                Console.WriteLine("[LOG-DEBUG] Đang test ED12: Cố tình gỡ disabled và gõ chữ vào Danh mục...");
                var categoryEl = driver.FindElement(By.Id("category"));

                // 1. Dùng JS gỡ disabled
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].removeAttribute('disabled');", categoryEl);
                Thread.Sleep(500);

                // 2. Cố tình gõ giá trị bậy bạ vào
                try { categoryEl.SendKeys("HACKED_CATEGORY_123"); } catch { }

                // 3. Kiểm tra xem chữ có ăn vào ô input không
                string currentValue = categoryEl.GetAttribute("value");
                if (currentValue != null && currentValue.Contains("HACKED"))
                {
                    // Nếu chữ ăn vào được -> Lỗi bảo mật UI
                    throw new Exception("LỖI UI: React không chặn việc nhập liệu sau khi gỡ disabled!");
                }
                else
                {
                    // Nếu chữ không ăn -> React chặn thành công -> Pass!
                    Console.WriteLine("[LOG-DEBUG] Pass UI: React đã chặn việc nhập chữ dù mất thuộc tính disabled.");
                }
            }
            // -------------------------------------------------------

            var priceEl = driver.FindElement(PriceInput);
            priceEl.Clear();
            priceEl.SendKeys(data.Price ?? "");

            var descEl = driver.FindElement(DescriptionInput);
            descEl.Clear();
            descEl.SendKeys(data.Description ?? "");

            // Chọn Dropdown
            SelectStandardDropdown(ConditionSelect, data.Condition);
            SelectStandardDropdown(HangSelect, data.Hang);

            // Địa chỉ cụ thể
            if (!string.IsNullOrEmpty(data.AddressDetail))
            {
                var addr = driver.FindElement(AddressInput);
                addr.Clear();
                addr.SendKeys(data.AddressDetail);
            }

            // 4. UPLOAD ẢNH MỚI
            if (data.Images > 0)
            {
                string filePaths = CreateRealDummyImagesAndGetPaths(data.Images, data.Id);
                driver.FindElement(ImageInput).SendKeys(filePaths);
                Thread.Sleep(3000);
            }

            // ==========================================================
            // 5. BẤM SUBMIT (FIX LỖI CHE KHUẤT & BẮT HTML5)
            // ==========================================================

            // Ẩn banner Firebase nếu có để tránh bị cướp click
            try
            {
                ((IJavaScriptExecutor)driver).ExecuteScript(@"
                    var banner = document.querySelector('.firebase-emulator-warning');
                    if(banner) { banner.style.display = 'none'; }
                ");
                Thread.Sleep(200);
            }
            catch { }

            var submitBtn = driver.FindElement(SubmitBtn);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", submitBtn);
            Thread.Sleep(500);

            if (data.Id == "ED15") // Spam click
            {
                for (int i = 0; i < 5; i++)
                {
                    try { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", submitBtn); } catch { }
                }
            }
            else
            {
                // Dùng Submit hoặc Click nguyên bản để không vượt rào HTML5 Validation của trình duyệt
                try { submitBtn.Submit(); } catch { submitBtn.Click(); }
            }
        }

        private void SelectStandardDropdown(By locator, string? value)
        {
            if (string.IsNullOrEmpty(value)) return;
            try
            {
                var inputEl = wait.Until(ExpectedConditions.ElementExists(locator));
                if (inputEl.TagName.ToLower() == "select")
                {
                    var selectElement = new SelectElement(inputEl);
                    try { selectElement.SelectByValue(value); } catch { selectElement.SelectByText(value); }
                }
                else
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", inputEl);
                    var option = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath($"//*[text()='{value}']")));
                    option.Click();
                }
            }
            catch { }
        }

        private string CreateRealDummyImagesAndGetPaths(int count, string testId)
        {
            string fileName = "hoa.png";
            string relativePath = $@"..\..\..\..\{fileName}";
            string realImagePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath));
            string paths = "";
            for (int i = 0; i < count; i++)
            {
                paths += realImagePath;
                if (i < count - 1) paths += "\n";
            }
            return paths;
        }
    }
}