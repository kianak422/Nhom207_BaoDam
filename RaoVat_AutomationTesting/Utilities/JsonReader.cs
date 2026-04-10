using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System;

namespace RaoVat_AutomationTesting.Utilities
{
    // 1. Data model cho Đăng nhập
    public class LoginData
    {
        public string? Id { get; set; }
        public string? Email { get; set; }
        public string? Pass { get; set; }
        public string? Expected { get; set; }
        public string? Msg { get; set; }
    }

    // 2. Data model cho Đăng ký
    public class RegisterData
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Pass { get; set; }
        public string? ConfirmPass { get; set; }
        public bool AgreeTerms { get; set; }
        public string? Expected { get; set; }
        public string? Msg { get; set; }
    }

    // 3. Data model cho Đăng tin / Sửa tin
    public class CreatePostData
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Price { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Condition { get; set; }
        public string Hang { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
        public string AddressDetail { get; set; }
        public int Images { get; set; }
        public string Expected { get; set; }
        public string Msg { get; set; }
        public string Area { get; set; }
    }

    // 4. Data model cho Tìm kiếm (Search)
    public class SearchData
    {
        public string? Id { get; set; }
        public string? Keyword { get; set; }
        public List<string>? Keywords { get; set; }
        public string? Category { get; set; }
        public string? Subcategory { get; set; }
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? MinPrice { get; set; }
        public string? MaxPrice { get; set; }
        public int? HomeBtnIndex { get; set; }
        public string? StartUrl { get; set; }
        public bool? Reset { get; set; }
        public bool? ClearModal { get; set; }
        public string? Expected { get; set; }
        public string? Msg { get; set; }
    }


    // 5. Data model cho Admin
    public class AdminAccountData
    {
        public string? Email { get; set; }
        public string? Pass { get; set; }
    }

    public class AdminConfigData
    {
        public AdminAccountData? AdminAccount { get; set; }
        public string? SearchKeyword { get; set; } // Dùng cho MP10
        public string? HideReason { get; set; }    // Dùng cho MP08 (giá trị: spam, fraud, prohibited, other)
    }
    public class InteractionData
    {
        public string? Id { get; set; }
        public string? Feature { get; set; }
        public string? InputText { get; set; }
        public string? Expected { get; set; }
        public string? Msg { get; set; }
    }

    // 6. Data model cho Integration Test
    public class IntegrationData
    {
        public string? Id { get; set; }
        public string? Scenario { get; set; }
        public string? UserEmail { get; set; }
        public string? UserPass { get; set; }
        public string? AdminEmail { get; set; }
        public string? PostTitle { get; set; }
        public string? SearchKeyword { get; set; }
        public string? Category { get; set; }
        public string? MinPrice { get; set; }
        public string? MaxPrice { get; set; }
        public string? ExpectedUrlContains { get; set; }
        public string? ExpectedStatus { get; set; }
    }

    // 7. THÊM MỚI: Data model cho Upgrade VIP
    public class VipAccountData
    {
        public string? Email { get; set; }
        public string? Pass { get; set; }
    }

    public class CardData
    {
        public string? Number { get; set; }
        public string? Expiry { get; set; }
        public string? Cvv { get; set; }
    }

    public class UpgradeVipConfigData
    {
        public VipAccountData? NormalAccount { get; set; }
        public VipAccountData? VipAccount { get; set; }
        public VipAccountData? NewAccount { get; set; }
        public string? BasicPackageId { get; set; }
        public string? DiscountPackageId { get; set; }
        public CardData? ValidCard { get; set; }
        public CardData? InvalidCard { get; set; }
    }

    public static class JsonReader
    {
        // Hàm đọc file loginData.json
        public static IEnumerable<LoginData>? GetLoginTestData()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "loginData.json");
            if (!File.Exists(path)) throw new FileNotFoundException($"kh tìm thấy file JSON tại: {path}");

            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<LoginData>>(json) ?? new List<LoginData>();
        }

        // Hàm đọc file registerData.json
        public static IEnumerable<RegisterData>? GetRegisterTestData()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "registerData.json");
            if (!File.Exists(path)) throw new FileNotFoundException($"kh tìm thấy file JSON tại: {path}");

            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<RegisterData>>(json) ?? new List<RegisterData>();
        }

        // Hàm đọc file CreatePost_F3_1.json
        public static IEnumerable<CreatePostData>? GetCreatePostTestData()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "CreatePost_F3_1.json");
            if (!File.Exists(path)) throw new FileNotFoundException($"kh tìm thấy file JSON tại: {path}");

            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<CreatePostData>>(json) ?? new List<CreatePostData>();
        }

        // Hàm đọc file EditPost_F3_2.json
        public static IEnumerable<CreatePostData>? GetEditPostTestData()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "EditPost_F3_2.json");
            if (!File.Exists(path)) throw new FileNotFoundException($"kh tìm thấy file JSON tại: {path}");

            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<CreatePostData>>(json) ?? new List<CreatePostData>();
        }

        // Hàm đọc file TCs-F4.json cho chức năng Search
        public static IEnumerable<SearchData>? GetSearchTestData()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "TCs-F4.json");
            if (!File.Exists(path)) throw new FileNotFoundException($"kh tìm thấy file JSON tại: {path}");

            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<SearchData>>(json) ?? new List<SearchData>();
        }

        // Hàm đọc file F7_Admin.json
        public static AdminConfigData? GetAdminConfigData()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "F7_Admin.json");
            if (!File.Exists(path)) throw new FileNotFoundException($"Kh tìm thấy file JSON tại: {path}");

            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<AdminConfigData>(json);
        }

        // Hàm đọc file IntegrateTCs.json
        public static IEnumerable<IntegrationData>? GetIntegrationTestData()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "IntegrateTCs.json");
            if (!File.Exists(path)) throw new FileNotFoundException($"Kh tìm thấy file JSON tại: {path}");

            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<IntegrationData>>(json) ?? new List<IntegrationData>();
        }
        public static IEnumerable<InteractionData> GetChatTestData()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "F5_1_Chat.json");
            if (!File.Exists(path)) return new List<InteractionData>();
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<InteractionData>>(json) ?? new List<InteractionData>();
        }

        // --- ĐÃ TÁCH: HÀM ĐỌC DATA CHO F5.2 COMMENT ---
        public static IEnumerable<InteractionData> GetCommentTestData()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "F5_2_Comment.json");
            if (!File.Exists(path)) return new List<InteractionData>();
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<InteractionData>>(json) ?? new List<InteractionData>();
        }
        // THÊM MỚI: Hàm đọc file upgradeVipData.json
        public static UpgradeVipConfigData? GetUpgradeVipConfigData()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "upgradeVipData.json");
            if (!File.Exists(path)) throw new FileNotFoundException($"Kh tìm thấy file JSON tại: {path}");

            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<UpgradeVipConfigData>(json);
        }
    }
}