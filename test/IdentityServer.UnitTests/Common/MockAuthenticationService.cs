﻿using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace IdentityServer4.UnitTests.Common
{
    class MockAuthenticationService : IAuthenticationService
    {
        public AuthenticateResult Result { get; set; }

        public void SetUser(ClaimsPrincipal user, AuthenticationProperties properties = null)
        {
            Result = AuthenticateResult.Success(new AuthenticationTicket(user, properties, "scheme"));
        }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme)
        {
            return Task.FromResult(Result);
        }

        public Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties)
        {
            return Task.FromResult(0);
        }

        public Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties)
        {
            return Task.FromResult(0);
        }

        public Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties)
        {
            return Task.FromResult(0);
        }

        public Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties)
        {
            return Task.FromResult(0);
        }
    }
}
