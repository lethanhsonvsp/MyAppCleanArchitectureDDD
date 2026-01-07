using MediatR;
using MyApp.Application.Abstractions;
using MyApp.Application.Repository;
using MyApp.Domain.Charging;
using MyApp.Domain.Events;

namespace MyApp.Application.Commands;

// ═════════════════════════════════════════════════════════════
// START CHARGING COMMAND
// ═════════════════════════════════════════════════════════════

public record StartChargingCommand(
    double Voltage_V,
    double Current_A
) : IRequest<CommandResult>;

public class StartChargingCommandHandler : IRequestHandler<StartChargingCommand, CommandResult>
{
    private readonly IChargingRepository _repo;
    private readonly ICanCommandSender _canSender;
    private readonly IMessageBus _messageBus;

    public StartChargingCommandHandler(
        IChargingRepository repo,
        ICanCommandSender canSender,
        IMessageBus messageBus)
    {
        _repo = repo;
        _canSender = canSender;
        _messageBus = messageBus;
    }

    public async Task<CommandResult> Handle(StartChargingCommand cmd, CancellationToken ct)
    {
        // 1. Validate command
        var validation = ChargingRules.ValidateCommand(cmd.Voltage_V, cmd.Current_A, true);
        if (!validation.IsValid)
            return CommandResult.Failure(validation.Error!);

        // 2. Get or create charging state
        var state = await _repo.GetActiveAsync(ct) ?? ChargingState.Create();

        // 3. Check safety rules
        if (!ChargingRules.CanStartCharging(state))
            return CommandResult.Failure("Cannot start charging: safety rules violated");

        // 4. Send CAN command
        await _canSender.SendControlCommandAsync(
            cmd.Voltage_V,
            cmd.Current_A,
            powerStage1: true,
            clearFaults: false
        );

        // 5. Echo command
        state.EchoCommand(cmd.Voltage_V, cmd.Current_A, true);

        // 6. Publish domain events
        foreach (var evt in state.DomainEvents)
            await _messageBus.PublishAsync(evt, ct);

        state.ClearDomainEvents();

        // 7. Persist (if needed)
        await _repo.SaveAsync(state, ct);

        return CommandResult.Success();
    }
}

// ═════════════════════════════════════════════════════════════
// STOP CHARGING COMMAND
// ═════════════════════════════════════════════════════════════

public record StopChargingCommand : IRequest<CommandResult>;

public class StopChargingCommandHandler : IRequestHandler<StopChargingCommand, CommandResult>
{
    private readonly IChargingRepository _repo;
    private readonly ICanCommandSender _canSender;
    private readonly IMessageBus _messageBus;

    public StopChargingCommandHandler(
        IChargingRepository repo,
        ICanCommandSender canSender,
        IMessageBus messageBus)
    {
        _repo = repo;
        _canSender = canSender;
        _messageBus = messageBus;
    }

    public async Task<CommandResult> Handle(StopChargingCommand cmd, CancellationToken ct)
    {
        // 1. Get charging state
        var state = await _repo.GetActiveAsync(ct);
        if (state == null)
            return CommandResult.Success(); // Already stopped

        // 2. Send OFF command
        await _canSender.SendControlCommandAsync(0, 0, false, false);

        // 3. Echo command
        state.EchoCommand(0, 0, false);

        // 4. Publish events
        foreach (var evt in state.DomainEvents)
            await _messageBus.PublishAsync(evt, ct);

        state.ClearDomainEvents();

        // 5. Persist
        await _repo.SaveAsync(state, ct);

        return CommandResult.Success();
    }
}

// ═════════════════════════════════════════════════════════════
// CLEAR FAULTS COMMAND
// ═════════════════════════════════════════════════════════════

public record ClearChargingFaultsCommand : IRequest<CommandResult>;

public class ClearChargingFaultsCommandHandler : IRequestHandler<ClearChargingFaultsCommand, CommandResult>
{
    private readonly ICanCommandSender _canSender;

    public ClearChargingFaultsCommandHandler(ICanCommandSender canSender)
    {
        _canSender = canSender;
    }

    public async Task<CommandResult> Handle(ClearChargingFaultsCommand cmd, CancellationToken ct)
    {
        // Send clear faults command
        await _canSender.SendControlCommandAsync(0, 0, false, clearFaults: true);

        return CommandResult.Success();
    }
}

// ═════════════════════════════════════════════════════════════
// COMMAND RESULT
// ═════════════════════════════════════════════════════════════

public record CommandResult(bool IsSuccess, string? ErrorMessage = null)
{
    public static CommandResult Success() => new(true);
    public static CommandResult Failure(string error) => new(false, error);
}