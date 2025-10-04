using bk_arca.DTOs.Facturacion.FacturaB;
using bk_arca.Enums;
using System.ComponentModel.DataAnnotations;

namespace bk_arca.DTOs.Facturacion.FacturaA
{
    public class FacturaARequestDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int NumeroPuntoVenta { get; set; } = 4000;

        // 6 = Factura B (fijo)
        public TipoComprobante TipoComprobante { get; set; } = TipoComprobante.FacturaA;

        // Si no lo mandás, en el servicio podés consultar el último y sumarle 1
        public int? NumeroComprobante { get; set; }

        // yyyy-MM-dd; si viene null, usar DateTime.Today
        public DateOnly? FechaEmision { get; set; }

        // 1 = Productos (por defecto)
        public Concepto ComprobanteConcepto { get; set; } = Concepto.Productos;

        // Receptor
        [Required]
        public TipoDocumento TipoDocumentoReceptor { get; set; } = TipoDocumento.CUIT; // 99

        // Para CF = 0; para DNI/CUIT completar
        [Required]
        [MinLength(1)]
        public string NumeroDocumentoReceptor { get; set; } = "0";

        // Para B típicamente 5 = IVA 21% (usar lo que devuelva consultarCondicionesIVAReceptor si corresponde)
        [Required]
        public CondicionIVA CondicionIVAReceptor { get; set; } = CondicionIVA.Gravado21;

        // Totales (B trabaja con precios IVA incluido por ítem)


        [Range(0, double.MaxValue)]
        public decimal ImporteGravado { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ImporteNoGravado { get; set; } = 0m;

        [Range(0, double.MaxValue)]
        public decimal ImporteExento { get; set; } = 0m;


        [Range(0, double.MaxValue)]
        public decimal ImporteSubtotal { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ImporteTotal { get; set; }

        // Moneda
        [Required]
        [RegularExpression("^[A-Z]{3}$")]
        public string CodigoMoneda { get; set; } = "PES";

        [Range(0.0001, double.MaxValue)]
        public decimal CotizacionMoneda { get; set; } = 1.00m;


        // Ítems
        [Required]
        [MinLength(1)]
        public List<ItemARequestDto> Items { get; set; } = new();

        // (Opcional) Resumen de IVA validatorio
        public List<SubtotalIVARequestDto>? SubtotalesIVA { get; set; }  

         
    }
}
