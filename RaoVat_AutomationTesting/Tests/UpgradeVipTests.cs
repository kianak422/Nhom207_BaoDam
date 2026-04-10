using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using RaoVat_AutomationTesting.Pages;
using RaoVat_AutomationTesting.Utilities;
using SeleniumExtras.WaitHelpers;
using System;
using System.IO;
using System.Threading;
using static RaoVat_AutomationTesting.Utilities.JsonReader;

namespace RaoVat_AutomationTesting.Tests
{
    [TestFixture]
    public class UpgradeVipTests
    {
        private IWebDriver? driver;
        private VipPage vipPage;
        private LoginPage loginPage;
        private WebDriverWait wait;

        // CAU HINH EXCEL REPORT
        private ExcelHelper excelHelper;
        private string reportPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BaoDam_Report.xlsx"));
        private string sheetName = "TCs - F6";
        private string testerName = "Duy";
        private string actualUiMessage = "";

        // DATA TU JSON
        private UpgradeVipConfigData vipConfig;

        [SetUp]
        public void Setup()
        {
            excelHelper = new ExcelHelper(reportPath);
            actualUiMessage = "";

            // Doc data tu file JSON
            vipConfig = JsonReader.GetUpgradeVipConfigData()
                ?? throw new Exception("Loi khong the doc file upgradeVipData.json");

            driver = DriverFactory.CreateDriver();
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            vipPage = new VipPage(driver);
            loginPage = new LoginPage(driver);
        }

        // ===================================================================

        [Test, Order(1)]
        public void UV01_Truy_Cap_Trang_Upgrade_Khi_Chua_Login()
        {
            // 1. Vào thẳng trang khi chưa đăng nhập (Khách)
            vipPage.GoToUpgradePage();

            // 2. He thong cua ban se lap tuc vang ra Alert chan lai (lap lai 2 lan)
            // Nen ta dung vong lap de quet sach cac Alert nay
            string alertMsg = "";
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    WebDriverWait shortWait = new WebDriverWait(driver, TimeSpan.FromSeconds(2));
                    shortWait.Until(ExpectedConditions.AlertIsPresent());

                    IAlert alert = driver.SwitchTo().Alert();
                    // Luu lai text cua cai Alert dau tien bat duoc de kiem tra
                    if (string.IsNullOrEmpty(alertMsg)) alertMsg = alert.Text;

                    alert.Accept(); // Tat Alert
                    Thread.Sleep(500); // Nghi ngoi doi xem co cai Alert thu 2 nhay ra khong
                }
                catch (WebDriverTimeoutException)
                {
                    // Khi khong con Alert nao nua thi thoat vong lap
                    break;
                }
            }

            // 3. KIEM TRA
            // Dam bao rang he thong that su da hien Alert canh bao
            Assert.That(alertMsg, Does.Not.Null.Or.Empty, "Loi: Khach truy cap vao trang VIP ma khong bi he thong canh bao!");

            // Doi 1 chut de React thuc hien redirect (da ve trang chu hoac login)
            Thread.Sleep(1000);
            Assert.That(driver.Url, Does.Not.Contain("/nang-cap-vip"), "Loi: Tat Alert xong van dung o trang Upgrade VIP!");

