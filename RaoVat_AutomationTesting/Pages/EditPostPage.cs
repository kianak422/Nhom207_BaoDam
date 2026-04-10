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
        // CẬP NHẬT LẠI LOCATOR DỰA TRÊN SOURCE CODE REACT (Dùng Name thay vì data-testid)
        // ==========================================================
        private By ConditionSelect => By.Name("condition");
        private By HangSelect => By.Name("hang");
        private By AddressInput => By.Id("streetAddress");
        private By ImageInput => By.Id("imageUpload");
        private By SubmitBtn => By.Id("btn-submit-edit");

        private By GetSpecificFieldLocator(string fieldName) => By.Name(fieldName);

        public void FillEditPostForm(CreatePostData data)
        {
            // 1. ĐỢI FORM RENDER XONG (Đợi tiêu đề xuất hiện)
            IWebElement titleEl;
            try
            {
                var shortWait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                titleEl = shortWait.Until(ExpectedConditions.ElementIsVisible(GetSpecificFieldLocator("name")));
            }
            catch (WebDriverTimeoutException)
            {
                throw new Exception("[LỖI API/MẠNG]: Trang Edit không load được dữ liệu cũ của bài viết (Hoặc ID bài viết không tồn tại).");
            }

            // ==========================================================
            // 2. XỬ LÝ ẢNH CŨ (Đặc thù của màn hình Edit)
            // ==========================================================
            // Nếu data.Images = 0 (ED11, ED13) hoặc có up ảnh mới đè lên, ta dọn dẹp ảnh cũ trước
            if (data.Id == "ED11" || data.Id == "ED13" || data.Images > 0)
            {
                Console.WriteLine("[LOG] Đang tiến hành xóa các ảnh cũ trên giao diện...");
                var oldImageRemoveBtns = driver.FindElements(By.CssSelector("[id^='btn-remove-old-image-']"));

                foreach (var removeBtn in oldImageRemoveBtns)
                {
                    try
                    {
                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", removeBtn);
                        Thread.Sleep(300);
                    }
                    catch { }
                }
            }

            // ==========================================================
            // 3. ĐIỀN THÔNG TIN CHUNG VÀ CÁC TRƯỜNG CỤ THỂ
            // ==========================================================
            titleEl.Clear();

            // Logic bẻ khóa độ dài Tiêu đề (ED12, ED13 trong JSON mới)
            if (data.Id == "ED12" || data.Id == "ED13")
            {
                Console.WriteLine($"[LOG] {data.Id}: Dùng JS bẻ khóa thuộc tính maxlength của ô Tiêu đề...");
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].removeAttribute('maxlength');", titleEl);
            }

            titleEl.SendKeys(data.Title ?? "");

            var priceEl = driver.FindElement(By.Name("price"));
            priceEl.Clear();
            priceEl.SendKeys(data.Price ?? "");

            var descEl = driver.FindElement(By.Name("description"));
            descEl.Clear();
            descEl.SendKeys(data.Description ?? "");

            // Chọn Dropdown bằng Name
            SelectStandardDropdown(ConditionSelect, data.Condition);
            SelectStandardDropdown(HangSelect, data.Hang);

            // Bỏ chọn Province/District/Ward vì UI đã khóa (disabled id="region")
            // Cập nhật địa chỉ đường (Thường vẫn mở)
            try
            {
                if (!string.IsNullOrEmpty(data.AddressDetail))
                {
                    var addressEl = driver.FindElement(AddressInput);
                    addressEl.Clear();
                    addressEl.SendKeys(data.AddressDetail);
                }
            }
            catch { }

            // ==========================================================
            // 4. UPLOAD ẢNH MỚI
            // ==========================================================
            if (data.Images > 0)
            {
                string filePaths = CreateRealDummyImagesAndGetPaths(data.Images, data.Id);
                driver.FindElement(ImageInput).SendKeys(filePaths);

                // Chờ Firebase xử lý upload
                Thread.Sleep(3000);

                try
                {
                    var alertWait = new WebDriverWait(driver, TimeSpan.FromSeconds(1));
                    if (alertWait.Until(ExpectedConditions.AlertIsPresent()) != null) return;
                }
                catch { }
            }

            // Kích hoạt onBlur tính toán của form
            try
            {
                driver.FindElement(By.TagName("body")).Click();
                Thread.Sleep(1000);
            }
            catch { }

            // ==========================================================
            // 5. BẤM SUBMIT LƯU THAY ĐỔI
            // ==========================================================
            var submitBtn = driver.FindElement(SubmitBtn);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", submitBtn);
            Thread.Sleep(1500);

            // Spam click (ED15 trong JSON mới)
            if (data.Id == "ED15")
            {
                Console.WriteLine("[LOG] Đang spam click nhiều lần liên tục...");
                for (int i = 0; i < 5; i++)
                {
                    try { submitBtn.Click(); } catch { }
                }
            }
            else
            {
                try
                {
                    wait.Until(ExpectedConditions.ElementToBeClickable(submitBtn)).Click();
                }
                catch (ElementClickInterceptedException)
                {
                    submitBtn.SendKeys(Keys.Enter);
                }
                catch (Exception)
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", submitBtn);
                }
            }

            Console.WriteLine("[LOG] Đã bấm Lưu thay đổi. Chờ chuyển hướng/alert...");
            Thread.Sleep(1000);
        }

        // ==========================================================
        // CÁC HÀM TIỆN ÍCH DÙNG CHUNG
        // ==========================================================
        private void SelectStandardDropdown(By locator, string? value)
        {
            if (string.IsNullOrEmpty(value)) return;
            try
            {
                var inputEl = wait.Until(ExpectedConditions.ElementExists(locator));

                // Thẻ Select trong React của bạn là <select> chuẩn
                if (inputEl.TagName.ToLower() == "select")
                {
                    var selectElement = new SelectElement(inputEl);
                    try { selectElement.SelectByValue(value); } catch { selectElement.SelectByText(value); }
                }
                else
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", inputEl);
                    Thread.Sleep(200);
                    inputEl.Click();
                    Thread.Sleep(500);
                    var option = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath($"//*[text()='{value}']")));
                    option.Click();
                }
                Thread.Sleep(300);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cảnh báo Dropdown Edit] Bỏ qua chọn '{value}'. Lỗi hoặc thẻ đã bị disabled: {ex.Message}");
            }
        }

        private string CreateRealDummyImagesAndGetPaths(int count, string testId)
        {
            string fileName = "hoa.png";
            // Thêm case upload file độc hại nếu sau này bạn mở rộng JSON
            if (testId == "CP33") fileName = "fake_virus.txt";

            string relativePath = $@"..\..\..\..\{fileName}";
            string realImagePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath));

            if (testId == "CP33" && !File.Exists(realImagePath))
            {
                File.WriteAllText(realImagePath, "Fake executable content");
            }

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