using bk_arca.DTOs;
using bk_arca.DTOs.Facturacion.FacturaA;
using bk_arca.DTOs.Facturacion.FacturaB;
using bk_arca.services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using referencias_arca_ws;

namespace bk_arca.Controllers
{
    [Route("api/facturacion")]
    [ApiController]
    public class BillingController : ControllerBase
    {

        

        public FacturacionService service { get; set; }

        public BillingController()
        {
            service = new FacturacionService();
        }

        

        [HttpPost("b")]
        public async Task<ActionResult<autorizarComprobanteResponse>> PostFacturaB([FromBody] FacturaBRequestDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var resp = await service.AutorizarFacturaBAsync(dto);
            return Ok(resp.comprobanteResponse);
        }
        [HttpPost("a")]
        public async Task<ActionResult<autorizarComprobanteResponse>> PostFacturaA([FromBody] FacturaARequestDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var resp = await service.AutorizarFacturaAAsync(dto);
            return Ok(resp.comprobanteResponse);
        }

        [HttpGet("consultar-tipos-comprobantes")]
        public async Task<ActionResult<ResponseDto>> consultarTiposComprobantes()
        {
            var resu = await service.ConsultarTipodeComprobantes();
            return new ResponseDto { Data = resu.arrayTiposComprobante , Success = true};
        }

        [HttpGet("consultar-condiciones-iva-receptor")]
        public async Task<ActionResult<ResponseDto>> consultarCondicionesIvaReceptor(short tipoComprobante)
        {

            var resu = await service.ConsultarCondicionIVAReceptor(tipoComprobante);
            return new ResponseDto { Data = resu.arrayCondicionesIVAReceptor, Success = true };
        }

        [HttpGet("consultar-condiciones-iva")]
        public async Task<ActionResult<ResponseDto>> consultarCondicionesIva()
        {

            var resu = await service.ConsultarCondicionesIVA();
            return new ResponseDto { Data = resu.arrayCondicionesIVA, Success = true };
        }

        [HttpGet("consultar-monedas")]
        public async Task<ActionResult<ResponseDto>> consultarModenas()
        {

            var resu = await service.ConsultarMonedas();
            return new ResponseDto { Data = resu.arrayMonedas, Success = true };
        }

        [HttpGet("consultar-cotizacion-moneda")]
        public async Task<ActionResult<ResponseDto>> consultarModenas(string cod , DateTime fecha)
        {

            var resu = await service.ConsultarCotizacionModena(cod ,fecha);
            return new ResponseDto { Data = resu.cotizacionMoneda, Success = true };
        }

        [HttpGet("consultar-unidades-medida")]
        public async Task<ActionResult<ResponseDto>> consultarUnidadesMedida()
        {

            var resu = await service.ConsultarUnidadesMedida();
            return new ResponseDto { Data = resu.arrayUnidadesMedida, Success = true };
        }

        [HttpGet("consultar-puntos-ventas")]
        public async Task<ActionResult<ResponseDto>> consultarPuntosDeVenta()
        {

            var resu = await service.ConsultarPuntoDeVenta();
            return new ResponseDto { Data = resu.arrayPuntosVenta, Success = true };
        }

        [HttpGet("consultar-comprobante")]
        public async Task<ActionResult<ResponseDto>> consultarComprobante(int tipoComprobante , int nroComprobante , int nroPuntoVenta)
        {

            var resu = await service.ConsultarComprobante(tipoComprobante,nroComprobante ,nroPuntoVenta);
            return new ResponseDto { Data = resu.comprobante, Success = true };
        }

        [HttpGet("consultar-ultimo-comprobante-autorizado")]
        public async Task<ActionResult<ResponseDto>> consultarUltimoComprobanteAutorizado(int tipoComprobante , int nroPuntoVenta)
        {

            var resu = await service.ConsultarUltimoComprobanteAutorizado(tipoComprobante , nroPuntoVenta);
            return new ResponseDto { Data = resu.numeroComprobante, Success = true };
        }

        [HttpGet("consultar-tipos-documentos")]
        public async Task<ActionResult<ResponseDto>> consultarTipoDocumentos()
        {
            var resu = await service.ConsultarTiposDocumentos();
            return new ResponseDto { Data = resu.arrayTiposDocumento, Success = true };
        }

    }
}
