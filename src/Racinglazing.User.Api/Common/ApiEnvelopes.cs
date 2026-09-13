namespace Racinglazing.User.Api.Common;

// Response envelopes, matching the shape used across the RacingVacing services.
public sealed record DataEnvelope<T>(T Data);
public sealed record ErrorEnvelope(ErrorBody Error);
public sealed record ErrorBody(string Code, string Message);
