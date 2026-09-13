using LogiTrackWebV1._0.DTOs;
using Microsoft.Extensions.Options;

namespace LogiTrackWebV1._0.Services
{
    public class PricingService : IPricingService
    {
        private readonly PricingOptions _options;

        public PricingService(IOptions<PricingOptions> options)
        {
            _options = options.Value;
        }

        public PriceQuoteDto CalculateQuote(decimal weightKg, double distanceKm, DateTime receiveDate, DateTime? now = null)
        {
            var today = (now ?? DateTime.Now).Date;
            var target = receiveDate.Date;

            int daysUntil = (target - today).Days;
            if (daysUntil < 0)
                throw new ArgumentException("Receive date cannot be in the past.", nameof(receiveDate));

            decimal weightCharge = Math.Round(_options.PricePerKg * weightKg, 2, MidpointRounding.AwayFromZero);
            decimal distanceCharge = Math.Round(_options.PricePerKm * (decimal)distanceKm, 2, MidpointRounding.AwayFromZero);
            decimal subtotal = _options.BaseFee + weightCharge + distanceCharge;

            // Date surcharge: today = TodaySurchargePercent, decreasing by
            // SurchargeStepPercent per day, and dropped entirely once the receive
            // date is more than MaxSurchargeDays away.
            bool dateFactorApplied = daysUntil <= _options.MaxSurchargeDays;
            decimal surchargePercent = dateFactorApplied
                ? Math.Max(0m, _options.TodaySurchargePercent - (_options.SurchargeStepPercent * daysUntil))
                : 0m;

            decimal surchargeAmount = Math.Round(subtotal * (surchargePercent / 100m), 2, MidpointRounding.AwayFromZero);
            decimal total = subtotal + surchargeAmount;

            return new PriceQuoteDto
            {
                DistanceKm = Math.Round(distanceKm, 2),
                BaseFee = _options.BaseFee,
                WeightCharge = weightCharge,
                DistanceCharge = distanceCharge,
                Subtotal = subtotal,
                DaysUntilReceive = daysUntil,
                DateFactorApplied = dateFactorApplied,
                DateSurchargePercent = surchargePercent,
                DateSurchargeAmount = surchargeAmount,
                Total = total,
                Currency = _options.Currency
            };
        }

        public double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadiusKm = 6371.0;

            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadiusKm * c;
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
    }
}
