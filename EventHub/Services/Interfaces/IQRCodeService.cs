namespace EventHub.Services.Interfaces
{
    public interface IQRCodeService
    {
        string GenerateQRCode(string data);
        Task<bool> VerifyQRCodeAsync(string qrCodeData);
        string GenerateTicketQRData(int ticketId, int bookingId, int eventId);
    }
}