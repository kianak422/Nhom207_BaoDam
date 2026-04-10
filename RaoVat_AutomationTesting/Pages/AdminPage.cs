using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;

namespace RaoVat_AutomationTesting.Pages
{
    public class AdminPage
    {
        private IWebDriver driver;
        private WebDriverWait wait;

        public AdminPage(IWebDriver driver)
        {
            this.driver = driver;
            this.wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        // --- LOCATORS ---
        private By ManagePostsMenu => By.CssSelector("a[href='/admin/posts']");
        private By PendingReviewTab => By.CssSelector("[data-testid='admin-tab-pending_review']");
        private By AllPostsTab => By.CssSelector("[data-testid='admin-tab-all']");


        // 🌟 BỔ SUNG LOCATORS MỚI
        private By ActiveTab => By.CssSelector("[data-testid='admin-tab-active']");
        private By HiddenTab => By.CssSelector("[data-testid='admin-tab-hidden_by_admin']");
        private By SearchInput => By.CssSelector("[data-testid='admin-search-input']");

        // 🌟 LOCATORS CHO MODAL ẨN TIN
        private By HideReasonSelect => By.CssSelector("[data-testid='hide-reason-select']");
        private By HideConfirmBtn => By.CssSelector("[data-testid='hide-confirm-btn']");

        // Dynamic Locators
        private By GetPostTitleLink(string postId) => By.CssSelector($"[data-testid='post-link-{postId}']");
        private By GetApproveButton(string postId) => By.CssSelector($"[data-testid='btn-approve-{postId}']");
        private By GetHideButton(string postId) => By.CssSelector($"[data-testid='btn-hide-{postId}']");
        private By GetDeleteButton(string postId) => By.CssSelector($"[data-testid='btn-delete-{postId}']");


        // --- ACTIONS ---

        public void GoToAdminDashboard()
        {
            driver.Navigate().GoToUrl("http://localhost:5173/admin/dashboard");
        }

        public void GoToManagePosts()
        {
            var menu = wait.Until(ExpectedConditions.ElementToBeClickable(ManagePostsMenu));
            menu.Click();
        }

        public void ClickPendingReviewTab()
        {
            wait.Until(ExpectedConditions.ElementToBeClickable(PendingReviewTab)).Click();
        }

        // 🌟 BỔ SUNG ACTIONS MỚI
        public void ClickActiveTab()
        {
            wait.Until(ExpectedConditions.ElementToBeClickable(ActiveTab)).Click();
            Thread.Sleep(1000); // Chờ load data
        }

        public void ClickHiddenTab()
        {
            wait.Until(ExpectedConditions.ElementToBeClickable(HiddenTab)).Click();
            Thread.Sleep(1000); // Chờ load data
        }

        public void SearchPost(string keyword)
        {
            var searchBox = wait.Until(ExpectedConditions.ElementIsVisible(SearchInput));
            searchBox.Clear();
            searchBox.SendKeys(keyword);
            // Có thể cần gửi phím Enter nếu app yêu cầu: searchBox.SendKeys(Keys.Enter);
            Thread.Sleep(2000); // Đợi React filter lại danh sách
        }

        public void HidePost(string postId, string reasonValue = "spam")
        {
            // 1. Tìm và bấm nút Ẩn ở dòng tương ứng
            var hideBtn = wait.Until(ExpectedConditions.ElementToBeClickable(GetHideButton(postId)));
            hideBtn.Click();

            // 2. Đợi Modal hiện lên (khoảng 1s cho chắc)
            Thread.Sleep(1000);

            // 3. Chọn lý do trong Modal
            var selectElement = wait.Until(ExpectedConditions.ElementIsVisible(HideReasonSelect));
            var select = new SelectElement(selectElement);
            select.SelectByValue(reasonValue);

            // 4. Bấm nút "Xác nhận ẩn" trong Modal
            var confirmBtn = wait.Until(ExpectedConditions.ElementToBeClickable(HideConfirmBtn));
            confirmBtn.Click();

            // 🌟 KHÔNG CẦN AlertIsPresent ở đây nữa vì web không có alert.
            // Chúng ta chỉ cần đợi một chút để React cập nhật lại giao diện bảng.
            Thread.Sleep(2000);
        }

        public void DeletePost(string postId)
        {
            // Bấm nút Xóa
            wait.Until(ExpectedConditions.ElementToBeClickable(GetDeleteButton(postId))).Click();

            // Alert 1: Xác nhận bạn có chắc muốn xóa không?
            wait.Until(ExpectedConditions.AlertIsPresent());
            driver.SwitchTo().Alert().Accept();

            // Alert 2: Báo xóa thành công
            wait.Until(ExpectedConditions.AlertIsPresent());
            driver.SwitchTo().Alert().Accept();
        }

        public void ClickPostTitleToViewDetail(string postId)
        {
            var link = wait.Until(ExpectedConditions.ElementToBeClickable(GetPostTitleLink(postId)));
            link.Click();
        }

        public void ApprovePost(string postId)
        {
            wait.Until(ExpectedConditions.ElementToBeClickable(GetApproveButton(postId))).Click();
            wait.Until(ExpectedConditions.AlertIsPresent());
            driver.SwitchTo().Alert().Accept();
            wait.Until(ExpectedConditions.AlertIsPresent());
            driver.SwitchTo().Alert().Accept();
        }

        public string GetFirstPostId()
        {
            var firstLink = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-testid^='post-link-']")));
            string fullTestId = firstLink.GetAttribute("data-testid");
            string postId = fullTestId.Replace("post-link-", "");
            Console.WriteLine("Đã lấy ID: " + postId);
            return postId;
        }

