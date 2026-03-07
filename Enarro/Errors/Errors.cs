using CoreKernel.Functional.Results;

namespace Enarro.Errors
{
    public static class Errors
    {
        public static Error UserNotResolved() =>
        new("Auth.UserNotResolved", "Not able to resolve User Details.", ErrorType.Unauthorized);

        public static Error EmptyChatMessage() =>
        new("Chat.EmptyChatMessage", "Message cannot be empty.", ErrorType.BadRequest);
    }
}