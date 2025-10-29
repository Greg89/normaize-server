using MediatR;
using Normaize.DataNormalization.Application.Users.DTOs;

namespace Normaize.DataNormalization.Application.Users.Queries.GetUserPreferences;

/// <summary>
/// Query to get user preferences only
/// </summary>
public record GetUserPreferencesQuery(
    string Auth0UserId) : IRequest<UserPreferencesDto?>;
