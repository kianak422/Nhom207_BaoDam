using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Threading;

namespace RaoVat_AutomationTesting.Pages
{
    public class IntegratePage
    {
        private IWebDriver driver;
        private WebDriverWait wait;

        public IntegratePage(IWebDriver driver)
        {
            this.driver = driver;
            this.wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
        }

        // ==========================================================
        // LOCATORS CHUNG CHO CÁC LUỒNG TÍCH HỢP
        // ==========================================================
        private By FirstProductCard => By.CssSelector("div[class*='productCard'] a");
        private By FirstHeartIcon => By.CssSelector("div[class*='productCard'] button[class*='heart'], div[class*='productCard'] svg[class*='heart']");
        private By PendingTabBtn => By.XPath("//button[contains(text(), 'Chờ duyệt')]");
        private By SavedItemsList => By.CssSelector("div[class*='productCard']");
        private By HidePostBtn => By.XPath("//button[contains(text(), 'Ẩn tin')]");
        private By ConfirmHideReasonBtn => By.XPath("//button[contains(text(), 'TẠM THỜI KHÔNG BÁN')]");
        private By LoginErrorMsg => By.CssSelector(".error, .text-danger, p[class*='error']");

        // ==========================================================
        // CÁC HÀM XỬ LÝ (ACTIONS)
        // ==========================================================

        // Dùng cho INT_02: Click vào kết quả tìm kiếm đầu tiên
        public void ClickFirstSearchResult()
        {
            var firstProduct = wait.Until(ExpectedConditions.ElementToBeClickable(FirstProductCard));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", firstProduct);
            Thread.Sleep(500);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", firstProduct);
        }

        // Dùng cho INT_03: Bấm lưu tin (Trái tim) ở sản phẩm đầu tiên
        public void LikeFirstProduct()
        {
            var heartIcon = wait.Until(ExpectedConditions.ElementToBeClickable(FirstHeartIcon));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", heartIcon);
            Thread.Sleep(500);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", heartIcon);
        }

        // Dùng cho INT_03: Kiểm tra xem có sản phẩm nào trong trang Tin đã lưu không
        public bool HasSavedPosts()
        {
            try
            {
                var items = wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(SavedItemsList));
                return items.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        // Dùng cho INT_01: Chuyển sang tab Chờ duyệt và kiểm tra xem có bài viết đó không
        public bool CheckPostExistsInPendingTab(string postTitle)
        {
            // 1. Chuyển sang tab chờ duyệt
            var pendingTab = wait.Until(ExpectedConditions.ElementToBeClickable(PendingTabBtn));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", pendingTab);
            Thread.Sleep(2000);

            // 2. Tìm bài viết theo tên
            try
            {
                By postLocator = By.XPath($"//div[contains(@class, 'postItem')]//button[contains(text(), '{postTitle}')] | //div[contains(@class, 'postItem')]//a[contains(text(), '{postTitle}')]");
                var postItem = wait.Until(ExpectedConditions.ElementIsVisible(postLocator));
                return postItem.Displayed;
            }
            catch
            {
                return false;
            }
        }

        // Dùng cho INT_06: Bắt thông báo lỗi khi đăng nhập tài khoản bị khóa
        public string GetLoginBanMessage()
        {
            try
            {
                var errorMsg = wait.Until(ExpectedConditions.ElementIsVisible(LoginErrorMsg));
                return errorMsg.Text;
            }
            catch
            {
                return "";
            }
        }

        // Dùng cho INT_07: Bấm ẩn tin đang hiển thị
        public void HideFirstActivePost()
        {
            var hideBtn = wait.Until(ExpectedConditions.ElementToBeClickable(HidePostBtn));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", hideBtn);
            Thread.Sleep(500);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", hideBtn);
            Thread.Sleep(1000);

            // Xử lý Modal lý do ẩn tin (Nếu có)
            try
            {
                var confirmBtn = wait.Until(ExpectedConditions.ElementToBeClickable(ConfirmHideReasonBtn));
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", confirmBtn);
                Thread.Sleep(500);
                wait.Until(ExpectedConditions.AlertIsPresent()).Accept(); // Bấm OK trên Alert
            }
            catch { /* Im lặng bỏ qua nếu không có Modal hoặc Alert */ }
        }

        // Hàm hỗ trợ click nút Submit cho form Create Post (khi gọi chéo)
        public void ClickSubmitPost()
        {
            var btnSubmit = wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("[data-testid='post-submit-button']")));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", btnSubmit);
            Thread.Sleep(1000);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", btnSubmit);
        }
    }
}