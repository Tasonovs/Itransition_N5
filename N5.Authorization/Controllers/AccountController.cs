using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using N5.Authorization.ViewModels;
using N5.Authorization.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace N5.Authorization.Controllers
{
    public class AccountController : Controller
    {
        private UserContext database;
        public AccountController(UserContext context)
        {
            database = context;
        }


        //Sure, it should be made better, but I have no idea...
        public async Task<bool> CheckUserIsDeletedOrBlocked()
		{
            User user = await database.Users.FirstOrDefaultAsync<User>(u => u.Email == User.Identity.Name);
            return (user == null || user.IsBlocked);
		}



        [Authorize]
        public async Task<IActionResult> AccountInfo()
		{
            if (await CheckUserIsDeletedOrBlocked()) await Logout();
            User user = await database.Users.FirstOrDefaultAsync(u => u.Email == (string)User.Identity.Name);
            return View(user);
        }

        [Authorize]
        public async Task<IActionResult> UsersTable()
        {
            if (await CheckUserIsDeletedOrBlocked()) await Logout();
            return View(await database.Users.ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUsers(IFormCollection formCollection)
        {
            var users = ConvertFormCollectionToUsers(formCollection);
            database.Users.RemoveRange(users);
            database.SaveChanges();
            return RedirectToAction("UsersTable");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BlockUsers(IFormCollection formCollection)
        {
            ChangeBlockStatusTo(formCollection, true);
            return RedirectToAction("UsersTable");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UnblockUsers(IFormCollection formCollection)
        {
            ChangeBlockStatusTo(formCollection, false);
            return RedirectToAction("UsersTable");
        }

        private void ChangeBlockStatusTo(IFormCollection formCollection, bool status)
        {
            var users = ConvertFormCollectionToUsers(formCollection);
            foreach (var user in users)
			{
                user.IsBlocked = status;

            }

            database.Users.UpdateRange(users);
            database.SaveChanges();
        }

        private List<User> ConvertFormCollectionToUsers(IFormCollection formCollection)
		{
            string[] ids = formCollection["userId"].ToArray();

            List<User> users = new List<User>();
            foreach (var id in ids)
            {
                User user = database.Users.Find(int.Parse(id));
                if (user != null)
                {
                    users.Add(user);
                }
            }

            return users;
        }
        



        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                User user = await database.Users.FirstOrDefaultAsync(u => u.Email == model.Email && u.Password == model.Password);
                if (!await CheckUserIsDeletedOrBlocked())
                {
                    await Authenticate(model.Email);

                    user.LastLoginDate = DateTime.Now;
                    database.Users.Update(user);
                    database.SaveChanges();

                    return RedirectToAction("Index", "Home");
                }

                if (user == null) ModelState.AddModelError("", "Некорректные логин и(или) пароль");
                if (user != null && user.IsBlocked) ModelState.AddModelError("", "Ваш аккаунт заблокирован");
            }
            return View(model);
        }
        
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                User user = await database.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user == null)
                {
                    // добавляем пользователя в бд
                    database.Users.Add(new User 
                    {
                        Name = model.Name,
                        Email = model.Email,
                        Password = model.Password,
                        RegistrationDate = DateTime.Now,
                        LastLoginDate = DateTime.Now,
                        IsBlocked = false
                    });
                    await database.SaveChangesAsync();

                    await Authenticate(model.Email); // аутентификация

                    return RedirectToAction("Index", "Home");
                }
                else
                    ModelState.AddModelError("", "Некорректные логин и(или) пароль");
            }
            return View(model);
        }

        private async Task Authenticate(string userName)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, userName)
            };
            // создаем объект ClaimsIdentity                ?????????????????
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}