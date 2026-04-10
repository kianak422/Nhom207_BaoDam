using System;
using System.IO;
using OpenQA.Selenium;

namespace RaoVat_AutomationTesting.Utilities
{
    public class ScreenshotHelper
    {
        // Đường dẫn tương đối: Tạo thư mục Screenshots ngang hàng với Tests, Pages và file Excel
        private static readonly string BaseFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Screenshots"));

        public static string TakeAndReplaceScreenshot(IWebDriver driver, string testName, bool isError = false)
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

                if (!Directory.Exists(BaseFolder))
                {
                    Directory.CreateDirectory(BaseFolder);
                }

                // Chống spam: Tên file cố định theo ID Test Case
                string finalPath = Path.Combine(BaseFolder, $"{testName}.png");

                if (File.Exists(finalPath))
                {
                    File.Delete(finalPath);
                }

                ITakesScreenshot ts = (ITakesScreenshot)driver;
                ts.GetScreenshot().SaveAsFile(finalPath);

                return finalPath;
            }
            catch (UnhandledAlertException)
            {
                try { driver.SwitchTo().Alert().Accept(); } catch { }

                string finalPath = Path.Combine(BaseFolder, $"{testName}.png");
                if (File.Exists(finalPath))
                {
                    File.Delete(finalPath);
                }

                ITakesScreenshot ts = (ITakesScreenshot)driver;
                ts.GetScreenshot().SaveAsFile(finalPath);
                return finalPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi chụp ảnh: {ex.Message}");
                return "";
            }
        }
    }
}