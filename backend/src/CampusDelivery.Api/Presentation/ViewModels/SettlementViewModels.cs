using CampusDelivery.Api.Models;

namespace CampusDelivery.Api.Presentation.ViewModels;

public sealed class SettlementIndexViewModel
{
    public IReadOnlyList<SettlementSummaryViewModel> Settlements { get; set; } = Array.Empty<SettlementSummaryViewModel>();

    public int CandidatePaymentCount { get; set; }

    public decimal CandidatePayAmount { get; set; }

    public decimal PlatformFeeRate { get; set; }
}

public sealed class SettlementCandidatesViewModel
{
    public IReadOnlyList<SettlementRunnerGroupViewModel> RunnerGroups { get; set; } = Array.Empty<SettlementRunnerGroupViewModel>();

    public decimal PlatformFeeRate { get; set; }
}

public sealed class SettlementDetailsViewModel
{
    public SettlementSummaryViewModel Settlement { get; set; } = new();

    public IReadOnlyList<SettlementPaymentItemViewModel> Items { get; set; } = Array.Empty<SettlementPaymentItemViewModel>();
}

public sealed class RunnerSettlementIndexViewModel
{
    public IReadOnlyList<SettlementSummaryViewModel> Settlements { get; set; } = Array.Empty<SettlementSummaryViewModel>();

    public int SettlementCount { get; set; }

    public int WaitingCount { get; set; }

    public int DoneCount { get; set; }

    public int BlockedCount { get; set; }

    public decimal TotalNetIncome { get; set; }

    public decimal WaitingNetIncome { get; set; }

    public decimal DoneNetIncome { get; set; }
}

public sealed class RunnerSettlementDetailsViewModel
{
    public SettlementSummaryViewModel Settlement { get; set; } = new();

    public IReadOnlyList<SettlementPaymentItemViewModel> Items { get; set; } = Array.Empty<SettlementPaymentItemViewModel>();
}

public sealed class SettlementSummaryViewModel
{
    public int SettlementId { get; set; }

    public int RunnerId { get; set; }

    public string RunnerName { get; set; } = string.Empty;

    public decimal OrderTotal { get; set; }

    public decimal PlatformFee { get; set; }

    public decimal NetIncome { get; set; }

    public string SettlementStatus { get; set; } = "WAITING";

    public string SettlementStatusDisplayName { get; set; } = string.Empty;

    public static SettlementSummaryViewModel FromModel(
        Settlement settlement,
        string settlementStatusDisplayName)
    {
        return new SettlementSummaryViewModel
        {
            SettlementId = settlement.SettlementId,
            RunnerId = settlement.RunnerId,
            RunnerName = settlement.RunnerName,
            OrderTotal = settlement.OrderTotal,
            PlatformFee = settlement.PlatformFee,
            NetIncome = settlement.NetIncome,
            SettlementStatus = settlement.SettlementStatus,
            SettlementStatusDisplayName = settlementStatusDisplayName
        };
    }
}

public sealed class SettlementRunnerGroupViewModel
{
    public int RunnerId { get; set; }

    public string RunnerName { get; set; } = string.Empty;

    public int PaymentCount { get; set; }

    public decimal OrderTotal { get; set; }

    public decimal PlatformFee { get; set; }

    public decimal NetIncome { get; set; }

    public IReadOnlyList<SettlementCandidateViewModel> Payments { get; set; } = Array.Empty<SettlementCandidateViewModel>();
}

public sealed class SettlementCandidateViewModel
{
    public int PaymentId { get; set; }

    public int TaskId { get; set; }

    public int RecordId { get; set; }

    public string TaskTitle { get; set; } = string.Empty;

    public decimal PayAmount { get; set; }

    public string PayMethodDisplayName { get; set; } = string.Empty;

    public static SettlementCandidateViewModel FromModel(
        SettlementCandidate candidate,
        string payMethodDisplayName)
    {
        return new SettlementCandidateViewModel
        {
            PaymentId = candidate.PaymentId,
            TaskId = candidate.TaskId,
            RecordId = candidate.RecordId,
            TaskTitle = candidate.TaskTitle,
            PayAmount = candidate.PayAmount,
            PayMethodDisplayName = payMethodDisplayName
        };
    }
}

public sealed class SettlementPaymentItemViewModel
{
    public int PaymentId { get; set; }

    public int TaskId { get; set; }

    public int RecordId { get; set; }

    public string TaskTitle { get; set; } = string.Empty;

    public decimal PayAmount { get; set; }

    public string PayMethodDisplayName { get; set; } = string.Empty;

    public static SettlementPaymentItemViewModel FromModel(
        SettlementPaymentItem item,
        string payMethodDisplayName)
    {
        return new SettlementPaymentItemViewModel
        {
            PaymentId = item.PaymentId,
            TaskId = item.TaskId,
            RecordId = item.RecordId,
            TaskTitle = item.TaskTitle,
            PayAmount = item.PayAmount,
            PayMethodDisplayName = payMethodDisplayName
        };
    }
}

