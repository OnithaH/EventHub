using QRCoder;
using EventHub.Services.Interfaces;

namespace EventHub.Services.Implementations
{
    public class QRCodeService : IQRCodeService
    {
        public string GenerateQRCode(string data)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);

            var qrCodeBytes = qrCode.GetGraphic(20);
            return Convert.ToBase64String(qrCodeBytes);
        }

        public async Task<bool> VerifyQRCodeAsync(string qrCodeData)
        {
            try
            {
                // Parse QR code data and validate
                var parts = qrCodeData.Split('|');
                if (parts.Length < 3) return false;

                // Format: TicketID|BookingID|EventID|IssueDate
                if (int.TryParse(parts[0], out int ticketId) &&
                    int.TryParse(parts[1], out int bookingId) &&
                    int.TryParse(parts[2], out int eventId))
                {
                    // Additional validation logic here
                    return await Task.FromResult(true);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public string GenerateTicketQRData(int ticketId, int bookingId, int eventId)
        {
            var issueDate = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            return $"{ticketId}|{bookingId}|{eventId}|{issueDate}";
        }
    }
}