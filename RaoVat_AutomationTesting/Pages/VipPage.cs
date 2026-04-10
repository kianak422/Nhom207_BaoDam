using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;

namespace RaoVat_AutomationTesting.Pages
{
    public class VipPage
    {
        private IWebDriver driver;
        private WebDriverWait wait;

        public VipPage(IWebDriver driver)
        {
            this.driver = driver;
            this.wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        // --- LOCATORS ---
        private By AlreadyVipCard => By.CssSelector("[data-testid='already-vip-card']");
        private By CheckoutBtn => By.CssSelector("[data-testid='btn-checkout-vip']");

        // Hàm lấy locator động cho từng gói (ví dụ: vip_1m, vip_3m)
        private By GetPackageLocator(string packageId) => By.CssSelector($"[data-testid='vip-package-{packageId}']");

        // --- ACTIONS ---
        public void GoToUpgradePage()
        {
            driver.Navigate().GoToUrl("http://localhost:5173/upgrade-vip");
        }

        public bool IsAlreadyVip()
        {
            try
            {
                var card = wait.Until(ExpectedConditions.ElementIsVisible(AlreadyVipCard));
                return card.Displayed;
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }

        public void SelectPackage(string packageId)
        {
            var packageElement = wait.Until(ExpectedConditions.ElementToBeClickable(GetPackageLocator(packageId)));
            packageElement.Click();
        }

        public void ClickCheckout()
        {
            var btn = wait.Until(ExpectedConditions.ElementToBeClickable(CheckoutBtn));
            btn.Click();
        }

        // LOCATORS CHO TRANG THANH TOÁN
        private By TotalAmount => By.CssSelector("[data-testid='total-amount']");
        private By CountdownTimer => By.CssSelector("[data-testid='countdown-timer']");
        private By QrCodeDisplay => By.CssSelector("[data-testid='qr-code-display']");
        private By TabCard => By.CssSelector("[data-testid='tab-card']");
        private By BtnCancelQr => By.CssSelector("[data-testid='btn-cancel-qr']");
        private By BtnFakePay => By.CssSelector("[data-testid='btn-fake-pay']");
        private By StatusTitle => By.CssSelector("[data-testid='status-title']");
        private By ReceiptAmount => By.CssSelector("[data-testid='receipt-amount']");

        // ACTIONS CHO TRANG THANH TOÁN
        public bool IsPaymentFormRendered()
        {
            try
            {
                wait.Until(ExpectedConditions.ElementIsVisible(TotalAmount));
                wait.Until(ExpectedConditions.ElementIsVisible(CountdownTimer));
                return true;
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }

        public bool IsQrCodeVisible()
        {
            try
            {
                var qr = wait.Until(ExpectedConditions.ElementIsVisible(QrCodeDisplay));
                return qr.Displayed;
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }

        public void SwitchToCardTab()
        {
            wait.Until(ExpectedConditions.ElementToBeClickable(TabCard)).Click();
        }

        public void CancelTransaction()
        {
            // Bấm hủy ở tab QR (mặc định)
            wait.Until(ExpectedConditions.ElementToBeClickable(BtnCancelQr)).Click();

            // Xác nhận Alert "Bạn có chắc muốn hủy..."
            wait.Until(ExpectedConditions.AlertIsPresent());
            driver.SwitchTo().Alert().Accept();
        }

        public void ExecuteFakePayment()
        {
            SwitchToCardTab();
            var btn = wait.Until(ExpectedConditions.ElementIsVisible(BtnFakePay));

            // 🌟 DUNG JAVASCRIPT DE CLICK (Tranh loi bi Banner Emulator che)
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("arguments[0].click();", btn);
        }

        public string GetPaymentStatusTitle()
        {
            var title = wait.Until(ExpectedConditions.ElementIsVisible(StatusTitle));
            return title.Text;
        }

        public string GetReceiptAmount()
        {
            var amount = wait.Until(ExpectedConditions.ElementIsVisible(ReceiptAmount));
            return amount.Text;
        }
        // Locators cho Form nhap the
        private By CardNumberInput => By.CssSelector("input[placeholder='**** **** **** ****']");
        private By CardExpiryInput => By.CssSelector("input[placeholder='MM/YY']");
        private By CardCvvInput => By.CssSelector("input[placeholder='***']");

        // Actions cho Form nhap the
        public void InputCardDetails(string number, string expiry, string cvv)
        {
            // Chuyen sang tab the truoc khi nhap
            SwitchToCardTab();

            var numEl = wait.Until(ExpectedConditions.ElementIsVisible(CardNumberInput));
            numEl.Clear();
            numEl.SendKeys(number);

            var expiryEl = driver.FindElement(CardExpiryInput);
            expiryEl.Clear();
            expiryEl.SendKeys(expiry);

            var cvvEl = driver.FindElement(CardCvvInput);
            cvvEl.Clear();
            cvvEl.SendKeys(cvv);
        }

        public string GetCurrentUrl()
        {
            return driver.Url;
        }

        // Locators cho Transaction History
        private By EmptyHistoryMsg => By.CssSelector("[data-testid='empty-history-msg']");
        private By HistoryTable => By.CssSelector("[data-testid='history-table']");
        private By HistoryRows => By.CssSelector("[data-testid='history-row']");

        // Chuyen den trang Lich su (Doi URL thanh URL thuc te web ban neu khac)
        public void GoToTransactionHistory()
        {
            driver.Navigate().GoToUrl("http://localhost:5173/lich-su-giao-dich");
        }

        public bool IsHistoryEmpty()
        {
            try
            {
                var msg = wait.Until(ExpectedConditions.ElementIsVisible(EmptyHistoryMsg));
                return msg.Displayed;
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }

        public int GetHistoryRowCount()
        {
            try
            {
                // 1. Cho doi cai bang xuat hien (Toi da 10s vi phai doi Firebase load)
                wait.Until(ExpectedConditions.ElementExists(HistoryTable));

                // 2. Tim tat ca cac dong <tr> co data-testid='history-row'
                var rows = driver.FindElements(HistoryRows);
                return rows.Count;
            }
            catch (WebDriverTimeoutException)
            {
                // Neu qua 10s ma khong thay bang dau, tuc la bang rong hoac chua load duoc
                return 0;
            }
        }
    }
}