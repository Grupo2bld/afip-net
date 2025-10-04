using bk_arca.DTOs.Facturacion;
using bk_arca.DTOs.Facturacion.FacturaA;
using bk_arca.DTOs.Facturacion.FacturaB;
using bk_arca.Enums;

namespace bk_arca.services.Utils
{
    public static class CalculosTotalesFacturas
    {

        private static decimal GetAlicuota(CondicionIVA cod)
        {
            return cod switch
            {
                CondicionIVA.Gravado27 => 0.27m,
                CondicionIVA.Gravado21 => 0.21m,
                CondicionIVA.Gravado10_5 => 0.105m,
                CondicionIVA.Gravado0 => 0.00m,
                CondicionIVA.Exento => 0.00m,
                CondicionIVA.NoGravado => 0.00m,
                _ => 0.21m
            };
        }

        public static void RecalcularTotalesFacturaB(FacturaBRequestDto dto)
        {
            // 1) Recalcular importe por ítem (si no viene)
            foreach (var i in dto.Items)
            {
                var bonif = i.ImporteBonificacion ?? 0m;
                if (i.ImporteItem <= 0)
                {
                    i.ImporteItem = Math.Round((i.PrecioUnitarioConIva - bonif) * i.Cantidad, 2);
                }
            }

            // 2) Total bruto (con IVA)
            var totalConIva = dto.Items.Sum(x => x.ImporteItem);

            // 3) Si no te pasaron subtotales IVA, podemos construirlos simples (p.e. todo 21%).
            //    Si ya vienen en dto.SubtotalesIVA, los respetamos.
            if (dto.SubtotalesIVA == null || dto.SubtotalesIVA.Count == 0)
            {
                // Caso simple: un único código IVA en todos los ítems -> tomamos el primero
                var cod = dto.Items.GroupBy(x => x.CodigoCondicionIVA)
                                   .Select(g => g.Key)
                                   .SingleOrDefault();

                // Si hay mezcla de alícuotas, acá deberías agrupar y calcular por grupo (extender según tu negocio)
                if (cod == 0) cod = CondicionIVA.Gravado21;

                // Obtener alícuota numérica
                var alicuta = GetAlicuota(cod); // 0.21m, 0.105m, etc.

                // Base imponible aproximada: total / (1 + alícuota)
                var baseImponible = Math.Round(totalConIva / (1 + alicuta), 2);
                var iva = Math.Round(totalConIva - baseImponible, 2);

                dto.ImporteGravado = baseImponible;
                dto.ImporteNoGravado = 0m;
                dto.ImporteExento = 0m;

                dto.ImporteSubtotal = baseImponible; // si no hay otros tributos ni exentos/no gravados al subtotal
                dto.ImporteTotal = totalConIva;

                dto.SubtotalesIVA = new()
            {
                new SubtotalIVARequestDto { Codigo = cod, Importe = iva }
            };
            }
            else
            {
                // Si viene el detalle de IVA, ajustamos gravado y totales en base a esos subtotales
                var totalIva = dto.SubtotalesIVA.Sum(x => x.Importe);
                var baseImponible = Math.Round(totalConIva - totalIva, 2);

                dto.ImporteGravado = baseImponible;
                // Respetamos lo que venga en NoGravado/Exento si lo enviaste; si no, mantenemos 0
                dto.ImporteSubtotal = baseImponible;
                dto.ImporteTotal = totalConIva;
            }
        }


        public static void RecalcularTotalesFacturaA(FacturaARequestDto dto)
        {
            // 1) Recalcular importe por ítem (si no viene)
            foreach (var i in dto.Items)
            {
                var bonif = i.ImporteBonificacion ?? 0m;
                if (i.ImporteItem <= 0)
                {
                    i.ImporteItem = Math.Round((i.PrecioUnitario - bonif) * i.Cantidad, 2);
                }
            }

            
            var totalSinIva = Math.Round(dto.Items.Sum(x => x.PrecioUnitario * x.Cantidad ), 2);
            var totalIva = dto.Items.Sum(x => x.ImporteIva);

            // 3) Si no te pasaron subtotales IVA, podemos construirlos simples (p.e. todo 21%).
            //    Si ya vienen en dto.SubtotalesIVA, los respetamos.
            if (dto.SubtotalesIVA == null || dto.SubtotalesIVA.Count == 0)
            {
                // Caso simple: un único código IVA en todos los ítems -> tomamos el primero
                var cod = dto.Items.GroupBy(x => x.CodigoCondicionIVA)
                                   .Select(g => g.Key)
                                   .SingleOrDefault();

                // Si hay mezcla de alícuotas, acá deberías agrupar y calcular por grupo (extender según tu negocio)
                if (cod == 0) cod = CondicionIVA.Gravado21;

                // Obtener alícuota numérica
                var alicuta = GetAlicuota(cod); // 0.21m, 0.105m, etc.

                // Base imponible aproximada: total / (1 + alícuota)
                //var baseImponible = Math.Round(totalConIva / (1 + alicuta), 2);
                //var iva = Math.Round(totalConIva - baseImponible, 2);

                dto.ImporteGravado = totalSinIva;
                dto.ImporteNoGravado = 0m;
                dto.ImporteExento = 0m;

                dto.ImporteSubtotal = totalSinIva; // si no hay otros tributos ni exentos/no gravados al subtotal
                dto.ImporteTotal  = totalSinIva + totalIva;

                dto.SubtotalesIVA = new()
            {
                new SubtotalIVARequestDto { Codigo = cod, Importe = totalIva }
            };
            }
            else
            {
                // Si viene el detalle de IVA, ajustamos gravado y totales en base a esos subtotales
                totalIva = dto.SubtotalesIVA.Sum(x => x.Importe);
                var baseImponible = totalSinIva;

                dto.ImporteGravado = baseImponible;
                // Respetamos lo que venga en NoGravado/Exento si lo enviaste; si no, mantenemos 0
                dto.ImporteSubtotal = baseImponible;
                dto.ImporteTotal = totalSinIva + totalIva;
            }
        }



    }
}
