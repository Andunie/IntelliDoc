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

        public AuthController(UserManager<ApplicationUser> userManager, TokenService tokenService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
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

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { Message = "Kayıt başarılı." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return Unauthorized("Kullanıcı bulunamadı.");

            var result = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!result) return Unauthorized("Şifre yanlış.");

            var token = _tokenService.CreateToken(user);

            return Ok(new { Token = token, UserId = user.Id, FullName = user.FullName });
        }
    }

    public record RegisterDto(string Email, string Password, string FullName, string Department);
    public record LoginDto(string Email, string Password);
}
