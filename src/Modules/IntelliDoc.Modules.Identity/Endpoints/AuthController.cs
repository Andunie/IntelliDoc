using IntelliDoc.Modules.Identity.Entities;
using IntelliDoc.Modules.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelliDoc.Modules.Identity.Endpoints
{
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TokenService _tokenService;
        private readonly EmailService _emailService;

        public AuthController(UserManager<ApplicationUser> userManager, TokenService tokenService, EmailService emailService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                Department = dto.Department
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (result.Succeeded)
            {
                // 1. Token Üret
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // 2. Link Oluştur (Frontend URL'i)
                // Token'ı URL-Safe yapmak gerekebilir (HttpUtility.UrlEncode) ama şimdilik basit tutalım.
                var link = $"http://localhost:3000/auth/confirm-email?userId={user.Id}&token={System.Net.WebUtility.UrlEncode(token)}";

                // 3. Mail At
                await _emailService.SendEmailAsync(user.Email, "IntelliDoc - Hesabını Doğrula",
                    $"<h1>Hoşgeldin!</h1><p>Hesabını doğrulamak için <a href='{link}'>buraya tıkla</a>.</p>");

                return Ok(new { Message = "Kayıt başarılı. Lütfen e-postanızı kontrol edin." });
            }

            return Ok(new { Message = "Kayıt başarılı." });
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return BadRequest("Kullanıcı yok.");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded) return Ok("Email doğrulandı.");

            return BadRequest("Doğrulama başarısız.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            // 1. Kullanıcıyı Bul
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return Unauthorized("Kullanıcı bulunamadı.");

            // 2. Şifreyi Kontrol Et
            var checkPassword = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!checkPassword) return Unauthorized("Şifre yanlış.");

            // 3. E-Posta Doğrulaması Kontrolü
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return Unauthorized("Lütfen önce e-posta adresinizi doğrulayın. (Mail kutunuzu kontrol edin).");
            }

            // 4. Token Üret ve Ver
            var token = _tokenService.CreateToken(user);

            return Ok(new { Token = token, UserId = user.Id, FullName = user.FullName });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                // Güvenlik için "Kullanıcı yok" demek yerine "Varsa mail gönderdik" denir (User Enumeration Attack önlemek için)
                return Ok("Eğer kayıtlıysa, şifre sıfırlama bağlantısı gönderildi.");

            // 1. Token Üret (Reset Token)
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // 2. Link Oluştur (Frontend URL)
            // Örn: http://localhost:3000/auth/reset-password?email=...&token=...
            var link = $"http://localhost:3000/auth/reset-password?email={user.Email}&token={System.Net.WebUtility.UrlEncode(token)}";

            // 3. Mail At
            await _emailService.SendEmailAsync(user.Email, "IntelliDoc - Şifre Sıfırlama",
                $"<p>Şifrenizi sıfırlamak için <a href='{link}'>buraya tıklayın</a>.</p>");

            return Ok("Eğer kayıtlıysa, şifre sıfırlama bağlantısı gönderildi.");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return BadRequest("Geçersiz istek.");

            // Şifreyi Sıfırla
            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);

            if (result.Succeeded)
                return Ok("Şifreniz başarıyla değiştirildi. Giriş yapabilirsiniz.");

            return BadRequest(result.Errors);
        }
    }

    public record RegisterDto(string Email, string Password, string FullName, string Department);
    public record LoginDto(string Email, string Password);

    public record ForgotPasswordDto(string Email);
    public record ResetPasswordDto(string Email, string Token, string NewPassword);
}
