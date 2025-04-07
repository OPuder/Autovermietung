/*
 * Created by SharpDevelop.
 * User: aluca
 * Date: 03.04.2025
 * Time: 03:47
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace CarRentalHttpServer
{
    public static class PasswordRoutes
    {
public static bool TryHandle(HttpListenerRequest request, HttpListenerResponse response, User currentUser, List<User> users, out string html, out bool responseHandled)
        {
            html = "";
            responseHandled = false;

            if (request.Url.AbsolutePath == "/forgot-password")
            {
                string body = @"<h2>Passwort vergessen</h2>
<form method='post' action='/verify-security'>
<label for='email'>E-Mail:</label>
<input type='email' name='email' required />
<button #='weiter' type='submit' class='btn'>Weiter</button>
</form>";
                html = HtmlGenerator.GenerateStyledHtml("Passwort vergessen", body, null);
                return true;
            }
            else if (request.Url.AbsolutePath == "/verify-security" && request.HttpMethod == "POST")
            {
                var formData = ReadFormData(request);
                string email = formData["email"];
                var user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

                if (user != null)
                {
                    string question = user.SecurityQuestion.Replace("+", " ");
                    string securityForm = string.Format(@"<h2>Sicherheitsfrage</h2>
<form method='post' action='/check-security'>
<input type='hidden' name='email' value='{0}' />
<p><strong>{1}</strong></p>
<input type='text' name='securityAnswer' required placeholder='Antwort' />
<button type='submit' class='btn'>Weiter</button>
</form>", email, question);

                    html = HtmlGenerator.GenerateStyledHtml("Sicherheitsfrage", securityForm, null);
                }
                else
                {
                    html = HtmlGenerator.GenerateStyledHtml("Fehler", "<h3>E-Mail nicht gefunden.</h3><a href='/forgot-password'>Zurück</a>", null);
                }
                return true;
            }
            else if (request.Url.AbsolutePath == "/check-security" && request.HttpMethod == "POST")
            {
                var formData = ReadFormData(request);
                string email = formData["email"];
                string answer = formData["securityAnswer"];
                var user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

                if (user != null && user.SecurityAnswer.Equals(answer, StringComparison.OrdinalIgnoreCase))
                {
                    string pwForm = string.Format(@"<h2>Neues Passwort setzen</h2>
<form method='post' action='/reset-password'>
<input type='hidden' name='email' value='{0}' />
<div class='form-group'>
<label>Neues Passwort</label>
<input type='password' name='newPassword' id='resetPassword' required minlength='6' oninput='validateResetPassword()' />
</div>
<div class='form-group'>
<label>Passwort wiederholen</label>
<input type='password' name='newPasswordRepeat' id='resetPasswordRepeat' required minlength='6' oninput='validateResetPassword()' />
</div>
<div id='resetPasswordMessage' style='color:red; font-size:0.9em;'></div>
<button type='submit' class='btn'>Zurücksetzen</button>
</form>
<script>
function validateResetPassword() {{
var pw = document.getElementById('resetPassword').value;
var pwRepeat = document.getElementById('resetPasswordRepeat').value;
var msg = document.getElementById('resetPasswordMessage');
if (pw.length < 6) {{ msg.innerText = 'Das Passwort muss mindestens 6 Zeichen lang sein.'; }}
else if (pw !== pwRepeat) {{ msg.innerText = 'Die Passwörter stimmen nicht überein.'; }}
else {{ msg.innerText = ''; }}
}}
</script>", email);

                    html = HtmlGenerator.GenerateStyledHtml("Passwort ändern", pwForm, null);
                }
                else
                {
                    html = HtmlGenerator.GenerateStyledHtml("Fehler", "<h3>Antwort war falsch.</h3><a href='/forgot-password'>Zurück</a>", null);
                }
                return true;
            }
            else if (request.Url.AbsolutePath == "/reset-password" && request.HttpMethod == "POST")
            {
                var formData = ReadFormData(request);
                string email = formData["email"];
                string newPassword = formData["newPassword"];
                string newPasswordRepeat = formData["newPasswordRepeat"];
                var user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

                if (newPassword.Length < 6)
                {
                    html = HtmlGenerator.GenerateStyledHtml("Fehler", "<h3>Passwort muss mindestens 6 Zeichen lang sein.</h3><a href='/forgot-password'>Zurück</a>", null);
                }
                else if (newPassword != newPasswordRepeat)
                {
                    html = HtmlGenerator.GenerateStyledHtml("Fehler", "<h3>Passwörter stimmen nicht überein.</h3><a href='/forgot-password'>Zurück</a>", null);
                }
                else if (user != null)
                {
                    string salt = SecurityHelper.GenerateSalt();
                    user.Salt = salt;
                    user.PasswordHash = SecurityHelper.HashPassword(newPassword, salt);
                    DataService.SaveUsers(users);

                    html = HtmlGenerator.GenerateStyledHtml("Erfolg", "<h3>Passwort wurde zurückgesetzt.</h3><a href='/login'>Zum Login</a>", null);
                }
                else
                {
                    html = HtmlGenerator.GenerateStyledHtml("Fehler", "<h3>Benutzer nicht gefunden.</h3><a href='/forgot-password'>Zurück</a>", null);
                }
                return true;
            }

            return false;
        }

        private static NameValueCollection ReadFormData(HttpListenerRequest request)
        {
            var formData = new NameValueCollection();
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                string body = reader.ReadToEnd();
                foreach (var pair in body.Split('&'))
                {
                    var kv = pair.Split('=');
                    if (kv.Length == 2)
                        formData[kv[0]] = Uri.UnescapeDataString(kv[1]);
                }
            }
            return formData;
        }
    }
}
