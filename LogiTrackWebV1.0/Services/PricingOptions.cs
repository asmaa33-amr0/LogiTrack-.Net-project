namespace LogiTrackWebV1._0.Services
{
    // Bound from the "Pricing" section of appsettings.json. Defaults below make the
    // feature work out of the box; override any value in configuration to tune it.
    public class PricingOptions
    {
        public const string SectionName = "Pricing";

        public string Currency { get; set; } = "EGP";

        // Flat handling fee applied to every shipment.
        public decimal BaseFee { get; set; } = 20m;

        // Charge per kilogram of weight.
        public decimal PricePerKg { get; set; } = 2.5m;

        // Charge per kilometre of distance between warehouse and destination station.
        public decimal PricePerKm { get; set; } = 0.5m;

        // Surcharge for same-day delivery (percent). Each day further out reduces the
        // surcharge by SurchargeStepPercent: today 15%, tomorrow 10%, in 2 days 5%.
        public decimal TodaySurchargePercent { get; set; } = 15m;
        public decimal SurchargeStepPercent { get; set; } = 5m;

        // No date surcharge once the receive date is more than this many days away.
        // With the defaults above, days 0/1/2 are charged and day 3+ is not.
        public int MaxSurchargeDays { get; set; } = 2;
    }
}
