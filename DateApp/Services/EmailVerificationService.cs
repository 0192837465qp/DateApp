using System;
using System.Threading.Tasks;
using System.Text;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Maui.Storage;
using System.Net.Mail;
using System.Net;

namespace DateApp.Services
{
    public class EmailVerificationService
    {
        private readonly HttpClient _httpClient;

        public EmailVerificationService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<(bool success, string message)> SendVerificationCodeAsync(string email, string userName)
        {
            try
            {
                // Generează cod de verificare
                var verificationCode = GenerateVerificationCode();

                // Salvează codul local cu timestamp
                var codeData = new VerificationCodeData
                {
                    Code = verificationCode,
                    Email = email,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10), // Expiră în 10 minute
                    CreatedAt = DateTime.UtcNow
                };

                await SecureStorage.SetAsync($"verification_code_{email}", JsonSerializer.Serialize(codeData));

                // În modul DEBUG, afișează codul în consolă
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"=== EMAIL VERIFICATION CODE ===");
                System.Diagnostics.Debug.WriteLine($"Email: {email}");
                System.Diagnostics.Debug.WriteLine($"Code: {verificationCode}");
                System.Diagnostics.Debug.WriteLine($"Expires: {codeData.ExpiresAt}");
                System.Diagnostics.Debug.WriteLine($"==============================");
#endif

                // Pentru demo, simulează trimiterea emailului
                bool emailSent = await SimulateEmailSendAsync(email, userName, verificationCode);

                if (emailSent)
                {
                    return (true, "Verification code sent to your email!");
                }
                else
                {
                    return (false, "Failed to send verification email. Please try again.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Send verification error: {ex.Message}");
                return (false, "Error sending verification code. Please try again.");
            }
        }

        public async Task<(bool success, string message)> VerifyCodeAsync(string email, string enteredCode)
        {
            try
            {
                // Recuperează codul salvat
                var savedCodeJson = await SecureStorage.GetAsync($"verification_code_{email}");

                if (string.IsNullOrEmpty(savedCodeJson))
                {
                    return (false, "No verification code found. Please request a new one.");
                }

                var savedCodeData = JsonSerializer.Deserialize<VerificationCodeData>(savedCodeJson);

                // Verifică dacă codul a expirat
                if (DateTime.UtcNow > savedCodeData.ExpiresAt)
                {
                    // Șterge codul expirat
                    SecureStorage.Remove($"verification_code_{email}");
                    return (false, "Verification code has expired. Please request a new one.");
                }

                // Verifică dacă codul este corect
                if (savedCodeData.Code.Equals(enteredCode, StringComparison.OrdinalIgnoreCase))
                {
                    // Șterge codul după verificare reușită
                    SecureStorage.Remove($"verification_code_{email}");
                    return (true, "Email verified successfully!");
                }
                else
                {
                    return (false, "Invalid verification code. Please try again.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Verify code error: {ex.Message}");
                return (false, "Error verifying code. Please try again.");
            }
        }

        public async Task<(bool success, string message)> ResendVerificationCodeAsync(string email, string userName)
        {
            try
            {
                // Verifică dacă nu s-a trimis un cod recent (rate limiting)
                var lastCodeJson = await SecureStorage.GetAsync($"verification_code_{email}");

                if (!string.IsNullOrEmpty(lastCodeJson))
                {
                    var lastCodeData = JsonSerializer.Deserialize<VerificationCodeData>(lastCodeJson);
                    var timeSinceLastCode = DateTime.UtcNow - lastCodeData.CreatedAt;

                    if (timeSinceLastCode.TotalMinutes < 1) // Permite retrimite doar după 1 minut
                    {
                        var waitTime = 60 - (int)timeSinceLastCode.TotalSeconds;
                        return (false, $"Please wait {waitTime} seconds before requesting a new code.");
                    }
                }

                // Trimite cod nou
                return await SendVerificationCodeAsync(email, userName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Resend code error: {ex.Message}");
                return (false, "Error resending code. Please try again.");
            }
        }

        private string GenerateVerificationCode()
        {
            // Generează cod de 6 cifre
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private async Task<bool> SimulateEmailSendAsync(string email, string userName, string verificationCode)
        {
            try
            {
                // Simulează delay de rețea
                await Task.Delay(1000);

                // În aplicația reală, aici ai implementa trimiterea emailului prin:
                // 1. SendGrid API
                // 2. SMTP Server
                // 3. EmailJS
                // 4. Azure Communication Services
                // etc.

                System.Diagnostics.Debug.WriteLine($"📧 SIMULATED EMAIL SENT TO: {email}");
                System.Diagnostics.Debug.WriteLine($"👤 USER: {userName}");
                System.Diagnostics.Debug.WriteLine($"🔢 CODE: {verificationCode}");
                System.Diagnostics.Debug.WriteLine($"📅 TIME: {DateTime.Now}");

                // Pentru demo, consideră că emailul a fost trimis cu succes
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Simulate email error: {ex.Message}");
                return false;
            }
        }

        // Implementare pentru email real cu SMTP (comentată pentru demo)
        /*
        private async Task<bool> SendRealEmailAsync(string email, string userName, string verificationCode)
        {
            try
            {
                using var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential("your-email@gmail.com", "your-app-password")
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("your-email@gmail.com", "HeartSync"),
                    Subject = "HeartSync - Email Verification Code",
                    Body = $@"
                        <html>
                        <body>
                            <h2>Welcome to HeartSync, {userName}!</h2>
                            <p>Your verification code is:</p>
                            <h1 style='color: #FF6B6B; font-size: 32px; letter-spacing: 5px;'>{verificationCode}</h1>
                            <p>This code will expire in 10 minutes.</p>
                            <p>If you didn't create this account, please ignore this email.</p>
                        </body>
                        </html>",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);
                await client.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Real email error: {ex.Message}");
                return false;
            }
        }
        */

        public void ClearVerificationData(string email)
        {
            try
            {
                SecureStorage.Remove($"verification_code_{email}");
            }
            catch { }
        }
    }

    public class VerificationCodeData
    {
        public string Code { get; set; }
        public string Email { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}