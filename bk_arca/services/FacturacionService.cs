using bk_arca.DTOs.Facturacion.FacturaA;
using bk_arca.DTOs.Facturacion.FacturaB;
using bk_arca.Enums;
using bk_arca.services.Interfaces;
using bk_arca.services.Utils;
using referencias_arca_ws;

namespace bk_arca.services
{
    public class FacturacionService : IFacturacionService
    {

        public MTXCAServicePortTypeClient Client { get; set; } = new MTXCAServicePortTypeClient(MTXCAServicePortTypeClient.EndpointConfiguration.MTXCAServiceHttpSoap11Endpoint);

        //var client = new MTXCAServicePortTypeClient(MTXCAServicePortTypeClient.EndpointConfiguration.MTXCAServiceHttpSoap11Endpoint);

        public AuthRequestType Auth { get; set; } = new AuthRequestType
        {
            token = Environment.GetEnvironmentVariable("AFIP_TOKEN"),
            sign = Environment.GetEnvironmentVariable("AFIP_SIGN"),
            cuitRepresentada = long.Parse(Environment.GetEnvironmentVariable("AFIP_CUIT") ?? "0")
        };


         


        public async Task<autorizarComprobanteResponse> AutorizarFacturaBAsync(FacturaBRequestDto dto)
        {
            // Validaciones mínimas de negocio
            if (dto.Items == null || dto.Items.Count == 0)
                throw new ArgumentException("La factura debe contener al menos un ítem.");

            // Fecha por defecto
            var fecha = dto.FechaEmision?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today;

            // Número de comprobante
            int numeroComprobante;
            if (dto.NumeroComprobante.HasValue)
            {
                numeroComprobante = dto.NumeroComprobante.Value;
            }
            else
            {
                var ultimo = await Client.consultarUltimoComprobanteAutorizadoAsync(
                    new consultarUltimoComprobanteAutorizadoRequest
                    {
                        authRequest = Auth,
                        consultaUltimoComprobanteAutorizadoRequest = new ConsultaUltimoComprobanteAutorizadoRequestType
                        {
                            codigoTipoComprobante = (int)TipoComprobante.FacturaB,
                            numeroPuntoVenta = dto.NumeroPuntoVenta
                        }
                    });

                // Fix: Convert 'long' to 'int' explicitly
                numeroComprobante = Convert.ToInt32(ultimo.numeroComprobante);
            }

            CalculosTotalesFacturas.RecalcularTotalesFacturaB(dto);
            

            // Mapear a ComprobanteType (sin enviar campos en 0 innecesarios)
            var comp = MapToComprobanteTypeB(dto, numeroComprobante, fecha);

            // Autorizar
            var resp = await Client.autorizarComprobanteAsync(new autorizarComprobanteRequest
            {
                authRequest = Auth,
                comprobanteCAERequest = comp
            });

            return resp;
        }


        public async Task<autorizarComprobanteResponse> AutorizarFacturaAAsync(FacturaARequestDto dto) 
        {
            // Validaciones mínimas de negocio
            if (dto.Items == null || dto.Items.Count == 0)
                throw new ArgumentException("La factura debe contener al menos un ítem.");

            // Fecha por defecto
            var fecha = dto.FechaEmision?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today;


            var resu = await Client.consultarCondicionesIVAReceptorAsync(new consultarCondicionesIVAReceptorRequest
            {
                authRequest = Auth,
                consultaCondicionesIVAReceptorRequest = new ConsultaCondicionesIVARequestType
                {
                    // Asigna aquí las propiedades requeridas, por ejemplo:
                    codigoTipoComprobante = 6,
                    // ejemplo: DNI
                                               // ejemplo: CUIT o DNI del receptor
                }
            });

            // Número de comprobante
            int numeroComprobante;
            if (dto.NumeroComprobante.HasValue)
            {
                numeroComprobante = dto.NumeroComprobante.Value;
            }
            else
            {
                var ultimo = await Client.consultarUltimoComprobanteAutorizadoAsync(
                    new consultarUltimoComprobanteAutorizadoRequest
                    {
                        authRequest = Auth,
                        consultaUltimoComprobanteAutorizadoRequest = new ConsultaUltimoComprobanteAutorizadoRequestType
                        {
                            codigoTipoComprobante = (int)TipoComprobante.FacturaA,
                            numeroPuntoVenta = dto.NumeroPuntoVenta
                        }
                    });

                // Fix: Convert 'long' to 'int' explicitly
                numeroComprobante = Convert.ToInt32(ultimo.numeroComprobante);
            }

            CalculosTotalesFacturas.RecalcularTotalesFacturaA(dto);


            // Mapear a ComprobanteType (sin enviar campos en 0 innecesarios)
            var comp = MapToComprobanteTypeA(dto, numeroComprobante, fecha);

            // Autorizar
            var resp = await Client.autorizarComprobanteAsync(new autorizarComprobanteRequest
            {
                authRequest = Auth,
                comprobanteCAERequest = comp
            });

             
            return resp;
        }