        // Lấy tiêu đề của bài đăng đầu tiên (dùng cho test search)
        public string GetFirstPostTitle()
        {
            var firstLink = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-testid^='post-link-']")));
            return firstLink.Text;
        }

        // Thêm vào vùng ACTIONS trong class AdminPage:
        public void ClickAllPostsTab()
        {
            wait.Until(ExpectedConditions.ElementToBeClickable(AllPostsTab)).Click();
            Thread.Sleep(1000); // Đợi React render lại bảng dữ liệu
        }

        // 🌟 HÀM MỚI QUAN TRỌNG: Tìm ID của bài viết đầu tiên có chứa nút "Ẩn"
        public string GetFirstPostIdWithHideButton()
        {
            // Tìm phần tử đầu tiên là nút Ẩn (bắt đầu bằng btn-hide-)
            var firstHideBtn = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-testid^='btn-hide-']")));

            // Lấy chuỗi data-testid (ví dụ: "btn-hide-12345ABC")
            string fullTestId = firstHideBtn.GetAttribute("data-testid");

            // Cắt bỏ chữ "btn-hide-" để lấy ID thực tế
            string postId = fullTestId.Replace("btn-hide-", "");

            Console.WriteLine("Đã quét thấy bài viết có thể ẨN với ID: " + postId);
            return postId;
        }

        // 🌟 LOCATORS CHO QUẢN LÝ USER
        private By UserSearchInput => By.CssSelector("[data-testid='user-search-input']");
        private By RejectReasonSelect => By.CssSelector("[data-testid='reject-reason-select']");
        private By RejectConfirmBtn => By.CssSelector("[data-testid='reject-confirm-btn']");

        // 🌟 ACTIONS CHO QUẢN LÝ USER

        public void GoToManageUsers()
        {
            // Navigate thẳng qua URL cho nhanh gọn và ổn định
            driver.Navigate().GoToUrl("http://localhost:5173/admin/users");
            // Chờ ô search xuất hiện để đảm bảo trang đã load xong
            wait.Until(ExpectedConditions.ElementIsVisible(UserSearchInput));
        }

        public void SearchUser(string keyword)
        {
            var searchBox = wait.Until(ExpectedConditions.ElementIsVisible(UserSearchInput));
            searchBox.Clear(); // Xóa trắng trước khi nhập
            searchBox.SendKeys(keyword);
            Thread.Sleep(1500); // Chờ React filter danh sách
        }

        // Hàm lấy Email của user đầu tiên trong bảng (dùng để test MU02)
        public string GetFirstUserEmail()
        {
            var firstEmailCell = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-testid^='user-email-']")));
            return firstEmailCell.Text;
        }

        // Hàm tìm ID của một user chưa bị cấm (có chứa nút Cấm) để test MU05
        public string GetActiveUserIdForBanning()
        {
            var banBtn = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-testid^='btn-ban-']")));
            string fullTestId = banBtn.GetAttribute("data-testid");
            return fullTestId.Replace("btn-ban-", "");
        }

        public void BanUser(string userId, string reasonValue = "other")
        {
            // 1. Bấm nút Cấm
            wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector($"[data-testid='btn-ban-{userId}']"))).Click();
            Thread.Sleep(1000); // Đợi Dialog xuất hiện

            // 2. Chọn lý do
            var selectElement = wait.Until(ExpectedConditions.ElementIsVisible(RejectReasonSelect));
            var select = new SelectElement(selectElement);
            select.SelectByValue(reasonValue);

            // 3. Bấm xác nhận cấm
            var confirmBtn = wait.Until(ExpectedConditions.ElementToBeClickable(RejectConfirmBtn));
            confirmBtn.Click();

            // 4. Xử lý alert("Đã cấm người dùng thành công.") có trong code React của bạn
            wait.Until(ExpectedConditions.AlertIsPresent());
            driver.SwitchTo().Alert().Accept();

            Thread.Sleep(1000); // Chờ giao diện cập nhật nút Gỡ cấm
        }

        // Hàm tìm ID của một user ĐÃ BỊ CẤM để test MU06
        public string GetBannedUserIdForUnbanning()
        {
            var unbanBtn = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-testid^='btn-unban-']")));
            string fullTestId = unbanBtn.GetAttribute("data-testid");
            return fullTestId.Replace("btn-unban-", "");
        }

        public void UnbanUser(string userId)
        {
            // 1. Bấm nút Gỡ Cấm
            wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector($"[data-testid='btn-unban-{userId}']"))).Click();

            // 2. Xử lý alert confirm: window.confirm(`Bạn có chắc muốn GỠ CẤM...`)
            wait.Until(ExpectedConditions.AlertIsPresent());
            driver.SwitchTo().Alert().Accept();

            // Đợi React cập nhật giao diện
            Thread.Sleep(1000);
        }

        // Hàm kiểm tra xem một thẻ thống kê/biểu đồ trên Dashboard có xuất hiện không
        public bool IsDashboardElementVisible(string dataTestId)
        {
            // Đảm bảo đang ở trang Dashboard
            if (!driver.Url.Contains("/admin/dashboard"))
            {
                GoToAdminDashboard();
            }

            try
            {
                // Chờ component render xong (Chart.js load có thể mất 1 chút thời gian)
                var element = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector($"[data-testid='{dataTestId}']")));
                return element.Displayed;
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }

        // 🌟 LOCATORS MỚI
        private By AdminAvatarMenu => By.CssSelector("[data-testid='admin-avatar-menu']");
        private By AdminLogoutBtn => By.CssSelector("[data-testid='admin-logout-btn']");
        private By NavHistory => By.CssSelector("[data-testid='nav-history']");
        private By NavRevenue => By.CssSelector("[data-testid='nav-revenue']");

        // 🌟 ACTIONS MỚI

        public void ClickNavHistory()
        {
            wait.Until(ExpectedConditions.ElementToBeClickable(NavHistory)).Click();
        }

        public void ClickNavRevenue()
        {
            wait.Until(ExpectedConditions.ElementToBeClickable(NavRevenue)).Click();
        }

        public void LogoutAdmin()
        {
            // 1. Mở menu avatar
            wait.Until(ExpectedConditions.ElementToBeClickable(AdminAvatarMenu)).Click();

            // 2. 🌟 CHIÊU CUỐI: Dùng JavaScript để "XÓA SỔ" hàm alert và confirm của trình duyệt
            // Từ giây phút này, dù code React có gọi alert() bao nhiêu lần thì trình duyệt cũng im lặng.
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("window.alert = function() {}; window.confirm = function() { return true; };");
            Console.WriteLine("Đã vô hiệu hóa toàn bộ Alert của trình duyệt.");

            // 3. Bây giờ mới bấm nút Đăng xuất (Sẽ không có bất kỳ alert nào hiện lên làm phiền nữa)
            wait.Until(ExpectedConditions.ElementToBeClickable(AdminLogoutBtn)).Click();

            // 4. Đợi 3 giây để Firebase thực hiện Logout và React chuyển hướng (Navigate)
            Thread.Sleep(3000);
        }
        // --- BỔ SUNG LOCATORS MỚI ---
        private By RejectReasonTextarea => By.CssSelector("[data-testid='reject-reason-textarea']");
        private By RejectCancelBtn => By.CssSelector("[data-testid='reject-cancel-btn']");
        private By GetRejectButton(string postId) => By.CssSelector($"[data-testid='btn-reject-{postId}']");

        // --- BỔ SUNG ACTION MỚI ---

        public void RejectPost(string postId, string reasonValue, string customReason = "")
        {
            // 1. Tìm và bấm nút "Từ chối" ở dòng tương ứng
            wait.Until(ExpectedConditions.ElementToBeClickable(GetRejectButton(postId))).Click();
            Thread.Sleep(1000); // Chờ Modal hiện lên

            // 2. Chọn lý do trong thẻ Select
            var selectElement = wait.Until(ExpectedConditions.ElementIsVisible(RejectReasonSelect));
            var select = new SelectElement(selectElement);
            select.SelectByValue(reasonValue);

            // 3. Nếu chọn "other" (Lý do khác) thì điền text vào ô textarea
            if (reasonValue == "other")
            {
                var textArea = wait.Until(ExpectedConditions.ElementIsVisible(RejectReasonTextarea));
                textArea.Clear();
                textArea.SendKeys(customReason);
            }

            // 4. Bấm Xác nhận
            wait.Until(ExpectedConditions.ElementToBeClickable(RejectConfirmBtn)).Click();
        }

        public void CancelRejectModal()
        {
            wait.Until(ExpectedConditions.ElementToBeClickable(RejectCancelBtn)).Click();
            Thread.Sleep(500);
        }
    }
}