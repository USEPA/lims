using LimsServer.Entities;
using LimsServer.Helpers;
using LimsServer.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LimsServerTests
{
    public class UserServiceTest
    {
        public DataContext _context;

        private async Task<DataContext> InitContext()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new DataContext(options);
            return context;
        }

        [Theory]
        [InlineData("test1", "password1")]
        public void CreateTest(string userName, string password)
        {
            this._context = this.InitContext().Result;
            User user = new User()
            {
                Username = userName,
            };
            UserService uService = new UserService(this._context);
            var results = uService.Create(user, password);
            Assert.NotNull(results.PasswordHash);           // return test

            var dbResults = this._context.Users.SingleAsync(u => u.Username == userName).Result;
            Assert.Equal(dbResults.Username, userName);     // DB test
        }

        [Theory]
        [InlineData("test1", "password1")]
        public void AuthenticateTest(string userName, string password)
        {
            // Create user for db query
            this._context = this.InitContext().Result;
            User user = new User()
            {
                Username = userName,
            };
            UserService uService = new UserService(this._context);
            var results = uService.Create(user, password);
            Assert.NotNull(results.PasswordHash);           // return test

            var authResults = uService.Authenticate(userName, password);
            Assert.NotNull(authResults);

            var failedAuthResults = uService.Authenticate(userName, "badpassword");
            Assert.Null(failedAuthResults);
        }

        [Theory]
        [InlineData("user1", "user2", 2)]
        public void GetAllTest(string user1, string user2, int expected)
        {
            // Create users for db query
            this._context = this.InitContext().Result;
            UserService uService = new UserService(this._context);

            User u1 = new User(){ Username = user1 };
            var r1 = uService.Create(u1, user1);
            User u2 = new User() { Username = user2 };
            var r2 = uService.Create(u2, user2);

            var results = uService.GetAll().ToList();
            Assert.Equal(expected, results.Count);
        }

        [Theory]
        [InlineData("user1")]
        public void GetByIDTest(string user1)
        {
            // Create user for db query
            this._context = this.InitContext().Result;
            UserService uService = new UserService(this._context);

            User u1 = new User() { Username = user1 };
            var r1 = uService.Create(u1, user1);
            Assert.NotNull(r1);

            var results = uService.GetById(r1.Id);
            Assert.NotNull(results);

            var badResults = uService.GetById(1234567890);
            Assert.Null(badResults);
        }

        [Theory]
        [InlineData("oldUser", "newUser")]
        public void UpdateTest(string user, string updatedUser)
        {
            // Create user for db query
            this._context = this.InitContext().Result;
            UserService uService = new UserService(this._context);

            User u = new User() { Username = user };
            var r = uService.Create(u, user);
            Assert.NotNull(r);

            User uUser = r;
            uUser.FirstName = updatedUser;
            uUser.LastName = updatedUser;
            uService.Update(uUser, true);

            var result = this._context.Users.SingleAsync(u => u.Id == r.Id).Result;
            Assert.Equal(updatedUser, result.LastName);

            User nUser = new User();
            Assert.Throws<AppException>(() => uService.Update(nUser, true));
        }

        [Fact]
        public void DeleteTest()
        {
            // Create user for db query
            this._context = this.InitContext().Result;
            UserService uService = new UserService(this._context);

            User u = new User() { Username = "user1" };
            var r = uService.Create(u, "user1");
            Assert.NotNull(r);

            uService.Delete(r.Id);
            var result = this._context.Users.Where(u0 => u0.Id == u.Id).FirstOrDefault();
            Assert.Null(result);
        }

    }
}
