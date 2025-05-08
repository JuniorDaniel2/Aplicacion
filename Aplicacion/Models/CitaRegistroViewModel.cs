using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Aplicacion.Models
{
    public class CitaRegistroViewModel
    {
        public int VehiculoID { get; set; }
        public DateTime FechaHora { get; set; }
        public List<SelectListItem> Vehiculos { get; set; }
    }
}