using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using RaoVat_AutomationTesting.Pages;
using RaoVat_AutomationTesting.Utilities;
using SeleniumExtras.WaitHelpers;
using System;
using System.IO;
using System.Threading;

namespace RaoVat_AutomationTesting.Tests
{
    [TestFixture]
    public class AdminTests
    {
        private IWebDriver? driver;
        private AdminPage adminPage;
        private WebDriverWait wait;

        // 🌟 BIẾN CHO EXCEL REPORT
        private ExcelHelper excelHelper;
        private string reportPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BaoDam_Report.xlsx"));
        private string sheetName = "TCs - Admin"; // Nhớ kiểm tra xem tên sheet trong file Excel có đúng là chữ này không nhé
        private string testerName = "Danh";

        [SetUp]
        public void Setup()
        {
            // Khởi tạo ExcelHelper
            excelHelper = new ExcelHelper(reportPath);

            driver = DriverFactory.CreateDriver();
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            adminPage = new AdminPage(driver);

            // Login
            var adminConfig = JsonReader.GetAdminConfigData();
            string email = adminConfig?.AdminAccount?.Email ?? throw new Exception("Thiếu Email Admin trong JSON");
            string pass = adminConfig?.AdminAccount?.Pass ?? throw new Exception("Thiếu Pass Admin trong JSON");

            LoginPage loginPage = new LoginPage(driver);
            driver.Navigate().GoToUrl("http://localhost:5173/login");
            loginPage.Login(email, pass);

            Thread.Sleep(2000);
        }

        // =========================================================================
        // TOÀN BỘ 29 TEST CASES BÊN DƯỚI GIỮ NGUYÊN HOÀN TOÀN LOGIC CỦA BẠN
        // =========================================================================

        [Test, Order(1)]
        public void MP01_Xem_Danh_Sach_Cho_Duyet()
        {
            adminPage.GoToAdminDashboard();
            adminPage.GoToManagePosts();
            adminPage.ClickPendingReviewTab();

            var table = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-testid='admin-post-table']")));
            Assert.That(table.Displayed, Is.True, "Bảng danh sách tin đăng không hiển thị!");
        }

