using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.IO;
using System.Threading;

namespace RaoVat_AutomationTesting.Pages
{
    public class InteractionPage
    {
        // Đã thêm 'readonly' để sửa cảnh báo "Make field readonly"
        private readonly IWebDriver driver;
        private readonly WebDriverWait wait;

        public InteractionPage(IWebDriver driver)
        {
            this.driver = driver;
            this.wait = new(driver, TimeSpan.FromSeconds(60)); // Đã sửa cảnh báo "new expression can be simplified"
        }

        // Đã thêm 'static' để sửa cảnh báo "Member does not access instance data"
        private static By FirstChatItem => By.CssSelector("a[class*='chatItem']");
        private static By ChatInput => By.CssSelector("input[placeholder='Nhập tin nhắn...']");
        private static By ChatSendBtn => By.CssSelector("form[class*='inputForm'] button[type='submit']");
        private static By ChatFileInput => By.CssSelector("input[type='file'][accept='image/*']");

        private static By FirstProductItem => By.CssSelector("a[href^='/san-pham/']");
        private static By CommentInput => By.CssSelector("input[class*='commentInput']");
        private static By CommentSendBtn => By.CssSelector("button[class*='sendCommentBtn']");

        // === BỔ SUNG LOCATOR CÒN THIẾU CHO CASE C10 & C12 ===
        private static By ChatWithSellerBtn => By.XPath("//button[contains(translate(., 'CHAT', 'chat'), 'chat') or contains(translate(., 'NHẮN', 'nhắn'), 'nhắn')]");

        public void OpenFirstChatRoom()
        {
            var firstChat = wait.Until(ExpectedConditions.ElementToBeClickable(FirstChatItem));
            firstChat.Click();
            wait.Until(ExpectedConditions.ElementIsVisible(ChatInput));
            Thread.Sleep(500);
        }

        public void OpenFirstProduct()
        {
            var firstProduct = wait.Until(ExpectedConditions.ElementToBeClickable(FirstProductItem));
            firstProduct.Click();
            wait.Until(ExpectedConditions.ElementIsVisible(CommentInput));
            Thread.Sleep(500);
        }

        public void FillAndSendChat(string text)
        {
            var inputEl = wait.Until(ExpectedConditions.ElementIsVisible(ChatInput));
            inputEl.SendKeys(Keys.Control + "a");
            inputEl.SendKeys(Keys.Delete);

            if (!string.IsNullOrEmpty(text))
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].value = arguments[1];", inputEl, text);
                inputEl.SendKeys(" ");
                inputEl.SendKeys(Keys.Backspace);
            }

            var btn = driver.FindElement(ChatSendBtn);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", btn);
            Thread.Sleep(500);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", btn);
            Thread.Sleep(1000);
        }

        public void FillAndSendComment(string text)
        {
            var inputEl = wait.Until(ExpectedConditions.ElementIsVisible(CommentInput));
            inputEl.SendKeys(Keys.Control + "a");
            inputEl.SendKeys(Keys.Delete);

            if (!string.IsNullOrEmpty(text))
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].value = arguments[1];", inputEl, text);
                inputEl.SendKeys(" ");
                inputEl.SendKeys(Keys.Backspace);
            }

            var btn = driver.FindElement(CommentSendBtn);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", btn);
            Thread.Sleep(500);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", btn);
            Thread.Sleep(1500);
        }

        public void SendDummyImage()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "test_img.png");
            byte[] img = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");
            File.WriteAllBytes(tempPath, img);
            driver.FindElement(ChatFileInput).SendKeys(tempPath);
            Thread.Sleep(2000);
        }

        public void SendInvalidFile()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "test_file.txt");
            File.WriteAllText(tempPath, "File loi roi");
            driver.FindElement(ChatFileInput).SendKeys(tempPath);
            Thread.Sleep(1000);
        }

        public string GetAlertTextAndAccept()
        {
            try
            {
                var alert = wait.Until(ExpectedConditions.AlertIsPresent());
                string txt = alert.Text;
                alert.Accept();
                return txt;
            }
            catch { return string.Empty; }
        }

        public string GetReactErrorMessage()
        {
            try
            {
                var errorElements = driver.FindElements(By.CssSelector("p[class*='errorText']"));
                foreach (var el in errorElements) { if (!string.IsNullOrEmpty(el.Text)) return el.Text; }
                return string.Empty;
            }
            catch { return string.Empty; }
        }

        public string GetChatInputValue()
        {
            try { return driver.FindElement(ChatInput).GetAttribute("value") ?? string.Empty; }
            catch { return string.Empty; }
        }

        public bool IsChatSendButtonEnabled() { try { return driver.FindElement(ChatSendBtn).Enabled; } catch { return false; } }
        public bool IsCommentInputEnabled() { try { return driver.FindElement(CommentInput).Enabled; } catch { return false; } }
        public bool IsCommentSendButtonEnabled() { try { return driver.FindElement(CommentSendBtn).Enabled; } catch { return false; } }

        // ==========================================
        // BỔ SUNG 3 HÀM CÒN THIẾU CHO C10, C12
        // ==========================================
        public void ClickChatWithSellerButton()
        {
            var btn = wait.Until(ExpectedConditions.ElementIsVisible(ChatWithSellerBtn));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", btn);
            Thread.Sleep(500);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", btn);
        }

        public string GetChatWithSellerButtonText()
        {
            try { return wait.Until(ExpectedConditions.ElementIsVisible(ChatWithSellerBtn)).Text; }
            catch { return ""; }
        }

        public bool IsChatWithSellerButtonDisabled()
        {
            try
            {
                var btn = wait.Until(ExpectedConditions.ElementIsVisible(ChatWithSellerBtn));
                return !btn.Enabled || btn.GetAttribute("disabled") != null;
            }
            catch { return false; }
        }
    }
}