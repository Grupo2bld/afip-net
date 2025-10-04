using bk_arca.DTOs;
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


        public async Task<consultarTiposComprobanteResponse> ConsultarTipodeComprobantes()
        {
            var resu = await Client.consultarTiposComprobanteAsync(new consultarTiposComprobanteRequest
            {
                authRequest = Auth
            });

            return resu;
        }


        public async Task<consultarCondicionesIVAReceptorResponse> ConsultarCondicionIVAReceptor(int tipoComprobante)
        {

            var resu = await Client.consultarCondicionesIVAReceptorAsync(new consultarCondicionesIVAReceptorRequest
            {
                authRequest = Auth,
                consultaCondicionesIVAReceptorRequest = new ConsultaCondicionesIVARequestType
                {
                    codigoTipoComprobante = (short)tipoComprobante
                }

            });

            return resu;

        }


        public async Task<consultarCondicionesIVAResponse> ConsultarCondicionesIVA() 
        {

            var resu = await Client.consultarCondicionesIVAAsync(new consultarCondicionesIVARequest
            {
                authRequest = Auth,

            });

            return resu;

        }
        


        public async Task<consultarMonedasResponse> ConsultarMonedas()
        {
            var resu = await Client.consultarMonedasAsync(new consultarMonedasRequest
            {
                authRequest = Auth
            });

            return resu;
        }


        public async Task<consultarCotizacionMonedaResponse> ConsultarCotizacionModena(string cod, DateTime fecha)
        {
            var resu = await Client.consultarCotizacionMonedaAsync(new consultarCotizacionMonedaRequest
            {
                authRequest = Auth,
                codigoMoneda = cod,
                fechaCotizacion = fecha
            });

            return resu;
        }

        public async Task<consultarUnidadesMedidaResponse> ConsultarUnidadesMedida()
        {
            var resu = await Client.consultarUnidadesMedidaAsync(new consultarUnidadesMedidaRequest
            {
                authRequest = Auth,                
            });

            return resu;
        }


        public async Task<consultarPuntosVentaCAEResponse> ConsultarPuntoDeVenta()
        {
            var resu = await Client.consultarPuntosVentaCAEAsync(new consultarPuntosVentaCAERequest
            {
                authRequest =Auth
            });

            return resu;
        }


        public async Task<consultarComprobanteResponse> ConsultarComprobante( int  tipoComprobante, int nroComprobante , int nroPuntoVenta) 
        {

            var resu = await Client.consultarComprobanteAsync(new consultarComprobanteRequest
            {
                authRequest = Auth,
                consultaComprobanteRequest = new ConsultaComprobanteRequestType
                {
                        codigoTipoComprobante = (short) tipoComprobante,
                        numeroPuntoVenta = nroPuntoVenta,
                        numeroComprobante = nroComprobante


                }
            });


            return resu;
        }

        public async Task<consultarUltimoComprobanteAutorizadoResponse> ConsultarUltimoComprobanteAutorizado(int tipoComprobante, int nroPuntoVenta) 
        {

            var resu = await Client.consultarUltimoComprobanteAutorizadoAsync(new consultarUltimoComprobanteAutorizadoRequest
            {
                authRequest = Auth,
                consultaUltimoComprobanteAutorizadoRequest = new ConsultaUltimoComprobanteAutorizadoRequestType
                {
                    numeroPuntoVenta = nroPuntoVenta,
                    codigoTipoComprobante = (short) tipoComprobante
                }
            });
            return resu;   
        }

        public async Task<consultarTiposDocumentoResponse> ConsultarTiposDocumentos()
        {
            var resu = await Client.consultarTiposDocumentoAsync(new consultarTiposDocumentoRequest
            {
                authRequest = Auth
            });

            return resu;
        }

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
