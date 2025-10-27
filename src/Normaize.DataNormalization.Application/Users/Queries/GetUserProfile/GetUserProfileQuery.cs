using MediatR;
using Normaize.DataNormalization.Application.Users.DTOs;

namespace Normaize.DataNormalization.Application.Users.Queries.GetUserProfile;

/// <summary>
/// Query to get user profile by Auth0UserId
/// </summary>
public record GetUserProfileQuery(
    string Auth0UserId) : IRequest<UserProfileDto?>;
