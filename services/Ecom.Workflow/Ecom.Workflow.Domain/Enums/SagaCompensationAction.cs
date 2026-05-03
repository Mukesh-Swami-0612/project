namespace Ecom.Workflow.Domain.Enums;

/// <summary>
/// Defines compensation actions for saga rollback
/// </summary>
public enum SagaCompensationAction
{
    None,
    RevertValidation,
    RevertApproval,
    RevertPublish,
    NotifyFailure
}
