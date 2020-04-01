using Hangfire.Annotations;
using Hangfire.Dashboard;
using LimsServer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LimsServer.Helpers
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {

        public bool Authorize([NotNull] DashboardContext context)
        {
            var http = context.GetHttpContext();
            var jwt = http.Request.Cookies["JWT_TOKEN"];
            if(jwt == null) 
            { 
                return false; 
            }

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadToken(jwt) as JwtSecurityToken;
            if(token == null)
            {
                return false;
            }

            var userID = Int32.Parse(token.Claims.First(c => c.Type == "unique_name").Value);
            var userService = context.GetHttpContext().RequestServices.GetRequiredService<IUserService>();
            var user = userService.GetById(userID);
            if(user.Admin)
            {
                return true;
            }
            return false;
        }
    }
}
