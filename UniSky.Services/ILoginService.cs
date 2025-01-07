using System.Collections.Generic;
using UniSky.Models;

namespace UniSky.Services;

public interface ILoginService
{
    IEnumerable<LoginModel> GetLogins();
    LoginModel SaveLogin(string host, string username, string password);
}