        // ----------------- Helpers privados -----------------

        /// <summary>
        /// Factura B: cada item trae PrecioUnitario CON IVA. 
        /// Recomputamos:
        /// - ImporteItem = (precio - bonificación) * cantidad
        /// - ImporteGravado = suma de base imponible neta de IVA por alícuota
        /// - SubtotalesIVA = opcional para validación; si dto ya lo trae, respetamos
        /// Nota: si tenés múltiples alícuotas, agregá la lógica por grupo.
        /// </summary>
        //private static void RecalcularTotalesFacturaB(FacturaBRequestDto dto)
        //{
        //    // 1) Recalcular importe por ítem (si no viene)
        //    foreach (var i in dto.Items)
        //    {
        //        var bonif = i.ImporteBonificacion ?? 0m;
        //        if (i.ImporteItem <= 0)
        //        {
        //            i.ImporteItem = Math.Round((i.PrecioUnitarioConIva - bonif) * i.Cantidad, 2);
        //        }
        //    }

        //    // 2) Total bruto (con IVA)
        //    var totalConIva = dto.Items.Sum(x => x.ImporteItem);

        //    // 3) Si no te pasaron subtotales IVA, podemos construirlos simples (p.e. todo 21%).
        //    //    Si ya vienen en dto.SubtotalesIVA, los respetamos.
        //    if (dto.SubtotalesIVA == null || dto.SubtotalesIVA.Count == 0)
        //    {
        //        // Caso simple: un único código IVA en todos los ítems -> tomamos el primero
        //        var cod = dto.Items.GroupBy(x => x.CodigoCondicionIVA)
        //                           .Select(g => g.Key)
        //                           .SingleOrDefault();

        //        // Si hay mezcla de alícuotas, acá deberías agrupar y calcular por grupo (extender según tu negocio)
        //        if (cod == 0) cod = CondicionIVA.Gravado21;

        //        // Obtener alícuota numérica
        //        var alicuta = GetAlicuota(cod); // 0.21m, 0.105m, etc.

        //        // Base imponible aproximada: total / (1 + alícuota)
        //        var baseImponible = Math.Round(totalConIva / (1 + alicuta), 2);
        //        var iva = Math.Round(totalConIva - baseImponible, 2);

        //        dto.ImporteGravado = baseImponible;
        //        dto.ImporteNoGravado = 0m;
        //        dto.ImporteExento = 0m;

        //        dto.ImporteSubtotal = baseImponible; // si no hay otros tributos ni exentos/no gravados al subtotal
        //        dto.ImporteTotal = totalConIva;

        //        dto.SubtotalesIVA = new()
        //    {
        //        new SubtotalIVARequestDto { Codigo = cod, Importe = iva }
        //    };
        //    }
        //    else
        //    {
        //        // Si viene el detalle de IVA, ajustamos gravado y totales en base a esos subtotales
        //        var totalIva = dto.SubtotalesIVA.Sum(x => x.Importe);
        //        var baseImponible = Math.Round(totalConIva - totalIva, 2);

        //        dto.ImporteGravado = baseImponible;
        //        // Respetamos lo que venga en NoGravado/Exento si lo enviaste; si no, mantenemos 0
        //        dto.ImporteSubtotal = baseImponible;
        //        dto.ImporteTotal = totalConIva;
        //    }
        //}