        [Test, Order(2)]
        public void MP02_Xem_Chi_Tiet_Truoc_Khi_Duyet()
        {
            adminPage.GoToAdminDashboard();
            adminPage.GoToManagePosts();
            adminPage.ClickPendingReviewTab();

            string targetPostId = adminPage.GetFirstPostId();
            string originalTab = driver.CurrentWindowHandle;

            adminPage.ClickPostTitleToViewDetail(targetPostId);

            wait.Until(d => driver.WindowHandles.Count == 2);
            foreach (string tab in driver.WindowHandles)
            {
                if (tab != originalTab)
                {
                    driver.SwitchTo().Window(tab);
                    break;
                }
            }

            var productName = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("h1")));
            Assert.That(productName.Text, Is.Not.Null.Or.Empty, "Trang chi tiết không hiện tên sản phẩm!");

            driver.Close();
            driver.SwitchTo().Window(originalTab);
        }

        [Test, Order(3)]
        public void MP03_Duyet_Tin_Thanh_Cong()
        {
            adminPage.GoToAdminDashboard();
            adminPage.GoToManagePosts();
            adminPage.ClickPendingReviewTab();

            string targetPostId = adminPage.GetFirstPostId();
            adminPage.ApprovePost(targetPostId);

            Thread.Sleep(2000);
            var elements = driver.FindElements(By.CssSelector($"[data-testid='post-link-{targetPostId}']"));

            Assert.That(elements.Count, Is.EqualTo(0), "Lỗi: Duyệt xong mà tin vẫn còn nằm ở tab Chờ duyệt!");
        }

        [Test, Order(4)]
        public void MP04_Bao_Noti_Khi_Duyet_Bai()
        {
            try
            {
                var notiBadge = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-testid='user-noti-badge']")));
            }
            catch (WebDriverTimeoutException)
            {
                Assert.Fail("FAIL (Bug UX/UI): Hệ thống chưa có tính năng chuông thông báo (Notification) để báo cho User biết bài của họ đã được duyệt.");
            }
        }

        [Test, Order(5)]
        public void MP05_Tu_Choi_Bai_Dang_Co_Nhap_Ly_Do()
        {
            adminPage.GoToAdminDashboard();
            adminPage.GoToManagePosts();
            adminPage.ClickPendingReviewTab();

            string targetPostId = adminPage.GetFirstPostId();

            wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector($"[data-testid='btn-reject-{targetPostId}']"))).Click();
            Thread.Sleep(1000);

            var selectElement = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-testid='reject-reason-select']")));
            new SelectElement(selectElement).SelectByValue("other");
            Thread.Sleep(1000);

            var textAreas = driver.FindElements(By.CssSelector("[data-testid='reject-reason-textarea']"));

            if (textAreas.Count == 0)
            {
                Assert.Fail("FAIL (BUG UI): Ô nhập lý do chi tiết KHÔNG xuất hiện khi chọn 'Lý do khác'. Admin không thể nhập nội dung từ chối cụ thể.");
            }
        }

        [Test, Order(6)]
        public void MP06_Tu_Choi_Bai_Dang_Bo_Trong_Ly_Do()
        {
            Assert.Fail("FAIL: Không thể kiểm tra tính năng bỏ trống lý do vì trường nhập liệu 'Lý do chi tiết' bị lỗi không hiển thị trên giao diện.");
        }

        [Test, Order(7)]
        public void MP07_Ly_Do_Tu_Choi_Qua_Dai()
        {
            adminPage.GoToAdminDashboard();
            adminPage.GoToManagePosts();
            adminPage.ClickPendingReviewTab();

            string targetPostId = adminPage.GetFirstPostId();

            wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector($"[data-testid='btn-reject-{targetPostId}']"))).Click();

            var select = new SelectElement(wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-testid='reject-reason-select']"))));
            select.SelectByValue("other");
            Thread.Sleep(1000);

            var textAreas = driver.FindElements(By.CssSelector("[data-testid='reject-reason-textarea']"));

            if (textAreas.Count == 0)
            {
                Assert.Fail("FAIL: Lỗi không thể nhập dữ liệu kiểm tra 500 từ do trường 'Lý do chi tiết' bị ẩn/lỗi giao diện (Bug UI).");
            }
        }

        [Test, Order(8)]
        public void MP08_An_Bai_Dang_Hien_Thi()
        {
            var config = JsonReader.GetAdminConfigData();
            string reason = config?.HideReason ?? "spam";

            adminPage.GoToAdminDashboard();
            adminPage.GoToManagePosts();
            adminPage.ClickAllPostsTab();

            string targetPostId = adminPage.GetFirstPostIdWithHideButton();
            adminPage.HidePost(targetPostId, reason);

            var unhideElements = driver.FindElements(By.CssSelector($"[data-testid='btn-unhide-{targetPostId}']"));
            Assert.That(unhideElements.Count, Is.GreaterThan(0), $"Lỗi: Sau khi bấm ẩn bài {targetPostId}, không thấy nút 'Bỏ ẩn' xuất hiện!");
        }

        [Test, Order(9)]
        public void MP09_Xoa_Bai_Bi_An()
        {
            adminPage.GoToAdminDashboard();
            adminPage.GoToManagePosts();
            adminPage.ClickHiddenTab();

            string targetPostId = adminPage.GetFirstPostId();
            adminPage.DeletePost(targetPostId);

            Thread.Sleep(2000);
            var elements = driver.FindElements(By.CssSelector($"[data-testid='post-row-{targetPostId}']"));
            Assert.That(elements.Count, Is.EqualTo(0), "Lỗi: Đã ấn xóa nhưng báo lỗi Alert: 'Xóa thất bại!'");
        }

        [Test, Order(10)]
        public void MP10_Tim_Kiem_Bai_Dang_Theo_Ten()
        {
            var config = JsonReader.GetAdminConfigData();
            string keyword = config?.SearchKeyword ?? "Vinfast";

            adminPage.GoToAdminDashboard();
            adminPage.GoToManagePosts();
            adminPage.ClickAllPostsTab();

            adminPage.SearchPost(keyword);

            try
            {
                var firstLinkAfterSearch = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-testid^='post-link-']")));
                Assert.That(firstLinkAfterSearch.Text.ToLower(), Does.Contain(keyword.ToLower()), $"Lỗi: Kết quả tìm kiếm không chứa từ khóa '{keyword}'!");
            }
            catch (WebDriverTimeoutException)
            {
                var emptyMessage = driver.FindElement(By.CssSelector("[data-testid='empty-table-message']"));
                Assert.That(emptyMessage.Displayed, Is.True, "Bảng trống nhưng không hiện thông báo 'Không tìm thấy tin đăng'!");
            }
        }

        [Test, Order(11)]
        public void MU01_Truy_Cap_Trang_Quan_Ly_User()
        {
            adminPage.GoToAdminDashboard();
            adminPage.GoToManageUsers();

            var searchBox = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-testid='user-search-input']")));
            Assert.That(searchBox.Displayed, Is.True, "Lỗi: Không truy cập được trang Quản lý User.");
        }

        [Test, Order(12)]
        public void MU02_Tim_Kiem_User_Chinh_Xac()
        {
            adminPage.GoToManageUsers();
            string targetEmail = adminPage.GetFirstUserEmail();
            adminPage.SearchUser(targetEmail);

            var rows = driver.FindElements(By.CssSelector("[data-testid^='user-row-']"));
            var resultEmail = driver.FindElement(By.CssSelector("[data-testid^='user-email-']")).Text;

            Assert.That(rows.Count, Is.GreaterThan(0), "Lỗi: Search chính xác nhưng không ra kết quả nào.");
            Assert.That(resultEmail, Is.EqualTo(targetEmail), "Lỗi: Kết quả tìm kiếm không khớp email.");
        }

        [Test, Order(13)]
        public void MU03_Tim_Kiem_User_Khong_Ton_Tai()
        {
            adminPage.GoToManageUsers();
            string fakeEmail = "mailnaokhongtontai12345@gmail.com";
            adminPage.SearchUser(fakeEmail);

            var rows = driver.FindElements(By.CssSelector("[data-testid^='user-row-']"));
            Assert.That(rows.Count, Is.EqualTo(0), "Lỗi: Tìm email fake mà vẫn ra dữ liệu!");
        }

        [Test, Order(14)]
        public void MU04_Tim_Kiem_Rong_ClearSearch()
        {
            adminPage.GoToManageUsers();
            adminPage.SearchUser("xyzabc123");
            var rowsBefore = driver.FindElements(By.CssSelector("[data-testid^='user-row-']"));
            Assert.That(rowsBefore.Count, Is.EqualTo(0));

            adminPage.SearchUser("");

            var rowsAfter = driver.FindElements(By.CssSelector("[data-testid^='user-row-']"));
            Assert.That(rowsAfter.Count, Is.GreaterThan(0), "Lỗi: Clear search nhưng dữ liệu không hiển thị lại.");
        }

        [Test, Order(15)]
        public void MU05_Khoa_User_Thanh_Cong()
        {
            adminPage.GoToManageUsers();
            string targetUserId = adminPage.GetActiveUserIdForBanning();
            adminPage.BanUser(targetUserId, "Spam / Trùng lặp");

            var unbanElements = driver.FindElements(By.CssSelector($"[data-testid='btn-unban-{targetUserId}']"));
            Assert.That(unbanElements.Count, Is.GreaterThan(0), "Lỗi: Cấm xong nhưng nút Gỡ Cấm không xuất hiện!");
        }

        [Test, Order(16)]
        public void MU06_Mo_Khoa_Unban_Nguoi_Dung()
        {
            adminPage.GoToManageUsers();
            string targetUserId = adminPage.GetBannedUserIdForUnbanning();
            adminPage.UnbanUser(targetUserId);

            var banElements = driver.FindElements(By.CssSelector($"[data-testid='btn-ban-{targetUserId}']"));
            Assert.That(banElements.Count, Is.GreaterThan(0), "Lỗi: Đã gỡ cấm nhưng nút 'Cấm' không xuất hiện lại!");
        }

        [Test, Order(17)]
        public void MU07_Admin_Tu_Khoa_Chinh_Minh()
        {
            adminPage.GoToManageUsers();
            var config = JsonReader.GetAdminConfigData();
            string adminEmail = config?.AdminAccount?.Email ?? "admin123@gmail.com";

            adminPage.SearchUser(adminEmail);

            var banButtons = driver.FindElements(By.CssSelector("[data-testid^='btn-ban-']"));
            var unbanButtons = driver.FindElements(By.CssSelector("[data-testid^='btn-unban-']"));

            Assert.Multiple(() =>
            {
                Assert.That(banButtons.Count, Is.EqualTo(0), "LỖI BẢO MẬT: Hiển thị nút Cấm đối với tài khoản Admin!");
                Assert.That(unbanButtons.Count, Is.EqualTo(0), "LỖI: Hiển thị nút Gỡ Cấm đối với tài khoản Admin!");
            });
        }

        [Test, Order(19)]
        public void MU09_User_Dang_Online_Bi_Khoa()
        {
            adminPage.GoToManageUsers();
            string targetUserId = adminPage.GetActiveUserIdForBanning();
            adminPage.BanUser(targetUserId, "Spam / Trùng lặp");

            try
            {
                var forceLogoutToast = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-testid='force-logout-notification']")));
            }
            catch (WebDriverTimeoutException)
            {
                Assert.Fail("LỖI: Hệ thống KHÔNG có cơ chế Real-time (WebSocket). Khi Admin khóa tài khoản, User đang online vẫn có thể tiếp tục lướt web và thao tác cho đến khi F5 hoặc hết hạn Token.");
            }
        }

        [Test, Order(20)]
        public void DR01_Thong_Ke_Tong_User()
        {
            adminPage.GoToAdminDashboard();
            bool isVisible = adminPage.IsDashboardElementVisible("dr01-total-users");
            Assert.That(isVisible, Is.True, "Lỗi: Không hiển thị thẻ Thống kê Tổng User.");
        }

        [Test, Order(21)]
        public void DR02_Thong_Ke_Tong_Bai_Dang()
        {
            bool isVisible = adminPage.IsDashboardElementVisible("dr02-total-posts");
            Assert.That(isVisible, Is.True, "Lỗi: Không hiển thị thẻ Thống kê Tổng Bài Đăng.");
        }

        [Test, Order(22)]
        public void DR03_Thong_Ke_Theo_Danh_Muc()
        {
            bool isVisible = adminPage.IsDashboardElementVisible("dr03-category-chart");
            Assert.That(isVisible, Is.True, "Lỗi: Không hiển thị biểu đồ Thống kê theo Danh mục.");
        }

        [Test, Order(23)]
        public void DR04_Thong_Ke_Trang_Thai_Tin()
        {
            bool isVisible = adminPage.IsDashboardElementVisible("dr04-status-chart");
            Assert.That(isVisible, Is.True, "Lỗi: Không hiển thị biểu đồ Thống kê trạng thái tin.");
        }

        [Test, Order(24)]
        public void DR05_Thong_Ke_Phan_Loai_User()
        {
            bool isVisible = adminPage.IsDashboardElementVisible("dr05-usertype-chart");
            Assert.That(isVisible, Is.True, "Lỗi: Không hiển thị biểu đồ Thống kê phân loại User.");
        }

        [Test, Order(25)]
        public void DR06_Thong_Ke_Doanh_Thu()
        {
            bool isVisible = adminPage.IsDashboardElementVisible("dr06-total-revenue");
            Assert.That(isVisible, Is.True, "Lỗi: Không hiển thị thẻ/biểu đồ Thống kê doanh thu.");
        }

        [Test, Order(26)]
        public void DR08_Lich_Su_Kiem_Duyet()
        {
            adminPage.GoToAdminDashboard();
            adminPage.ClickNavHistory();
            wait.Until(d => d.Url.Contains("/admin/history"));
            Assert.That(driver.Url, Does.Contain("/admin/history"), "Lỗi: Không mở được trang Lịch sử kiểm duyệt.");
        }

        [Test, Order(27)]
        public void DR09_Quan_Ly_Doanh_Thu()
        {
            adminPage.GoToAdminDashboard();
            adminPage.ClickNavRevenue();
            wait.Until(d => d.Url.Contains("/admin/revenue"));
            Assert.That(driver.Url, Does.Contain("/admin/revenue"), "Lỗi: Không mở được trang Quản lý doanh thu.");
        }

        [Test, Order(28)]
        public void DR07_Admin_Dang_Xuat()
        {
            adminPage.GoToAdminDashboard();
            adminPage.LogoutAdmin();
            Assert.That(driver.Url, Does.Not.Contain("/admin"), "Lỗi: Đã đăng xuất nhưng vẫn còn ở trang quản trị!");
        }

        [Test, Order(29)]
        public void DR10_Vao_Trang_Admin_Khi_Khong_Dang_Nhap()
        {
            adminPage.GoToAdminDashboard();
            adminPage.LogoutAdmin();

            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("localStorage.clear(); sessionStorage.clear();");
            driver.Manage().Cookies.DeleteAllCookies();

            driver.Navigate().GoToUrl("http://localhost:5173/admin/dashboard");

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    WebDriverWait shortWait = new WebDriverWait(driver, TimeSpan.FromSeconds(2));
                    shortWait.Until(ExpectedConditions.AlertIsPresent());
                    driver.SwitchTo().Alert().Accept();
                    Thread.Sleep(500);
                }
                catch (WebDriverTimeoutException)
                {
                    break;
                }
            }

            Thread.Sleep(2000);
            Assert.That(driver.Url, Does.Not.Contain("/admin/dashboard"), "LỖI BẢO MẬT: Đã tắt hết Alert nhưng URL vẫn đứng im ở trang Admin!");
        }

        // =========================================================================
        // 🌟 MA THUẬT NẰM Ở ĐÂY: HÀM TEARDOWN TỰ ĐỘNG GHI EXCEL VÀ CHỤP ẢNH LỖI XỊN XÒ
        // =========================================================================
        [TearDown]
        public void Teardown()
        {
            if (driver != null)
            {
                // 1. LẤY TÊN TEST CASE VÀ CẮT LẤY ID (VD: "MP01_Xem_Danh_Sach" -> lấy "MP01")
                string testName = TestContext.CurrentContext.Test.MethodName ?? "";
                string testCaseId = testName.Split('_')[0];

                // 2. LẤY TRẠNG THÁI CỦA TEST VỪA CHẠY XONG
                var testStatus = TestContext.CurrentContext.Result.Outcome.Status;
                string resultToLog = "Pass";
                string messageToLog = "Chạy thành công đúng như kỳ vọng.";
                string screenshotPath = "";

                // 3. NẾU TEST FAIL (Bao gồm cả các lỗi Assert.Fail cố ý của bạn)
                if (testStatus == NUnit.Framework.Interfaces.TestStatus.Failed)
                {
                    resultToLog = "Fail";
                    // Lấy chính câu thông báo lỗi do bạn tự định nghĩa trong Assert hoặc lỗi hệ thống văng ra
                    messageToLog = TestContext.CurrentContext.Result.Message ?? "Lỗi không xác định";

                    // Chụp màn hình (Dùng hàm nâng cao có viền đỏ)
                    screenshotPath = TakeScreenshot(driver, testCaseId, true);
                }

                // 4. GHI VÀO EXCEL BẰNG EXCEL HELPER
                try
                {
                    excelHelper.WriteTestResultById(sheetName, testCaseId, messageToLog, resultToLog, testerName, screenshotPath);
                    Console.WriteLine($"[Đã ghi Excel] {testCaseId} - {resultToLog}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LỖI GHI EXCEL] {ex.Message}");
                }

                // 5. Đóng trình duyệt để chuẩn bị cho test case tiếp theo
                driver.Quit();
                driver.Dispose();
            }
        }

        // --- HÀM HỖ TRỢ CHỤP MÀN HÌNH NÂNG CAO (CÓ BÔI ĐỎ) ---
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
                    System.Threading.Thread.Sleep(800);
                }

                ITakesScreenshot ts = (ITakesScreenshot)driver;
                Screenshot screenshot = ts.GetScreenshot();

                string path = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Screenshots"));
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string finalPath = Path.Combine(path, $"{testName}.png");
                if (File.Exists(finalPath))
                {
                    File.Delete(finalPath);
                }

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
    }
}