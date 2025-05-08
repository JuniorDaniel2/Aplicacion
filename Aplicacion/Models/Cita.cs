using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Aplicacion.Models
{
    public class Cita
    {
        public int CitaID { get; set; }
        public int VehiculoID { get; set; }
        public DateTime FechaHora { get; set; }
        public string Estado { get; set; }
        public string Comentarios { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
    }
}