        /// <summary>
        /// Mapeo centralizado dentro del servicio (lo pediste así).
        /// Cuida de NO enviar campos en 0 que generen error (ej. OtrosTributos=0).
        /// </summary>
        private static ComprobanteType MapToComprobanteTypeB(FacturaBRequestDto dto, int numeroComprobante, DateTime fechaEmision)
        {
            var comp = new ComprobanteType
            {
                // Encabezado
                codigoTipoComprobante = (int)TipoComprobante.FacturaB,
                numeroPuntoVenta = dto.NumeroPuntoVenta,
                numeroComprobante = numeroComprobante + 1,
                fechaEmision = fechaEmision,
                fechaEmisionSpecified = true,
                codigoConcepto = (short)dto.ComprobanteConcepto,

                // Receptor
                codigoTipoDocumento = (short)dto.TipoDocumentoReceptor,
                numeroDocumento = long.TryParse(dto.NumeroDocumentoReceptor, out var nd) ? nd : 0L,
                condicionIVAReceptor = (short)dto.CondicionIVAReceptor,
                condicionIVAReceptorSpecified = true,

                // Totales (en B el precio es CON IVA; evitamos mandar 0s innecesarios)
                importeGravado = dto.ImporteGravado,
                importeGravadoSpecified = dto.ImporteGravado > 0,
                importeNoGravado = dto.ImporteNoGravado,
                importeNoGravadoSpecified = dto.ImporteNoGravado > 0,
                importeExento = dto.ImporteExento,
                importeExentoSpecified = dto.ImporteExento > 0,

                importeSubtotal = dto.ImporteSubtotal,
                importeTotal = dto.ImporteTotal,

                // Moneda
                codigoMoneda = dto.CodigoMoneda,
                cotizacionMoneda = dto.CotizacionMoneda,
                cotizacionMonedaSpecified = true,

                // Ítems
                arrayItems = dto.Items.Select(i => new ItemType
                {
                    codigoMtx = i.CodigoMtx,
                    codigo = i.Codigo,
                    descripcion = i.Descripcion,
                    unidadesMtx = (int)i.UnidadMtx,
                    unidadesMtxSpecified = true,
                    cantidad = i.Cantidad,
                    cantidadSpecified = true,
                    
                    // B: precio CON IVA
                    precioUnitario = i.PrecioUnitarioConIva,
                    precioUnitarioSpecified = true,

                    // No enviar importeIVA en B; solo código para el resumen
                    codigoCondicionIVA = (short)i.CodigoCondicionIVA,

                    importeItem = i.ImporteItem,
                    importeBonificacion = i.ImporteBonificacion ?? 0m,
                    importeBonificacionSpecified = i.ImporteBonificacion.HasValue
                }).ToArray(),

                // Resumen de IVA (opcional, útil para validar totales)
                arraySubtotalesIVA = dto.SubtotalesIVA?.Select(s => new SubtotalIVAType
                {
                    codigo = (short)s.Codigo,
                    importe = s.Importe
                }).ToArray()
            };

            // MUY IMPORTANTE: NO enviar importeOtrosTributos=0
            // comp.importeOtrosTributos = 0m;
            // comp.importeOtrosTributosSpecified = true; // <- NO HACER

            return comp;
        }

        private static ComprobanteType MapToComprobanteTypeA(FacturaARequestDto dto, int numeroComprobante, DateTime fechaEmision)
        {
            var comp = new ComprobanteType
            {
                // Encabezado
                codigoTipoComprobante = (int)TipoComprobante.FacturaA,
                numeroPuntoVenta = dto.NumeroPuntoVenta,
                numeroComprobante = numeroComprobante + 1,
                fechaEmision = fechaEmision,
                fechaEmisionSpecified = true,
                codigoConcepto = (short)dto.ComprobanteConcepto,

                // Receptor
                codigoTipoDocumento = (short)dto.TipoDocumentoReceptor, codigoTipoDocumentoSpecified = true,
                numeroDocumento = long.TryParse(dto.NumeroDocumentoReceptor, out var nd) ? nd : 0L, numeroDocumentoSpecified = true,
                condicionIVAReceptor = 1,
                condicionIVAReceptorSpecified = true,

                // Totales (en B el precio es CON IVA; evitamos mandar 0s innecesarios)
                importeGravado = dto.ImporteGravado,
                importeGravadoSpecified = dto.ImporteGravado > 0,
                importeNoGravado = dto.ImporteNoGravado,
                importeNoGravadoSpecified = dto.ImporteNoGravado > 0,
                importeExento = dto.ImporteExento,
                importeExentoSpecified = dto.ImporteExento > 0,

                importeSubtotal = dto.ImporteSubtotal,
                importeTotal = dto.ImporteTotal,

                // Moneda
                codigoMoneda = dto.CodigoMoneda,
                cotizacionMoneda = dto.CotizacionMoneda,
                cotizacionMonedaSpecified = true,

                // Ítems
                arrayItems = dto.Items.Select(i => new ItemType
                {
                    codigoMtx = i.CodigoMtx,
                    codigo = i.Codigo,
                    descripcion = i.Descripcion,
                    unidadesMtx = (int)i.UnidadMtx,
                    unidadesMtxSpecified = true,
                    cantidad = i.Cantidad,
                    cantidadSpecified = true,

                    // B: precio SIN IVA
                    precioUnitario = i.PrecioUnitario,
                    precioUnitarioSpecified = true,
                    importeIVA = i.ImporteIva,
                    importeIVASpecified = true,
                    // No enviar importeIVA en B; solo código para el resumen
                    codigoCondicionIVA = (short)i.CodigoCondicionIVA,

                    importeItem = i.ImporteItem,
                    importeBonificacion = i.ImporteBonificacion ?? 0m,
                    importeBonificacionSpecified = i.ImporteBonificacion.HasValue
                }).ToArray(),

                // Resumen de IVA (opcional, útil para validar totales)
                arraySubtotalesIVA = dto.SubtotalesIVA?.Select(s => new SubtotalIVAType
                {
                    codigo = (short)s.Codigo,
                    importe = s.Importe
                }).ToArray()
            };

            // MUY IMPORTANTE: NO enviar importeOtrosTributos=0
            // comp.importeOtrosTributos = 0m;
            // comp.importeOtrosTributosSpecified = true; // <- NO HACER

            return comp;
        }
    }
}
