using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Aplicacion.Models;

namespace Aplicacion.Controllers
{
    public class ClientesController : Controller
    {
        //public ActionResult Index()
        //{
        //    if (Session["usuario"] == null)
        //        return RedirectToAction("Index", "Login");

        //    return View();
        //}
        private string connectionString = ConfigurationManager.ConnectionStrings["ConexionTaller"].ConnectionString;

        public ActionResult Index()
        {
            var lista = new List<Cliente>();
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM Clientes", con);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    lista.Add(new Cliente
                    {
                        ClienteID = (int)reader["ClienteID"],
                        Nombre = reader["Nombre"].ToString(),
                        Correo = reader["Correo"].ToString(),
                        Telefono = reader["Telefono"].ToString()
                    });
                }
            }
            return View(lista);
        }

        public ActionResult Editar(int id)
        {
            Cliente cliente = null;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM Clientes WHERE ClienteID = @id", con);
                cmd.Parameters.AddWithValue("@id", id);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    cliente = new Cliente
                    {
                        ClienteID = (int)reader["ClienteID"],
                        Nombre = reader["Nombre"].ToString(),
                        Correo = reader["Correo"].ToString(),
                        Telefono = reader["Telefono"].ToString()
                    };
                }
            }
            return View(cliente);
        }
        [HttpPost]
        public ActionResult Editar(Cliente model)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("UPDATE Clientes SET Nombre = @nombre, Correo = @correo, Telefono = @telefono WHERE ClienteID = @id", con);
                cmd.Parameters.AddWithValue("@nombre", model.Nombre);
                cmd.Parameters.AddWithValue("@correo", model.Correo);
                cmd.Parameters.AddWithValue("@telefono", model.Telefono);
                cmd.Parameters.AddWithValue("@id", model.ClienteID);

                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }

        public ActionResult Eliminar(int id)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                // 1. Eliminar usuarios relacionados
                SqlCommand eliminarUsuarios = new SqlCommand("DELETE FROM Usuarios WHERE ClienteID = @id", con);
                eliminarUsuarios.Parameters.AddWithValue("@id", id);
                eliminarUsuarios.ExecuteNonQuery();

                // 2. Eliminar cliente
                SqlCommand eliminarCliente = new SqlCommand("DELETE FROM Clientes WHERE ClienteID = @id", con);
                eliminarCliente.Parameters.AddWithValue("@id", id);
                eliminarCliente.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }



        public ActionResult Registrar()
        {
            return View();
        }
      //  [HttpPost]
        //public ActionResult Registrar(ClienteRegistroViewModel model)
        //{
        //    if (!ModelState.IsValid || string.IsNullOrEmpty(model.Nombre) || string.IsNullOrEmpty(model.Correo) || string.IsNullOrEmpty(model.Usuario) || string.IsNullOrEmpty(model.Contrasena))
        //    {
        //        ViewBag.Error = "Todos los campos son obligatorios.";
        //        return View(model);
        //    }

        //    string hash = GetMD5(model.Contrasena);
        //    string connectionString = ConfigurationManager.ConnectionStrings["ConexionTaller"].ConnectionString;

        //    using (SqlConnection con = new SqlConnection(connectionString))
        //    {
        //        con.Open();

        //        // 1. Insertar Cliente
        //        string insertCliente = "INSERT INTO Clientes (Nombre, Correo, Telefono) OUTPUT INSERTED.ClienteID VALUES (@nombre, @correo, @telefono)";
        //        SqlCommand cmdCliente = new SqlCommand(insertCliente, con);
        //        cmdCliente.Parameters.AddWithValue("@nombre", model.Nombre);
        //        cmdCliente.Parameters.AddWithValue("@correo", model.Correo);
        //        cmdCliente.Parameters.AddWithValue("@telefono", model.Telefono);

        //        int clienteId = (int)cmdCliente.ExecuteScalar();

        //        // 2. Insertar Usuario
        //        string insertUsuario = "INSERT INTO Usuarios (NombreUsuario, ContrasenaHash, Rol, ClienteID) VALUES (@usuario, @hash, 'Cliente', @clienteId)";
        //        SqlCommand cmdUsuario = new SqlCommand(insertUsuario, con);
        //        cmdUsuario.Parameters.AddWithValue("@usuario", model.Usuario);
        //        cmdUsuario.Parameters.AddWithValue("@hash", hash);
        //        cmdUsuario.Parameters.AddWithValue("@clienteId", clienteId);

        //        cmdUsuario.ExecuteNonQuery();
        //    }

        //    ViewBag.Mensaje = "Cliente registrado con éxito.";
        //    return RedirectToAction("Index", "Login");
        //}

        [HttpPost]
        public JsonResult RegistrarAjax(ClienteRegistroViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Nombre) || string.IsNullOrWhiteSpace(model.Usuario) ||
                string.IsNullOrWhiteSpace(model.Correo) || string.IsNullOrWhiteSpace(model.Contrasena))
            {
                return Json(new { mensaje = "Todos los campos son obligatorios." }, JsonRequestBehavior.AllowGet);
            }

            string hash = GetMD5(model.Contrasena);

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                // Validar si ya existe el nombre de usuario
                SqlCommand validar = new SqlCommand("SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario = @usuario", con);
                validar.Parameters.AddWithValue("@usuario", model.Usuario);
                int existe = (int)validar.ExecuteScalar();
                if (existe > 0)
                {
                    return Json(new { mensaje = "El nombre de usuario ya está en uso." }, JsonRequestBehavior.AllowGet);
                }

                // Insertar cliente
                SqlCommand cmdCliente = new SqlCommand("INSERT INTO Clientes (Nombre, Correo, Telefono) OUTPUT INSERTED.ClienteID VALUES (@n, @c, @t)", con);
                cmdCliente.Parameters.AddWithValue("@n", model.Nombre);
                cmdCliente.Parameters.AddWithValue("@c", model.Correo);
                cmdCliente.Parameters.AddWithValue("@t", model.Telefono);
                int clienteId = (int)cmdCliente.ExecuteScalar();

                // Insertar usuario
                SqlCommand cmdUsuario = new SqlCommand("INSERT INTO Usuarios (NombreUsuario, ContrasenaHash, Rol, ClienteID) VALUES (@u, @p, 'Cliente', @id)", con);
                cmdUsuario.Parameters.AddWithValue("@u", model.Usuario);
                cmdUsuario.Parameters.AddWithValue("@p", hash);
                cmdUsuario.Parameters.AddWithValue("@id", clienteId);
                cmdUsuario.ExecuteNonQuery();
            }

            return Json(new { mensaje = "Registro exitoso. Ahora puede iniciar sesión." }, JsonRequestBehavior.AllowGet);
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
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

    }
}