            actualUiMessage = $"He thong chan khach thanh cong voi thong bao: '{alertMsg}'";
        }

        [Test, Order(2)]
        public void UV02_Truy_Cap_Trang_Upgrade_Hop_Le()
        {
            string email = vipConfig.NormalAccount?.Email ?? throw new Exception("Thieu Email Tai khoan thuong");
            string pass = vipConfig.NormalAccount?.Pass ?? throw new Exception("Thieu Pass Tai khoan thuong");

            driver.Navigate().GoToUrl("http://localhost:5173/login");
            loginPage.Login(email, pass);
            Thread.Sleep(2000);

            vipPage.GoToUpgradePage();

            bool isVip = vipPage.IsAlreadyVip();
            Assert.That(isVip, Is.False, "Loi: Tai khoan thuong ma he thong lai bao la Da VIP!");

            var checkoutBtn = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-testid='btn-checkout-vip']")));
            actualUiMessage = $"Tai khoan thuong truy cap hop le, hien thi nut: '{checkoutBtn.Text}'";
        }

        [Test, Order(3)]
        public void UV03_Truy_Cap_Trang_Upgrade_Khi_DANG_LA_VIP()
        {
            string email = vipConfig.VipAccount?.Email ?? throw new Exception("Thieu Email Tai khoan VIP");
            string pass = vipConfig.VipAccount?.Pass ?? throw new Exception("Thieu Pass Tai khoan VIP");

            driver.Navigate().GoToUrl("http://localhost:5173/login");
            loginPage.Login(email, pass);
            Thread.Sleep(2000);

            vipPage.GoToUpgradePage();

            bool isVip = vipPage.IsAlreadyVip();
            Assert.That(isVip, Is.True, "Loi: Tai khoan dang la VIP nhung he thong lai hien thi bang mua goi!");

            var cardText = driver.FindElement(By.CssSelector("[data-testid='already-vip-card']")).Text;
            actualUiMessage = $"He thong nhan dien dung, hien thi: '{cardText.Replace("\n", " - ")}'";
        }

        [Test, Order(4)]
        public void UV04_Chon_Goi_VIP_Co_Ban()
        {
            string email = vipConfig.NormalAccount?.Email ?? "";
            string pass = vipConfig.NormalAccount?.Pass ?? "";
            string packageId = vipConfig.BasicPackageId ?? "vip_1m";

            driver.Navigate().GoToUrl("http://localhost:5173/login");
            loginPage.Login(email, pass);
            Thread.Sleep(2000);

            vipPage.GoToUpgradePage();
            vipPage.SelectPackage(packageId);
            vipPage.ClickCheckout();

            wait.Until(d => d.Url.Contains("/thanh-toan"));

            Assert.That(driver.Url, Does.Contain("/thanh-toan"), "Loi: Khong the khoi tao thanh toan cho goi Co ban.");
            actualUiMessage = $"Khoi tao giao dich thanh cong. URL chuyen den: {driver.Url}";
        }

        [Test, Order(5)]
        public void UV05_Chon_Goi_VIP_Co_Giam_Gia()
        {
            string email = vipConfig.NormalAccount?.Email ?? "";
            string pass = vipConfig.NormalAccount?.Pass ?? "";
            string packageId = vipConfig.DiscountPackageId ?? "vip_3m";

            driver.Navigate().GoToUrl("http://localhost:5173/login");
            loginPage.Login(email, pass);
            Thread.Sleep(2000);

            vipPage.GoToUpgradePage();
            vipPage.SelectPackage(packageId);
            vipPage.ClickCheckout();

            wait.Until(d => d.Url.Contains("/thanh-toan"));

            Assert.That(driver.Url, Does.Contain("/thanh-toan"), "Loi: Khong the khoi tao thanh toan cho goi Giam gia.");
            actualUiMessage = $"Khoi tao giao dich thanh cong. URL chuyen den: {driver.Url}";
        }

        // =========================================================================
        // TEARDOWN GHI KET QUA VAO EXCEL 
        // =========================================================================
        [TearDown]
        public void Teardown()
        {
            if (driver != null)
            {
                string testName = TestContext.CurrentContext.Test.MethodName ?? "";
                string testCaseId = testName.Split('_')[0];

                var testStatus = TestContext.CurrentContext.Result.Outcome.Status;
                string resultToLog = "Pass";
                string messageToLog = "";
                string screenshotPath = "";

                if (testStatus == NUnit.Framework.Interfaces.TestStatus.Failed)
                {
                    resultToLog = "Fail";
                    string rawError = TestContext.CurrentContext.Result.Message ?? "Loi khong xac dinh";

                    if (rawError.Contains("Expected:"))
                        messageToLog = rawError.Split("Expected:")[0].Trim();
                    else
                        messageToLog = rawError.Split('\n')[0].Trim();

                    screenshotPath = TakeScreenshot(driver, testCaseId, true);
                }
                else
                {
                    resultToLog = "Pass";
                    messageToLog = string.IsNullOrEmpty(actualUiMessage) ? "Hoan thanh." : actualUiMessage;
                }

                try
                {
                    excelHelper.WriteTestResultById(sheetName, testCaseId, messageToLog, resultToLog, testerName, screenshotPath);
                    Console.WriteLine($"[Excel Log] {testCaseId} | {resultToLog} | {messageToLog}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LOI GHI EXCEL] {ex.Message}");
                }

                driver.Quit();
                driver.Dispose();
            }
        }

        private string TakeScreenshot(IWebDriver driver, string testName, bool isError = false)
        {
            try
            {
                if (isError)
                {
                    IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                    js.ExecuteScript(@"
                        var errors = document.querySelectorAll('p[class*=""error""], span[class*=""error""], div[class*=""error""], .text-danger, [class*=""MuiFormHelperText""]');
                        if (errors.length > 0) {
                            errors[0].scrollIntoView({behavior: 'smooth', block: 'center'});
                            for(var i = 0; i < errors.length; i++) {
                                var el = errors[i];
                                el.style.border = '2px dashed #ff4d4f';
                                el.style.backgroundColor = 'rgba(255, 77, 79, 0.1)';
                                el.style.padding = '4px';
                            }
                        }
                    ");
                    Thread.Sleep(800);
                }

                ITakesScreenshot ts = (ITakesScreenshot)driver;
                Screenshot screenshot = ts.GetScreenshot();

                string path = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Screenshots"));
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                string finalPath = Path.Combine(path, $"{testName}.png");
                if (File.Exists(finalPath)) File.Delete(finalPath);

                screenshot.SaveAsFile(finalPath);
                return finalPath;
            }
            catch (UnhandledAlertException)
            {
                try { driver.SwitchTo().Alert().Accept(); } catch { }
                ITakesScreenshot ts = (ITakesScreenshot)driver;
                string path = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Screenshots"));
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                string finalPath = Path.Combine(path, $"{testName}.png");
                if (File.Exists(finalPath)) File.Delete(finalPath);
                ts.GetScreenshot().SaveAsFile(finalPath);
                return finalPath;
            }
            catch
            {
                return "";
            }
        }


        // =========================================================================
        // PAYMENT PAGE TESTS (PP01 -> PP05)
        // =========================================================================

        // Hàm Hỗ Trợ: Giúp đi nhanh đến trang thanh toán để bắt đầu test
        private void NavigateToPaymentPage()
        {
            string email = vipConfig.NormalAccount?.Email ?? "";
            string pass = vipConfig.NormalAccount?.Pass ?? "";
            string packageId = vipConfig.BasicPackageId ?? "vip_1m";

            driver.Navigate().GoToUrl("http://localhost:5173/login");
            loginPage.Login(email, pass);
            Thread.Sleep(2000);

            vipPage.GoToUpgradePage();
            vipPage.SelectPackage(packageId);
            vipPage.ClickCheckout();

            wait.Until(d => d.Url.Contains("/thanh-toan"));
            Thread.Sleep(1000); // Chờ load data Firebase
        }

        [Test, Order(6)]
        public void PP01_Render_Form_Hoa_Don()
        {
            NavigateToPaymentPage();

            bool isRendered = vipPage.IsPaymentFormRendered();
            Assert.That(isRendered, Is.True, "Loi: Khong the render form hoa don hoac dong ho dem nguoc.");

            actualUiMessage = "Render thanh cong: Tong tien va Dong ho dem nguoc da xuat hien tren DOM.";
        }

        [Test, Order(7)]
        public void PP02_Sinh_Ma_QR_Code_Dong()
        {
            NavigateToPaymentPage();

            bool isQrVisible = vipPage.IsQrCodeVisible();
            Assert.That(isQrVisible, Is.True, "Loi: Ma QR Code khong duoc tao hoac khong hien thi.");

            actualUiMessage = "Ma QR SVG da duoc sinh ra tren giao dien.";
        }

        [Test, Order(8)]
        public void PP03_Bao_Mat_Doi_Gia_Tien_Bang_DevTools()
        {
            NavigateToPaymentPage();

            // 1. Dung JavaScript de Hack giao dien (Doi gia tien hien thi thanh 1 VND)
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("document.querySelector('[data-testid=\"total-amount\"]').innerText = '1 VNĐ';");
            Console.WriteLine("Da hack giao dien: Doi gia thanh 1 VND");

            // 2. Thuc hien thanh toan
            vipPage.ExecuteFakePayment();

            // 3. Cho doi xu ly (do ban random 3-10 giay nen cho toi da 15s)
            WebDriverWait longWait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
            longWait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-testid='status-title']")));

            // 4. Kiem tra bien lai xem co bi anh huong boi giao dien bi hack khong
            string finalStatus = vipPage.GetPaymentStatusTitle();
            string receiptAmount = vipPage.GetReceiptAmount();

            Assert.That(finalStatus, Does.Contain("Thanh toán thành công"));
            Assert.That(receiptAmount, Does.Not.Contain("1 VNĐ"), "LOI BAO MAT NGHIEM TRONG: Bien lai in ra 1 VND do bi hack HTML!");

            actualUiMessage = $"He thong bao mat tot. Du bi hack DOM nhung bien lai van dung gia goc: {receiptAmount}";
        }

        [Test, Order(9)]
        public void PP04_Het_Han_Giao_Dich_Timeout()
        {
            NavigateToPaymentPage();

            Console.WriteLine("Bat dau cho doi he thong het han giao dich (du kien 120 giay)...");

            // 1. Tao mot trinh doi long (Max 150 giay de du phong tre mang)
            WebDriverWait timeoutWait = new WebDriverWait(driver, TimeSpan.FromSeconds(150));

            try
            {
                // 2. Cho cho den khi cai status-title chuyen thanh "Phiên đã hết hạn"
                // He thong cua ban dung status 'expired' se render ra tieu de nay
                timeoutWait.Until(ExpectedConditions.TextToBePresentInElementLocated(
                    By.CssSelector("[data-testid='status-title']"), "Phiên đã hết hạn"));

                // 3. Kiem tra them cac thong tin di kem de chac chan UI render dung
                string finalTitle = vipPage.GetPaymentStatusTitle();

                Assert.That(finalTitle, Is.EqualTo("Phiên đã hết hạn"), "Loi: He thong khong tu dong chuyen trang thai khi het gio.");

                actualUiMessage = $"He thong tu dong het han sau 2 phut. Thong bao thuc te: '{finalTitle}'";
                Console.WriteLine("PP04 PASS: Giao dich da tu dong het han thanh cong.");
            }
            catch (WebDriverTimeoutException)
            {
                // Neu qua 150 giay ma van khong thay chu "Het han"
                Assert.Fail("LOI: Qua thoi gian quy dinh (2 phut) ma giao dich van khong tu dong chuyen sang trang thai Het han.");
            }
        }

        [Test, Order(10)]
        public void PP05_Huy_Bo_Giao_Dich()
        {
            NavigateToPaymentPage();

            // Thuc hien thao tac Huy
            vipPage.CancelTransaction();

            // Kiem tra man hinh ket qua
            string status = vipPage.GetPaymentStatusTitle();

            Assert.That(status, Does.Contain("Giao dịch đã bị hủy"), "Loi: Huy giao dich nhung khong hien thi dung trang thai man hinh.");

            actualUiMessage = $"Huy thanh cong. He thong hien thi: '{status}'";
        }

        [Test, Order(11)]
        public void PP06_Reload_Trang_Khi_Dang_Cho_Thanh_Toan()
        {
            NavigateToPaymentPage();

            // 1. Lay gia tien truoc khi reload
            string amountBefore = driver.FindElement(By.CssSelector("[data-testid='total-amount']")).Text;

            // 2. F5 Refresh trang
            driver.Navigate().Refresh();
            Thread.Sleep(2000); // Cho Firebase reconnect

            // 3. Kiem tra du lieu co con ton tai khong
            string amountAfter = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-testid='total-amount']"))).Text;

            Assert.That(amountAfter, Is.EqualTo(amountBefore), "Loi: Du lieu hoa don bi mat sau khi reload trang.");
            actualUiMessage = $"Du lieu duoc duy tri sau khi F5. Tong tien: {amountAfter}";
        }

        [Test, Order(12)]
        public void PP07_Dong_Tab_Trinh_Duyet_Dot_Ngot()
        {
            NavigateToPaymentPage();
            string paymentUrl = driver.Url;

            // Gia lap dong tab bang cach chuyen ve trang chu dot ngot
            // Logic React: useEffect return se goi cancelSession
            driver.Navigate().GoToUrl("http://localhost:5173/");
            Thread.Sleep(1000);

            // Quay lai link thanh toan cu de xem trang thai
            driver.Navigate().GoToUrl(paymentUrl);
            Thread.Sleep(2000);

            string status = vipPage.GetPaymentStatusTitle();

            // Ky vong: He thong phai tu dong chuyen sang "Giao dịch đã bị hủy"
            Assert.That(status, Does.Contain("Giao dịch đã bị hủy"), "Loi: Giao dich khong tu dong huy khi nguoi dung thoat trang dot ngot.");

            actualUiMessage = $"He thong tu dong huy giao dich khi detect thoat trang. Trang thai: '{status}'";
        }

        [Test, Order(13)]
        public void PP08_Thanh_Toan_Gia_Lap_Thanh_Cong()
        {
            NavigateToPaymentPage();

            // 1. Lay thong tin the hop le tu JSON
            var card = vipConfig.ValidCard ?? throw new Exception("Thieu data ValidCard");

            // 2. Nhap lieu (Gia su ban muon nhap the truoc khi bam Fake Pay)
            vipPage.InputCardDetails(card.Number, card.Expiry, card.Cvv);

            // 3. Thuc hien Fake Pay
            vipPage.ExecuteFakePayment();

            WebDriverWait successWait = new WebDriverWait(driver, TimeSpan.FromSeconds(25));
            successWait.Until(ExpectedConditions.TextToBePresentInElementLocated(By.CssSelector("[data-testid='status-title']"), "Thanh toán thành công"));

            string finalStatus = vipPage.GetPaymentStatusTitle();
            Assert.That(finalStatus, Does.Contain("Thanh toán thành công"));

            actualUiMessage = $"Giao dich hoan tat voi the {card.Number}. Man hinh hien thi: '{finalStatus}'";
        }

        [Test, Order(14)]
        public void PP09_Thanh_Toan_Gia_Lap_Voi_Thong_Tin_Rac()
        {
            NavigateToPaymentPage();

            // 1. Lay thong tin the rac tu JSON (VD: ABC-XYZ, NAM/NAY...)
            var card = vipConfig.InvalidCard ?? throw new Exception("Thieu data InvalidCard");

            // 2. Nhap thong tin the khong hop le vao form
            vipPage.InputCardDetails(card.Number, card.Expiry, card.Cvv);

            // 3. Bam nut Thanh toan
            vipPage.ExecuteFakePayment();

            try
            {
                // 4. Cho doi xem he thong co cho phep thanh toan thanh cong hay khong
                WebDriverWait shortWait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
                shortWait.Until(ExpectedConditions.TextToBePresentInElementLocated(
                    By.CssSelector("[data-testid='status-title']"), "Thanh toán thành công"));

                // Ham Assert.Fail se lam Test Case bao do ruc, dong thoi kich hoat TearDown chup anh va ghi Excel la "Fail".
                Assert.Fail($"FAIL (LOI VALIDATION): He thong van cho phep the rac '{card.Number}' thanh toan thanh cong. Can bo sung logic kiem tra dinh dang the tai Frontend/Backend.");
            }
            catch (WebDriverTimeoutException)
            {
                // Neu sau 15s ma khong thay chu "Thanh cong", tuc la he thong da chan lai (Dung ky vong)
                actualUiMessage = "Pass: He thong bao mat tot, khong xu ly giao dich khi nhap thong tin the rac.";
                Console.WriteLine("PP09 PASS: Validation hoat dong tot.");
            }
        }
        [Test, Order(15)]
        public void PP10_Thanh_Toan_Gia_Lap_Voi_Thong_Tin_Rong()
        {
            NavigateToPaymentPage();

            // 1. Chuyen sang tab The nhung KHONG nhap bat ky thong tin nao
            vipPage.SwitchToCardTab();
            Console.WriteLine("PP10: Dang thu bam Thanh toan voi form the de trong...");

            // 2. Bam truc tiep nut Thanh toan (Fake Pay)
            vipPage.ExecuteFakePayment();

            try
            {
                // 3. Cho doi xem he thong co cho phep "thong quan" hay khong (doi 15s)
                WebDriverWait shortWait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
                shortWait.Until(ExpectedConditions.TextToBePresentInElementLocated(
                    By.CssSelector("[data-testid='status-title']"), "Thanh toán thành công"));

                // 🌟 EP FAIL: Neu web hien chu "Thanh toan thanh cong" tuc la dang bi bug Validation
                Assert.Fail("FAIL (LOI VALIDATION): He thong cho phep thanh toan khi thong tin the dang de trong. Can yeu cau nguoi dung nhap day du thong tin truoc khi bam nut.");
            }
            catch (WebDriverTimeoutException)
            {
                // Neu sau 15s ma khong thanh cong, tuc la he thong da chan dung (hoac khong lam gi ca)
                actualUiMessage = "Pass: He thong khong xu ly giao dich khi form the de trong (Dung ky vong).";
                Console.WriteLine("PP10 PASS: He thong da chan thanh toan rong.");
            }
        }

        // =========================================================================
        // TRANSACTION HISTORY TESTS (TH01 -> TH02)
        // =========================================================================

        [Test, Order(16)]
        public void TH01_Xem_Lich_Su_Giao_Dich_Rong()
        {
            // 1. Lay thong tin tai khoan moi tao, chua mua gi
            string email = vipConfig.NewAccount?.Email ?? throw new Exception("Thieu data NewAccount");
            string pass = vipConfig.NewAccount?.Pass ?? throw new Exception("Thieu Pass NewAccount");

            // 2. Login
            driver.Navigate().GoToUrl("http://localhost:5173/login");
            loginPage.Login(email, pass);
            Thread.Sleep(2000);

            // 3. Vao trang Lich su
            vipPage.GoToTransactionHistory();

            // 4. Kiem tra hien thi cau thong bao "Ban chua co giao dich nao"
            bool isEmpty = vipPage.IsHistoryEmpty();
            Assert.That(isEmpty, Is.True, "Loi: User moi ma lai khong hien thi thong bao rong hoac hien bang du lieu sai.");

            actualUiMessage = "He thong hien thi chinh xac thong bao: 'Ban chua co giao dich nao.'";
        }

        [Test, Order(17)]
        public void TH02_Xem_Lich_Su_Giao_Dich_Hop_Le()
        {
            // ĐỔI THÀNH TÀI KHOẢN VIP ĐÃ CÓ GIAO DỊCH 
            string email = vipConfig.VipAccount?.Email ?? throw new Exception("Thieu data VipAccount");
            string pass = vipConfig.VipAccount?.Pass ?? throw new Exception("Thieu Pass VipAccount");

            // Login
            driver.Navigate().GoToUrl("http://localhost:5173/login");
            loginPage.Login(email, pass);
            Thread.Sleep(2000);

            // Vao trang Lich su
            vipPage.GoToTransactionHistory();

            // KIỂM TRA TRỰC QUAN HƠN: Cố gắng chờ bảng dữ liệu xuất hiện
            try
            {
                int rowCount = vipPage.GetHistoryRowCount();
                Assert.That(rowCount, Is.GreaterThan(0), "Loi: User Vip da co giao dich ma bang van rong (Khong tim thay <tr data-testid='history-row'>)!");

                actualUiMessage = $"He thong load thanh cong bang du lieu voi {rowCount} giao dich.";
                Console.WriteLine(actualUiMessage);
            }
            catch (Exception ex)
            {
                Assert.Fail($"LỖI RENDER BẢNG: Web tra ve Null hoac khong the load duoc <table>. Chi tiet: {ex.Message}");
            }
        }
    }
}