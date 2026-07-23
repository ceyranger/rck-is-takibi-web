using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface ICrashRecoveryWizardService
{
    CrashRecoveryWizardChoice? Show(CrashRecoveryWizardRequest request);
}
