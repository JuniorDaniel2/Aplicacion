using Aplicacion.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Aplicacion.Controllers
{
    public class CitasController : Controller
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["ConexionTaller"].ConnectionString;

        public ActionResult Crear()
        {
            var model = new CitaRegistroViewModel
            {
                Vehiculos = ObtenerVehiculosDelCliente()
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult Crear(CitaRegistroViewModel model)
        {
            if (model.FechaHora == DateTime.MinValue)
            {
                ViewBag.Error = "Debe seleccionar una fecha válida.";
                model.Vehiculos = ObtenerVehiculosDelCliente();
                return View(model);
            }

            // Validar empalme
            if (ExisteEmpalme(model.FechaHora))
            {
                ViewBag.Error = "Ya existe una cita programada en ese horario o se cruza con otra.";
                model.Vehiculos = ObtenerVehiculosDelCliente();
                return View(model);
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = @"INSERT INTO Citas (VehiculoID, FechaHora, Estado) 
                                 VALUES (@vehiculoId, @fecha, 'Pendiente')";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@vehiculoId", model.VehiculoID);
                cmd.Parameters.AddWithValue("@fecha", model.FechaHora);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index", "Home");
        }

        private bool ExisteEmpalme(DateTime fecha)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = @"SELECT COUNT(*) FROM Citas 
                                 WHERE 
                                 (FechaHora <= @fin AND DATEADD(HOUR, 2, FechaHora) > @inicio)";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@inicio", fecha);
                cmd.Parameters.AddWithValue("@fin", fecha.AddHours(2));

                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }

        private List<SelectListItem> ObtenerVehiculosDelCliente()
        {
            var lista = new List<SelectListItem>();

            if (Session["usuario"] == null)
                return lista;

            string usuario = Session["usuario"].ToString();
            int clienteId = 0;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("SELECT ClienteID FROM Usuarios WHERE NombreUsuario = @usuario", con);
                cmd.Parameters.AddWithValue("@usuario", usuario);
                var result = cmd.ExecuteScalar();
                if (result != null)
                    clienteId = (int)result;
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("SELECT VehiculoID, Marca + ' ' + Modelo + ' (' + Placa + ')' as Nombre FROM Vehiculos WHERE ClienteID = @clienteId", con);
                cmd.Parameters.AddWithValue("@clienteId", clienteId);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(new SelectListItem
                    {
                        Value = reader["VehiculoID"].ToString(),
                        Text = reader["Nombre"].ToString()
                    });
                }
            }

            return lista;
        }

        public ActionResult Revisar()
        {
            if (Session["rol"]?.ToString() != "Administrador")
                return RedirectToAction("SinPermiso", "Login");

            var lista = new List<Cita>();
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM Citas WHERE Estado = 'Pendiente'", con);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    lista.Add(new Cita
                    {
                        CitaID = (int)reader["CitaID"],
                        VehiculoID = (int)reader["VehiculoID"],
                        FechaHora = (DateTime)reader["FechaHora"],
                        Estado = reader["Estado"].ToString(),
                        Comentarios = reader["Comentarios"]?.ToString()
                    });
                }
            }
            return View(lista);
        }

        public ActionResult Aprobar(int id)
        {
            var cita = ObtenerCita(id);
            return View("EditarEstado", cita);
        }

        public ActionResult Rechazar(int id)
        {
            var cita = ObtenerCita(id);
            return View("EditarEstado", cita);
        }

        [HttpPost]
        public ActionResult EditarEstado(Cita model, string accion)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = @"UPDATE Citas SET Estado = @estado, Comentarios = @comentarios, FechaFinalizacion = @fecha 
                         WHERE CitaID = @id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@estado", accion == "aprobar" ? "Aprobada" : "Rechazada");
                cmd.Parameters.AddWithValue("@comentarios", model.Comentarios ?? "");
                cmd.Parameters.AddWithValue("@fecha", accion == "aprobar" ? (object)DateTime.Now.AddDays(1) : DBNull.Value);
                cmd.Parameters.AddWithValue("@id", model.CitaID);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Revisar");
        }

        private Cita ObtenerCita(int id)
        {
            Cita cita = null;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM Citas WHERE CitaID = @id", con);
                cmd.Parameters.AddWithValue("@id", id);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    cita = new Cita
                    {
                        CitaID = (int)reader["CitaID"],
                        FechaHora = (DateTime)reader["FechaHora"],
                        Estado = reader["Estado"].ToString(),
                        Comentarios = reader["Comentarios"]?.ToString()
                    };
                }
            }
            return cita;
        }
        public ActionResult MisCitas()
        {
            if (Session["usuario"] == null || Session["rol"].ToString() != "Cliente")
                return RedirectToAction("Index", "Login");

            string usuario = Session["usuario"].ToString();
            var lista = new List<Cita>();

            int clienteId = 0;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("SELECT ClienteID FROM Usuarios WHERE NombreUsuario = @usuario", con);
                cmd.Parameters.AddWithValue("@usuario", usuario);
                var result = cmd.ExecuteScalar();
                if (result != null)
                    clienteId = (int)result;
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(@"
            SELECT c.CitaID, c.FechaHora, c.Estado, c.Comentarios, c.FechaFinalizacion
            FROM Citas c
            INNER JOIN Vehiculos v ON c.VehiculoID = v.VehiculoID
            WHERE v.ClienteID = @clienteId", con);

                cmd.Parameters.AddWithValue("@clienteId", clienteId);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(new Cita
                    {
                        CitaID = (int)reader["CitaID"],
                        FechaHora = (DateTime)reader["FechaHora"],
                        Estado = reader["Estado"].ToString(),
                        Comentarios = reader["Comentarios"]?.ToString(),
                        FechaFinalizacion = reader["FechaFinalizacion"] as DateTime?
                    });
                }
            }

            return View(lista);
        }

        [HttpPost]
        public JsonResult CrearAjax(CitaRegistroViewModel model)
        {
            if (model.FechaHora == DateTime.MinValue || model.VehiculoID == 0)
                return Json(new { mensaje = "Debe llenar todos los campos." }, JsonRequestBehavior.AllowGet);

            if (ExisteEmpalme(model.FechaHora))
                return Json(new { mensaje = "Error: La hora seleccionada ya está ocupada." }, JsonRequestBehavior.AllowGet);

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = @"INSERT INTO Citas (VehiculoID, FechaHora, Estado) 
                         VALUES (@vehiculoId, @fecha, 'Pendiente')";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@vehiculoId", model.VehiculoID);
                cmd.Parameters.AddWithValue("@fecha", model.FechaHora);
                cmd.ExecuteNonQuery();
            }

            return Json(new { mensaje = "¡Cita agendada con éxito!" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ActualizarEstadoAjax(int id, string estado, string comentario)
        {
            if (Session["rol"]?.ToString() != "Administrador")
                return Json(new { mensaje = "Acceso no autorizado." }, JsonRequestBehavior.AllowGet);

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(@"
            UPDATE Citas 
            SET Estado = @estado, Comentarios = @comentarios, FechaFinalizacion = @fecha
            WHERE CitaID = @id", con);

                cmd.Parameters.AddWithValue("@estado", estado);
                cmd.Parameters.AddWithValue("@comentarios", comentario ?? "");
                cmd.Parameters.AddWithValue("@fecha", estado == "Aprobada" ? (object)DateTime.Now.AddDays(1) : DBNull.Value);
                cmd.Parameters.AddWithValue("@id", id);

                cmd.ExecuteNonQuery();
            }

            return Json(new { mensaje = $"Cita {estado.ToLower()} con éxito." }, JsonRequestBehavior.AllowGet);
        }



    }
}