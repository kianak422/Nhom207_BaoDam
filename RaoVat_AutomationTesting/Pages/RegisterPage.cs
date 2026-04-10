using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using RaoVat_AutomationTesting.Utilities;
using System;

namespace RaoVat_AutomationTesting.Pages
{
    public class RegisterPage
    {
        private IWebDriver driver;
        private WebDriverWait wait;

        public RegisterPage(IWebDriver driver)
        {
            this.driver = driver;
            this.wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        // Locators: Đã chuyển toàn bộ sang sử dụng data-testid thông qua CssSelector
        private By NameInput => By.CssSelector("[data-testid='register-displayname-input']");
        private By PhoneInput => By.CssSelector("[data-testid='register-phone-input']");
        private By EmailInput => By.CssSelector("[data-testid='register-email-input']");
        private By PassInput => By.CssSelector("[data-testid='register-password-input']");
        private By ConfirmPassInput => By.CssSelector("[data-testid='register-confirm-password-input']");
        private By TermsCheckbox => By.CssSelector("[data-testid='register-terms-checkbox']");
        private By RegisterBtn => By.CssSelector("[data-testid='register-submit-button']");

        // Actions
        public void Register(RegisterData data)
        {
            // Điền Tên
            var nameEl = wait.Until(ExpectedConditions.ElementIsVisible(NameInput));
            nameEl.Clear();
            nameEl.SendKeys(data.Name ?? "");

            // Điền SĐT
            var phoneEl = driver.FindElement(PhoneInput);
            phoneEl.Clear();
            phoneEl.SendKeys(data.Phone ?? "");

            // Điền Email
            var emailEl = driver.FindElement(EmailInput);
            emailEl.Clear();
            emailEl.SendKeys(data.Email ?? "");

            // Điền Pass
            var passEl = driver.FindElement(PassInput);
            passEl.Clear();
            passEl.SendKeys(data.Pass ?? "");

            // Điền Confirm Pass (Đã khôi phục)
            var confirmPassEl = driver.FindElement(ConfirmPassInput);
            confirmPassEl.Clear();
            confirmPassEl.SendKeys(data.ConfirmPass ?? "");

            // 1. Ép trình duyệt cuộn xuống chỗ Checkbox
            var checkbox = driver.FindElement(TermsCheckbox);
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", checkbox);

            // Tạm nghỉ để UI ổn định và React cập nhật state
            System.Threading.Thread.Sleep(500);

            // 2. Xử lý Checkbox bằng Javascript
            if (data.AgreeTerms && !checkbox.Selected)
            {
                js.ExecuteScript("arguments[0].click();", checkbox);
            }
            else if (!data.AgreeTerms && checkbox.Selected)
            {
                js.ExecuteScript("arguments[0].click();", checkbox);
            }

            System.Threading.Thread.Sleep(200);

            // 3. Click Đăng ký bằng Javascript (Chỉ click 1 lần duy nhất)
            var registerBtn = driver.FindElement(RegisterBtn);
            js.ExecuteScript("arguments[0].click();", registerBtn);
        }

        // Hàm lấy tin nhắn từ hộp thoại alert() của trình duyệt
        public string GetJavaScriptAlertText()
        {
            try
            {
                IAlert alert = wait.Until(ExpectedConditions.AlertIsPresent());
                string text = alert.Text;
                alert.Accept();
                return text;
            }
            catch
            {
                return "";
            }
        }

        // Hàm lấy tin nhắn từ HTML5 Validation
        public string GetBrowserValidationMessage(string fieldType)
        {
            try
            {
                IWebElement element = fieldType switch
                {
                    "name" => driver.FindElement(NameInput),
                    "phone" => driver.FindElement(PhoneInput),
                    "email" => driver.FindElement(EmailInput),
                    "password" => driver.FindElement(PassInput),
                    "confirmpass" => driver.FindElement(ConfirmPassInput),
                    "terms" => driver.FindElement(TermsCheckbox),
                    _ => driver.FindElement(EmailInput)
                };
                return element.GetAttribute("validationMessage");
            }
            catch
            {
                return "";
            }
        }
    }
}