using LogiTrackWebV1._0.DTOs;

namespace LogiTrackWebV1._0.Services
{
    public interface IPricingService
    {
        // Builds a full price breakdown for a shipment. Throws ArgumentException when
        // the receive date is in the past.
        PriceQuoteDto CalculateQuote(decimal weightKg, double distanceKm, DateTime receiveDate, DateTime? now = null);

        // Great-circle distance in kilometres between two lat/lng coordinates.
        double HaversineKm(double lat1, double lon1, double lat2, double lon2);
    }
}
