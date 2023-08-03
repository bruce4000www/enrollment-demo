using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Synapxe.FhirWebApi.Enrollment.Security
{
    public class HeadersAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SecurityHeaderName = "X-Ihis-SourceApplication";

        public HeadersAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var request = Context.Request;
            if (request.Headers.ContainsKey(SecurityHeaderName))
            {
                var identity = new ClaimsIdentity("TestU");
                var principal = new ClaimsPrincipal(identity);
                return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "Test")));
            }
            else
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing header"));
            }
        }
    }
}