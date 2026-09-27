using System.Security.Claims;

namespace DIBA_Backend.Helpers
{
    public static class UserHelper
    {
        public static bool TryGetUserId(
            ClaimsPrincipal user,
            out Guid userId)
        {
            userId = Guid.Empty;

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return false;
            }

            return Guid.TryParse(userIdClaim.Value, out userId);
        }
    }
}