using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Aplicacion.Controllers
{
    public class LoginController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        public ActionResult Index(string nombreUsuario, string contrasena)
        {
            string hash = GetMD5(contrasena);

            string connectionString = ConfigurationManager.ConnectionStrings["ConexionTaller"].ConnectionString;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT Rol FROM Usuarios WHERE NombreUsuario = @usuario AND ContrasenaHash = @contrasena";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@usuario", nombreUsuario);
                cmd.Parameters.AddWithValue("@contrasena", hash);

                var rol = cmd.ExecuteScalar();

                if (rol != null)
                {
                    Session["usuario"] = nombreUsuario;
                    Session["rol"] = rol.ToString();

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ViewBag.Error = "Usuario o contraseña inválidos";
                    return View();
                }
            }
        }
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Login");
        }



        private string GetMD5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2")); // hex string compatible con .NET Framework 4.8
                }
                return sb.ToString();
            }
        }



    }
}