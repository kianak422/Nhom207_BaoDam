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
        private readonly IWebDriver driver;
        private readonly WebDriverWait wait;

        public InteractionPage(IWebDriver driver)
        {
            this.driver = driver;
            // Đã giảm xuống 10 giây để test chạy nhanh, không bị treo mòn mỏi
            this.wait = new(driver, TimeSpan.FromSeconds(10));
        }

        private static By FirstChatItem => By.CssSelector("a[class*='chatItem']");
        private static By ChatInput => By.CssSelector("input[placeholder='Nhập tin nhắn...']");
        private static By ChatSendBtn => By.CssSelector("form[class*='inputForm'] button[type='submit']");
        private static By ChatFileInput => By.CssSelector("input[type='file'][accept='image/*']");

        private static By FirstProductItem => By.CssSelector("a[href^='/san-pham/']");
        private static By CommentInput => By.CssSelector("input[class*='commentInput']");
        private static By CommentSendBtn => By.CssSelector("button[class*='sendCommentBtn']");
        private static By ChatWithSellerBtn => By.XPath("//button[contains(translate(., 'CHAT', 'chat'), 'chat') or contains(translate(., 'NHẮN', 'nhắn'), 'nhắn')]");

        // Bổ sung nút 3 chấm để giải quyết case C26, C27
        private static By MoreBtn => By.CssSelector("button[class*='moreButton']");

        public void OpenFirstChatRoom()
        {
            var firstChat = wait.Until(ExpectedConditions.ElementToBeClickable(FirstChatItem));
            firstChat.Click();
            wait.Until(ExpectedConditions.ElementIsVisible(ChatInput));
            Thread.Sleep(500);
        }

        public void OpenFirstProduct()
        {
            // FIX LỖI TIMEOUT: Nếu đã ở trong trang chi tiết sản phẩm rồi thì bỏ qua bước click
            if (driver.Url.Contains("/san-pham/"))
            {
                wait.Until(ExpectedConditions.ElementIsVisible(CommentInput));
                return;
            }

            var firstProduct = wait.Until(ExpectedConditions.ElementToBeClickable(FirstProductItem));
            firstProduct.Click();
            wait.Until(ExpectedConditions.ElementIsVisible(CommentInput));
            Thread.Sleep(500);
        }

        public void FillAndSendChat(string text)
        {
            var inputEl = wait.Until(ExpectedConditions.ElementIsVisible(ChatInput));
            inputEl.Click();
            inputEl.SendKeys(Keys.Control + "a");
            inputEl.SendKeys(Keys.Backspace);
            Thread.Sleep(200);

            if (!string.IsNullOrEmpty(text))
            {
                // FIX LỖI C07 EMOJI: Dùng JS Event để nhét ký tự đặc biệt thay vì gõ phím
                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "var nativeInputValueSetter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;" +
                    "nativeInputValueSetter.call(arguments[0], arguments[1]);" +
                    "arguments[0].dispatchEvent(new Event('input', { bubbles: true }));",
                    inputEl, text);
                Thread.Sleep(500);
            }

            try
            {
                var btn = wait.Until(ExpectedConditions.ElementIsVisible(ChatSendBtn));
                if (btn.Enabled)
                {
                    btn.Click();
                    Thread.Sleep(1500);
                }
            }
            catch (WebDriverTimeoutException) { /* Nút mờ không click được thì bỏ qua */ }
        }

        public void FillAndSendComment(string text)
        {
            var inputEl = wait.Until(ExpectedConditions.ElementIsVisible(CommentInput));
            inputEl.Click();
            inputEl.SendKeys(Keys.Control + "a");
            inputEl.SendKeys(Keys.Backspace);
            Thread.Sleep(200);

            if (!string.IsNullOrEmpty(text))
            {
                // FIX LỖI C07 EMOJI CHO BÌNH LUẬN
                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "var nativeInputValueSetter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;" +
                    "nativeInputValueSetter.call(arguments[0], arguments[1]);" +
                    "arguments[0].dispatchEvent(new Event('input', { bubbles: true }));",
                    inputEl, text);
                Thread.Sleep(500);
            }

            try
            {
                var btn = wait.Until(ExpectedConditions.ElementIsVisible(CommentSendBtn));
                if (btn.Enabled)
                {
                    btn.Click();
                    Thread.Sleep(1500);
                }
            }
            catch (WebDriverTimeoutException) { }
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
                WebDriverWait shortWait = new WebDriverWait(driver, TimeSpan.FromSeconds(2));
                var alert = shortWait.Until(ExpectedConditions.AlertIsPresent());
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

        public void ClickChatWithSellerButton()
        {
            var btn = wait.Until(ExpectedConditions.ElementIsVisible(ChatWithSellerBtn));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", btn);
            Thread.Sleep(500);
            btn.Click();
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

        // Bổ sung hàm thao tác click menu 3 chấm
        public void ClickMoreButton()
        {
            try
            {
                var btn = wait.Until(ExpectedConditions.ElementToBeClickable(MoreBtn));
                btn.Click();
                Thread.Sleep(1500);
            }
            catch { Console.WriteLine("Không tìm thấy nút 3 chấm"); }
        }
    }
}