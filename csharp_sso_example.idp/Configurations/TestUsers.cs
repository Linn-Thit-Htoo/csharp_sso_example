using Duende.IdentityServer.Test;
using IdentityModel;
using System.Collections.Generic;
using System.Security.Claims;

namespace csharp_sso_example.idp.Configurations
{
    public static class TestUsers
    {
        public static List<TestUser> Users =>
            new()
            {
                new TestUser
                {
                    SubjectId = "1",
                    Username = "alice",
                    Password = "Pass123$",
                    Claims =
                    {
                        new Claim(JwtClaimTypes.Name, "Alice Anderson"),
                        new Claim(JwtClaimTypes.Email, "alice@example.com")
                    }
                },
                new TestUser
                {
                    SubjectId = "2",
                    Username = "bob",
                    Password = "Pass123$",
                    Claims =
                    {
                        new Claim(JwtClaimTypes.Name, "Bob Brown"),
                        new Claim(JwtClaimTypes.Email, "bob@example.com")
                    }
                }
            };
    }
}
