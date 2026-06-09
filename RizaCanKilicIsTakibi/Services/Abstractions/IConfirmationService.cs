using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IConfirmationService
{
    bool Confirm(ConfirmationRequest request);
}
