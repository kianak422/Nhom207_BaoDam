using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace RaoVat_AutomationTesting.Pages
{
    public class LoginPage
    {
        private IWebDriver driver;
        private WebDriverWait wait;

        public LoginPage(IWebDriver driver)
        {
            this.driver = driver;
            this.wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        // Locators (Sử dụng data-testid mày đã thêm)
        private By EmailInput => By.CssSelector("[data-testid='login-email-input']");
        private By PassInput => By.CssSelector("[data-testid='login-password-input']");
        private By LoginBtn => By.CssSelector("[data-testid='login-submit-button']");

        // Giả sử mày dùng alert của trình duyệt hoặc một cái div báo lỗi
        // Ở đây tao ví dụ là một cái thẻ hiện lỗi có class error-message
        private By ErrorMsg => By.CssSelector(".alert");

        // Actions
        public void Login(string email, string pass)
        {
            var emailEl = wait.Until(ExpectedConditions.ElementIsVisible(EmailInput));
            emailEl.Clear();
            emailEl.SendKeys(email);

            var passEl = driver.FindElement(PassInput);
            passEl.Clear();
            passEl.SendKeys(pass);

            driver.FindElement(LoginBtn).Click();
        }

        public string GetErrorMessage()
        {
            try
            {
                // Chờ cho đến khi cái hộp thoại Alert hiện ra
                IAlert alert = wait.Until(ExpectedConditions.AlertIsPresent());

                // Lấy nội dung tin nhắn trong đó
                string alertText = alert.Text;

                // Bấm nút OK để đóng cái Alert đó lại (CỰC KỲ QUAN TRỌNG)
                // Nếu đéo đóng, nó sẽ kẹt ở đó và đéo chạy được case tiếp theo
                alert.Accept();

                return alertText;
            }
            catch
            {
                return ""; // Nếu không có alert nào hiện ra
            }
        }
        public string GetServerErrorMessage()
        {
            try
            {
                // Mày check lại xem cái thông báo "Email hoặc mật khẩu không chính xác" 
                // nó hiện ở đâu, dùng ID hay Class gì thì sửa lại đây.
                return wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(".alert"))).Text;
            }
            catch
            {
                return "";
            }
        }


        // Hàm lấy tin nhắn từ hộp thoại alert() của trình duyệt (Dùng cho L02, L03)
        public string GetJavaScriptAlertText()
        {
            try
            {
                // Chờ tối đa 5 giây xem có cái popup alert nào hiện ra không
                IAlert alert = wait.Until(ExpectedConditions.AlertIsPresent());
                string text = alert.Text;
                alert.Accept(); // Bấm OK để đóng nó lại, không đóng là case sau tèo
                return text;
            }
            catch
            {
                return "";
            }
        }

        // Hàm lấy tin nhắn từ bong bóng (HTML5) - Dùng cho L04, L05, L06
        public string GetBrowserValidationMessage(string fieldType)
        {
            try
            {
                IWebElement element = (fieldType == "email")
                    ? driver.FindElement(EmailInput)
                    : driver.FindElement(PassInput);
                return element.GetAttribute("validationMessage");
            }
            catch
            {
                return "";
            }
        }
    }
}