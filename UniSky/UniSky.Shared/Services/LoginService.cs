using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniSky.Models;
using Windows.Security.Credentials;

namespace UniSky.Services;

//
// TODO: figure out if we actually need this or not
//
public class LoginService
{
    private readonly PasswordVault vault = new PasswordVault();

    public IEnumerable<LoginModel> GetLogins()
    {
        var creds = vault.RetrieveAll();
        foreach (var cred in creds)
        {
            cred.RetrievePassword();

            yield return new LoginModel(cred.Resource, cred.UserName, cred.Password);
        }
    }

    public LoginModel SaveLogin(string host, string username, string password)
    {
        var credential = new PasswordCredential(host, username, password);
        vault.Add(credential);

        return new LoginModel(host, username, password);
    }
}
