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

        private void SendKeysRobust(IWebElement element, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            try
            {
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                js.ExecuteScript(@"
                    var el = arguments[0];
                    var val = arguments[1];
                    var nativeInputValueSetter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
                    var nativeTextAreaValueSetter = Object.getOwnPropertyDescriptor(window.HTMLTextAreaElement.prototype, 'value').set;
                    if (el.tagName.toLowerCase() === 'textarea') {
                        nativeTextAreaValueSetter.call(el, val);
                    } else {
                        nativeInputValueSetter.call(el, val);
                    }
                    el.dispatchEvent(new Event('input', { bubbles: true }));
                    el.dispatchEvent(new Event('change', { bubbles: true }));
                ", element, text);
            }
            catch
            {
                element.SendKeys(text);
            }
        }

        // ========== METHOD RIÊNG CHO CP20 - TRẢ VỀ STRING THỰC TẾ ==========
        public string ValidateCP20_WardDropdownEnabled(CreatePostData data)
        {
            string cat = string.IsNullOrEmpty(data.Category) ? "do-dien-tu" : data.Category;
            string subCat = string.IsNullOrEmpty(data.SubCategory) ? "dien-thoai" : data.SubCategory;
            By specificCategoryLocator = By.CssSelector($"[data-testid='category-{cat}-{subCat}']");

            var categoryItem = wait.Until(ExpectedConditions.ElementExists(specificCategoryLocator));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", categoryItem);
            Thread.Sleep(500);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", categoryItem);
            Thread.Sleep(2000);

            SelectStandardDropdown(ProvinceSelect, "79");
            Thread.Sleep(1000);

            SelectStandardDropdown(DistrictSelect, "760");
            Thread.Sleep(1000);

            // Bắt đầu quét thực tế từ DOM
            bool isWardEnabled = false;
            try
            {
                wait.Until(d => d.FindElement(WardSelect).Enabled);
                isWardEnabled = true;
            }
            catch { }

            // Lấy trạng thái thực tế gán vào chuỗi
            string actualState = isWardEnabled
                ? "Thực tế UI: Dropdown Phường/Xã đã được mở khóa (Enabled = True)"
                : "Thực tế UI: Dropdown Phường/Xã vẫn bị mờ (Enabled = False)";

            if (!isWardEnabled)
            {
                throw new Exception(actualState);
            }

            return actualState; // Trả về chuỗi kết quả thực tế để log ra file
        }

        // ========== METHOD RIÊNG CHO CP22 - TRẢ VỀ STRING THỰC TẾ ==========
        // ========== METHOD RIÊNG CHO CP22 - BẮT DOM SIÊU NHẠY & FIX LỖI EVENT REACT ==========
        public string ValidateCP22_DropdownReset(CreatePostData data)
        {
            // 1. Chờ load danh mục
            string cat = string.IsNullOrEmpty(data.Category) ? "do-dien-tu" : data.Category;
            string subCat = string.IsNullOrEmpty(data.SubCategory) ? "dien-thoai" : data.SubCategory;
            By specificCategoryLocator = By.CssSelector($"[data-testid='category-{cat}-{subCat}']");

            var categoryItem = wait.Until(ExpectedConditions.ElementExists(specificCategoryLocator));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", categoryItem);
            Thread.Sleep(500);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", categoryItem);
            Thread.Sleep(2000);

            // 2. Hard-code chọn Tỉnh -> Huyện -> Xã
            SelectStandardDropdown(ProvinceSelect, "79"); // Chọn HCM
            Thread.Sleep(1000);
            SelectStandardDropdown(DistrictSelect, "760"); // Chọn Quận 1
            Thread.Sleep(1000);
            SelectStandardDropdown(WardSelect, "Phường Bến Nghé"); // Chọn Phường Bến Nghé
            Thread.Sleep(1000);

            // =======================================================
            // 3. ĐỔI TỈNH (HÀNH ĐỘNG CỐT LÕI) - ÉP REACT NHẬN SỰ KIỆN
            // =======================================================
            // =======================================================
            // 3. ĐỔI TỈNH (CÁCH TIẾP CẬN MỚI: ÉP REACT TRIGGER)
            // =======================================================
            Console.WriteLine("[LOG] CP22: Đang ép React thực hiện hành động đổi Tỉnh...");
            var provElement = driver.FindElement(ProvinceSelect);

            ((IJavaScriptExecutor)driver).ExecuteScript(@"
                var el = arguments[0];
                var val = '01'; // Mã Hà Nội

                // 1. Tìm hàm Setter gốc của HTMLSelectElement (React thường chặn cái này)
                var nativeSelectValueSetter = Object.getOwnPropertyDescriptor(window.HTMLSelectElement.prototype, 'value').set;
    
                // 2. Ép Setter thực thi giá trị mới
                nativeSelectValueSetter.call(el, val);

                // 3. Bắn một chuỗi sự kiện để 'đánh thức' các hàm onChange của React
                el.dispatchEvent(new Event('change', { bubbles: true }));
                el.dispatchEvent(new Event('input', { bubbles: true }));
            ", provElement);

            Console.WriteLine("[LOG] Đã ép đổi Tỉnh xong. Chờ React xử lý logic reset...");
            Thread.Sleep(2000); // Cho React 2 giây để chạy hàm reset Quận/Huyện ngầm

            // BƯỚC B: Bắn event trực tiếp bằng JavaScript để trị dứt điểm React
            // Hành động này ép React phải chạy hàm onChange() và reset Quận/Huyện
            ((IJavaScriptExecutor)driver).ExecuteScript(@"
                var el = arguments[0];
                el.value = '01';
                el.dispatchEvent(new Event('input', { bubbles: true }));
                el.dispatchEvent(new Event('change', { bubbles: true }));
            ", provElement);

            Console.WriteLine("[LOG] Đã bắn sự kiện đổi Tỉnh xong. Bắt đầu quét DOM...");

            // 4. Vòng lặp quét DOM siêu nhạy (Chờ tối đa 5 giây)
            string actualDistText = "";
            string actualWardText = "";
            string actualDistValue = "";
            string actualWardValue = "";
            bool isResetSuccess = false;

            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(500);
                try
                {
                    var distSelect = new SelectElement(driver.FindElement(DistrictSelect));
                    var wardSelect = new SelectElement(driver.FindElement(WardSelect));

                    var distOpt = distSelect.SelectedOption;
                    var wardOpt = wardSelect.SelectedOption;

                    actualDistText = distOpt.Text.Trim();
                    actualWardText = wardOpt.Text.Trim();
                    actualDistValue = distOpt.GetAttribute("value")?.Trim() ?? "";
                    actualWardValue = wardOpt.GetAttribute("value")?.Trim() ?? "";

                    // ĐIỀU KIỆN PASS: Bất kỳ cái nào (text hoặc value) là rỗng, HOẶC chứa chữ "chọn"
                    bool isDistReset = string.IsNullOrEmpty(actualDistValue) || string.IsNullOrEmpty(actualDistText) || actualDistText.ToLower().Contains("chọn");
                    bool isWardReset = string.IsNullOrEmpty(actualWardValue) || string.IsNullOrEmpty(actualWardText) || actualWardText.ToLower().Contains("chọn");

                    if (isDistReset && isWardReset)
                    {
                        isResetSuccess = true;
                        break;
                    }
                }
                catch (StaleElementReferenceException) { continue; }
                catch (NoSuchElementException) { continue; }
            }

            string actualState = $"Huyện: [Text: '{actualDistText}', Value: '{actualDistValue}'] | Xã: [Text: '{actualWardText}', Value: '{actualWardValue}']";

            if (!isResetSuccess)
            {
                throw new Exception($"Lỗi UI: Đã đổi Tỉnh nhưng thẻ Select không tự động reset rỗng. Thực tế hiển thị -> {actualState}");
            }

            return $"Đã reset rỗng Dropdown thành công. {actualState}";
        }

        public void FillPostForm(CreatePostData data)
        {
            string cat = string.IsNullOrEmpty(data.Category) ? "do-dien-tu" : data.Category;
            string subCat = string.IsNullOrEmpty(data.SubCategory) ? "dien-thoai" : data.SubCategory;
            By specificCategoryLocator = By.CssSelector($"[data-testid='category-{cat}-{subCat}']");

            var categoryItem = wait.Until(ExpectedConditions.ElementExists(specificCategoryLocator));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", categoryItem);
            Thread.Sleep(500);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", categoryItem);
            Thread.Sleep(2000);

            var titleEl = wait.Until(ExpectedConditions.ElementIsVisible(GetSpecificFieldLocator("name")));
            titleEl.Clear();
            if (data.Id == "CP31")
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].removeAttribute('maxlength');", titleEl);
            }
            SendKeysRobust(titleEl, data.Title ?? "");

            var priceEl = driver.FindElement(GetSpecificFieldLocator("price"));
            priceEl.Clear();
            SendKeysRobust(priceEl, data.Price ?? "");

            var descEl = driver.FindElement(GetSpecificFieldLocator("description"));
            descEl.Clear();
            SendKeysRobust(descEl, data.Description ?? "");

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

            if (data.Images > 0)
            {
                string filePaths = CreateRealDummyImagesAndGetPaths(data.Images, data.Id);
                driver.FindElement(ImageInput).SendKeys(filePaths);
                Thread.Sleep(3000);
                try { wait.Until(ExpectedConditions.AlertIsPresent()); return; } catch { }
            }

            try { driver.FindElement(By.TagName("body")).Click(); Thread.Sleep(1000); } catch { }

            var btn = driver.FindElement(SubmitBtn);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", btn);
            Thread.Sleep(1500);

            if (data.Id == "CP32")
            {
                for (int i = 0; i < 5; i++) { try { btn.Click(); } catch { } }
            }
            else
            {
                try { wait.Until(ExpectedConditions.ElementToBeClickable(btn)).Click(); }
                catch (ElementClickInterceptedException) { btn.SendKeys(Keys.Enter); }
                catch (Exception) { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", btn); }
            }
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
            catch { }
        }

        private string CreateRealDummyImagesAndGetPaths(int count, string testId)
        {
            string fileName = "hoa.png";
            if (testId == "CP33") fileName = "fake_virus.txt";
            else if (testId == "CP17") fileName = "20mb.png";
            else if (testId == "CP27") fileName = "Ảnh Đại Diện_Sếp@!.🔥_2026.png";

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

        public string GetReactErrorMessage()
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
                    return '';
                ") ?? "";
            }
            catch { return ""; }
        }
    }
}