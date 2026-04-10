using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace RaoVat_AutomationTesting.Pages
{
    public class SearchPage
    {
        private IWebDriver driver;
        private WebDriverWait wait;

        public SearchPage(IWebDriver driver)
        {
            this.driver = driver;
            this.wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        // Locators cho Tìm kiếm
        private By SearchInput => By.XPath("//input[@placeholder='Tìm kiếm trên Chợ Tốt'] | //input[@type='text'] | //input");
        private By SearchButton => By.XPath("(.//*[normalize-space(text()) and normalize-space(.)='Toàn quốc'])[1]/following::*[name()='svg'][3]");
        private By SubmitButton => By.XPath("//button[@type='submit']");
        private By CssSearchButton => By.CssSelector("button._searchButton_a6qzp_97");

        // Locators cho Bộ lọc (FA tests)
        private By FilterModalBtn => By.XPath("//button[contains(text(), 'Lọc')] | //div[@id='root']/div/main/div/div/button | //button[.//svg]");
        private By ProvinceSelect => By.XPath("//select[contains(@class, 'province')] | //div[@id='root']/div/main/div/div[2]/div/div[2]/div/div/select");
        private By DistrictSelect => By.XPath("//select[contains(@class, 'district')] | //div[@id='root']/div/main/div/div[2]/div/div[2]/div[2]/div/select");
        private By ClearModalBtn => By.XPath("//div[@id='root']/div/main/div/div[2]/div/div[3]/button");
        private By ApplyModalBtn => By.XPath("//button[contains(text(), 'Áp dụng')] | //div[@id='root']/div/main/div/div[2]/div/div[3]/button[2]");
        private By ApplyFilterBtn => By.XPath("//button[text()='Áp dụng'] | //button[contains(text(), 'Áp dụng')] | //div[@id='root']/div/main/div/div/button[2]");
        private By MinPriceInput => By.XPath("//input[@placeholder='Giá tối thiểu'] | //div[@id='root']/div/main/div/div/input");
        private By MaxPriceInput => By.XPath("//input[@placeholder='Giá tối đa'] | //div[@id='root']/div/main/div/div/input[2]");
        private By CategorySelect => By.XPath("//select[contains(@class, 'category')] | //select[@name='category'] | //select[contains(@id,'category')]");
        private By SubcategorySelect => By.XPath("//select[contains(@class, 'subcategory')] | //div[@id='root']/div/main/div/div/select");
        private By ResetFilterBtn => By.XPath("//div[@id='root']/div/main/div/div/button[3]");

        public void SearchForKeyword(string keyword)
        {
            // Tương ứng logic project: Đảm bảo ô input sẵn sàng
            var input = wait.Until(ExpectedConditions.ElementExists(SearchInput));
            
            // Xử lý click để focus
            try {
                input.Click();
            } catch { JsClick(input); }

            // Xóa trắng
            input.Clear();

            // Nhập từ khóa
            if (!string.IsNullOrEmpty(keyword))
            {
                input.SendKeys(keyword);
                System.Threading.Thread.Sleep(1000); // Chờ gợi ý hiện ra ổn định
                
                // Thử nhấn Enter trước
                try {
                    input.SendKeys(Keys.Enter);
                    
                    // Đóng gợi ý nếu vẫn còn mở
                    System.Threading.Thread.Sleep(500);
                    input.SendKeys(Keys.Escape);
                    System.Threading.Thread.Sleep(500);

                    // Nếu URL đã thay đổi (không còn ở trang chủ hoặc đã có query q=) thì dừng
                    if (driver.Url.Contains("q=") || !driver.Url.EndsWith(":5173/")) return;
                } catch { /* Bỏ qua nếu lỗi Enter */ }
            }
            
            // Thực hiện click nút tìm kiếm (thử nhiều phương án)
            try {
                var btn = wait.Until(ExpectedConditions.ElementToBeClickable(SearchButton));
                btn.Click();
            } catch {
                try {
                    var btn = driver.FindElement(SearchButton);
                    JsClick(btn);
                } catch {
                    try {
                        var btn = driver.FindElement(SubmitButton);
                        JsClick(btn);
                    } catch {
                        input.Submit();
                    }
                }
            }
            System.Threading.Thread.Sleep(1000); // Đợi trang kết quả load
        }

        public bool FilterByCategory(string categoryName)
        {
            var normalizedTarget = NormalizeText(categoryName);
            bool selected = false;

            // Chiến lược 1: link text khớp hoàn toàn.
            try
            {
                var categoryLink = wait.Until(ExpectedConditions.ElementToBeClickable(By.LinkText(categoryName)));
                categoryLink.Click();
                selected = true;
            }
            catch { }

            // Chiến lược 2: click theo alt của ảnh.
            if (!selected)
            {
                try
                {
                    var img = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath($"//img[@alt='{categoryName}' or contains(@alt,'{categoryName}')]")));
                    img.Click();
                    selected = true;
                }
                catch { }
            }

            // Chiến lược 3: dò các phần tử có text để match "gần đúng" (không phân biệt dấu/hoa-thường).
            if (!selected)
            {
                try
                {
                    var categoryEl = wait.Until(ExpectedConditions.ElementIsVisible(CategorySelect));
                    var select = new SelectElement(categoryEl);
                    wait.Until(_ => select.Options.Count > 1);

                    selected = TrySelectOptionByApproxText(select, categoryName, normalizedTarget);
                }
                catch { }
            }

            // Chiến lược 4: dò text tự do trên UI.
            if (!selected)
            {
                var candidates = driver.FindElements(By.XPath("//a | //button | //label | //div | //span | //h1 | //h2 | //h3 | //p"));
                foreach (var candidate in candidates)
                {
                    try
                    {
                        var text = (candidate.Text ?? string.Empty).Trim();
                        if (string.IsNullOrEmpty(text)) continue;

                        var normalizedText = NormalizeText(text);
                        if (normalizedText.Contains(normalizedTarget) || normalizedTarget.Contains(normalizedText))
                        {
                            try { candidate.Click(); } catch { JsClick(candidate); }
                            selected = true;
                            break;
                        }
                    }
                    catch
                    {
                        // Bỏ qua element stale/không tương tác được.
                    }
                }
            }

            if (!selected)
            {
                // Không fail cứng để các bước lọc khác vẫn tiếp tục được.
                Console.WriteLine($"[WARN] Không tìm thấy category tương ứng: {categoryName}. Bỏ qua bước chọn category.");
                return false;
            }

            ApplyFilter();
            return true;
        }

        private string NormalizeText(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            string normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (char c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Replace('đ', 'd').Replace("  ", " ").Trim();
        }

        private bool TrySelectOptionByApproxText(SelectElement select, string rawCategoryName, string normalizedTarget)
        {
            try
            {
                select.SelectByText(rawCategoryName);
                return true;
            }
            catch { }

            foreach (var option in select.Options)
            {
                string optionText = (option.Text ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(optionText)) continue;

                string normalizedOption = NormalizeText(optionText);
                if (normalizedOption.Contains(normalizedTarget) || normalizedTarget.Contains(normalizedOption))
                {
                    select.SelectByText(optionText);
                    return true;
                }
            }

            return false;
        }

        public void SelectSubcategory(string subcategoryName)
        {
            try {
                var subEl = wait.Until(ExpectedConditions.ElementIsVisible(SubcategorySelect));
                var select = new SelectElement(subEl);
                
                // Đợi dropdown có dữ liệu
                wait.Until(d => select.Options.Count > 1);
                select.SelectByText(subcategoryName);
                ApplyFilter();
            } catch (NoSuchElementException) {
                // Bỏ qua nếu không có subcategory
            }
        }

        public void ClearFilterInModal()
        {
            var clearBtn = wait.Until(ExpectedConditions.ElementToBeClickable(ClearModalBtn));
            try { clearBtn.Click(); } catch { JsClick(clearBtn); }
        }

        public void FilterByLocation(string province, string? district = null, bool clearFirst = false)
        {
            // Đóng gợi ý nếu đang mở để tránh bị chặn click
            try { driver.FindElement(By.TagName("body")).SendKeys(Keys.Escape); } catch { }

            var filterBtn = wait.Until(ExpectedConditions.ElementExists(FilterModalBtn));
            try { filterBtn.Click(); } catch { JsClick(filterBtn); }

            if (clearFirst)
            {
                ClearFilterInModal();
            }

            var provinceEl = wait.Until(ExpectedConditions.ElementIsVisible(ProvinceSelect));
            var selectProvince = new SelectElement(provinceEl);
            
            // Đợi dropdown tỉnh thành load dữ liệu
            wait.Until(d => selectProvince.Options.Count > 1);
            
            try {
                selectProvince.SelectByText(province);
            } catch (NoSuchElementException) {
                bool selected = false;
                string cleanProvince = province.Replace("Thành phố ", "").Replace("TP. ", "").Replace("TP ", "");
                foreach (var option in selectProvince.Options) {
                    if (option.Text.Contains(cleanProvince)) {
                        selectProvince.SelectByText(option.Text);
                        selected = true;
                        break;
                    }
                }
                if (!selected) throw;
            }

            if (!string.IsNullOrEmpty(district))
            {
                var districtEl = wait.Until(ExpectedConditions.ElementIsVisible(DistrictSelect));
                var selectDistrict = new SelectElement(districtEl);
                
                // Đợi dropdown quận huyện load dữ liệu dựa trên tỉnh đã chọn
                wait.Until(d => selectDistrict.Options.Count > 1);

                try {
                    selectDistrict.SelectByText(district);
                } catch (NoSuchElementException) {
                    bool selected = false;
                    string cleanDistrict = district.Replace("Quận ", "").Replace("Huyện ", "");
                    foreach (var option in selectDistrict.Options) {
                        if (option.Text.Contains(cleanDistrict)) {
                            selectDistrict.SelectByText(option.Text);
                            selected = true;
                            break;
                        }
                    }
                    if (!selected) throw;
                }
            }

            var applyBtn = wait.Until(ExpectedConditions.ElementExists(ApplyModalBtn));
            try { applyBtn.Click(); } catch { JsClick(applyBtn); }
            
            ApplyFilter();
        }

        public void FilterByPrice(string minPrice, string maxPrice)
        {
            if (minPrice != null) // Cho phép chuỗi rỗng để test xóa giá
            {
                var minEl = wait.Until(ExpectedConditions.ElementIsVisible(MinPriceInput));
                minEl.Clear();
                if (!string.IsNullOrEmpty(minPrice)) minEl.SendKeys(minPrice);
            }

            if (maxPrice != null)
            {
                var maxEl = wait.Until(ExpectedConditions.ElementIsVisible(MaxPriceInput));
                maxEl.Clear();
                if (!string.IsNullOrEmpty(maxPrice)) maxEl.SendKeys(maxPrice);
            }

            ApplyFilter();
        }

        public void ApplyFilter()
        {
            System.Threading.Thread.Sleep(800); // Tăng độ trễ để UI cập nhật các lựa chọn
            try {
                // Ưu tiên tìm nút có chữ "Áp dụng" vì nó tường minh nhất
                var btn = wait.Until(ExpectedConditions.ElementExists(ApplyFilterBtn));
                try { 
                    wait.Until(ExpectedConditions.ElementToBeClickable(btn)).Click(); 
                } catch { 
                    JsClick(btn); 
                }
                System.Threading.Thread.Sleep(500); // Chờ hiệu ứng chuyển trang bắt đầu
            } catch { 
                // Nếu không thấy nút sau khi chờ, có thể nó dùng locator khác hoặc đã tự apply
                Console.WriteLine("[DEBUG] Không tìm thấy nút Áp dụng, thử phương án dự phòng...");
                try {
                    var fallbackBtn = driver.FindElement(By.XPath("//div[@id='root']/div/main/div/div/button[2]"));
                    JsClick(fallbackBtn);
                } catch { /* Bỏ qua nếu không còn phương án nào */ }
            }
        }

        private void JsClick(IWebElement element)
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", element);
        }

        public void ClickHomeButton(int index = 1)
        {
            By locator = index == 1 
                ? By.XPath("//div[@id='root']/div/main/div/div[2]/div/button")
                : By.XPath($"//div[@id='root']/div/main/div/div[2]/div/button[{index}]");
            
            var btn = wait.Until(ExpectedConditions.ElementToBeClickable(locator));
            btn.Click();
        }

        public void ResetFilter()
        {
            var btn = wait.Until(ExpectedConditions.ElementToBeClickable(ResetFilterBtn));
            try { btn.Click(); } catch { JsClick(btn); }
        }
    }
}
