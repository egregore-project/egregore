// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using egregore.Configuration;
using egregore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace egregore.Controllers
{
    public class TokenController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IOptionsSnapshot<WebServerOptions> _options;

        public TokenController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IOptionsSnapshot<WebServerOptions> options)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _options = options;
        }

        [AllowAnonymous]
        [HttpPost("api/tokens")]
        public async Task<IActionResult> GenerateToken([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return ValidationProblem();

            var user = await _userManager.FindByEmailAsync(model.Identity);
            if (user == null)
                return BadRequest();

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
                return BadRequest();

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            // FIXME: Derive this key from the private key
            var key = Encoding.UTF8.GetBytes("0123456789abcdef");
            var securityKey = new SymmetricSecurityKey(key);
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            const string audience = "https://localhost:5001";
            const string issuer = "https://localhost:5001";

            var token = new JwtSecurityToken(issuer, audience,
                claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: signingCredentials);

            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });

        }
    }
}